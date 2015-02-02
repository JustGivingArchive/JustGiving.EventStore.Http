using System;
using System.Net;

namespace JustGiving.EventStore.Http.Client.Exceptions
{
    public class EventNotFoundException : Exception
    {
        private readonly string _eventUrl;

        public EventNotFoundException(string eventUrl)
        {
            _eventUrl = eventUrl;
            HttpStatusCode = HttpStatusCode.NotFound;
        }

        public EventNotFoundException(string eventUrl, HttpStatusCode httpStatusCode, string httpResponseContent)
        {
            _eventUrl = eventUrl;
            HttpStatusCode = httpStatusCode;
            HttpResponseContent = httpResponseContent;
        }

        public string HttpResponseContent { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }

        public new string Message
        {
            get
            {
                return string.Format("Event not found. Url: {0} HttpStatusCode: {1} HttpResponseContent: {2}",
                    _eventUrl, HttpStatusCode, HttpResponseContent);
            }
        }

        public string EventUrl
        {
            get { return _eventUrl; }
        }
    }
}
