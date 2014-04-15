using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace JustGiving.EventStore.Http.Client.Tests
{
    [TestFixture]
    public class TestHarness
    {
        private const string StreamName = "JonsDonations";
        private IEventStoreHttpConnection _connection;

        [SetUp]
        public void Setup()
        {
            _connection = EventStoreHttpConnection.Create(ConnectionSettings.Default, "http://127.0.0.1:9113", "ShinyConnection");
        }

        [Test]
        public async void Load()
        {
            await Load(2);
        }

        private async Task Load(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var d = new JBDonation
                {
                    Id = Guid.NewGuid(),
                    Amount = i,
                    Donor = "Jon",
                    Success = true
                };

                var @event = NewEventData.Create((JBDonation)null);

                await _connection.AppendToStreamAsync(StreamName, ExpectedVersion.Any, @event);
            }
        }

        [Test]
        public async void Retrieve()
        {
            var @event = await _connection.ReadEventAsync(StreamName, StreamPosition.End);
        }

        [Test]
        public async void ListForwards()
        {
            var @event = await _connection.ReadStreamEventsForwardAsync(StreamName, 10, 5);
        }

        [Test]
        public async void HeadShouldNotBeCached()
        {
            var event1 = await _connection.ReadEventAsync(StreamName, StreamPosition.End);
            await Load(1);
            var event2 = await _connection.ReadEventAsync(StreamName, StreamPosition.End);

            event1.EventInfo.Id.Should().NotBe(event2.EventInfo.Id);
        }

        
        public class JBDonation
        {
            public Guid Id { get; set; }
            public int Amount { get; set; }
            public string Donor { get; set; }
            public bool Success { get; set; }
        }


    }
}
