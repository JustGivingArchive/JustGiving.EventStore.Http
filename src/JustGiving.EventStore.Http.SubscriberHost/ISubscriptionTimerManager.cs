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
        void Add(string stream, TimeSpan interval, Func<Task> handler, Action tickMonitor);
        void Remove(string stream);
        void Pause(string stream);
        void Resume(string stream);
    }
}