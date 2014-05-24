using System;
using System.Collections.Generic;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
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

        public void TidyRecords()
        {
            while (head.EndTime < DateTime.Now)
            {
                head = new PerformanceRecord(recordPeriod, head.EndTime.AddTicks(1));
                records.Enqueue(head);

                if (records.Count > maxRecordCount)
                {
                    records.Dequeue();
                }
            }
            
        }

        public IEnumerable<PerformanceRecord> Records
        {
            get { return records; }
        }
    }
}