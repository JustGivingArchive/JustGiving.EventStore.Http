using System;

namespace JustGiving.EventStore.Http.Client
{
    /// <summary>
    /// Represents an event to be written.
    /// </summary>
    public sealed class NewEventData
    {
        /// <summary>
        /// The ID of the event, used as part of the idempotent write check.
        /// </summary>
        public readonly Guid EventId;
        /// <summary>
        /// The name of the event type. It is strongly recommended that these
        /// use lowerCamelCase if projections are to be used.
        /// </summary>
        public readonly string EventType;
        /// <summary>
        /// The raw bytes of the event data.
        /// </summary>
        public readonly object Data;

        /// <summary>
        /// Constructs a new <see cref="NewEventData" />.
        /// </summary>
        /// <param name="eventId">The ID of the event, used as part of the idempotent write check.</param>
        /// <param name="eventType">The name of the event type. It is strongly recommended that these
        /// use lowerCamelCase if projections are to be used.</param>
        /// <param name="data">The raw bytes of the event data.</param>
        private NewEventData(Guid eventId, string eventType, object data)
        {
            EventId = eventId;
            EventType = eventType;
            Data = data;
        }

        public static NewEventData Create<T>(T data)
        {
            return Create(Guid.NewGuid(), data);
        }

        public static NewEventData Create<T>(Guid eventId, T data)
        {
            return new NewEventData(eventId, typeof(T).FullName, data);
        }
    }
}