using System;
using System.Threading.Tasks;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    /// <summary>
    /// Provides an abstraction around a collection of timers to make the <see cref="EventStreamSubscriber"/> testable
    /// </summary>
    /// <remarks>This interface does not need to be implemented separately, but exists for testing purposes</remarks>
    public interface ISubscriptionTimerManager
    {
        void Add(string stream, string subscriberId, TimeSpan interval, Func<Task> handler, Action streamIntervalMonitor);
        void Remove(string stream, string subscriberId);
        void Pause(string stream, string subscriberId);
        void Resume(string stream, string subscriberId);
    }
}