namespace JustGiving.EventStore.Http.Client
{
    /// <summary>
    /// A structure representing a single event or an resolved link event.
    /// </summary>
    public class EventInfo : BasicEventInfo
    {
        public RecordedEvent Content { get; set; }
    }
}