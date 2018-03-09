using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Xunit;

namespace Psyfon.Tests
{
    public class IntegrationTests
    {
        [EnvVarIgnoreFact("EH_CN_PSYFON")]
        public void CanTrulyPush()
        {
            var cn = Environment.GetEnvironmentVariable("EH_CN_PSYFON");

            Action<TraceLevel, string> logger = (t, s) =>
            {
                if (t == TraceLevel.Error)
                    throw new Exception(s);
            };


            var bed = new BufferingEventDispatcher(cn, logger: logger);
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

    public class EnvVarIgnoreFactAttribute : FactAttribute
    {
        public EnvVarIgnoreFactAttribute(string envVar)
        {
            var env = Environment.GetEnvironmentVariable(envVar);
            if(string.IsNullOrEmpty(env))
            {
                Skip = $"Please set {envVar} env var to run.";
            }
        }
    }
}
