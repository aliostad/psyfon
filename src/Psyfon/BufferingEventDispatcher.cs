using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.EventHubs;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Psyfon
{
    public class BufferingEventDispatcher : IDisposable
    {
        private const int DefaultBatchSize = 64 * 1024; // 64KB
        private const int DefaultMaxIntervalSeconds = 5;

        private ConcurrentQueue<Tuple<EventData, string>> _queue = new ConcurrentQueue<Tuple<EventData, string>>();
        private readonly IHasher _hasher;
        private readonly IEventHubClientWrapper _client;
        private readonly int _batchBufferSize;
        private readonly int _maxSendIntervalSeconds;
        private string[] _partitions;
        private bool _isAccepting = true;
        private Thread _worker;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private ConcurrentDictionary<int, Lazy<PartitionCommitter>> _committers = new ConcurrentDictionary<int, Lazy<PartitionCommitter>>();
        private Action<TraceLevel, string> _logger;

        /// <summary>
        /// Main Constructor
        /// </summary>
        /// <param name="connectionString">For the EventHub</param>
        /// <param name="maxBatchSize">Maximum size of the batch sent to EventHub. 64KB by default</param>
        /// <param name="maxSendIntervalSeconds">Maximum number of seconds before flushing events to EventHub. 5 seconds by default</param>
        /// <param name="hasher">An implementation of uniform hashing. By default uses MD5</param>
        /// <param name="logger">A tracer/logger. By default uses Trace.WriteLine</param>
        public BufferingEventDispatcher(string connectionString,
            int maxBatchSize = DefaultBatchSize,
            int maxSendIntervalSeconds = DefaultMaxIntervalSeconds,
            IHasher hasher = null,
            Action<TraceLevel, string> logger = null):
            this(new DefaultClientWrapper(EventHubClient.CreateFromConnectionString(connectionString)), 
                maxBatchSize, maxSendIntervalSeconds, hasher, logger)
        {
        }
        /// <summary>
        /// Useful for custom EventHub client or testing
        /// </summary>
        /// <param name="client"></param>
        /// <param name="maxBatchSize">Maximum size of the batch sent to EventHub. 128K by default</param>
        /// <param name="maxSendIntervalSeconds">Maximum number of seconds before flushing events to EventHub. 5 seconds by default</param>
        /// <param name="hasher">An implementation of uniform hashing. By default uses MD5</param>
        /// <param name="logger">A tracer/logger. By default uses Trace.WriteLine</param>
        public BufferingEventDispatcher(IEventHubClientWrapper client,
            int maxBatchSize = DefaultBatchSize,
            int maxSendIntervalSeconds = DefaultMaxIntervalSeconds,
            IHasher hasher = null,
            Action<TraceLevel, string> logger = null
            )
        {
            _hasher = hasher ?? new Md5Hasher();
            _client = client;
            _batchBufferSize = maxBatchSize;
            _maxSendIntervalSeconds = maxSendIntervalSeconds;
            _logger = logger ?? ((TraceLevel level, string message) => {
                switch (level)
                {
                    case TraceLevel.Error:
                        Trace.TraceError(message);
                        break;
                    case TraceLevel.Warning:
                        Trace.TraceWarning(message);
                        break;
                    default:
                        Trace.TraceInformation(message);
                        break;
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        /// <param name="partitionKey"></param>
        /// <returns>Whether message was accepted</returns>
        public bool Add(EventData @event, string partitionKey = null)
        {
            if(_isAccepting)
                _queue.Enqueue(new Tuple<EventData, string>(@event, partitionKey));

            return _isAccepting;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="events"></param>
        /// <param name="partitionKey"></param>
        /// <returns>Whether message was accepted. If one is not accepted it will be false.</returns>
        public bool Add(IEnumerable<EventData> events, string partitionKey = null)
        {
            return events.Select(x => Add(x, partitionKey)).Aggregate((a,b) => a && b);
        }

        private void Work()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Tuple<EventData, string> data;
                if(_queue.TryDequeue(out data))
                {
                    var hash = _hasher.Hash(data.Item2 ?? Guid.NewGuid().ToString(), _partitions.Count());
                    var committer = _committers[hash];
                    committer.Value.Add(data.Item1);
                }
                else
                {
                    try
                    {
                        Task.Delay(100, _cancellationTokenSource.Token).GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // ignore
                    }                   
                }
            }
        }

        /// <summary>
        /// Closes connections and stops threads
        /// </summary>
        public void Dispose()
        {
            try
            {
                _isAccepting = false;
                _cancellationTokenSource.Cancel();
                foreach (var kv in _committers)
                {
                    kv.Value.Value.Dispose();
                }

                _client.Dispose();
            }
            catch
            {
                // ignore 
            }           
        }

        /// <summary>
        /// This must be called for the dispatcher to work
        /// </summary>
        public void Start()
        {
            _partitions = _client.GetPartitions().GetAwaiter().GetResult();
            int i = 0;
            foreach (var pid in _partitions)
            {
                _committers.GetOrAdd(i++,
                        new Lazy<PartitionCommitter>(
                            () => new PartitionCommitter(_client.CreatePartitionSender(pid), _batchBufferSize, _maxSendIntervalSeconds , _logger)
                            {
                                Name = $"PartitionCommitter-{pid}"
                            }));
            }

            _worker = new Thread(Work);
            _worker.Start();
        }
    }
}
