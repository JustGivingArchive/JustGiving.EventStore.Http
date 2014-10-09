using System;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    /// <summary>
    /// Indicates that the message handler is only to be processed when being invoked for a named-subscriber
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class  NonDefaultSubscriberAttribute : Attribute
    {
        /// <summary>
        /// The SubscriberId that the marked-up handler responds to
        /// </summary>
        public NonDefaultSubscriberAttribute(string supportedSubscriberId)
        {
            SupportedSubscriberId = supportedSubscriberId;
        }

        /// <summary>
        /// The SubscriberId that the marked-up handler responds to
        /// </summary>
        public string SupportedSubscriberId { get; private set; }
    }
}