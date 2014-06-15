using System;
using System.Threading.Tasks;
using JustGiving.EventStore.Http.Client;
using JustGiving.EventStore.Http.SubscriberHost;
using Moq;
using NUnit.Framework;

namespace JG.EventStore.Http.SubscriberHost.Tests
{
    //[TestFixture]
    public class Yeah
    {
        //[Test]
        public async void Test()
        {
            var connection = EventStoreHttpConnection.Create(new ConnectionSettingsBuilder(), "http://127.0.0.1:9113");
            var resolver = new Mock<IEventHandlerResolver>();
            resolver.Setup(x=>x.GetHandlersOf(It.IsAny<Type>())).Returns(new[]{new SomeEventHandler()});
            var subscriber = EventStreamSubscriber.Create(new EventStreamSubscriberSettingsBuilder(connection, resolver.Object , new MemoryBackedStreamPositionRepositoryForDebugging()));

            await connection.AppendToStreamAsync("abc", ExpectedVersion.Any, NewEventData.Create(new SomeEvent()));
            await connection.AppendToStreamAsync("abc", ExpectedVersion.Any, NewEventData.Create(new SomeEvent()));
            await connection.AppendToStreamAsync("abc", ExpectedVersion.Any, NewEventData.Create(new SomeEvent()));
            
            subscriber.SubscribeTo("abc");

            await Task.Delay(TimeSpan.FromDays(1));
        }

        public class SomeEvent
        {
            private static Random r = new Random();
            public SomeEvent()
            {
                Id = r.Next(0, int.MaxValue);
            }
            public int Id { get; set; }
        }

        public class SomeEventHandler : IHandleEventsOf<SomeEvent>
        {
            public Task Handle(SomeEvent @event)
            {
                return Task.FromResult(0);
            }

            public void OnError(Exception ex, SomeEvent @event)
            {}
        }
    }
}