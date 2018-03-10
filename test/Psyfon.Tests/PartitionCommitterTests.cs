using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace Psyfon.Tests
{
    public class PartitionCommitterTests
    {
        const int PartCount = 32;

        [Fact]
        public void SendsAll()
        {
            var cli = new DummyClient(PartCount);
            var batch = new List<EventData>();
            var pc = new PartitionCommitter(cli, 1000, (a,b) => { });
            
            for (int i = 0; i < 100; i++)
            {
                pc.Add(new EventData(new byte[200]));
            }

            Thread.Sleep(1000);
            pc.Dispose();

            Assert.Equal(100, cli.Events.Count);
            Assert.Equal(20, cli.CountSent);
        }

    }
}
