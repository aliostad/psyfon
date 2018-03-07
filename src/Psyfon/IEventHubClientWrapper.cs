using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Psyfon
{
    public interface IEventHubClientWrapper: IDisposable
    {
        Task SendBatchAsync(IEnumerable<EventData> batch, string partitionKey);

        Task<int> GetPartitionCount();
    }
}
