using System;
using System.Collections;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    /// <summary>
    /// A strategy for finding event handlers able to handle an event from a stream
    /// </summary>
    /// <remarks>A nuget-based ninject package exists, but users of other containers must implement their own versions (or submit a pull request! :p)</remarks>
    public interface IEventHandlerResolver 
    {
        /// <summary>
        /// Get all instances of <see cref="IHandleEventsOf{T}"/> where T is the eventType
        /// </summary>
        /// <param name="eventType">The .Net event type read from the stream</param>
        /// <returns></returns>
        IEnumerable GetHandlersFor(Type eventType);
    }
}