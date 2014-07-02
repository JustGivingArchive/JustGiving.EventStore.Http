using System;
using System.Collections.Generic;
using JustGiving.EventStore.Http.Client;
using JustGiving.EventStore.Http.SubscriberHost.Monitoring;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public interface IEventStreamSubscriber
    {
        /// <summary>
        /// Poll the apecified stream for new events, and invoke event processors accordingly
        /// </summary>
        /// <param name="stream">The name of the stream to be polled</param>
        /// <param name="pollInterval">The period after receiving no new events that the <see cref="EventStoreHttpConnection"/> should try again. If not specified, the subscriber's default period will be used</param>
        /// <remarks>Note that if a poll yields even a single event, the next poll will occur immediately afterwards</remarks>
        void SubscribeTo(string stream, TimeSpan? pollInterval = null);

        /// <summary>
        /// Stop polling the specified stream for updates
        /// </summary>
        /// <param name="stream">The name of the stream to be polled</param>
        void UnsubscribeFrom(string stream);

        /// <summary>
        /// Time-series statistics relating to the processing of every possible message from the event store
        /// </summary>
        PerformanceStats AllEventsStats { get; }

        /// <summary>
        /// Time-series statistics relating to the processing of every pprocessed message from the event store
        /// </summary>
        PerformanceStats ProcessedEventsStats { get; }

        /// <summary>
        /// Stores the latest ticks and their intervals for all streams
        /// </summary>
        StreamTickMonitor StreamTicks { get; }
    }
}