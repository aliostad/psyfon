using Microsoft.Azure.EventHubs;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Psyfon.Tests
{
    [TestFixture]
    public class PartitionCommitterTests
    {
        const int PartCount = 32;

        [Test]
        public void SendsAll()
        {
            var cli = new DummyClient(PartCount);
            var batch = new List<EventData>();
            var pc = new PartitionCommitter(cli, 1000, "c", (a,b) => { });
            
            for (int i = 0; i < 100; i++)
            {
                pc.Add(new EventData(new byte[200]));
            }

            Thread.Sleep(1000);
            pc.Dispose();

            Assert.AreEqual(100, cli.Events.Count);
            Assert.AreEqual(20, cli.CountSent);
        }

    }
}
