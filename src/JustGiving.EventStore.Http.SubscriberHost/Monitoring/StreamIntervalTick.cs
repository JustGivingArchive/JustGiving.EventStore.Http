using System;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public class StreamIntervalTick
    {
        public TimeSpan Interval { get; set; }
        public DateTime LastTick { get; set; }
    }
}
