using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace Psyfon.Tests
{
    public class BufferingEventDispatcherTests
    {
        const int PartCount = 32;

        [Fact]
        public void DoesWhatItSaysOnTheTin()
        {
            var cli = new DummyClient(PartCount);
            var batch = new List<EventData>();
            var bed = new BufferingEventDispatcher(cli, batchBufferSize: 1000);
            bed.Start();

            for (int i = 0; i < 100; i++)
            {
                bed.Add(new EventData(new byte[200]), partitionKey: "samething");
            }

            Thread.Sleep(1000);
            bed.Dispose();
            Thread.Sleep(200);

            Assert.Equal(100, cli.Events.Count);
            Assert.Equal(20, cli.CountSent);

        }
    }
}
