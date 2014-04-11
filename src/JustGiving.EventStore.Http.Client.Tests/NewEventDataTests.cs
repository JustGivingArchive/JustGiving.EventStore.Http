using FluentAssertions;
using NUnit.Framework;

namespace JustGiving.EventStore.Http.Client.Tests
{
    [TestFixture]
    public class NewEventDataTests
    {
        [Test]
        public void WhenCreating_ShouldSetEventTypeCorrectly()
        {
            var eventData = NewEventData.Create(new Something());

            eventData.EventType.Should().Be(typeof(Something).FullName);
        }

        [Test]
        public void WhenCreating_ShouldSetDataCorrectly()
        {
            var expectedData = new Something { Id = 123, Foo = "bar" };

            var eventData = NewEventData.Create(expectedData);
            eventData.Data.Should().Be(expectedData);
        }

        [Test]
        public void WhenCreatingWithoutSpecifyingEventId_ShouldCreateAnEventId()
        {
            var expectedData = new Something { Id = 123, Foo = "bar" };

            var eventData = NewEventData.Create(expectedData);
            eventData.Data.Should().Be(expectedData);
        }



        private class Something
        {
            public int Id { get; set; }
            public string Foo { get; set; }
        }

    }
}