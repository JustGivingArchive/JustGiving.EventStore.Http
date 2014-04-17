using System;
using System.Collections;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public interface IEventHandlerResolver 
    {
        IEnumerable GetHandlersFor(Type eventType);
    }
}