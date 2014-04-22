using System.Threading.Tasks;
using JustGiving.EventStore.Http.Client;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    /// <summary>
    /// A handler for events coming from the <see cref="EventStoreHttpConnection"/>
    /// </summary>
    /// <typeparam name="T">The type of event this handler handles</typeparam>
    public interface IHandleEventsOf<T>
    {
        /// <summary>
        /// Runs the main event-processing logic for an event.  
        /// </summary>
        /// <param name="@event">The event to be processed</param>
        /// <returns>A <see cref="Task"/> to be awaited. The task must not be null</returns>
        Task Handle(T @event);
    }
}