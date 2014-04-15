using System;
using FluentAssertions;
using NUnit.Framework;

namespace JustGiving.EventStore.Http.Client.Tests
{
    [TestFixture]
    public class ConnectionSettingsBuilderTests
    {
        [Test]
        public void SetDefaultUserCredentials_ShouldStoreSpecifiedCredentials()
        {
            var expectedCredentials = new UserCredentials("a", "b");
            var builder = new ConnectionSettingsBuilder();
            builder.SetDefaultUserCredentials(expectedCredentials);

            ((ConnectionSettings)builder).DefaultUserCredentials.Should().Be(expectedCredentials);
        }

        [Test]
        public void WithConnectionTimeoutOf_ShouldStoreRequiredTimeout()
        {
            var expectedTimeout = TimeSpan.FromSeconds(30);
            var builder = new ConnectionSettingsBuilder();
            builder.WithConnectionTimeoutOf(expectedTimeout);

            ((ConnectionSettings)builder).ConnectionTimeout.Should().Be(expectedTimeout);
        }

        [Test]
        public void OnErrorOccured_ShouldSetPassedErrorHandler()
        {
            Action<IEventStoreHttpConnection, Exception> expectedHandler = (x, y) => { };
            var builder = new ConnectionSettingsBuilder();
            builder.OnErrorOccured(expectedHandler);

            ((ConnectionSettings)builder).ErrorHandler.Should().Be(expectedHandler);
        }
    }
}