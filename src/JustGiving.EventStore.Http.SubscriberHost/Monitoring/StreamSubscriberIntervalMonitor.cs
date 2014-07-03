using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public class StreamSubscriberIntervalMonitor : ConcurrentDictionary<string, StreamIntervalTick>, IStreamSubscriberIntervalMonitor
    {
        public bool IsAnyStreamBehind()
        {
            var now = DateTime.Now;
            if (Values.Any(value => IsStreamSubscriberBehind(now, value)))
            {
                return true;
            }
            return false;
        }

        private static bool IsStreamSubscriberBehind(DateTime now, StreamIntervalTick tick)
        {
            return now.AddSeconds(-tick.Interval.TotalSeconds * 2) > tick.LastTick;
        }

        public IEnumerable<StreamSubscriberIntervalStats> GetStreamsIntervalStats()
        {
            return this.Select(item => new StreamSubscriberIntervalStats(item.Value)
            {
                StreamName = item.Key,
                IsStreamBehind = IsStreamSubscriberBehind(DateTime.Now, item.Value)
            }).ToList();
        }

        public void UpdateEventStreamSubscriberIntervalMonitor(string stream, TimeSpan interval)
        {
            this[stream] = new StreamIntervalTick()
            {
                Interval = interval,
                LastTick = DateTime.Now
            };
        }

        public void RemoveEventStreamMonitor(string stream)
        {
            StreamIntervalTick removed;
            this.TryRemove(stream, out removed);
        }

        public StreamSubscriberIntervalStats GetStreamIntervalStats(string stream)
        {
            try
            {
                var streamTick = this[stream];
                return new StreamSubscriberIntervalStats(streamTick)
                {
                    StreamName = stream,
                    IsStreamBehind = IsStreamSubscriberBehind(DateTime.Now, streamTick)
                };
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        public bool? IsStreamBehind(string stream)
        {
            try
            {
                var streamTick = this[stream];
                return IsStreamSubscriberBehind(DateTime.Now, streamTick);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }
    }
}