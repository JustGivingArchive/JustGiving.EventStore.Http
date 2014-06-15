using System.Collections.Generic;
using System.Linq;

namespace JustGiving.EventStore.Http.Client
{
    public class BasicEventInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }

        public int SequenceNumber
        {
            get
            {
                var idString = Title.Split('@')[0];
                return int.Parse(idString);
            }
        }

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