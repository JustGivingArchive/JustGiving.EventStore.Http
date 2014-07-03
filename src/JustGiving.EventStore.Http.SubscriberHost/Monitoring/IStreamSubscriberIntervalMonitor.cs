using System;

namespace JustGiving.EventStore.Http.SubscriberHost.Monitoring
{
    public interface IStreamSubscriberIntervalMonitor
    {
        void RemoveEventStreamMonitor(string stream);
      
        void UpdateEventStreamSubscriberIntervalMonitor(string stream, TimeSpan interval);
        
        string GetStreamsIntervalReport();
        
        bool AnyStreamBehind();
    }
}
