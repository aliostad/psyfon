using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.EventHubs;


namespace Psyfon
{
    public class EventBuffer : IDisposable
    {
        private ConcurrentQueue<EventData> _queue = new ConcurrentQueue<EventData>();
        private const int DefaultBatchSize = 128 * 1024; // 128KB
        private readonly IHasher _hasher;
        private readonly IEventHubClientWrapper _client;
        private readonly int _batchBufferSize;
        private readonly int _partitionCount;
        private bool _isAccepting = true;
        private Thread _worker;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public EventBuffer(string connectionString,
            int batchBufferSize = DefaultBatchSize,
            IHasher hasher = null):
            this(new DefaultWrapper(EventHubClient.CreateFromConnectionString(connectionString)), batchBufferSize, hasher)
        {
        }

        public EventBuffer(IEventHubClientWrapper client,
            int batchBufferSize = DefaultBatchSize,
            IHasher hasher = null
            )
        {
            _hasher = hasher ?? new Md5Hasher();
            _client = client;
            _batchBufferSize = batchBufferSize;
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
                _queue.Enqueue(@event);

            return _isAccepting;
        }

        public void Add(IEnumerable<EventData> events, string partitionKey = null)
        {
            foreach (var item in events)
            {
                Add(item, partitionKey);
            }
        }

        private void Work()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // _queue.TryDequeue()
            }
        }

        public void Dispose()
        {
            _isAccepting = false;
            Flush();
            _client.Dispose();
        }

        public void Start()
        {

        }

        private void Flush()
        {

        }
    }
}
