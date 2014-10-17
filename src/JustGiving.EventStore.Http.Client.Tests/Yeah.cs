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
            _connection = EventStoreHttpConnection.Create(ConnectionSettings.Default, "http://127.0.0.1:2113");
        }

        //[Test]
        public async void Load()
        {
            await Insert(2);
        }

        private async Task Insert(int count)
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
                
                await _connection.AppendToStreamAsync(StreamName, d);
            }
        }

        //[Test]
        public async void Retrieve()
        {
            await Insert(7);
            var event1 = await _connection.ReadEventAsync(StreamName, 4);

            var event2 = await _connection.ReadEventAsync(StreamName, 6);
        }

        //[Test]
        public async void ListForwards()
        {
            await Insert(20);
            var @event = await _connection.ReadStreamEventsForwardAsync(StreamName, 10, 5, null);
        }

        //[Test]
        public async void HeadShouldNotBeCached()
        {
            await Insert(1);
            var event1 = await _connection.ReadEventAsync(StreamName, StreamPosition.End);
            await Insert(1);
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