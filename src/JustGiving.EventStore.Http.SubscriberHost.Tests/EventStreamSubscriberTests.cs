using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using JustGiving.EventStore.Http.Client;
using JustGiving.EventStore.Http.Client.Exceptions;
using JustGiving.EventStore.Http.SubscriberHost;
using JustGiving.EventStore.Http.SubscriberHost.Monitoring;
using Moq;
using NUnit.Framework;

namespace JG.EventStore.Http.SubscriberHost.Tests
{
    [TestFixture]
    public class EventStreamSubscriberTests
    {
        Mock<IEventStoreHttpConnection> _eventStoreHttpConnectionMock;
        Mock<IEventHandlerResolver> _eventHandlerResolverMock;
        Mock<IStreamPositionRepository> _streamPositionRepositoryMock;
        Mock<ISubscriptionTimerManager> _subscriptionTimerManagerMock;
        Mock<IEventTypeResolver> _eventTypeResolverMock;
        Mock<IStreamSubscriberIntervalMonitor> _streamSubscriberIntervalMonitorMock;

        private EventStreamSubscriber _subscriber;

        private const string StreamName = "abc";
        private const string SubscriberId = "def";
        private static readonly DateTime EventDate = new DateTime(1, 2, 3);
        private static readonly BasicEventInfo EventInfo = new BasicEventInfo { Title = "1@2" , Updated = EventDate};

        private const int EventNotFoundRetryCount = 10;

        [SetUp]
        public void Setup()
        {
            _eventStoreHttpConnectionMock = new Mock<IEventStoreHttpConnection>();
            _eventHandlerResolverMock = new Mock<IEventHandlerResolver>();
            _streamPositionRepositoryMock = new Mock<IStreamPositionRepository>();
            _subscriptionTimerManagerMock = new Mock<ISubscriptionTimerManager>();
            _eventTypeResolverMock = new Mock<IEventTypeResolver>();
            _streamSubscriberIntervalMonitorMock = new Mock<IStreamSubscriberIntervalMonitor>();

            var builder =
                new EventStreamSubscriberSettingsBuilder(_eventStoreHttpConnectionMock.Object,
                    _eventHandlerResolverMock.Object, _streamPositionRepositoryMock.Object)
                    .WithCustomEventTypeResolver(_eventTypeResolverMock.Object)
                    .WithCustomSubscriptionTimerManager(_subscriptionTimerManagerMock.Object)
                    .WithCustomEventStreamSubscriberIntervalMonitor(_streamSubscriberIntervalMonitorMock.Object)
                    .WithEventNotFoundRetryCountOf(EventNotFoundRetryCount);

            _subscriber = (EventStreamSubscriber)EventStreamSubscriber.Create(builder);
        }

        [Test]
        public void SubscribeTo_ShouldInvokeSubscriptionManagerWithCorrectStreamName()
        {
            _subscriber.SubscribeTo(StreamName, SubscriberId, It.IsAny<TimeSpan>());
            _subscriptionTimerManagerMock.Verify(x => x.Add(StreamName, SubscriberId, It.IsAny<TimeSpan>(), It.IsAny<Func<Task>>(), It.IsAny<Action>()));
        }

        [Test]
        public void SubscribeTo_ShouldInvokeSubscriptionManagerWithSuppliedPeriodIfPassed()
        {
            _subscriber.SubscribeTo(It.IsAny<string>(), It.IsAny<string>(), TimeSpan.FromDays(123));
            _subscriptionTimerManagerMock.Verify(x => x.Add(It.IsAny<string>(), It.IsAny<string>(), TimeSpan.FromDays(123), It.IsAny<Func<Task>>(), It.IsAny<Action>()));
        }

        [Test]
        public void SubscribeTo_ShouldInvokeSubscriptionManagerWithDefaultPeriodIfNoneIsPassed()
        {
            var builder = new EventStreamSubscriberSettingsBuilder(_eventStoreHttpConnectionMock.Object, _eventHandlerResolverMock.Object, _streamPositionRepositoryMock.Object).WithDefaultPollingInterval(TimeSpan.FromDays(456)).WithCustomEventTypeResolver(_eventTypeResolverMock.Object).WithCustomSubscriptionTimerManager(_subscriptionTimerManagerMock.Object);
            _subscriber = (EventStreamSubscriber)EventStreamSubscriber.Create(builder);
            _subscriber.SubscribeTo(It.IsAny<string>(), It.IsAny<string>());
            _subscriptionTimerManagerMock.Verify(x => x.Add(It.IsAny<string>(), It.IsAny<string>(), TimeSpan.FromDays(456), It.IsAny<Func<Task>>(), It.IsAny<Action>()));
        }

        [Test]
        public void UnsubscribeFrom_ShouldInvokeSubscriptionManager()
        {
            _subscriber.UnsubscribeFrom(StreamName, SubscriberId);
            _subscriptionTimerManagerMock.Verify(x => x.Remove(StreamName, SubscriberId));
        }

        [Test]
        public void UnsubscribeFrom_ShouldRemoveStreamSubscriberIntervalMonitor()
        {
            _subscriber.UnsubscribeFrom(StreamName, SubscriberId);

            _streamSubscriberIntervalMonitorMock.Verify(x => x.RemoveEventStreamMonitor(StreamName, SubscriberId));
        }

        [TestCase(typeof(SomeImplicitHandler))]
        [TestCase(typeof(SomeExplicitHandler))]
        public void GetMethodFromHandler_ShouldBeAbleToFindImplementedInterfaceMethods(Type handlerType)
        {
            var method = _subscriber.GetMethodFromHandler(handlerType, typeof(EventANoBaseOrInterface), "Handle");
            Assert.NotNull(method);//fluent assertions does not support .NotBeNull() on MethodInfo objects
            method.Name.Should().Be("Handle");
        }

        [TestCase(typeof(SomeImplicitHandlerForParentType))]
        [TestCase(typeof(SomeExplicitHandlerForParentType))]
        public void GetMethodFromHandler_ShouldBeAbleToFindImplementedInterfaceMethodsWhenParentTypeHandled(Type handlerType)
        {
            var method = _subscriber.GetMethodFromHandler(handlerType, typeof(EventANoBaseOrInterface), "Handle");
            Assert.NotNull(method);//fluent assertions does not support .NotBeNull() on MethodInfo objects
            method.Name.Should().Be("Handle");
        }

        [TestCase(typeof(SomeImplicitHandlerForInterface))]
        [TestCase(typeof(SomeExplicitHandlerForInterface))]
        public void GetMethodFromHandler_ShouldBeAbleToFindImplementedInterfaceMethodsWhenInterfaceTypeHandled(Type handlerType)
        {
            var method = _subscriber.GetMethodFromHandler(handlerType, typeof(EventAWithInterface), "Handle");
            Assert.NotNull(method);//fluent assertions does not support .NotBeNull() on MethodInfo objects
            method.Name.Should().Be("Handle");
        }

        [Test]
        public void GetMethodFromHandler_ShouldBeAbleToFindImplementedInterfaceMethodsForMetadataHandler()
        {
            var method = _subscriber.GetMethodFromHandler(typeof(SomeImplicitMetadataHandler), typeof(EventANoBaseOrInterface), "Handle");
            Assert.NotNull(method);//fluent assertions does not support .NotBeNull() on MethodInfo objects
            method.Name.Should().Be("Handle");
        }

        [Test]
        public async Task PollAsync_ShouldPauseTheEventTimerDuringPolling()//to prevent dual polling
        {
            var result = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo>()
            };

            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => result);
            await _subscriber.PollAsync(StreamName, SubscriberId);

            _subscriptionTimerManagerMock.Verify(x => x.Pause(StreamName, SubscriberId));
        }

        [Test]
        public async Task PollAsync_IfNoEvents_ShouldImmediatelyResumeTheEventTimerDuringPolling()//to keep the polling going at the preferred rate
        {
            var result = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo>()
            };

            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => result);
            await _subscriber.PollAsync(StreamName, SubscriberId);

            _subscriptionTimerManagerMock.Verify(x => x.Pause(StreamName, SubscriberId));
        }

        [Test]
        public async Task PollAsync_IfEventsFound_ShouldImmediatelyRepoll()//until we have no more events, to speedily exhaust the queue
        {
            var count = 0;
            var streamSliceResult = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo> { new BasicEventInfo { Title = "1@Stream" } }//Reflection ahoy
            };


            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(string));
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => streamSliceResult).Callback(
                () =>
                {
                    if (count++ == 3000)
                    {
                        streamSliceResult.Entries.Clear();
                    }
                });
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(StreamName, It.IsAny<int>())).Returns(async () => new EventReadResult(EventReadStatus.Success, new EventInfo { EventType = typeof(EventANoBaseOrInterface).FullName }));
            await _subscriber.PollAsync(StreamName, SubscriberId);

            _subscriptionTimerManagerMock.Verify(x => x.Pause(StreamName, SubscriberId), Times.Once);
            _subscriptionTimerManagerMock.Verify(x => x.Resume(StreamName, SubscriberId), Times.Once);
        }

        [Test]
        public async Task PollAsync_IfEventBodyNotFound_ShouldKeepRetryingUntilItsFound()
        {
            var count = 0;
            var streamSliceResult = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo> { new BasicEventInfo { Title = "1@Stream", Links = new List<Link> { new Link { Relation = "edit" } }} }
            };


            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(string));
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => streamSliceResult).Callback(
                () =>
                {
                    if (count++ == 3000)
                    {
                        streamSliceResult.Entries.Clear();
                    }
                });

            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(StreamName, It.IsAny<int>())).Returns(async () => new EventReadResult(EventReadStatus.Success, new EventInfo { EventType = typeof(EventANoBaseOrInterface).FullName }));

            _eventHandlerResolverMock.Setup(x => x.GetHandlersOf(It.IsAny<Type>())).Returns(new List<object> { new SomeImplicitHandler() });
            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(EventANoBaseOrInterface));

            _eventStoreHttpConnectionMock.SetupSequence(x => x.ReadEventBodyAsync(It.IsAny<Type>(), It.IsAny<string>()))
                .Throws(new EventNotFoundException(""))
                .Throws(new EventNotFoundException(""))
                .Throws(new EventNotFoundException(""))
                .Returns(Task.FromResult((object)new EventANoBaseOrInterface()));

            await _subscriber.PollAsync(StreamName, null);

            _eventStoreHttpConnectionMock.Verify(x => x.ReadEventBodyAsync(It.IsAny<Type>(), It.IsAny<string>()), Times.Exactly(4));
        }

        [Test]
        public async Task PollAsync_IfEventBodyNotFound_ShouldStopRetryingWhenItReachesMaxRetryCount()
        {
            var count = 0;
            var streamSliceResult = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo> { new BasicEventInfo { Title = "1@Stream", Links = new List<Link> { new Link { Relation = "edit" } } } }
            };


            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(string));
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => streamSliceResult).Callback(
                () =>
                {
                    if (count++ == 3000)
                    {
                        streamSliceResult.Entries.Clear();
                    }
                });

            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(StreamName, It.IsAny<int>())).Returns(async () => new EventReadResult(EventReadStatus.Success, new EventInfo { EventType = typeof(EventANoBaseOrInterface).FullName }));

            _eventHandlerResolverMock.Setup(x => x.GetHandlersOf(It.IsAny<Type>())).Returns(new List<object> { new SomeImplicitHandler() });
            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(EventANoBaseOrInterface));

            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventBodyAsync(It.IsAny<Type>(), It.IsAny<string>()))
                .Throws(new EventNotFoundException(""));

            await _subscriber.PollAsync(StreamName, null);

            _eventStoreHttpConnectionMock.Verify(x => x.ReadEventBodyAsync(It.IsAny<Type>(), It.IsAny<string>()), Times.Exactly(EventNotFoundRetryCount));
        }

        [Test]
        public async Task PollAsync_IfUnsubscribed_ShouldNotRepoll()
        {
            var count = 0;
            var streamSliceResult = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo> { new BasicEventInfo { Title = "1@Stream" } }
            };

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(string));
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => streamSliceResult).Callback(
                () =>
                {
                    if (count++ == 2)
                    {
                        streamSliceResult.Entries.Clear();
                    }
                });
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(StreamName, It.IsAny<int>())).Returns(async () => new EventReadResult(EventReadStatus.Success, new EventInfo { EventType = typeof(EventANoBaseOrInterface).FullName }));

            await _subscriber.PollAsync(StreamName, SubscriberId);

            _eventTypeResolverMock.Verify(r => r.Resolve(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task PollAsync_IfUnsubscribed_ShouldStopProcessingBatch()
        {
            var streamSliceResult = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo> {
                    new BasicEventInfo { Title = "1@Stream" },
                    new BasicEventInfo { Title = "2@Stream" },
                    new BasicEventInfo { Title = "3@Stream" }
                }
            };

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(string));
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => streamSliceResult);
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(StreamName, It.IsAny<int>())).Returns(async () => new EventReadResult(EventReadStatus.Success, new EventInfo { EventType = typeof(EventANoBaseOrInterface).FullName }));

            await _subscriber.PollAsync(StreamName, SubscriberId);

            _eventTypeResolverMock.Verify(r => r.Resolve(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task PollAsync_IfUnsuccessful_ShouldNotTryToProcessResults()
        {
            var result = new StreamEventsSlice
            {
                Status = StreamReadStatus.StreamNotFound,
                Entries = null
            };

            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => result);
            await _subscriber.PollAsync(StreamName, SubscriberId);
        }

        [Test]
        public async Task PollAsync_ShouldStoreStreamPositionAfterHandlersInvoked_EvenIfNoneWereFound()
        {
            var streamSliceResult = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo> { new BasicEventInfo { PositionEventNumber = 123 } }
            };
            int count = 0;
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => await Task.FromResult(streamSliceResult)).Callback(
                () =>
                {
                    if (count++ == 1)
                    {
                        streamSliceResult.Entries.Clear();
                    }
                });

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(string));

            await _subscriber.PollAsync(StreamName, SubscriberId);

            _streamPositionRepositoryMock.Verify(x => x.SetPositionForAsync(StreamName, SubscriberId, 123));
        }

        [Test]
        public void GetEventHandlersFor_ShouldRequestCorrectHandlers()
        {
            _eventTypeResolverMock.Setup(x => x.Resolve(typeof(EventANoBaseOrInterface).FullName)).Returns(typeof(EventANoBaseOrInterface));

            _subscriber.GetEventHandlersFor(typeof(EventANoBaseOrInterface).FullName, null);

            _eventHandlerResolverMock.Verify(x => x.GetHandlersOf(typeof(IHandleEventsOf<EventANoBaseOrInterface>)));
        }

        [Test]
        public void GetEventHandlersFor_ShouldReturnAnEumptyEnumerableIfANullEventTypeIsPassed()
        {
            var result = _subscriber.GetEventHandlersFor(null, null);
            result.Should().NotBeNull();
        }

        [Test]
        public void GetHandlersFor_ShouldRequestCorrectMetadataHandlers()
        {
            _eventTypeResolverMock.Setup(x => x.Resolve(typeof(EventANoBaseOrInterface).FullName)).Returns(typeof(EventANoBaseOrInterface));

            _subscriber.GetEventHandlersFor(typeof(EventANoBaseOrInterface).FullName, null);

            _eventHandlerResolverMock.Verify(x => x.GetHandlersOf(typeof(IHandleEventsAndMetadataOf<EventANoBaseOrInterface>)));
        }

        [Test]
        public void GetHandlersApplicableToSubscriberId_ShouldOnlyYieldDefaultHandlersWhenRunningForDefaultSubscriber()
        {
            var input = new object[] {new SomeHandlerForTheDefaultSubscriberId(), new SomeHandlerForACustomSubscriberId()};

            var result = _subscriber.GetHandlersApplicableToSubscriberId(input, null).ToList();

            result.Count.Should().Be(1);
            result[0].Should().BeOfType<SomeHandlerForTheDefaultSubscriberId>();
        }

        [Test]
        public void GetHandlersApplicableToSubscriberId_ShouldOnlyYieldCustomHandlersWhenRunningForCustomSubscriber()
        {
            var input = new object[] { new SomeHandlerForTheDefaultSubscriberId(), new SomeHandlerForACustomSubscriberId() };

            var result = _subscriber.GetHandlersApplicableToSubscriberId(input, "SomeSubscriberId").ToList();

            result.Count.Should().Be(1);
            result[0].Should().BeOfType<SomeHandlerForACustomSubscriberId>();
        }

        [Test]
        public void GetHandlersApplicableToSubscriberId_ShouldYieldCorrectCustomHandlersWhenRunningForCustomSubscriber()
        {
            var input = new object[] { new SomeHandlerForACustomSubscriberId(), new SomeOtherHandlerForACustomSubscriberId() };

            var result = _subscriber.GetHandlersApplicableToSubscriberId(input, "SomeSubscriberId").ToList();

            result.Count.Should().Be(1);
            result[0].Should().BeOfType<SomeHandlerForACustomSubscriberId>();
        }

        [Test]
        public async Task InvokeMessageHandlersForEventMessageAsync_ShouldInvokeFoundHandlers()
        {
            var @implicit = new SomeImplicitHandler();
            var implicitForParentType = new SomeImplicitHandlerForParentType();
            var @explicit = new SomeExplicitHandler();
            var explicitForParentType = new SomeExplicitHandlerForParentType();
            var metadataHandler = new SomeImplicitMetadataHandler();

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(EventANoBaseOrInterface));

            var handlers = new object[] { metadataHandler, @implicit, implicitForParentType, @explicit, explicitForParentType };

            await _subscriber.InvokeMessageHandlersForEventMessageAsync(StreamName, typeof(EventANoBaseOrInterface), handlers, new EventANoBaseOrInterface(), EventInfo);

            @implicit.EventA.Should().NotBeNull();
            implicitForParentType.@event.Should().NotBeNull();
            @explicit.EventA.Should().NotBeNull();
            explicitForParentType.@event.Should().NotBeNull();
            metadataHandler.EventA.Should().NotBeNull();
            metadataHandler.Metadata.Should().NotBeNull();
        }

        [Test]
        public async Task InvokeMessageHandlersForEventMessageAsync_ShouldInvokeFoundHandlersForInterfaceType()
        {
            var @implicit = new SomeImplicitHandlerForInterface();
            var @explicit = new SomeExplicitHandlerForInterface();

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(EventAWithInterface));

            var handlers = new IHandleEventsOf<IEvent>[] { @implicit, @explicit };

            await _subscriber.InvokeMessageHandlersForEventMessageAsync(StreamName, typeof(EventAWithInterface), handlers, new EventAWithInterface(), EventInfo);

            @implicit.@event.Should().NotBeNull();
            @explicit.@event.Should().NotBeNull();
        }

        [Test]
        public async Task PollAsync_WhenNoEventHandlersFound_ShouldInvokeAllRegisteredPerformanceMonitorsWithAppropriateInfo()
        {
            var performanceMonitor = new Mock<IEventStreamSubscriberPerformanceMonitor>();

            var streamSliceResult = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo> { new BasicEventInfo { Title = "1@Stream", EventType = "SomeType", Updated = EventDate } }
            };

            var count = 0;
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => streamSliceResult).Callback(
                () =>
                {
                    if (count++ == 2)
                    {
                        streamSliceResult.Entries.Clear();
                    }
                });

            var builder = new EventStreamSubscriberSettingsBuilder(_eventStoreHttpConnectionMock.Object, _eventHandlerResolverMock.Object, _streamPositionRepositoryMock.Object).WithCustomEventTypeResolver(_eventTypeResolverMock.Object).AddPerformanceMonitor(performanceMonitor.Object);
            _subscriber = (EventStreamSubscriber)EventStreamSubscriber.Create(builder);

            await _subscriber.PollAsync(StreamName, SubscriberId);

            performanceMonitor.Verify(x => x.Accept(StreamName, "SomeType", EventDate, 0, It.IsAny<IEnumerable<KeyValuePair<Type, Exception>>>()));
        }

        [Test]
        public async Task InvokeMessageHandlersForEventMessageAsync_ShouldInvokeAllRegisteredPerformanceMonitorsWithAppropriateInfo()
        {
            var performanceMonitor1 = new Mock<IEventStreamSubscriberPerformanceMonitor>();
            var performanceMonitor2 = new Mock<IEventStreamSubscriberPerformanceMonitor>();

            var handlers = new[] { Mock.Of<IHandleEventsOf<object>>(), Mock.Of<IHandleEventsOf<object>>() };

            var builder = new EventStreamSubscriberSettingsBuilder(_eventStoreHttpConnectionMock.Object, _eventHandlerResolverMock.Object, _streamPositionRepositoryMock.Object).WithCustomEventTypeResolver(_eventTypeResolverMock.Object).AddPerformanceMonitor(performanceMonitor1.Object, performanceMonitor2.Object);
            _subscriber = (EventStreamSubscriber)EventStreamSubscriber.Create(builder);

            await _subscriber.InvokeMessageHandlersForEventMessageAsync(StreamName, typeof(object), handlers, new object(), EventInfo);

            performanceMonitor1.Verify(x => x.Accept(StreamName, typeof(object).FullName, EventDate, 2, It.IsAny<IEnumerable<KeyValuePair<Type, Exception>>>()));
            performanceMonitor2.Verify(x => x.Accept(StreamName, typeof(object).FullName, EventDate, 2, It.IsAny<IEnumerable<KeyValuePair<Type, Exception>>>()));
        }

        [Test]
        public async Task InvokeMessageHandlersForEventMessageAsync_ShouldInvokeAllRegisteredPerformanceMonitorsWithAppropriateErrorInfoWhenAHandlerBlows()
        {
            var performanceMonitor = new Mock<IEventStreamSubscriberPerformanceMonitor>();


            var expectedException = new InvalidTimeZoneException("Summat");
            var handler = new Mock<IHandleEventsOf<object>>();
            handler.Setup(x => x.Handle(It.IsAny<object>())).Throws(expectedException);

            var called = false;
            performanceMonitor.Setup(x => x.Accept(StreamName, typeof(object).FullName, EventDate, 1, It.IsAny<IEnumerable<KeyValuePair<Type, Exception>>>())).Callback<string, string, DateTime, int, IEnumerable<KeyValuePair<Type, Exception>>>(
                (stream, type, createdDate, handlerCount, exceptions) =>
                {
                    called = true;
                    exceptions.Should().BeEquivalentTo(new[] { new KeyValuePair<Type, Exception>(handler.Object.GetType(), expectedException) });
                });

            var builder = new EventStreamSubscriberSettingsBuilder(_eventStoreHttpConnectionMock.Object, _eventHandlerResolverMock.Object, _streamPositionRepositoryMock.Object).WithCustomEventTypeResolver(_eventTypeResolverMock.Object).AddPerformanceMonitor(performanceMonitor.Object);
            _subscriber = (EventStreamSubscriber)EventStreamSubscriber.Create(builder);

            await _subscriber.InvokeMessageHandlersForEventMessageAsync(StreamName, typeof(object), new[] { handler.Object }, new object(), EventInfo);

            called.Should().BeTrue();
        }

        [TestCase(typeof(EventANoBaseOrInterface), "EventNoBaseOrInterface")]
        [TestCase(typeof(EventAWithInterface), "EventAWithInterface")]
        [TestCase(typeof(EventAWithBase), "EventAWithBase")]
        [TestCase(typeof(EventAWithBaseAndInterface), "EventAWithBaseAndInterface")]
        [TestCase(typeof(EventBNoBaseOrInterface), "object")]
        [TestCase(typeof(EventBWithInterface), "IEvent")]
        [TestCase(typeof(EventBWithBase), "EventBase")]
        [TestCase(typeof(EventBWithBaseAndInterface), "IEvent")]
        [TestCase(typeof(EventCWithInterface), "object")]
        [TestCase(typeof(EventCWithBase), "object")]
        [TestCase(typeof(EventCWithBaseAndInterface), "object")]
        [TestCase(typeof(EventDWithBaseWhichHasInterface), "object")]
        [TestCase(typeof(EventEWithBaseWhichHasInterface), "EventEWithBaseWhichHasInterface")]
        public async Task InvokeMessageHandlersForEventMessageAsync_ShouldInvokeCorrectHandlerOverloadForType(Type eventType, string expectedMethod)
        {
            var @implicit = new MultiTypeImplicitHandler();
            var @explicit = new MultiTypeExplicitHandler();

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(eventType);

            var handlers = new IHandleEventsOf<object>[] { @implicit, @explicit };

            await _subscriber.InvokeMessageHandlersForEventMessageAsync(StreamName, eventType, handlers, Activator.CreateInstance(eventType), EventInfo);

            @implicit.Method.Should().Be(expectedMethod, "The expected method overload was not called on implicit handler");
            @explicit.Method.Should().Be(expectedMethod, "The expected method overload was not called on explicit handler");
        }

        [Test]
        public async Task AdHocInvokeAsync_ShouldAttemptToRetrieveCorrectEvent()
        {
            var result = new EventReadResult(EventReadStatus.NotFound, null);
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(It.IsAny<string>(), It.IsAny<int>())).Returns(async () => result);
            await _subscriber.AdHocInvokeAsync(StreamName, 123);

            _eventStoreHttpConnectionMock.Verify(x=>x.ReadEventAsync(It.Is<string>(s=>s==StreamName), It.Is<int>(i=>i==123)));
        }

        [Test]
        public async Task AdHocInvokeAsync_IfEventCannotBeRead_ReturnAppropriateResult()
        {
            var result = new EventReadResult(EventReadStatus.NotFound, null);
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(It.IsAny<string>(), It.IsAny<int>())).Returns(async () => result);
            var invocationResult = await _subscriber.AdHocInvokeAsync(StreamName, 123);

            invocationResult.ResultCode.Should().Be(AdHocInvocationResult.AdHocInvocationResultCode.CouldNotFindEvent);
        }

        [Test]
        public async Task AdHocInvokeAsync_IfEventHasNoHandlers_ReturnAppropriateResult()
        {
            var result = new EventReadResult(EventReadStatus.Success, new EventInfo
            {
                EventType = typeof(SomeHandlerForACustomSubscriberId).FullName
            });
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(It.IsAny<string>(), It.IsAny<int>())).Returns(async () => result);
            var invocationResult = await _subscriber.AdHocInvokeAsync(StreamName, 123);

            invocationResult.ResultCode.Should().Be(AdHocInvocationResult.AdHocInvocationResultCode.NoHandlersFound);
        }

        [Test]
        public async Task AdHocInvokeAsync_IfEventHasHandlers_ReturnAppropriateResult()
        {
            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof (EventANoBaseOrInterface));
            _eventHandlerResolverMock.Setup(x => x.GetHandlersOf(It.IsAny<Type>())).Returns(new [] { new SomeImplicitHandler() });

            var result = new EventReadResult(EventReadStatus.Success, new EventInfo
            {
                EventType = typeof(EventANoBaseOrInterface).FullName
            });
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(It.IsAny<string>(), It.IsAny<int>())).Returns(async () => result);
            var invocationResult = await _subscriber.AdHocInvokeAsync(StreamName, 123);

            invocationResult.ResultCode.Should().Be(AdHocInvocationResult.AdHocInvocationResultCode.Success);
        }

        [Test]
        public async Task AdHocInvokeAsync_IfEventHasHandlers_AndHandlerThrowsAnException_Return_AppropriateResult()
        {
            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(EventANoBaseOrInterface));
            _eventHandlerResolverMock.Setup(x => x.GetHandlersOf(It.IsAny<Type>())).Returns(new[] { new HandlerThatThrowsAnException() });

            var result = new EventReadResult(EventReadStatus.Success, new EventInfo
            {
                EventType = typeof(EventANoBaseOrInterface).FullName
            });
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(It.IsAny<string>(), It.IsAny<int>())).Returns(async () => result);
            var invocationResult = await _subscriber.AdHocInvokeAsync(StreamName, 123);

            invocationResult.ResultCode.Should().Be(AdHocInvocationResult.AdHocInvocationResultCode.HandlerThrewException);
        }

        [Test]
        public async Task AdHocInvokeAsync_IfEventHasHandlers_AndHandlerThrowsAnException_ExceptionShouldBeRecorded()
        {
            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(EventANoBaseOrInterface));
            _eventHandlerResolverMock.Setup(x => x.GetHandlersOf(It.IsAny<Type>())).Returns(new[] { new HandlerThatThrowsAnException() });

            var result = new EventReadResult(EventReadStatus.Success, new EventInfo
            {
                EventType = typeof(EventANoBaseOrInterface).FullName
            });
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(It.IsAny<string>(), It.IsAny<int>())).Returns(async () => result);
            var invocationResult = await _subscriber.AdHocInvokeAsync(StreamName, 123);

            invocationResult.Errors.Should().NotBeNull();
            invocationResult.Errors.Should().NotBeEmpty();
            invocationResult.Errors[typeof(HandlerThatThrowsAnException)].Message.Should().Be(HandlerThatThrowsAnException.SomeException.Message);
        }

        //monkey

        public interface IEvent { }
        public abstract class EventBase { }

        public class EventANoBaseOrInterface { }
        public class EventAWithInterface : IEvent { }
        public class EventAWithBase : EventBase { }
        public class EventAWithBaseAndInterface : EventBase, IEvent { }

        public class EventBNoBaseOrInterface { }
        public class EventBWithInterface : IEvent { }
        public class EventBWithBase : EventBase { }
        public class EventBWithBaseAndInterface : EventBase, IEvent { }

        public interface IEvent2 { }
        public abstract class EventBase2 { }
        public class EventCNoBaseOrInterface { }
        public class EventCWithInterface : IEvent2 { }
        public class EventCWithBase : EventBase2 { }
        public class EventCWithBaseAndInterface : EventBase2, IEvent2 { }

        public interface IEvent3 { }
        public abstract class EventBase3 : IEvent3 { }
        public class EventDWithBaseWhichHasInterface : EventBase3 { }
        public class EventEWithBaseWhichHasInterface : EventBase3 { }

        public class SomeImplicitHandler : IHandleEventsOf<EventANoBaseOrInterface>
        {
            public EventANoBaseOrInterface EventA;
            public Task Handle(EventANoBaseOrInterface EventA) { return Task.Run(() => this.EventA = EventA); }
            public void OnError(Exception ex, EventANoBaseOrInterface @event) { }
        }

        public class SomeImplicitMetadataHandler : IHandleEventsAndMetadataOf<EventANoBaseOrInterface>
        {
            public EventANoBaseOrInterface EventA;
            public BasicEventInfo Metadata;
            public Task Handle(EventANoBaseOrInterface eventA, BasicEventInfo metadata) {
                return Task.Run(() =>
                {
                    EventA = eventA;
                    Metadata = metadata;
                });
            }
            public void OnError(Exception ex, EventANoBaseOrInterface @event) { }
        }

        public class SomeImplicitHandlerForParentType : IHandleEventsOf<object>
        {
            public object @event;
            public Task Handle(object EventA) { return Task.Run(() => this.@event = EventA); }
            public void OnError(Exception ex, object @event) { }
        }

        public class SomeImplicitHandlerForInterface : IHandleEventsOf<IEvent>
        {
            public IEvent @event;
            public Task Handle(IEvent EventA) { return Task.Run(() => this.@event = EventA); }
            public void OnError(Exception ex, IEvent @event) { }
        }

        public class SomeExplicitHandler : IHandleEventsOf<EventANoBaseOrInterface>
        {
            public EventANoBaseOrInterface EventA;
            Task IHandleEventsOf<EventANoBaseOrInterface>.Handle(EventANoBaseOrInterface EventA) { return Task.Run(() => this.EventA = EventA); }
            void IHandleEventsOf<EventANoBaseOrInterface>.OnError(Exception ex, EventANoBaseOrInterface @event) { }
        }

        public class SomeExplicitHandlerForParentType : IHandleEventsOf<object>
        {
            public object @event;
            Task IHandleEventsOf<object>.Handle(object EventA) { return Task.Run(() => this.@event = EventA); }
            void IHandleEventsOf<object>.OnError(Exception ex, object @event) { }
        }

        public class SomeExplicitHandlerForInterface : IHandleEventsOf<IEvent>
        {
            public IEvent @event;
            Task IHandleEventsOf<IEvent>.Handle(IEvent EventA) { return Task.Run(() => this.@event = EventA); }
            void IHandleEventsOf<IEvent>.OnError(Exception ex, IEvent @event) { }
        }

        public class SomeHandlerForTheDefaultSubscriberId : IHandleEventsOf<IEvent>
        {
            public IEvent @event;
            Task IHandleEventsOf<IEvent>.Handle(IEvent EventA) { return Task.Run(() => this.@event = EventA); }
            void IHandleEventsOf<IEvent>.OnError(Exception ex, IEvent @event) { }
        }

        [NonDefaultSubscriber("SomeSubscriberId")]
        public class SomeHandlerForACustomSubscriberId : IHandleEventsOf<IEvent>
        {
            public IEvent @event;
            Task IHandleEventsOf<IEvent>.Handle(IEvent EventA) { return Task.Run(() => @event = EventA); }
            void IHandleEventsOf<IEvent>.OnError(Exception ex, IEvent @event) { }
        }
        [NonDefaultSubscriber("SomeOtherSubscriberId")]
        public class SomeOtherHandlerForACustomSubscriberId : IHandleEventsOf<IEvent>
        {
            public IEvent @event;
            Task IHandleEventsOf<IEvent>.Handle(IEvent EventA) { return Task.Run(() => @event = EventA); }
            void IHandleEventsOf<IEvent>.OnError(Exception ex, IEvent @event) { }
        }

        public class MultiTypeImplicitHandler :
            IHandleEventsOf<object>,
            IHandleEventsOf<EventANoBaseOrInterface>,
            IHandleEventsOf<IEvent>,
            IHandleEventsOf<EventAWithInterface>,
            IHandleEventsOf<EventBase>,
            IHandleEventsOf<EventAWithBase>,
            IHandleEventsOf<EventAWithBaseAndInterface>,
            IHandleEventsOf<EventEWithBaseWhichHasInterface>
        {
            public string Method { get; set; }

            public Task Handle(object @event)
            {
                return Task.Run(() => Method = "object");
            }

            public Task Handle(EventANoBaseOrInterface @event)
            {
                return Task.Run(() => Method = "EventNoBaseOrInterface");
            }

            public Task Handle(IEvent @event)
            {
                return Task.Run(() => Method = "IEvent");
            }

            public Task Handle(EventAWithInterface @event)
            {
                return Task.Run(() => Method = "EventAWithInterface");
            }

            public Task Handle(EventBase @event)
            {
                return Task.Run(() => Method = "EventBase");
            }

            public Task Handle(EventAWithBase @event)
            {
                return Task.Run(() => Method = "EventAWithBase");
            }

            public Task Handle(EventAWithBaseAndInterface @event)
            {
                return Task.Run(() => Method = "EventAWithBaseAndInterface");
            }

            public Task Handle(EventEWithBaseWhichHasInterface @event)
            {
                return Task.Run(() => Method = "EventEWithBaseWhichHasInterface");
            }

            public void OnError(Exception ex, object @event) { }
            public void OnError(Exception ex, EventANoBaseOrInterface @event) { }
            public void OnError(Exception ex, IEvent @event) { }
            public void OnError(Exception ex, EventAWithInterface @event) { }
            public void OnError(Exception ex, EventBase @event) { }
            public void OnError(Exception ex, EventAWithBase @event) { }
            public void OnError(Exception ex, EventAWithBaseAndInterface @event) { }
            public void OnError(Exception ex, EventEWithBaseWhichHasInterface @event) { }
        }

        public class MultiTypeExplicitHandler :
            IHandleEventsOf<object>,
            IHandleEventsOf<EventANoBaseOrInterface>,
            IHandleEventsOf<IEvent>,
            IHandleEventsOf<EventAWithInterface>,
            IHandleEventsOf<EventBase>,
            IHandleEventsOf<EventAWithBase>,
            IHandleEventsOf<EventAWithBaseAndInterface>,
            IHandleEventsOf<EventEWithBaseWhichHasInterface>
        {
            public string Method { get; set; }

            Task IHandleEventsOf<object>.Handle(object @event)
            {
                return Task.Run(() => Method = "object");
            }

            Task IHandleEventsOf<EventANoBaseOrInterface>.Handle(EventANoBaseOrInterface @event)
            {
                return Task.Run(() => Method = "EventNoBaseOrInterface");
            }

            Task IHandleEventsOf<IEvent>.Handle(IEvent @event)
            {
                return Task.Run(() => Method = "IEvent");
            }

            Task IHandleEventsOf<EventAWithInterface>.Handle(EventAWithInterface @event)
            {
                return Task.Run(() => Method = "EventAWithInterface");
            }

            Task IHandleEventsOf<EventBase>.Handle(EventBase @event)
            {
                return Task.Run(() => Method = "EventBase");
            }

            Task IHandleEventsOf<EventAWithBase>.Handle(EventAWithBase @event)
            {
                return Task.Run(() => Method = "EventAWithBase");
            }

            Task IHandleEventsOf<EventAWithBaseAndInterface>.Handle(EventAWithBaseAndInterface @event)
            {
                return Task.Run(() => Method = "EventAWithBaseAndInterface");
            }

            Task IHandleEventsOf<EventEWithBaseWhichHasInterface>.Handle(EventEWithBaseWhichHasInterface @event)
            {
                return Task.Run(() => Method = "EventEWithBaseWhichHasInterface");
            }

            public void OnError(Exception ex, object @event) { }
            public void OnError(Exception ex, EventANoBaseOrInterface @event) { }
            public void OnError(Exception ex, IEvent @event) { }
            public void OnError(Exception ex, EventAWithInterface @event) { }
            public void OnError(Exception ex, EventBase @event) { }
            public void OnError(Exception ex, EventAWithBase @event) { }
            public void OnError(Exception ex, EventAWithBaseAndInterface @event) { }
            public void OnError(Exception ex, EventEWithBaseWhichHasInterface @event) { }
        }

        public class HandlerThatThrowsAnException : IHandleEventsOf<object>
        {
            public static Exception SomeException { get { return new Exception("Some arbitrary, but unique exception message"); } }

            public Task Handle(object @event)
            {
                throw SomeException;
            }

            public void OnError(Exception ex, object @event)
            {}
        }
    }
}
