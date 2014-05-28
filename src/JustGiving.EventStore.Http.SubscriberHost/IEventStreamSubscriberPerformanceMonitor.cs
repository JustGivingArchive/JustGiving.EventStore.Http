using System;
using System.Collections.Generic;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    /// <summary>
    /// Hook to get metadata on the subscriber's performance, e.g. to call out to a logging platform
    /// </summary>
    public interface IEventStreamSubscriberPerformanceMonitor
    {
        /// <summary>
        /// Invoked every time an event is seen. n.b. an event will not actually be downloaded if there are no handlers to receive it 
        /// </summary>
        /// <param name="stream">The name of the stream in which the event was found</param>
        /// <param name="messageType">The event's subject</param>
        /// <param name="handlerCount">The number of event handlers registered which were able to process the message</param>
        /// <param name="processingExceptions">Exceptions raised by the invoked handlers</param>
        void Accept(string stream, string messageType, int handlerCount, IEnumerable<KeyValuePair<Type, Exception>> processingExceptions);
    }
}