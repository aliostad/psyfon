using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace Psyfon.Tests
{
    class DummyClient : IEventHubClientWrapper, IPartitionSenderWrapper
    {

        public List<EventData> Events = new List<EventData>();
        public int CountSent = 0;

        public DummyClient(int partitionCount)
        {
            PartitionCount = partitionCount;
        }

        public int PartitionCount { get; }

        public IPartitionSenderWrapper CreatePartitionSender(string partitionId)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public Task<int> GetPartitionCount()
        {
            return Task.FromResult(PartitionCount);
        }

        public Task<string[]> GetPartitions()
        {
            return Task.FromResult(Enumerable.Range(0, 32).Select(x => x.ToString()).ToArray());
        }

        public async Task SendBatchAsync(IEnumerable<EventData> events)
        {
            foreach (var item in events)
            {
                Events.Add(item);
            }

            CountSent++;
        }
    }
}
