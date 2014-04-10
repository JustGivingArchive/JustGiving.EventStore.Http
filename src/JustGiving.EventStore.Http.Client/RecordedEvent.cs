using Newtonsoft.Json.Linq;

namespace JustGiving.EventStore.Http.Client
{
    public struct RecordedEvent
    {
        public string EventStreamId { get; set; }
        public long EventNumber { get; set; }
        public string EventType { get; set; }
        public JToken Data { get; set; }

        public T GetObject<T>() where T : class
        {
            if (Data == null)
            {
                return null;
            }

            return Data.ToObject<T>();
        }
    }
}