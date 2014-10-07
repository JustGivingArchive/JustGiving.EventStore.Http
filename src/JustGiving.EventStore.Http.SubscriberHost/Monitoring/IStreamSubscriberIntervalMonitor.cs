using System;
using System.Collections.Generic;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public interface IStreamSubscriberIntervalMonitor
    {
        void RemoveEventStreamMonitor(string stream, string subscriberId);

        void UpdateEventStreamSubscriberIntervalMonitor(string stream, TimeSpan interval, string subscriberId=null);

        IEnumerable<StreamSubscriberIntervalStats> GetStreamsIntervalStats();

        StreamSubscriberIntervalStats GetStreamIntervalStats(string stream, string subscriberId=null);

        bool IsAnyStreamBehind();

        bool? IsStreamBehind(string stream, string subscriberId=null);
    }
}
