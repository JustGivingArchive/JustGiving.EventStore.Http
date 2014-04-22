using System;
using FluentAssertions;
using JustGiving.EventStore.Http.Client;
using JustGiving.EventStore.Http.SubscriberHost;
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
        private Mock<ISubscriptionTimerManager> _subscriptionTimerManagerMock;
        private EventStreamSubscriberSettingsBuilder _builder;

        [SetUp]
        public void Setup()
        {
            _connectionMock = new Mock<IEventStoreHttpConnection>();
            _eventHandlerResolverMock = new Mock<IEventHandlerResolver>();
            _streamPositionRepositoryMock = new Mock<IStreamPositionRepository>();
            _subscriptionTimerManagerMock = new Mock<ISubscriptionTimerManager>();

            _builder = new EventStreamSubscriberSettingsBuilder(_connectionMock.Object, _eventHandlerResolverMock.Object, _streamPositionRepositoryMock.Object, _subscriptionTimerManagerMock.Object);
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
        public void WithMaximumEventHandlerConcurrencyPerSubscription_ShouldStoreRequiredConcurrencyLimit()
        {
            var expectedConcurrencyLimit = 123;
            _builder.WithMaximumEventHandlerConcurrencyPerSubscription(expectedConcurrencyLimit);

            ((EventStreamSubscriberSettings)_builder).MaxConcurrency.Should().Be(expectedConcurrencyLimit);
        }

        [Test]
        public void WhenWithMaximumEventHandlerConcurrencyPerSubscriptionNotSet_ShouldDefaultToNull()
        {
            ((EventStreamSubscriberSettings)_builder).MaxConcurrency.Should().NotHaveValue();
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
    }
}