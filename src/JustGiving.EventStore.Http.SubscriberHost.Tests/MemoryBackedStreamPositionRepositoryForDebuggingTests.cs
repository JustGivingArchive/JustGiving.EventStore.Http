using FluentAssertions;
using JustGiving.EventStore.Http.SubscriberHost;
using NUnit.Framework;

namespace JG.EventStore.Http.SubscriberHost.Tests
{
    [TestFixture]
    public class MemoryBackedStreamPositionRepositoryForDebuggingTests
    {
        private const string StreamName = "abc";
        private const string SubscriberId = "def";

        [Test]
        public async void GetPositionFor_ShouldReturnNullWhenNoStreamCanBeFound()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            var result = await sut.GetPositionForAsync(StreamName, SubscriberId);
            result.Should().NotHaveValue();
        }

        [Test]
        public async void GetPositionFor_ShouldReturnStoredValueWhenNoStreamCanBeFound()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            await sut.SetPositionForAsync(StreamName, SubscriberId, 123);
            var result = await sut.GetPositionForAsync(StreamName, SubscriberId);
            result.Should().Be(123);
        }

        [Test]
        public async void ShouldWorkForMultipleStreams()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            await sut.SetPositionForAsync(StreamName, SubscriberId, 123);
            await sut.SetPositionForAsync("def", SubscriberId, 456);

            var result = await sut.GetPositionForAsync(StreamName, SubscriberId);
            result.Should().Be(123);

            result = await sut.GetPositionForAsync("def", SubscriberId);
            result.Should().Be(456);
        }

        [Test]
        public async void ShouldWorkForMultipleSubscriberIds()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            await sut.SetPositionForAsync(StreamName, SubscriberId, 123);
            await sut.SetPositionForAsync(StreamName, "XYZ", 456);

            var result = await sut.GetPositionForAsync(StreamName, SubscriberId);
            result.Should().Be(123);

            result = await sut.GetPositionForAsync(StreamName, "XYZ");
            result.Should().Be(456);
        }
    }
}