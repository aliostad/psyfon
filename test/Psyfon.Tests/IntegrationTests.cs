using Microsoft.Azure.EventHubs;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Psyfon.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        [Test]
        public void CanTrulyPush()
        {
            var cn = Environment.GetEnvironmentVariable("EH_CN_PSYFON");
            if(string.IsNullOrEmpty(cn))
            {
                Assert.Inconclusive("Please set EH_CN_PSYFON env var to run.");
                return;
            }

            var bed = new BufferingEventDispatcher(cn);
            bed.Start();
            for (int i = 0; i < 100; i++)
            {
                bed.Add(new EventData(new byte[10 * 1024]));
            }

            Thread.Sleep(2000);
            bed.Dispose();
            Thread.Sleep(200);
        }
    }
}
