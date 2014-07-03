
namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public class StreamSubscriberIntervalStats : StreamIntervalTick
    {
        public StreamSubscriberIntervalStats(StreamIntervalTick tick)
        {
            base.Interval = tick.Interval;
            base.LastTick = tick.LastTick;
        }

        public string StreamName { get; set; }
        public bool IsStreamBehind { get; set; }
    }
}
