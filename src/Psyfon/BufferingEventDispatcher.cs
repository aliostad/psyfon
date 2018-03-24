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
    /// <summary>
    /// The main class responsible for buffering and sending events to EventHub
    /// Per EventHub, you need to have a single instance of this class in your process.
    /// </summary>
    public class BufferingEventDispatcher : IEventDispatcher
    {
        public static readonly int DefaultBatchSize = 64 * 1024; // 64KB
        public static readonly int DefaultMaxIntervalMillis = 1000;

        private ConcurrentQueue<Tuple<EventData, string>> _queue = new ConcurrentQueue<Tuple<EventData, string>>();
        private readonly IEventHubClientWrapper _client;
        private string[] _partitions;
        private bool _isAccepting = true;
        private Thread _worker;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private ConcurrentDictionary<int, Lazy<PartitionCommitter>> _committers = new ConcurrentDictionary<int, Lazy<PartitionCommitter>>();
        private DateTimeOffset _lastPoke;
        private readonly DispatchSettings _settings;
        private bool _bufferFull = false;

        public int EventsInBuffer { get => _committers.Values.Sum(x => x.Value.CurrentBatchSize); }

        /// <summary>
        /// Main Constructor
        /// </summary>
        /// <param name="connectionString">For the EventHub</param>
        /// <param name="settings"></param>
        public BufferingEventDispatcher(string connectionString,
            DispatchSettings settings = null):
            this(new DefaultClientWrapper(EventHubClient.CreateFromConnectionString(connectionString)), 
                settings)
        {
        }
        /// <summary>
        /// Useful for custom EventHub client or testing
        /// </summary>
        /// <param name="client"></param>
        /// <param name="settings"></param>
        public BufferingEventDispatcher(IEventHubClientWrapper client,
            DispatchSettings settings)
        {
            _settings = settings ?? new DispatchSettings();
            _client = client;
        }

        /// <summary>
        /// Adds events to be dispatched.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="partitionKey"></param>
        /// <returns>Whether message was accepted</returns>
        public bool Add(EventData @event, string partitionKey = null)
        {
            if (_bufferFull)
                throw new InvalidOperationException($"Buffer is full. Number of events in buffer: {EventsInBuffer}");

            if(_isAccepting)
                _queue.Enqueue(new Tuple<EventData, string>(@event, partitionKey));

            return _isAccepting;
        }
        /// <summary>
        /// Adds events to be dispatched.
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
            _lastPoke = DateTimeOffset.Now;

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Tuple<EventData, string> data;
                if(_queue.TryDequeue(out data))
                {
                    var hash = _settings.Hasher.Hash(data.Item2 ?? Guid.NewGuid().ToString(), _partitions.Count());
                    var committer = _committers[hash];
                    committer.Value.Add(data.Item1);
                }
                else
                {
                    try
                    {
                        Task.Delay(100, _cancellationTokenSource.Token).GetAwaiter().GetResult();
                        _bufferFull = this.EventsInBuffer > _settings.MaximumBufferedEvents;
                    }
                    catch
                    {
                        // ignore
                    }                   
                }
            }

            if(DateTimeOffset.Now.Subtract(_lastPoke).TotalSeconds > _settings.MaxSendIntervalMillis)
            {
                _lastPoke = DateTimeOffset.Now;
                foreach (var c in _committers.Values)
                {
                    c.Value.CheckTimeElapsedSinceLastRebatch();
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
                            () => new PartitionCommitter(_client.CreatePartitionSender(pid), _settings)
                            {
                                Name = $"PartitionCommitter-{pid}"
                            }));
            }

            _worker = new Thread(Work);
            _worker.IsBackground = _settings.UseBackgroundThreads;
            _worker.Start();
        }
    }
}
