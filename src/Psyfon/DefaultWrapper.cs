using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace Psyfon
{
    internal class DefaultWrapper : IEventHubClientWrapper
    {
        private readonly EventHubClient _client;

        public DefaultWrapper(EventHubClient client)
        {
            _client = client;
        }

        public void Dispose()
        {
            _client.Close();
        }

        public async Task<int> GetPartitionCount()
        {
            var info = await _client.GetRuntimeInformationAsync();
            return info.PartitionCount;
        }

        public Task SendBatchAsync(IEnumerable<EventData> batch, string partitionKey)
        {
            return _client.SendAsync(batch, partitionKey);            
        }
    }
}
