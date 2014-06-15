using JustGiving.EventStore.Http.Client.Common.Utils;

namespace JustGiving.EventStore.Http.Client
{
    /// <summary>
    /// A Event Read Result is the result of a single event read operation to the event store.
    /// </summary>
    public class EventReadResult
    {
        /// <summary>
        /// The <see cref="EventReadStatus"/> representing the status of this read attempt
        /// </summary>
        public readonly EventReadStatus Status;

        /// <summary>
        /// The event read represented as an <see cref="EventInfo"/>
        /// </summary>
        public readonly EventInfo EventInfo;

        public EventReadResult(EventReadStatus status, EventInfo eventInfo)
        {
            Status = status;
            EventInfo = eventInfo;
        }
    }
}