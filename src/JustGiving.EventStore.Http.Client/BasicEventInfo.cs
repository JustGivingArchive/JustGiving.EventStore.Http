using System;

namespace JustGiving.EventStore.Http.Client
{
    public class BasicEventInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime? Updated { get; set; }
        public string Summary { get; set; }

        public int SequenceNumber
        {
            get
            {
                var idString = Title.Split('@')[0];
                return int.Parse(idString);
            }
        }
    }
}