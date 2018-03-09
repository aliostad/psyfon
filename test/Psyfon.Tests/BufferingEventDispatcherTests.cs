using Microsoft.Azure.EventHubs;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Psyfon.Tests
{
    [TestFixture]
    public class BufferingEventDispatcherTests
    {
        const int PartCount = 32;

        [Test]
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

            Assert.AreEqual(100, cli.Events.Count);
            Assert.AreEqual(20, cli.CountSent);

        }
    }
}
