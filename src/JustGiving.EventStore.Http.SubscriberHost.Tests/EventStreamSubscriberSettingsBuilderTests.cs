using System;
using FluentAssertions;
using JustGiving.EventStore.Http.Client;
using JustGiving.EventStore.Http.SubscriberHost;
using log4net;
using Moq;
using NUnit.Framework;

namespace JG.EventStore.Http.SubscriberHost.Tests
{
    [TestFixture]
    public class EventStreamSubscriberSettingsBuilderTests
    {
        private Mock<IEventStoreHttpConnection> _connectionMock;
        private Mock<IEventHandlerResolver> _eventHandlerResolverMock;
        private Mock<IStreamPositionRepository> _streamPositionRepositoryMock;
        private Mock<IEventTypeResolver> _eventTypeResolverMock;
        private EventStreamSubscriberSettingsBuilder _builder;

        [SetUp]
        public void Setup()
        {
            _connectionMock = new Mock<IEventStoreHttpConnection>();
            _eventHandlerResolverMock = new Mock<IEventHandlerResolver>();
            _streamPositionRepositoryMock = new Mock<IStreamPositionRepository>();
            _eventTypeResolverMock = new Mock<IEventTypeResolver>();

            _builder = new EventStreamSubscriberSettingsBuilder(_connectionMock.Object, _eventHandlerResolverMock.Object, _streamPositionRepositoryMock.Object).WithCustomEventTypeResolver(_eventTypeResolverMock.Object);
        }

        [Test]
        public void WithDefaultPollingInterval_ShouldStoreSpecifiedPollingInterval()
        {
            var expected = TimeSpan.FromDays(123);
            _builder.WithDefaultPollingInterval(expected);

            ((EventStreamSubscriberSettings)_builder).DefaultPollingInterval.Should().Be(expected);
        }

        [Test]
        public void WhenDefaultPollingIntervalNotSet_ShouldStore30SecondsPollingInterval()
        {
            ((EventStreamSubscriberSettings)_builder).DefaultPollingInterval.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Test]
        public void WithSliceSizeOf_ShouldStoreRequiredSliceSize()
        {
            var expectedSliceSize = 123;
            _builder.WithSliceSizeOf(expectedSliceSize);

            ((EventStreamSubscriberSettings)_builder).SliceSize.Should().Be(expectedSliceSize);
        }

        [Test]
        public void WhenSliceSizeNotSet_ShouldDefaultTo100()
        {
            ((EventStreamSubscriberSettings)_builder).SliceSize.Should().Be(100);
        }

        [Test]
        public void WithLogger_ShouldStoreRequiredLogger()
        {
            var expected = Mock.Of<ILog>();
            _builder.WithLogger(expected);

            ((EventStreamSubscriberSettings)_builder).Log.Should().Be(expected);
        }

        [Test]
        public void WhenLoggerNotSet_ShouldDefaultToNull()
        {
            ((EventStreamSubscriberSettings)_builder).Log.Should().BeNull();
        }

        [Test]
        public void WithCustomEventTypeResolver_ShouldStoreRequiredEventTypeResolver()
        {
            var expected = Mock.Of<IEventTypeResolver>();
            _builder.WithCustomEventTypeResolver(expected);

            ((EventStreamSubscriberSettings)_builder).EventTypeResolver.Should().Be(expected);
        }

        [Test]
        public void WhenEventTypeResolverNotSet_ShouldDefaultToTheStandardEventTypeResolver()
        {
            ((EventStreamSubscriberSettings)_builder).EventTypeResolver.Should().NotBeNull();
        }

        [Test]
        public void WithCustomSubscriptionTimerManager_ShouldStoreRequiredEventTypeResolver()
        {
            var expected = Mock.Of<ISubscriptionTimerManager>();
            _builder.WithCustomSubscriptionTimerManager(expected);

            ((EventStreamSubscriberSettings)_builder).SubscriptionTimerManager.Should().Be(expected);
        }

        [Test]
        public void WhenSubscriptionTimerManagerNotSet_ShouldDefaultToTheStandardSubscriptionTimerManager()
        {
            ((EventStreamSubscriberSettings)_builder).SubscriptionTimerManager.Should().NotBeNull();
        }

        [Test]
        public void WithMessageProcessingStatsWindowPeriodOf_ShouldStoreRequiredStatsPeriod()
        {
            var expected = TimeSpan.FromSeconds(1);
            _builder.WithMessageProcessingStatsWindowPeriodOf(expected);

            ((EventStreamSubscriberSettings)_builder).MessageProcessingStatsWindowPeriod.Should().Be(expected);
        }

        [Test]
        public void WhenMessageProcessingStatsWindowPeriodNotSet_ShouldDefaultToTheStandardMessageProcessingStatsWindowPeriod()
        {
            ((EventStreamSubscriberSettings)_builder).MessageProcessingStatsWindowPeriod.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Test]
        public void WithMessageProcessingStatsWindowCountOf_ShouldStoreRequiredStatsWindowCount()
        {
            var expected = 123;
            _builder.WithMessageProcessingStatsWindowCountOf(expected);

            ((EventStreamSubscriberSettings)_builder).MessageProcessingStatsWindowCount.Should().Be(expected);
        }

        [Test]
        public void WhenMessageProcessingStatsWindowCountNotSet_ShouldDefaultToTheStandardMessageProcessingStatsWindowPeriod()
        {
            ((EventStreamSubscriberSettings)_builder).MessageProcessingStatsWindowPeriod.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Test]
        public void WithLongPollingTimeoutOf_ShouldStoreRequiredLongPOllingTimeout()
        {
            var expected = TimeSpan.FromSeconds(123);
            _builder.WithLongPollingTimeoutOf(expected);
            ((EventStreamSubscriberSettings)_builder).LongPollingTimeout.Should().Be(expected);
        }

        [Test]
        public void WhenLongPollingTimeoutNotSet_ShouldDefaultToNull()
        {
            ((EventStreamSubscriberSettings)_builder).LongPollingTimeout.Should().NotHaveValue();
        }
        
        [Test]
        public void WhenNoPerformanceMonitorsAreAdded_PerformanceMonitorsShouldBeEmpty()
        {
            ((EventStreamSubscriberSettings)_builder).PerformanceMonitors.Should().BeEmpty();
        }

        [Test]
        public void WhenANullPerformanceMonitorIsAdded_PerformanceMonitorsShouldBeEmpty()
        {
            _builder.AddPerformanceMonitor(null, null);
            ((EventStreamSubscriberSettings)_builder).PerformanceMonitors.Should().BeEmpty();
        }

        [Test]
        public void WhenANullPerformanceMonitorsAreAdded_PerformanceMonitorsShouldContainSaidPerformanceMonitors()
        {
            var firstExpected = Mock.Of<IEventStreamSubscriberPerformanceMonitor>();
            var secondExpected = Mock.Of<IEventStreamSubscriberPerformanceMonitor>();
            var thirdExpected = Mock.Of<IEventStreamSubscriberPerformanceMonitor>();

            _builder.AddPerformanceMonitor(firstExpected, secondExpected);
            _builder.AddPerformanceMonitor(thirdExpected);

            ((EventStreamSubscriberSettings)_builder).PerformanceMonitors.Should().Contain(new[]{firstExpected, secondExpected, thirdExpected});
        }
    }
}