using System;
using FluentAssertions;
using NUnit.Framework;

namespace JustGiving.EventStore.Http.Client.Tests
{
    [TestFixture]
    public class NewEventDataTests
    {
        [Test]
        public void WhenCreating_AndNotSpeifyingAGenericType_ShouldSetEventTypeAsMostSpecificType()
        {
            var eventData = NewEventData.Create(new Something());

            eventData.EventType.Should().Be(typeof(Something).FullName);
        }

        [Test]
        public void WhenCreating_AndDeclaringAsObject_ShouldSetEventTypeAsMostSpecificType() // when using params array, you probably don't wish to publish 'object's
        {
            var eventData = NewEventData.Create<object>(new SubSomething());

            // The EventType should be based on the actual concrete class of the event. Not the genmeric-declared class.
            eventData.EventType.Should().Be(typeof(SubSomething).FullName);
        }

        [Test]
        public void WhenCreating_AndDeclaringAsABaseClass_ShouldSetEventTypeAsGenericBaseClass()
        {
            var eventData = NewEventData.Create<Something>(new SubSomething());

            // The EventType should be based on the actual concrete class of the event. Not the base class.
            eventData.EventType.Should().Be(typeof(Something).FullName);
        }

        [Test]
        public void WhenCreating_ShouldSetDataCorrectly()
        {
            var expectedData = new { Id = 123, Foo = "bar" };

            var eventData = NewEventData.Create(expectedData);
            eventData.Data.Should().Be(expectedData);
        }

        [Test]
        public void WhenCreating_ShouldSetMetadataCorrectly()
        {
            var metadata = new { Id = 123, Foo = "bar" };

            var eventData = NewEventData.Create("something", metadata);
            eventData.Metadata.Should().Be(metadata);
        }

        [Test]
        public void WhenCreatingWithoutSpecifyingEventId_ShouldCreateAnEventId()
        {
            var expectedData = new { Id = 123, Foo = "bar" };

            var eventData = NewEventData.Create(expectedData);
            eventData.EventId.Should().NotBeEmpty();
        }

        [Test]
        public void WhenCreatingAndSpecifyingEventId_ShouldUsedPassedEventId()
        {
            var expectedData = new { Id = 123, Foo = "bar" };
            var expectedId = Guid.NewGuid();

            var eventData = NewEventData.Create(expectedId, expectedData);
            eventData.EventId.Should().Be(expectedId);
        }

        private class Something
        {
            public int Id { get; set; }
            public string Foo { get; set; }
        }

        private class SubSomething : Something { }
    }
}