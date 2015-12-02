using System;
using System.Collections.Generic;
using System.Linq;

namespace JustGiving.EventStore.Http.Client
{
    public class BasicEventInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string EventType { get; set; }
        public DateTime Updated { get; set; }

        // Leaving sequence number here to be compatible backwards
        [Obsolete("Use PositionEventNumber instead.")]
        public int SequenceNumber { get { return PositionEventNumber; } }

        public int PositionEventNumber { get; set; }

        public List<Link> Links { get; set; }

        public string CanonicalEventLink
        {
            get { return Links.First(x => x.Relation == "edit").Uri; }
        }
    }

    public class Link
    {
        public string Uri {get; set; }
        public string Relation { get; set; }
    }
}
