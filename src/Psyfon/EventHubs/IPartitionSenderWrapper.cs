using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Psyfon
{
    public interface IPartitionSenderWrapper
    {
        Task SendBatchAsync(IEnumerable<EventData> events);
    }
}
