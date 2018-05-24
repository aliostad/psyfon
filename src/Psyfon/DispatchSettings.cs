using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Psyfon
{
    /// <summary>
    /// Settings for the Dispatcher
    /// </summary>
    public class DispatchSettings
    {
        /// <summary>
        /// Maximum size of the batch sent to EventHub. 64KB by default
        /// </summary>
        public int MaxBatchSize { get; set; } = BufferingEventDispatcher.DefaultBatchSize;

        /// <summary>
        /// Maximum number of milliseconds before flushing events to EventHub. 1 second by default
        /// </summary>
        public int MaxSendIntervalMillis { get; set; } = BufferingEventDispatcher.DefaultMaxIntervalMillis;

        /// <summary>
        /// An implementation of uniform hashing. By default uses MD5
        /// </summary>
        public IHasher Hasher { get; set; } = new Md5Hasher();

        /// <summary>
        /// A tracer/logger. By default uses Trace.WriteLine
        /// </summary>
        public Action<TraceLevel, string> Logger { get; set; } = ((TraceLevel level, string message) =>
        {
            switch (level)
            {
                case TraceLevel.Error:
                    Trace.TraceError(message);
                    break;
                case TraceLevel.Warning:
                    Trace.TraceWarning(message);
                    break;
                default:
                    Trace.TraceInformation(message);
                    break;
            }
        });

        /// <summary>
        /// Whether the dedicated threadpool should be background thread. Default is FALSE meaning the threads will prevent process terminating until the are done.
        /// Default behaviour ensures all buffered events get committed before process terminates. 
        /// </summary>
        public bool UseBackgroundThreads { get; set; } = false;

        /// <summary>
        /// Maximum number of events buffered. Default is 1 million events.
        /// More than that and Add() will start throwing exception.
        /// </summary>
        public int MaximumBufferedEvents { get; set; } = 1000 * 1000;
    }
}
