using System;
using System.Collections.Generic;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public interface IStreamSubscriberIntervalMonitor
    {
        void RemoveEventStreamMonitor(string stream, string subscriberId);

        void UpdateEventStreamSubscriberIntervalMonitor(string stream, string subscriberId, TimeSpan interval);

        IEnumerable<StreamSubscriberIntervalStats> GetStreamsIntervalStats();

        StreamSubscriberIntervalStats GetStreamIntervalStats(string stream, string subscriberId);

        bool IsAnyStreamBehind();

        bool? IsStreamBehind(string stream, string subscriberId=null);
    }
}
