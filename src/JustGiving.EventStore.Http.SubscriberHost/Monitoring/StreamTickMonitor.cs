using System;
using System.Collections.Concurrent;
using System.Text;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public class StreamTickMonitor : ConcurrentDictionary<string, Tuple<TimeSpan, DateTime>>
    {
        public StreamTickMonitor()
        {

        }

        public bool AnyStreamBehind()
        {
            var now = DateTime.Now;
            foreach (var value in Values)
            {
                if (IsBehind(now, value))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsBehind(DateTime now, Tuple<TimeSpan, DateTime> value)
        {
            return now.AddSeconds(-value.Item1.TotalSeconds * 2) > value.Item2;
        }

        public string GetDeailStats()
        {
            var builder = new StringBuilder();
            var now = DateTime.Now;
            builder.AppendLine("Stream tick stats:");
            foreach (var item in this)
            {
                builder.AppendLine(string.Format("Stream: {0}, Interval: {1}, LastTick: {2}, Now: {4}, IsBehind: {3}", item.Key, item.Value.Item1, item.Value.Item2, IsBehind(now, item.Value), now));
            }
            return builder.ToString();
        }
    }
}