using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Psyfon
{
    internal class DefaultPartitionSender : IPartitionSenderWrapper
    {
        private readonly PartitionSender _sender;

        public DefaultPartitionSender(PartitionSender sender)
        {
            _sender = sender;
            
        }

        public async Task SendBatchAsync(IEnumerable<EventData> events)
        {
#if NET452
            foreach (var item in events)
	        {
                await _sender.SendAsync(item);
	        }
#else
            var batch = new EventDataBatch(250 * 1024);
            foreach (var item in events)
            {
                if(!batch.TryAdd(item))
                {
                    await _sender.SendAsync(batch);
                    batch = new EventDataBatch(250 * 1024);
                    batch.TryAdd(item);
                }
            }
            await _sender.SendAsync(batch);
#endif
        }
    }
}
