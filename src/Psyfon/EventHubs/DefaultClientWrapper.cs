using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace Psyfon
{
    internal class DefaultClientWrapper : IEventHubClientWrapper
    {
        private readonly EventHubClient _client;

        public DefaultClientWrapper(EventHubClient client)
        {
            _client = client;
        }

        public void Dispose()
        {
            _client.Close();
        }

        public async Task<string[]> GetPartitions()
        {
            var info = await _client.GetRuntimeInformationAsync();
            return info.PartitionIds;
        }

        public IPartitionSenderWrapper CreatePartitionSender(string partitionId)
        {
            return new DefaultPartitionSender(_client.CreatePartitionSender(partitionId)); 
        }
    }
}
