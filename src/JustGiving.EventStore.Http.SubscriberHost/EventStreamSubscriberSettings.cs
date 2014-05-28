using System;
using JustGiving.EventStore.Http.Client;
using log4net;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class EventStreamSubscriberSettings
    {
        internal EventStreamSubscriberSettings(IEventStoreHttpConnection connection, IEventHandlerResolver eventHandlerResolver, IStreamPositionRepository streamPositionRepository, ISubscriptionTimerManager subscriptionTimerManager, IEventTypeResolver eventTypeResolver, TimeSpan pollingInterval, int sliceSize, ILog log, TimeSpan messageProcessingStatsWindowPeriod, int messageProcessingStatsWindowCount)
        {
            Connection = connection;
            EventHandlerResolver = eventHandlerResolver;
            StreamPositionRepository = streamPositionRepository;
            SubscriptionTimerManager = subscriptionTimerManager;
            EventTypeResolver = eventTypeResolver;
            DefaultPollingInterval = pollingInterval;
            SliceSize = sliceSize;
            Log = log;
            MessageProcessingStatsWindowPeriod = messageProcessingStatsWindowPeriod;
            MessageProcessingStatsWindowCount = messageProcessingStatsWindowCount;
        }

        /// <summary>
        /// Creates a new set of <see cref="EventStreamSubscriberSettings"/>
        /// </summary>
        /// <returns>A <see cref="EventStreamSubscriberSettings"/> that can be used to build up an <see cref="EventStreamSubscriber"/></returns>
        public static EventStreamSubscriberSettings Default(IEventStoreHttpConnection connection, IEventHandlerResolver eventHandlerResolver, IStreamPositionRepository streamPositionRepository)
        {
            return new EventStreamSubscriberSettingsBuilder(connection, eventHandlerResolver, streamPositionRepository);
        }

        public IEventStoreHttpConnection Connection { get; private set; }
        public IEventHandlerResolver EventHandlerResolver { get; private set; }
        public IStreamPositionRepository StreamPositionRepository { get; private set; }
        public ISubscriptionTimerManager SubscriptionTimerManager { get; private set; }
        public IEventTypeResolver EventTypeResolver { get; private set; }
        public ILog Log { get; private set; }
        public TimeSpan MessageProcessingStatsWindowPeriod { get; private set; }
        public int MessageProcessingStatsWindowCount { get; private set; }

        public TimeSpan DefaultPollingInterval { get; private set; }

        public int SliceSize { get; private set; }
    }
}