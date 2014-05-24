using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public class PerformanceRecord
    {
        private readonly TimeSpan recordPeriod;
        private readonly ConcurrentDictionary<string, long> queueCounts = new ConcurrentDictionary<string, long>();
        public long TotalProcessedEventCount { get; private set; }

        public object syncRoot = new object();

        public PerformanceRecord(TimeSpan recordPeriod, DateTime? startDate = null)
        {
            StartTime = startDate ?? DateTime.Now;
            this.recordPeriod = recordPeriod;
        }

        public DateTime StartTime { get; private set; }

        public DateTime EndTime
        {
            get { return StartTime + recordPeriod; }
        }

        public void EventProcessed(string queue)
        {
            lock (syncRoot)
            {
                queueCounts.AddOrUpdate(queue, x => 1, (k, v) => v + 1);
                TotalProcessedEventCount++; //already atomic
            }
        }

        public IReadOnlyDictionary<string, long> QueueStats()
        {
            lock (syncRoot)
            {
                return new ReadOnlyDictionary<string, long>(queueCounts);
            }
        }

        public long GetProcessedEventsFor(string queue)
        {
            var count = 0L;
            queueCounts.TryGetValue(queue, out count);
            return count;
        }
    }
}