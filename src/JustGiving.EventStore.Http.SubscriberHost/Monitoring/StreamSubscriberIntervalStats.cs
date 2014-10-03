
namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public class StreamSubscriberIntervalStats : StreamIntervalTick
    {
        public StreamSubscriberIntervalStats(StreamIntervalTick tick)
        {
            Interval = tick.Interval;
            LastTick = tick.LastTick;
        }

        public string StreamName { get; set; }
        public string SubscriberId { get; set; }
        public bool IsStreamBehind { get; set; }
    }
}
