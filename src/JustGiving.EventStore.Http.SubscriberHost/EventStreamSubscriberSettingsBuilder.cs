using System;
using JustGiving.EventStore.Http.Client;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class EventStreamSubscriberSettingsBuilder
    {
        private IEventStoreHttpConnection _connection;
        private IEventHandlerResolver _eventHandlerResolver;
        private IStreamPositionRepository _streamPositionRepository;
        private TimeSpan _defaultPollingInterval = TimeSpan.FromSeconds(30);
        private int? _maxConcurrency;
        private int _sliceSize = 100;
        
        public EventStreamSubscriberSettingsBuilder(IEventStoreHttpConnection connection, IEventHandlerResolver eventHandlerResolver, IStreamPositionRepository streamPositionRepository)
        {
            _connection = connection;
            _eventHandlerResolver = eventHandlerResolver;
            _streamPositionRepository = streamPositionRepository;
        }

        public EventStreamSubscriberSettingsBuilder WithDefaultPollingInterval(TimeSpan interval)
        {
            _defaultPollingInterval = interval;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithMaximumEventHandlerConcurrencyPerSubscription(int concurrentOperations)
        {
            _maxConcurrency = concurrentOperations;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithSliceSizeOf(int sliceSize)
        {
            _sliceSize = sliceSize;
            return this;
        }

        public static implicit operator EventStreamSubscriberSettings(EventStreamSubscriberSettingsBuilder builder)
        {
            return new EventStreamSubscriberSettings(builder._connection, builder._eventHandlerResolver, builder._streamPositionRepository, builder._defaultPollingInterval, builder._maxConcurrency, builder._sliceSize);
        }
    }
}