using System;
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
            for (var i = 0; i < 100; i++)
            {
                var d = new Donation
                {
                    Id = Guid.NewGuid(),
                    Amount = i,
                    Donor = "Jon",
                    Success = true
                };

                var @event = NewEventData.Create(d);

                await _connection.AppendToStreamAsync(StreamName, ExpectedVersion.Any, @event);
            }
        }

        [Test]
        public async void Retrieve()
        {
            var @event = await _connection.ReadEventAsync(StreamName, 12);
        }

        [Test]
        public async void ListForwards()
        {
            var @event = await _connection.ReadStreamEventsForwardAsync(StreamName, 10, 5);
        }
        
        public class Donation
        {
            public Guid Id { get; set; }
            public int Amount { get; set; }
            public string Donor { get; set; }
            public bool Success { get; set; }
        }


    }
}
