using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Psyfon
{
    internal class PartitionCommitter : IDisposable
    {
        private ProperEventDataBatch _currentBatch;
        private readonly IPartitionSenderWrapper _sender;
        private readonly int _batchSize;
        private readonly Action<TraceLevel, string> _logger;
        private readonly BlockingCollection<ProperEventDataBatch> _batches = new BlockingCollection<ProperEventDataBatch>();
        private object _lock = new object();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Thread _worker;

        public PartitionCommitter(IPartitionSenderWrapper sender, int batchSize, Action<TraceLevel, string> logger)
        {
            _sender = sender;
            _batchSize = batchSize;
            _logger = logger;
            _currentBatch = new ProperEventDataBatch(batchSize);
            _worker = new Thread(Work);
            _worker.Start();
        }

        public string Name { get; set; }

        public void Add(EventData @event)
        {
            if (!_currentBatch.TryAdd(@event))
            {
                lock (_lock)
                {
                    if (!_currentBatch.TryAdd(@event))
                    {
                        _batches.Add(_currentBatch);
                        _currentBatch = new ProperEventDataBatch(_batchSize);
                        _currentBatch.Add(@event);
                    }
                }
            }
        }

        private void Commit(ProperEventDataBatch batch)
        {
            _logger(TraceLevel.Verbose, $"{Name}: About to commit batch of size: {batch.CurrentSize}");            
            _sender.SendBatchAsync(batch.EventData).GetAwaiter().GetResult(); // no point in doing async, dedicated thread would be waiting anyway
            _logger(TraceLevel.Verbose, $"{Name}: Successfully sent batch. {_batches.Count} batches left");
        }

        private void Work()
        {
            var token = _cancellationTokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                ProperEventDataBatch batch = null;

                try
                {
                    batch = _batches.Take(token);
                    Commit(batch);
                }
                catch (OperationCanceledException tce)
                {
                    // CANNOT CATCH - I KNOW
                    // ignore
                }
                catch (Exception e)
                {
                    if (batch != null && batch.RetryCount++ < 3) 
                        _batches.Add(batch);

                    _logger(TraceLevel.Error, e.ToString());
                }
            }            
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();

            if (_batches.Count > 0)
            {
                Thread.Sleep(200);
            }

            Commit(_currentBatch);
        }

        public string PartitionKey { get; }
    }
}
