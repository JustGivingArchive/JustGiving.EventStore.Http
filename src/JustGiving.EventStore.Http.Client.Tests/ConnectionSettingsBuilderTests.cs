using System;
using FluentAssertions;
using log4net;
using Moq;
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

        [Test]
        public void WithLogger_ShouldStoreRequiredLogger()
        {
            var expected = Mock.Of<ILog>();
            var builder = new ConnectionSettingsBuilder();
            builder.WithLog(expected);

            ((ConnectionSettings)builder).Log.Should().Be(expected);
        }

        [Test]
        public void WhenLoggerNotSet_ShouldDefaultToNull()
        {
            var builder = new ConnectionSettingsBuilder();
            ((ConnectionSettings)builder).Log.Should().BeNull();
        }

        [Test]
        public void WithHttpClientProxy_ShouldStoreRequiredHttpClientProxy()
        {
            var expected = Mock.Of<IHttpClientProxy>();
            var builder = new ConnectionSettingsBuilder();
            builder.WithHttpClientProxy(expected);

            ((ConnectionSettings)builder).HttpClientProxy.Should().Be(expected);
        }

        [Test]
        public void WhenHttpClientProxyNotSet_ShouldDefaultToAHttpClientProxy()
        {
            var builder = new ConnectionSettingsBuilder();
            ((ConnectionSettings)builder).HttpClientProxy.Should().NotBeNull();
        }

        [Test]
        public void WithConnectionName_ShouldStoreGivenConnectionName()
        {
            var expected = "SuperName9000";
            var builder = new ConnectionSettingsBuilder();
            builder.WithConnectionName(expected);

            ((ConnectionSettings)builder).ConnectionName.Should().Be(expected);
        }

        [Test]
        public void WhenConnectionNameNotSet_ShouldDefaultToAUniqueConnectionName()
        {
            var builder = new ConnectionSettingsBuilder();
            var connectionName = ((ConnectionSettings)builder).ConnectionName;
            connectionName.Should().NotBeNull();
            connectionName.Should().StartWith("ES-");
            var expectedGuid = connectionName.Substring(3);
            Guid parsed;
            Guid.TryParse(expectedGuid, out parsed).Should().BeTrue();
        }

    }
}