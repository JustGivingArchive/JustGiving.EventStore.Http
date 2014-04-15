using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JustGiving.EventStore.Http.Client.Tests
{
    [TestFixture]
    public class RecordedEventTests
    {
        [Test]
        public void WhenJsonDoesNotContainDataField_DataShouldBeReturnedAsNull() //for my own benefit
        {
            var json = @"{EventStreamId:'SomeStream', EventNumber:123, EventType:'SomeType'}";
            var recordedEvent = JsonConvert.DeserializeObject<RecordedEvent>(json);
            recordedEvent.Data.Should().BeNull();
        }

        [Test]
        public void WhenJsonDoesNotContainDataField_GetObjectShouldReturnNull() //for my own benefit
        {
            var json = @"{EventStreamId:'SomeStream', EventNumber:123, EventType:'SomeType'}";
            var recordedEvent = JsonConvert.DeserializeObject<RecordedEvent>(json);
            recordedEvent.GetObject<Something>().Should().BeNull();
        }

        [Test]
        public void WhenJsonContainsADataField_GetObjectShouldReturnARehydratedObject() //for my own benefit
        {
            var json = @"{EventStreamId:'SomeStream', EventNumber:123, EventType:'SomeType', Data:{Id:456, Name:'Foo'}}";
            var recordedEvent = JsonConvert.DeserializeObject<RecordedEvent>(json);
            
            var result = recordedEvent.GetObject<Something>();
            result.Should().NotBeNull();

            result.Id.Should().Be(456);
            result.Name.Should().Be("Foo");
        }

        private class Something
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}