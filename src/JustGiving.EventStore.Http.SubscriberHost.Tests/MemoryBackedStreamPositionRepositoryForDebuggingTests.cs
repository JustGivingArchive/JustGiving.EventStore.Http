using FluentAssertions;
using JustGiving.EventStore.Http.SubscriberHost;
using NUnit.Framework;

namespace JG.EventStore.Http.SubscriberHost.Tests
{
    [TestFixture]
    public class MemoryBackedStreamPositionRepositoryForDebuggingTests
    {
        [Test]
        public void GetPositionFor_ShouldReturnNullWhenNoStreamCanBeFound()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            sut.GetPositionFor("abc").Should().NotHaveValue();
        }

        [Test]
        public void GetPositionFor_ShouldReturnStoredValueWhenNoStreamCanBeFound()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            sut.SetPositionFor("abc", 123);
            sut.GetPositionFor("abc").Should().Be(123);
        }

        [Test]
        public void ShouldWorkForMultipleKeys()
        {
            var sut = new MemoryBackedStreamPositionRepositoryForDebugging();
            sut.SetPositionFor("abc", 123);
            sut.SetPositionFor("def", 456);
            sut.GetPositionFor("abc").Should().Be(123);
            sut.GetPositionFor("def").Should().Be(456);
        }
    }
}