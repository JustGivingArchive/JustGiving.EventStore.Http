using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public class StreamSubscriberIntervalMonitor : ConcurrentDictionary<string, StreamIntervalTick>, IStreamSubscriberIntervalMonitor
    {
        // this is "purposly" left hidden out of the consumer, is up to this app to decide what delayend subscriber means 
        private const int StreamConsideredFallBehindTickCount = 5;

        public bool IsAnyStreamBehind()
        {
            var now = DateTime.Now;
            if (Values.Any(value => IsStreamSubscriberBehind(now, value, StreamConsideredFallBehindTickCount)))
            {
                return true;
            }
            return false;
        }

        private static bool IsStreamSubscriberBehind(DateTime now, StreamIntervalTick tick, int failIntervalThresh)
        {
            // if we miss {failIntervalThresh}  * tick intervals we should start getting worried
            return now.AddSeconds(-tick.Interval.TotalSeconds * failIntervalThresh) > tick.LastTick;
        }

        public IEnumerable<StreamSubscriberIntervalStats> GetStreamsIntervalStats()
        {
            return this.Select(item => new StreamSubscriberIntervalStats(item.Value)
            {
                StreamName = SubscriberDetailsFromKey(item.Key).Item1,
                SubscriberId = SubscriberDetailsFromKey(item.Key).Item2,
                IsStreamBehind = IsStreamSubscriberBehind(DateTime.Now, item.Value, StreamConsideredFallBehindTickCount)
            }).ToList();
        }

        public void UpdateEventStreamSubscriberIntervalMonitor(string stream, TimeSpan interval, string subscriberId=null)
        {
            this[TimerKeyFor(stream, subscriberId)] = new StreamIntervalTick
            {
                Interval = interval,
                LastTick = DateTime.Now
            };
        }

        public void RemoveEventStreamMonitor(string stream, string subscriberId)
        {
            StreamIntervalTick removed;
            TryRemove(TimerKeyFor(stream, subscriberId), out removed);
        }

        public StreamSubscriberIntervalStats GetStreamIntervalStats(string stream, string subscriberId)
        {
            try
            {
                var streamTick = this[TimerKeyFor(stream, subscriberId)];
                return new StreamSubscriberIntervalStats(streamTick)
                {
                    StreamName = stream,
                    SubscriberId = subscriberId,
                    IsStreamBehind = IsStreamSubscriberBehind(DateTime.Now, streamTick, StreamConsideredFallBehindTickCount)
                };
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns false if the subscriber falls 5 ticks behind
        /// </summary>
        /// <param name="stream">name of the stream</param>
        /// <param name="subscriberId">subscriber id</param>
        /// <returns></returns>
        public bool? IsStreamBehind(string stream, string subscriberId)
        {
            try
            {
                var streamTick = this[TimerKeyFor(stream, subscriberId)];
                return IsStreamSubscriberBehind(DateTime.Now, streamTick, StreamConsideredFallBehindTickCount);
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        private string TimerKeyFor(string stream, string subscriberId)
        {
            return string.IsNullOrEmpty(subscriberId) ? stream : string.Concat(stream, EventStreamSubscriber.StreamIdentifierSeparator, subscriberId);
        }

        private Tuple<string, string> SubscriberDetailsFromKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            var keyParts = key.Split(new[] { EventStreamSubscriber.StreamIdentifierSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (keyParts.Length == 1)
            {
                return Tuple.Create(keyParts[0], (string)null);
            }

            return Tuple.Create(keyParts[0], keyParts[1]);
        }
    }
}