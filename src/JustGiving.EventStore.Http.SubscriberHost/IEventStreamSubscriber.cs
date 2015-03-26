using System;
using System.Collections.Generic;
using JustGiving.EventStore.Http.Client;
using JustGiving.EventStore.Http.SubscriberHost.Monitoring;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public interface IEventStreamSubscriber
    {
        /// <summary>
        /// Poll the specified stream for new events, and invoke event processors accordingly.
        /// </summary>
        /// <param name="stream">The name of the stream to be polled</param>
        /// <param name="subscriberId">The arbitrary name of a specific subscriber to a stream, to support multiple subscribers in the same app, which may be at different positions</param>
        /// <param name="pollInterval">The period after receiving no new events that the <see cref="EventStoreHttpConnection"/> should try again. If not specified, the subscriber's default period will be used</param>
        /// <remarks>Note that if a poll yields even a single event, the next poll will occur immediately afterwards</remarks>
        void SubscribeTo(string stream, string subscriberId = null, TimeSpan? pollInterval = null);

        /// <summary>
        /// Stop polling the specified stream for updates.
        /// </summary>
        /// <param name="stream">The name of the stream to be polled</param>
        /// <param name="subscriberId">The arbitrary name of a specific subscriber to a stream, to support multiple subscribers in the same app, which may be at different positions</param>
        void UnsubscribeFrom(string stream, string subscriberId = null);

        /// <summary>
        /// Time-series statistics relating to the processing of every possible message from the event store.
        /// </summary>
        PerformanceStats AllEventsStats { get; }

        /// <summary>
        /// Time-series statistics relating to the processing of every processed message from the event store.
        /// </summary>
        PerformanceStats ProcessedEventsStats { get; }

        /// <summary>
        /// Stores the latest ticks and their intervals for all streams.
        /// </summary>
        IStreamSubscriberIntervalMonitor StreamSubscriberMonitor { get; }

        IEnumerable<StreamSubscription> GetSubscriptions();
    }
}