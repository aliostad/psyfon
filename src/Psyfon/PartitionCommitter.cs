using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Psyfon
{
    public class PartitionCommitter : IDisposable
    {
        private ProperEventDataBatch _currentBatch;
        private readonly IEventHubClientWrapper _client;
        private readonly int _batchSize;
        private readonly BlockingCollection<ProperEventDataBatch> _batches = new BlockingCollection<ProperEventDataBatch>();
        private object _lock = new object();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Thread _worker;

        public PartitionCommitter(IEventHubClientWrapper client, int batchSize, string partitionKey)
        {
            _client = client;
            _batchSize = batchSize;
            PartitionKey = partitionKey;
            _currentBatch = new ProperEventDataBatch(batchSize, partitionKey);
            _worker = new Thread(Work);
            _worker.Start();
        }

        public void Add(EventData @event)
        {
            if (!_currentBatch.TryAdd(@event))
            {
                lock (_lock)
                {
                    if (!_currentBatch.TryAdd(@event))
                    {
                        _batches.Add(_currentBatch);
                        _currentBatch = new ProperEventDataBatch(_batchSize, PartitionKey);
                        _currentBatch.Add(@event);
                    }
                }
            }
        }

        private void Commit(ProperEventDataBatch batch)
        {
            _client.SendBatchAsync(batch.EventData, PartitionKey).GetAwaiter().GetResult(); // no point in doing async, dedicated thread would be waiting anyway
        }

        private void Work()
        {
            var token = _cancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                var batch = _batches.Take(token);
                Commit(batch);
            }            
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            if (_batches.Count > 0)
            {
                Thread.Sleep(200);
            }
        }

        public string PartitionKey { get; }
    }
}
