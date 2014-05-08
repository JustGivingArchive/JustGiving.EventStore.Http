using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using JustGiving.EventStore.Http.Client;
using JustGiving.EventStore.Http.SubscriberHost;
using Moq;
using Newtonsoft.Json.Linq;
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

        private EventStreamSubscriber _subscriber;

        private const string StreamName = "abc";

        [SetUp]
        public void Setup()
        {
            _eventStoreHttpConnectionMock = new Mock<IEventStoreHttpConnection>();
            _eventHandlerResolverMock = new Mock<IEventHandlerResolver>();
            _streamPositionRepositoryMock = new Mock<IStreamPositionRepository>();
            _subscriptionTimerManagerMock = new Mock<ISubscriptionTimerManager>();
            _eventTypeResolverMock = new Mock<IEventTypeResolver>();

            var builder = new EventStreamSubscriberSettingsBuilder(_eventStoreHttpConnectionMock.Object, _eventHandlerResolverMock.Object, _streamPositionRepositoryMock.Object).WithCustomEventTypeResolver(_eventTypeResolverMock.Object).WithCustomSubscriptionTimerManager(_subscriptionTimerManagerMock.Object);
            _subscriber = (EventStreamSubscriber)EventStreamSubscriber.Create(builder);
        }

        [Test]
        public void SubscribeTo_ShouldInvokeSubscriptionManagerWithCorrectStreamName()
        {
            _subscriber.SubscribeTo(StreamName, It.IsAny<TimeSpan>());
            _subscriptionTimerManagerMock.Verify(x => x.Add(StreamName, It.IsAny<TimeSpan>(), It.IsAny<Func<Task>>()));
        }

        [Test]
        public void SubscribeTo_ShouldInvokeSubscriptionManagerWithSuppliedPeriodIfPassed()
        {
            _subscriber.SubscribeTo(It.IsAny<string>(), TimeSpan.FromDays(123));
            _subscriptionTimerManagerMock.Verify(x => x.Add(It.IsAny<string>(), TimeSpan.FromDays(123), It.IsAny<Func<Task>>()));
        }

        [Test]
        public void SubscribeTo_ShouldInvokeSubscriptionManagerWithDefaultPeriodIfNoneIsPassed()
        {
            var builder = new EventStreamSubscriberSettingsBuilder(_eventStoreHttpConnectionMock.Object, _eventHandlerResolverMock.Object, _streamPositionRepositoryMock.Object).WithDefaultPollingInterval(TimeSpan.FromDays(456)).WithCustomEventTypeResolver(_eventTypeResolverMock.Object).WithCustomSubscriptionTimerManager(_subscriptionTimerManagerMock.Object);
            _subscriber = (EventStreamSubscriber)EventStreamSubscriber.Create(builder);
            _subscriber.SubscribeTo(It.IsAny<string>());
            _subscriptionTimerManagerMock.Verify(x => x.Add(It.IsAny<string>(), TimeSpan.FromDays(456), It.IsAny<Func<Task>>()));
        }

        [Test]
        public void UnsubscribeFrom_ShouldInvokeSubscriptionManager()
        {
            _subscriber.UnsubscribeFrom(StreamName);
            _subscriptionTimerManagerMock.Verify(x => x.Remove(StreamName));
        }

        [TestCase(typeof(SomeImplicitHandler))]
        [TestCase(typeof(SomeExplicitHandler))]
        public void GetMethodFromHandler_ShouldBeAbleToFindImplementedInterfaceMethods(Type handlerType)
        {
            var method = _subscriber.GetMethodFromHandler(handlerType, typeof(SomeEvent), "Handle");
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

            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>())).Returns(async () => result);
            await _subscriber.PollAsync(StreamName);

            _subscriptionTimerManagerMock.Verify(x => x.Pause(StreamName));
        }

        [Test]
        public async Task PollAsync_IfNoEvents_ShouldImmediatelyResumeTheEventTimerDuringPolling()//to keep the polling going at the preferred rate
        {
            var result = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo>()
            };

            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>())).Returns(async () => result);
            await _subscriber.PollAsync(StreamName);

            _subscriptionTimerManagerMock.Verify(x => x.Pause(StreamName));
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
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>())).Returns(async () => streamSliceResult).Callback(
                () =>
            {
                if (count++ == 2)
                {
                    streamSliceResult.Entries.Clear();
                }
            });
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(StreamName, It.IsAny<int>())).Returns(async () => new EventReadResult(EventReadStatus.Success, StreamName, It.IsAny<int>(), new EventInfo { Summary = typeof(SomeEvent).FullName }));
            await _subscriber.PollAsync(StreamName);

            _subscriptionTimerManagerMock.Verify(x => x.Pause(StreamName), Times.Exactly(3));
            _subscriptionTimerManagerMock.Verify(x => x.Resume(StreamName), Times.Once);
        }

        [Test]
        public async Task PollAsync_IfUnsuccessful_ShouldNotTryToProcessResults()
        {
            var result = new StreamEventsSlice
            {
                Status = StreamReadStatus.StreamNotFound,
                Entries = null
            };

            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>())).Returns(async () => result);
            await _subscriber.PollAsync(StreamName);
        }

        [Test]
        public async void PollAsync_ShouldStoreStreamPositionAfterHandlersInvoked_EvenIfNoneWereFound()
        {
            var streamSliceResult = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo> { new BasicEventInfo { Title = "123@Stream" } }
            };
            int count = 0;
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>())).Returns(async () => await Task.FromResult(streamSliceResult)).Callback(
                () =>{
                if (count++ == 1)
                {
                    streamSliceResult.Entries.Clear();
                }
            });

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(string));           

            await _subscriber.PollAsync(StreamName);
            
            _streamPositionRepositoryMock.Verify(x => x.SetPositionForAsync(StreamName, 123));
        }

        [Test]
        public void GetEventHandlersFor_ShouldRequestCorrectHandlers()
        {
            _eventTypeResolverMock.Setup(x=>x.Resolve(typeof(SomeEvent).FullName)).Returns(typeof(SomeEvent));

            _subscriber.GetEventHandlersFor(typeof(SomeEvent).FullName);

            _eventHandlerResolverMock.Verify(x => x.GetHandlersOf(typeof(IHandleEventsOf<SomeEvent>)));
        }

        [Test]
        public void GetEventHandlersFor_ShouldReturnAnEumptyEnumerableIfANullEventTypeIsPassed()
        {
            var result = _subscriber.GetEventHandlersFor(null);
            result.Should().NotBeNull();
        }

        [Test]
        public async void InvokeMessageHandlersForEventMessageAsync_ShouldInvokeFoundHandlers()
        {
            var @implicit = new SomeImplicitHandler();
            var @explicit = new SomeExplicitHandler();

            var streamItem = new EventReadResult(EventReadStatus.Success, StreamName, 123, new EventInfo { Summary = typeof(SomeEvent).FullName, Content = new RecordedEvent { Data = new JObject() } });

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(SomeEvent));
            
            var handlers = new IHandleEventsOf<SomeEvent>[] {@implicit, @explicit};

            await _subscriber.InvokeMessageHandlersForEventMessageAsync(StreamName, typeof(SomeEvent), handlers, streamItem);

            @implicit.@event.Should().NotBeNull();
            @explicit.@event.Should().NotBeNull();
        }

        public class SomeEvent { }

        public class SomeImplicitHandler : IHandleEventsOf<SomeEvent>
        {
            public SomeEvent @event;
            public Task Handle(SomeEvent @event) { return Task.Run(() => this.@event = @event); }
            public void OnError(Exception ex) { }
        }

        public class SomeExplicitHandler : IHandleEventsOf<SomeEvent>
        {
            public SomeEvent @event;
            Task IHandleEventsOf<SomeEvent>.Handle(SomeEvent @event) { return Task.Run(() => this.@event = @event); }
            void IHandleEventsOf<SomeEvent>.OnError(Exception ex) { }
        }
    }
}