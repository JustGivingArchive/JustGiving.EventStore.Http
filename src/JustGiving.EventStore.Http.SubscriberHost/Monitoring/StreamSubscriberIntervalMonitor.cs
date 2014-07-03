using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using JustGiving.EventStore.Http.Client.Common.Utils;
using log4net;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public class StreamSubscriberIntervalMonitor : ConcurrentDictionary<string, Tuple<TimeSpan, DateTime>>, IStreamSubscriberIntervalMonitor
    {
        public StreamSubscriberIntervalMonitor()
        {
            
        }

        public bool AnyStreamBehind()
        {
            var now = DateTime.Now;
            return Values.All(value => !IsStreamSubscriberBehind(now, value));
        }

        private static bool IsStreamSubscriberBehind(DateTime now, Tuple<TimeSpan, DateTime> value)
        {
            return now.AddSeconds(-value.Item1.TotalSeconds * 2) > value.Item2;
        }

        public string GetStreamsIntervalReport()
        {
            var builder = new StringBuilder();
            var now = DateTime.Now;
            builder.AppendLine("Event Stream Subscriber Interval stats:");
            foreach (var item in this)
            {
                builder.AppendLine(string.Format("Stream: {0}, IsBehind: {3}, Interval: {1}, LastTick: {2}, Now: {4}", item.Key, item.Value.Item1, item.Value.Item2, IsStreamSubscriberBehind(now, item.Value), now));
            }
            return builder.ToString();
        }

        public void UpdateEventStreamSubscriberIntervalMonitor(string stream, TimeSpan interval)
        {
            this[stream] = Tuple.Create(interval, DateTime.Now);
        }

        public void RemoveEventStreamMonitor(string stream)
        {
            Tuple<TimeSpan, DateTime> removed;
            this.TryRemove(stream, out removed);
        }
    }
}