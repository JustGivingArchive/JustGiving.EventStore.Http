using System.Collections.Generic;

namespace JustGiving.EventStore.Http.Client
{
    public class StreamEventsSlice : BasicEventInfo
    {
        public string StreamId { get; set; }
        public string HeadOfStream { get; set; }
        public StreamReadStatus Status { get; set; }

        public List<BasicEventInfo> Entries { get; set; }

        public static StreamEventsSlice StreamNotFound()
        {
            return new StreamEventsSlice
            {
                Status = StreamReadStatus.StreamNotFound
            };
        }

        public static StreamEventsSlice StreamDeleted()
        {
            return new StreamEventsSlice
            {
                Status = StreamReadStatus.StreamDeleted
            };
        }
    }
}