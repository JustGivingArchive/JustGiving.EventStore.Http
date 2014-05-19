using System;
using System.Collections.Generic;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public interface IPerformanceCounter
    {
        void MessageProcessed(string queueName);
        IEnumerable<PerformanceRecord> Records { get; } 
    }

    public class PerformanceStats
    {
        private readonly TimeSpan recordPeriod;
        private readonly int maxRecordCount;
        private PerformanceRecord head;

        private readonly Queue<PerformanceRecord> records = new Queue<PerformanceRecord>();

        public PerformanceStats(TimeSpan recordPeriod, int maxRecordCount)
        {
            this.recordPeriod = recordPeriod;
            this.maxRecordCount = maxRecordCount;
            head = new PerformanceRecord(recordPeriod);
            records.Enqueue(head);
        }

        public void MessageProcessed(string queueName)
        {
            TidyRecords();
            head.EventProcessed(queueName);//no need to await
        }

        public void TidyRecords()//This should create a solid queue of events up until the present.  Currently it is sparse
        {
            if (head.EndTime < DateTime.Now)
            {
                head = new PerformanceRecord(recordPeriod);
                records.Enqueue(head);
            }
            if (records.Count > maxRecordCount)
            {
                records.Dequeue();
            }
        }

        public IEnumerable<PerformanceRecord> Records
        {
            get { return records; }
        }
    }
}