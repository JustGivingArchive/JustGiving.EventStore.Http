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
        public async Task PollAsync_ShouldPauseTheEventTimerDuringPolling()//to prevent dual polling
        {
            var result = new StreamEventsSlice
            {
                Entries = new List<BasicEventInfo>()
            };

            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => result);
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

            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => result);
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
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => streamSliceResult).Callback(
                () =>
            {
                if (count++ == 2)
                {
                    streamSliceResult.Entries.Clear();
                }
            });
            _eventStoreHttpConnectionMock.Setup(x => x.ReadEventAsync(StreamName, It.IsAny<int>())).Returns(async () => new EventReadResult(EventReadStatus.Success, StreamName, It.IsAny<int>(), new EventInfo { Summary = typeof(EventANoBaseOrInterface).FullName }));
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

            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => result);
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
            _eventStoreHttpConnectionMock.Setup(x => x.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeSpan?>())).Returns(async () => await Task.FromResult(streamSliceResult)).Callback(
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
            _eventTypeResolverMock.Setup(x=>x.Resolve(typeof(EventANoBaseOrInterface).FullName)).Returns(typeof(EventANoBaseOrInterface));

            _subscriber.GetEventHandlersFor(typeof(EventANoBaseOrInterface).FullName);

            _eventHandlerResolverMock.Verify(x => x.GetHandlersOf(typeof(IHandleEventsOf<EventANoBaseOrInterface>)));
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
            var @implicitForParentType = new SomeImplicitHandlerForParentType();
            var @explicit = new SomeExplicitHandler();
            var @explicitForParentType = new SomeExplicitHandlerForParentType();

            var streamItem = new EventReadResult(EventReadStatus.Success, StreamName, 123, new EventInfo { Summary = typeof(EventANoBaseOrInterface).FullName, Content = new RecordedEvent { Data = new JObject() } });

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(EventANoBaseOrInterface));
            
            var handlers = new IHandleEventsOf<EventANoBaseOrInterface>[] {@implicit, @implicitForParentType, @explicit, @explicitForParentType};

            await _subscriber.InvokeMessageHandlersForEventMessageAsync(StreamName, typeof(EventANoBaseOrInterface), handlers, streamItem);

            @implicit.EventA.Should().NotBeNull();
            @implicitForParentType.@event.Should().NotBeNull();
            @explicit.EventA.Should().NotBeNull();
            @explicitForParentType.@event.Should().NotBeNull();
        }

        [Test]
        public async void InvokeMessageHandlersForEventMessageAsync_ShouldInvokeFoundHandlersForInterfaceType()
        {
            var @implicit = new SomeImplicitHandlerForInterface();
            var @explicit = new SomeExplicitHandlerForInterface();

            var streamItem = new EventReadResult(EventReadStatus.Success, StreamName, 123, new EventInfo { Summary = typeof(EventAWithInterface).FullName, Content = new RecordedEvent { Data = new JObject() } });

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(typeof(EventAWithInterface));

            var handlers = new IHandleEventsOf<IEvent>[] { @implicit, @explicit };

            await _subscriber.InvokeMessageHandlersForEventMessageAsync(StreamName, typeof(EventAWithInterface), handlers, streamItem);

            @implicit.@event.Should().NotBeNull();
            @explicit.@event.Should().NotBeNull();
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
        public async void InvokeMessageHandlersForEventMessageAsync_ShouldInvokeCorrectHandlerOverloadForType(Type evenType, string expectedMethod)
        {
            var @implicit = new MultiTypeImplicitHandler();
            var @explicit = new MultiTypeExplicitHandler();

            var streamItem = new EventReadResult(EventReadStatus.Success, StreamName, 123, new EventInfo { Summary = evenType.FullName, Content = new RecordedEvent { Data = new JObject() } });

            _eventTypeResolverMock.Setup(x => x.Resolve(It.IsAny<string>())).Returns(evenType);

            var handlers = new IHandleEventsOf<object>[] { @implicit, @explicit };

            await _subscriber.InvokeMessageHandlersForEventMessageAsync(StreamName, evenType, handlers, streamItem);

            @implicit.Method.Should().Be(expectedMethod, "The expected method overload was not called on implicit handler");
            @explicit.Method.Should().Be(expectedMethod, "The expected method overload was not called on explicit handler");
        }
        
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
        public class EventCWithInterface : IEvent2  { }
        public class EventCWithBase : EventBase2  { }
        public class EventCWithBaseAndInterface : EventBase2, IEvent2 { }

        public interface IEvent3 { }
        public abstract class EventBase3 : IEvent3 { }
        public class EventDWithBaseWhichHasInterface : EventBase3 { }
        public class EventEWithBaseWhichHasInterface : EventBase3 { }

        public class SomeImplicitHandler : IHandleEventsOf<EventANoBaseOrInterface>
        {
            public EventANoBaseOrInterface EventA;
            public Task Handle(EventANoBaseOrInterface EventA) { return Task.Run(() => this.EventA = EventA); }
            public void OnError(Exception ex) { }
        }

        public class SomeImplicitHandlerForParentType : IHandleEventsOf<object>
        {
            public object @event;
            public Task Handle(object EventA) { return Task.Run(() => this.@event = EventA); }
            public void OnError(Exception ex) {}
        }

        public class SomeImplicitHandlerForInterface : IHandleEventsOf<IEvent>
        {
            public IEvent @event;
            public Task Handle(IEvent EventA) { return Task.Run(() => this.@event = EventA); }
            public void OnError(Exception ex) { }
        }
        
        public class SomeExplicitHandler : IHandleEventsOf<EventANoBaseOrInterface>
        {
            public EventANoBaseOrInterface EventA;
            Task IHandleEventsOf<EventANoBaseOrInterface>.Handle(EventANoBaseOrInterface EventA) { return Task.Run(() => this.EventA = EventA); }
            void IHandleEventsOf<EventANoBaseOrInterface>.OnError(Exception ex) { }
        }

        public class SomeExplicitHandlerForParentType : IHandleEventsOf<object>
        {
            public object @event;
            Task IHandleEventsOf<object>.Handle(object EventA) { return Task.Run(() => this.@event = EventA); }
            void IHandleEventsOf<object>.OnError(Exception ex) { }
        }

        public class SomeExplicitHandlerForInterface : IHandleEventsOf<IEvent>
        {
            public IEvent @event;
            Task IHandleEventsOf<IEvent>.Handle(IEvent EventA) { return Task.Run(() => this.@event = EventA); }
            void IHandleEventsOf<IEvent>.OnError(Exception ex) { }
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

            public void OnError(Exception ex)
            {
            }
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

            public void OnError(Exception ex)
            {
            }
        }
    }
}