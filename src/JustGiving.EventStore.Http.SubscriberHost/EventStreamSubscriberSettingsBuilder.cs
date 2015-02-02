using System;
using System.Collections.Generic;
using System.Linq;
using JustGiving.EventStore.Http.Client;
using JustGiving.EventStore.Http.SubscriberHost.Monitoring;
using log4net;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class EventStreamSubscriberSettingsBuilder
    {
        private IEventStoreHttpConnection _connection;
        private IEventHandlerResolver _eventHandlerResolver;
        private IStreamPositionRepository _streamPositionRepository;
        private ISubscriptionTimerManager _subscriptionTimerManager;
        private IEventTypeResolver _eventTypeResolver = new EventTypeResolver();
        private ILog _log;
        private TimeSpan _messageProcessingStatsWindowPeriod = TimeSpan.FromSeconds(30);
        private int _messageProcessingStatsWindowCount = 120;
        private TimeSpan? _longPollingTimeout;
        private List<IEventStreamSubscriberPerformanceMonitor> _performanceMonitors = new List<IEventStreamSubscriberPerformanceMonitor>();
        private IStreamSubscriberIntervalMonitor _subscriberIntervalMonitor;

        private TimeSpan _defaultPollingInterval = TimeSpan.FromSeconds(30);
        private int _sliceSize = 100;
        
        private int _eventNotFoundRetryCount = 3;
        private TimeSpan _eventNotFoundRetryDelay = TimeSpan.FromMilliseconds(100);

        public EventStreamSubscriberSettingsBuilder(IEventStoreHttpConnection connection, IEventHandlerResolver eventHandlerResolver, IStreamPositionRepository streamPositionRepository)
        {
            _connection = connection;
            _eventHandlerResolver = eventHandlerResolver;
            _streamPositionRepository = streamPositionRepository;
            _subscriptionTimerManager = new SubscriptionTimerManager();
            _subscriberIntervalMonitor = new StreamSubscriberIntervalMonitor();
        }

        public EventStreamSubscriberSettingsBuilder WithDefaultPollingInterval(TimeSpan interval)
        {
            _defaultPollingInterval = interval;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithSliceSizeOf(int sliceSize)
        {
            _sliceSize = sliceSize;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithLogger(ILog log)
        {
            _log = log;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithCustomEventTypeResolver(IEventTypeResolver eventTypeResolver)
        {
            _eventTypeResolver = eventTypeResolver;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithCustomSubscriptionTimerManager(ISubscriptionTimerManager subscriptionTimerManager)
        {
            _subscriptionTimerManager = subscriptionTimerManager;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithMessageProcessingStatsWindowPeriodOf(TimeSpan period)
        {
            _messageProcessingStatsWindowPeriod = period;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithMessageProcessingStatsWindowCountOf(int count)
        {
            _messageProcessingStatsWindowCount = count;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithLongPollingTimeoutOf(TimeSpan? longPollingTimeout)
        {
            _longPollingTimeout = longPollingTimeout;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder AddPerformanceMonitor(params IEventStreamSubscriberPerformanceMonitor[] subscriberPerformanceMonitors)
        {
            _performanceMonitors.AddRange(subscriberPerformanceMonitors.Where(x => x != null));
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithCustomEventStreamSubscriberIntervalMonitor(IStreamSubscriberIntervalMonitor monitor)
        {
            _subscriberIntervalMonitor = monitor;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithEventNotFoundRetryCountOf(int eventNotFoundRetryCount)
        {
            _eventNotFoundRetryCount = eventNotFoundRetryCount;
            return this;
        }

        public EventStreamSubscriberSettingsBuilder WithEventNotFoundRetryDelayOf(TimeSpan eventNotFoundRetryDelay)
        {
            _eventNotFoundRetryDelay = eventNotFoundRetryDelay;
            return this;
        }

        public static implicit operator EventStreamSubscriberSettings(EventStreamSubscriberSettingsBuilder builder)
        {
            return new EventStreamSubscriberSettings(builder._connection, builder._eventHandlerResolver, builder._streamPositionRepository, builder._subscriptionTimerManager, builder._eventTypeResolver, builder._defaultPollingInterval, builder._sliceSize, builder._log, builder._messageProcessingStatsWindowPeriod, builder._messageProcessingStatsWindowCount, builder._longPollingTimeout, builder._performanceMonitors, builder._subscriberIntervalMonitor, builder._eventNotFoundRetryCount, builder._eventNotFoundRetryDelay);
        }
    }
}