using System;
using System.Collections.Generic;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public interface IStreamSubscriberIntervalMonitor
    {
        void RemoveEventStreamMonitor(string stream);

        void UpdateEventStreamSubscriberIntervalMonitor(string stream, TimeSpan interval);

        IEnumerable<StreamSubscriberIntervalStats> GetStreamsIntervalStats();

        StreamSubscriberIntervalStats GetStreamIntervalStats(string streamName);

        bool IsAnyStreamBehind();

        bool? IsStreamBehind(string stream);
    }
}
