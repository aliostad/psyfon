using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Psyfon
{
    public interface IEventHubClientWrapper: IDisposable
    {
        IPartitionSenderWrapper CreatePartitionSender(string partitionId);

        Task<string[]> GetPartitions();
    }
}
