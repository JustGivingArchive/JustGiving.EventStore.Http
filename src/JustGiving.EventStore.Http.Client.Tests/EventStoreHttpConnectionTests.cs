using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using Moq;
using NUnit.Framework;

namespace JustGiving.EventStore.Http.Client.Tests
{
    [TestFixture]
    public class EventStoreHttpConnectionTests
    {
        private const string endpoint = "some-endpoint";
        private const string conection_name = "some-name";

        Mock<HttpClientHandler> _handlerMock;
        Mock<IHttpClientProxy> _httpClientProxyMock;
        private ConnectionSettings _defaultConnectionSettings;
        private EventStoreHttpConnection _connection;

        [SetUp]
        public void Setup()
        {
            _handlerMock = new Mock<HttpClientHandler>();
            _httpClientProxyMock = new Mock<IHttpClientProxy>();
            _defaultConnectionSettings = GetConnectionSettings();
            _connection = (EventStoreHttpConnection)EventStoreHttpConnection.Create(_defaultConnectionSettings, _httpClientProxyMock.Object, endpoint, conection_name);
            _connection._TestingClientHandler = _handlerMock.Object;
        }

        [Test]
        public void a()
        {
            
        }

        private ConnectionSettings GetConnectionSettings()
        {
            var cs = new ConnectionSettingsBuilder()
                .SetDefaultUserCredentials(new UserCredentials("user", "pass"))
                .WithConnectionTimeoutOf(TimeSpan.FromSeconds(30));

            return cs;
        }
    }
}