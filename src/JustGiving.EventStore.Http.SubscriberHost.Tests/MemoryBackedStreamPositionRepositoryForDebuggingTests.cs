using FluentAssertions;
using JustGiving.EventStore.Http.SubscriberHost;
using NUnit.Framework;

namespace JG.EventStore.Http.SubscriberHost.Tests
{
    [TestFixture]
    public class MemoryBackedStreamPositionRepositoryForDebuggingTests
    {
        [Test]
        public async void GetPositionFor_ShouldReturnNullWhenNoStreamCanBeFound()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            var result = await sut.GetPositionForAsync("abc");
            result.Should().NotHaveValue();
        }

        [Test]
        public async void GetPositionFor_ShouldReturnStoredValueWhenNoStreamCanBeFound()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            await sut.SetPositionForAsync("abc", 123);
            var result = await sut.GetPositionForAsync("abc");
            result.Should().Be(123);
        }

        [Test]
        public async void ShouldWorkForMultipleKeys()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            await sut.SetPositionForAsync("abc", 123);
            await sut.SetPositionForAsync("def", 456);

            var result = await sut.GetPositionForAsync("abc");
            result.Should().Be(123);

            result = await sut.GetPositionForAsync("def");
            result.Should().Be(456);
        }
    }
}