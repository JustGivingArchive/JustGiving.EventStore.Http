using System;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BindsToAttribute : Attribute
    {
        public BindsToAttribute(string eventType)
        {
            EventType = eventType;
        }

        public string EventType { get; private set; }
    }
}