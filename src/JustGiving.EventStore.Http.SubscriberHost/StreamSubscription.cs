using System;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class StreamSubscription
    {
        public StreamSubscription(string streamName, string subscriberId, TimeSpan interval, bool active)
        {
            StreamName = streamName;
            SubscriberId = subscriberId;
            Interval = interval;
            Active = active;
        }

        public string StreamName { get; private set; }
        public string SubscriberId { get; private set; }
        public TimeSpan Interval { get; private set; }
        public bool Active { get; private set; }
    }
}