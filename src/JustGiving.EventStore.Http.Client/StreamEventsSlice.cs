using System.Collections.Generic;

namespace JustGiving.EventStore.Http.Client
{
    public class StreamEventsSlice : BasicEventInfo
    {
        public string StreamId { get; set; }
        public string HeadOfStream { get; set; }

        public List<BasicEventInfo> Entries { get; set; }
    }
}