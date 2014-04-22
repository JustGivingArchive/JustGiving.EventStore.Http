using System;
using JustGiving.EventStore.Http.Client;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class EventStreamSubscriberSettings
    {
        internal EventStreamSubscriberSettings(IEventStoreHttpConnection connecion, IEventHandlerResolver eventHandlerResolver, IStreamPositionRepository streamPositionRepository, ISubscriptionTimerManager _subscriptionTimerManager, TimeSpan pollingInterval, int? maxConcurrency, int sliceSize)
        {
            Connection = connecion;
            EventHandlerResolver = eventHandlerResolver;
            StreamPositionRepository = streamPositionRepository;
            SubscriptionTimerManager = _subscriptionTimerManager;
            MaxConcurrency = maxConcurrency;
            DefaultPollingInterval = pollingInterval;
            SliceSize = sliceSize;
        }

        /// <summary>
        /// Creates a new set of <see cref="EventStreamSubscriberSettings"/>
        /// </summary>
        /// <returns>A <see cref="EventStreamSubscriberSettings"/> that can be used to build up an <see cref="EventStreamSubscriber"/></returns>
        public static EventStreamSubscriberSettings Default(IEventStoreHttpConnection connection, IEventHandlerResolver eventHandlerResolver, IStreamPositionRepository streamPositionRepository, ISubscriptionTimerManager subscriptionTimerManager)
        {
            return new EventStreamSubscriberSettingsBuilder(connection, eventHandlerResolver, streamPositionRepository, subscriptionTimerManager);
        }

        public IEventStoreHttpConnection Connection { get; private set; }
        public IEventHandlerResolver EventHandlerResolver { get; private set; }
        public IStreamPositionRepository StreamPositionRepository { get; private set; }
        public ISubscriptionTimerManager SubscriptionTimerManager { get; private set; }

        public TimeSpan DefaultPollingInterval { get; private set; }
        public int? MaxConcurrency { get; private set; }
        public int SliceSize { get; private set; }
    }
}