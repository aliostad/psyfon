using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Psyfon
{
    public class ProperEventDataBatch
    {
        private readonly int _maxSize;
        private ConcurrentBag<EventData> _events = new ConcurrentBag<EventData>();
        private volatile int _currentSize = 0;

        public int RetryCount { get; set; }

        public ProperEventDataBatch(int maxSize, string partitionKey)
        {
            _maxSize = maxSize;
            PartitionKey = partitionKey;
        }

        public string PartitionKey { get; }

        public IEnumerable<EventData> EventData => _events;

        public bool TryAdd(EventData @event)
        {
            if (_currentSize + @event.Body.Array.Length <= _maxSize)
            {
                _events.Add(@event);
                Interlocked.Add(ref _currentSize, @event.Body.Array.Length);
                return true;
            }
            else
                return false;
        }

        public void Add(EventData @event)
        {
            _events.Add(@event);
            Interlocked.Add(ref _currentSize, @event.Body.Array.Length);
        }
    }
}
