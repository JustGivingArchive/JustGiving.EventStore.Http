using System;
using System.Net;

namespace JustGiving.EventStore.Http.Client.Exceptions
{
    public class EventStoreHttpException : Exception
    {
        public EventStoreHttpException(string message, string reason, HttpStatusCode statusCode) : base(message)
        {
            Reason = reason;
            Message = message;
            StatusCode = statusCode;
        }

        public string Reason { get; private set; }
        public new string Message { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }

        public override string ToString()
        {
            return string.Format("Reason: {0}{1}Message:{2}{1}Code:{3}", Reason, Environment.NewLine, Message, (int)StatusCode);
        }
    }
}
