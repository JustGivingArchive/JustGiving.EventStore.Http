using System.Threading.Tasks;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public interface IHandleEventsOf<T>
    {
        Task Handle(T @event);
    }
}