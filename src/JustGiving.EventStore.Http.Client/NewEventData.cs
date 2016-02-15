using System;

namespace JustGiving.EventStore.Http.Client
{
    /// <summary>
    /// Represents an event to be written.
    /// <para>
    /// When using any of the <c>Create</c> factory methods, the CLR full typename
    /// of the generic type argument is used as the <see cref="EventType"/>.
    /// </para>
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
        /// Event metadata.
        /// </summary>
        public readonly object Metadata;

        /// <summary>
        /// Constructs a new <see cref="NewEventData" />.
        /// </summary>
        /// <param name="eventId">The ID of the event, used as part of the idempotent write check.</param>
        /// <param name="eventType">The name of the event type. It is strongly recommended that these
        /// use lowerCamelCase if projections are to be used.</param>
        /// <param name="data">The raw bytes of the event data.</param>
        /// <param name="metadata">Event metadata.</param>
        public NewEventData(Guid eventId, string eventType, object data, object metadata)
        {
            EventId = eventId;
            EventType = eventType;
            Data = data;
            Metadata = metadata;
        }

        /// <summary>
        /// Construct a new <see cref="NewEventData"/> from <paramref name="data"/>, with a random <see cref="EventId"/> and without metadata.
        /// </summary>
        /// <remarks>The CLR full typename of the generic type argument is used as the <see cref="EventType"/>.</remarks>
        public static NewEventData Create<T>(T data)
        {
            return Create(Guid.NewGuid(), data);
        }

        /// <summary>
        /// Construct a new <see cref="NewEventData"/> from <paramref name="data"/>, with a random <see cref="EventId"/> and <paramref name="metadata"/>.
        /// </summary>
        /// <remarks>The CLR full typename of the generic type argument is used as the <see cref="EventType"/>.</remarks>
        public static NewEventData Create<T>(T data, object metadata)
        {
            return Create(Guid.NewGuid(), data, metadata);
        }

        /// <summary>
        /// Construct a new <see cref="NewEventData"/> with the given <paramref name="eventId"/>, <paramref name="data"/> optional <paramref name="metadata"/>.
        /// </summary>
        /// <remarks>The CLR full typename of the generic type argument is used as the <see cref="EventType"/>.</remarks>
        public static NewEventData Create<T>(Guid eventId, T data, object metadata = null)
        {
            var eventType = typeof(T) == typeof(object) ? data.GetType() : typeof (T);
            return new NewEventData(eventId, eventType.FullName, data, metadata);
        }
    }
}
