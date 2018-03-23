using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Psyfon
{
    public interface IEventDispatcher: IDisposable
    {
        /// <summary>
        /// Starts the dispatcher. Should be called when process starts.
        /// </summary>
        void Start();

        /// <summary>
        /// Adds events to be dispatched.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="partitionKey"></param>
        /// <returns>Whether message was accepted</returns>
        bool Add(EventData @event, string partitionKey = null);


        /// <summary>
        /// Adds events to be dispatched.
        /// </summary>
        /// <param name="events"></param>
        /// <param name="partitionKey"></param>
        /// <returns>Whether message was accepted. If one is not accepted it will be false.</returns>
        bool Add(IEnumerable<EventData> events, string partitionKey = null);
     
        /// <summary>
        /// Number of events not yet sent
        /// </summary>
        int EventsInBuffer { get; }
    }
}
