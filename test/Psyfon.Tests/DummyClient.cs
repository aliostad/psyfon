using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace Psyfon.Tests
{
    class DummyClient : IEventHubClientWrapper
    {

        public List<EventData> Events = new List<EventData>();
        public int CountSent = 0;

        public DummyClient(int partitionCount)
        {
            PartitionCount = partitionCount;
        }

        public int PartitionCount { get; }

        public void Dispose()
        {
        }

        public Task<int> GetPartitionCount()
        {
            return Task.FromResult(PartitionCount);
        }

        public Task SendBatchAsync(IEnumerable<EventData> batch, string partitionKey)
        {
            foreach (var item in batch)
            {
                Events.Add(item);
            }

            CountSent++;

            return Task.FromResult(false);
        }
    }
}
