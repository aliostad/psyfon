using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Psyfon
{
    internal class PartitionCommitter : IDisposable
    {
        private ProperEventDataBatch _currentBatch;
        private readonly IPartitionSenderWrapper _sender;
        private readonly BlockingCollection<ProperEventDataBatch> _batches = new BlockingCollection<ProperEventDataBatch>();
        private object _lock = new object();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Thread _worker;
        private DateTimeOffset _lastSent;
        private readonly DispatchSettings _settings;

        public PartitionCommitter(IPartitionSenderWrapper sender, DispatchSettings settings)
        {
            _sender = sender;
            _worker = new Thread(Work);
            _worker.IsBackground = settings.UseBackgroundThreads;
            _worker.Start();
            _currentBatch = new ProperEventDataBatch(settings.MaxBatchSize);
            _settings = settings;
        }

        public string Name { get; set; }

        public int CurrentBatchSize
        {
            get
            {
                return (_currentBatch?.Count ?? 0) + _batches.Sum(x => x.Count);
            }
        }

        public void Add(EventData @event)
        {
            if (_lastSent == default(DateTimeOffset))
                _lastSent = DateTimeOffset.Now;

            Func<bool> predicate = () => DateTimeOffset.Now.Subtract(_lastSent).TotalSeconds > _settings.MaxSendIntervalMillis || 
                    (!_currentBatch.TryAdd(@event));
            RebatchIfConditionMet(predicate, () => _currentBatch.Add(@event));               
        }

        private bool RebatchIfConditionMet(Func<bool> predicate, Action doIfRebatch = null)
        {
            if(predicate())
            {
                lock (_lock)
                {
                    if(predicate())
                    {
                        _lastSent = DateTimeOffset.Now;
                        _batches.Add(_currentBatch);
                        _currentBatch = new ProperEventDataBatch(_settings.MaxBatchSize);
                        if(doIfRebatch != null)
                            doIfRebatch();
                        return true;
                    }
                }
            }

            return false;
        }

        public void CheckTimeElapsedSinceLastRebatch()
        {
            RebatchIfConditionMet(() => DateTimeOffset.Now.Subtract(_lastSent).TotalSeconds > _settings.MaxSendIntervalMillis);
        }

        private void Commit(ProperEventDataBatch batch)
        {
            if (batch.Count > 0)
            {
                _settings.Logger(TraceLevel.Verbose, $"{Name}: About to commit batch of size: {batch.CurrentSize}");
                _sender.SendBatchAsync(batch.EventData).GetAwaiter().GetResult(); // no point in doing async, dedicated thread would be waiting anyway
                _settings.Logger(TraceLevel.Verbose, $"{Name}: Successfully sent batch. {_batches.Count} batches left");
            }
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

                    _settings.Logger(TraceLevel.Error, e.ToString());
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
