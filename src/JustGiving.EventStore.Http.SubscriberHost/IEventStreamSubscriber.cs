using System;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public interface IEventStreamSubscriber
    {
        void SubscribeTo(string stream, TimeSpan? pollInterval = null);
        void UnsubscribeFrom(string stream);
    }
}