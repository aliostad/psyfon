using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Psyfon
{
    internal class ProperEventDataBatch
    {
        private readonly int _maxSize;
        private ConcurrentBag<EventData> _events = new ConcurrentBag<EventData>();
        private volatile int _currentSize = 0;
        private object _lock = new object();

        public int CurrentSize => _currentSize;

        public int RetryCount { get; set; }

        public ProperEventDataBatch(int maxSize)
        {
            _maxSize = maxSize;
        }

        public IEnumerable<EventData> EventData => _events;

        public bool TryAdd(EventData @event)
        {
            if (_currentSize + @event.Body.Array.Length <= _maxSize)
            {
                lock (_lock)
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
