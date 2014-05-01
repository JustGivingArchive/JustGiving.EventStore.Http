using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using JustGiving.EventStore.Http.Client.Exceptions;
using Moq;
using NUnit.Framework;

namespace JustGiving.EventStore.Http.Client.Tests
{
    [TestFixture]
    public class EventStoreHttpConnectionTests
    {
        private const string Endpoint = "http://some-endpoint";
        private const string ConectionName = "some-name";
        private const string StreamName = "some-stream";
        private const string Username = "some-user";
        private const string Password = "some-password";

        Mock<IHttpClientProxy> _httpClientProxyMock;
        private ConnectionSettings _defaultConnectionSettings;
        private EventStoreHttpConnection _connection;
        private ConnectionSettingsBuilder _connectionSettingsBuilder;

        [SetUp]
        public void Setup()
        {
            _httpClientProxyMock = new Mock<IHttpClientProxy>();
            _defaultConnectionSettings = GetConnectionSettings();
            _connection = (EventStoreHttpConnection)EventStoreHttpConnection.Create(_defaultConnectionSettings, Endpoint);
        }

        [Test]
        public void GetHandler_WhenNoDefaultCredentialsAreConfigured_TheClientShouldNotHaveAnyCredentials()
        {
            var builder = new ConnectionSettingsBuilder().WithHttpClientProxy(_httpClientProxyMock.Object);
            _connection = (EventStoreHttpConnection)EventStoreHttpConnection.Create(builder, Endpoint);

            var handler = _connection.GetHandler();
            handler.Credentials.Should().BeNull();
        }

        [Test]
        public void GetHandler_WhenConnectionContainsDefaultCredentials_TheClientShouldContainTheDefaultCredentials()
        {
            var handler = _connection.GetHandler();
            var credentials = handler.Credentials.GetCredential(It.IsAny<Uri>(), "");

            credentials.UserName.Should().Be(Username);
            credentials.Password.Should().Be(Password);
        }

        [Test]
        public void GetClient_WhenNoDefaultTimeoutIsConfigured_TheClientShouldBe100Seconds()
        {
            _connection = (EventStoreHttpConnection)EventStoreHttpConnection.Create(ConnectionSettings.Default, Endpoint);

            var client = _connection.GetClient();
            client.Timeout.Should().Be(TimeSpan.FromSeconds(100));
        }

        [Test]
        public void GetClient_WhenADefaultTimeoutIsConfigured_TheClientShouldHaveTheSpecifiedTimeout()
        {
            var client = _connection.GetClient();
            client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        }

        [Test]
        public async void DeleteStreamAsync_URIShouldBeCorrect()
        {
            await DeleteTest(It.IsAny<int>(), It.IsAny<bool>(), (client, request) =>
            {
                request.RequestUri.AbsoluteUri.Should().Be(string.Format("{0}/streams/{1}", Endpoint, StreamName));
            });
        }

        [Test]
        public async void DeleteStreamAsync_VerbShouldBeDelete()
        {
            await DeleteTest(It.IsAny<int>(), It.IsAny<bool>(), (client, request) =>
            {
                request.Method.Should().Be(HttpMethod.Delete);
            });
        }

        [Test]
        public async void DeleteStreamAsync_WhenPerformingHardDelete_ShouldContainHardDeleteHeader()
        {
            await DeleteTest(It.IsAny<int>(), true, (client, request) =>
            {
                request.Headers.GetValues("ES-HardDelete").ShouldAllBeEquivalentTo(new[] { "true" });
            });
        }

        [Test]
        public async void DeleteStreamAsync_WhenPerformingSoftDelete_ShouldNotContainHardDeleteHeader()
        {
            await DeleteTest(It.IsAny<int>(), false, (client, request) =>
            {
                request.Headers.Contains("ES-HardDelete").Should().BeFalse();
            });
        }

        [Test]
        public async void DeleteStreamAsync_ShouldContainExpectedVersionHeader()
        {
            await DeleteTest(123, It.IsAny<bool>(), (client, request) =>
            {
                request.Headers.GetValues("ES-ExpectedVersion").ShouldBeEquivalentTo(new[] { "123" });
            });
        }

        [Test]
        public void DeleteStreamAsync_HttpErrorCodesShouldYieldAnException()
        {
            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) {Content = new StringContent("") });

            Assert.Throws<EventStoreHttpException>(async () =>
            {
                await _connection.DeleteStreamAsync(StreamName, It.IsAny<int>(), It.IsAny<bool>());
            });
        }

        private async Task DeleteTest(int expectedVersion, bool hardDelete, Action<HttpClient, HttpRequestMessage> test)
        {
            var called = false;

            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
                .Callback<HttpClient, HttpRequestMessage>(
                    (client, request) =>
                    {
                        test(client, request);

                        called = true;
                    });

            await _connection.DeleteStreamAsync(StreamName, expectedVersion, hardDelete);

            called.Should().BeTrue();
        }

        [Test]
        public async void AppendToStreamAsync_URIShouldBeCorrect()
        {
            await AppendToStreamTest(It.IsAny<int>(), It.IsAny<NewEventData>(), (client, request) =>
            {
                request.RequestUri.AbsoluteUri.Should().Be(string.Format("{0}/streams/{1}", Endpoint, StreamName));
            });
        }

        [Test]
        public async void AppendToStreamAsync_VerbShouldBePost()
        {
            await AppendToStreamTest(It.IsAny<int>(), It.IsAny<NewEventData>(), (client, request) =>
            {
                request.Method.Should().Be(HttpMethod.Post);
            });
        }

        [Test]
        public async void AppendToStreamAsync_ShouldContainExpectedVersionHeader()
        {
            await AppendToStreamTest(123, It.IsAny<NewEventData>(), (client, request) =>
            {
                request.Headers.GetValues("ES-ExpectedVersion").ShouldBeEquivalentTo(new[] { "123" });
            });
        }

        [Test]
        public async void AppendToStreamAsync_MediaTypeShouldBeUTF8JsonApplication()
        {
            await AppendToStreamTest(It.IsAny<int>(), It.IsAny<NewEventData>(), (client, request) =>
            {
                request.Content.Headers.GetValues("Content-Type").ShouldBeEquivalentTo(new[] { "application/json; charset=utf-8" });
            });
        }

        [Test]
        public void AppendToStreamAsync_HttpErrorCodesShouldYieldAnException()
        {
            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("") });

            Assert.Throws<EventStoreHttpException>(async () =>
            {
                await _connection.AppendToStreamAsync(StreamName, It.IsAny<int>(), It.IsAny<NewEventData>());
            });
        }

        private async Task AppendToStreamTest(int expectedVersion, NewEventData data, Action<HttpClient, HttpRequestMessage> test)
        {
            var called = false;

            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
                .Callback<HttpClient, HttpRequestMessage>(
                    (client, request) =>
                    {
                        test(client, request);

                        called = true;
                    });

            await _connection.AppendToStreamAsync(StreamName, expectedVersion, data);

            called.Should().BeTrue();
        }

        [Test]
        public async void ReadEventAsync_KnownEventURIShouldBeCorrect()
        {
            await ReadEventTest(123, (client, request) =>
            {
                request.RequestUri.AbsoluteUri.Should().Be(string.Format("{0}/streams/{1}/123", Endpoint, StreamName));
            });
        }

        [Test]
        public async void ReadEventAsync_HeadEventURIShouldBeCorrect()
        {
            await ReadEventTest(StreamPosition.End, (client, request) =>
            {
                request.RequestUri.AbsoluteUri.Should().Be(string.Format("{0}/streams/{1}/head", Endpoint, StreamName));
            });
        }

        [Test]
        public async void ReadEventAsync_VerbShouldBeGet()
        {
            await ReadEventTest(It.IsAny<int>(), (client, request) =>
            {
                request.Method.Should().Be(HttpMethod.Get);
            });
        }

        [Test]
        public async void ReadEventAsync_MediaTypeShould_NOT_BeUTF8JsonApplication()//because we lost useful type info, preventing casting back to c#
        {
            await ReadEventTest(It.IsAny<int>(), (client, request) =>
            {
                request.Headers.Accept.Should().BeEmpty();
            });
        }

        [Test]
        public async void ReadEventAsync_Http404ShouldYieldANotFoundResult()
        {
            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            var result = await _connection.ReadEventAsync(StreamName, It.IsAny<int>());
            result.Status.Should().Be(EventReadStatus.NotFound);
        }

        [Test]
        public async void ReadEventAsync_Http410ShouldYieldANotFoundResult()
        {
            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Gone));

            var result = await _connection.ReadEventAsync(StreamName, It.IsAny<int>());
            result.Status.Should().Be(EventReadStatus.StreamDeleted);
        }

        [Test]
        public void ReadEventAsync_HttpErrorCodesShouldYieldAnException()
        {
            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("") });

            Assert.Throws<EventStoreHttpException>(async () =>
            {
                await _connection.ReadEventAsync(StreamName, It.IsAny<int>());
            });
        }

        private async Task ReadEventTest(int position, Action<HttpClient, HttpRequestMessage> test)
        {
            var called = false;

            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") })
                .Callback<HttpClient, HttpRequestMessage>(
                    (client, request) =>
                    {
                        test(client, request);

                        called = true;
                    });

            await _connection.ReadEventAsync(StreamName, position);

            called.Should().BeTrue();
        }

        [Test]
        public async void ReadStreamEventsForwardAsync_KnownEventURIShouldBeCorrect()
        {
            await ReadStreamEventsForwardTest(123, 15, (client, request) =>
            {
                request.RequestUri.AbsoluteUri.Should().Be(string.Format("{0}/streams/{1}/123/forward/15", Endpoint, StreamName));
            });
        }


        [Test]
        public async void ReadStreamEventsForwardAsync_VerbShouldBeGet()
        {
            await ReadStreamEventsForwardTest(It.IsAny<int>(), It.IsAny<int>(), (client, request) =>
            {
                request.Method.Should().Be(HttpMethod.Get);
            });
        }

        [Test]
        public async void ReadStreamEventsForwardAsync_MediaTypeShouldNotBeUTF8JsonApplication()//Because it causes the result to be returned without metadata
        {
            await ReadStreamEventsForwardTest(It.IsAny<int>(), It.IsAny<int>(), (client, request) =>
            {
                request.Headers.Accept.Should().BeEmpty();
            });
        }

        [Test]
        public async void ReadStreamEventsForwardAsync_Http404ShouldYieldANotFoundResult()
        {
            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            var result = await _connection.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>());
            result.Status.Should().Be(StreamReadStatus.StreamNotFound);
        }

        [Test]
        public async void ReadStreamEventsForwardAsync_Http410ShouldYieldANotFoundResult()
        {
            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Gone));

            var result = await _connection.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>());
            result.Status.Should().Be(StreamReadStatus.StreamDeleted);
        }

        [Test]
        public void ReadStreamEventsForwardAsync_HttpErrorCodesShouldYieldAnException()
        {
            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("") });

            Assert.Throws<EventStoreHttpException>(async () =>
            {
                await _connection.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>());
            });
        }

        [Test]
        public void ReadStreamEventsForwardAsync_ResultsShouldBeReturnedInReverseOrder()
        {
            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("") });

            Assert.Throws<EventStoreHttpException>(async () =>
            {
                await _connection.ReadStreamEventsForwardAsync(StreamName, It.IsAny<int>(), It.IsAny<int>());
            });
        }

        private async Task ReadStreamEventsForwardTest(int start, int count, Action<HttpClient, HttpRequestMessage> test)
        {
            var called = false;

            _httpClientProxyMock.Setup(x => x.SendAsync(It.IsAny<HttpClient>(), It.IsAny<HttpRequestMessage>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{Entries:[{Id:2}, {Id:1}]}") })
                .Callback<HttpClient, HttpRequestMessage>(
                    (client, request) =>
                    {
                        test(client, request);

                        called = true;
                    });

            var result = await _connection.ReadStreamEventsForwardAsync(StreamName, start, count);
            result.Entries[0].Id.Should().Be("1");
            result.Entries[1].Id.Should().Be("2");

            called.Should().BeTrue();
        }

        private ConnectionSettings GetConnectionSettings()
        {
            var cs = new ConnectionSettingsBuilder()
                .SetDefaultUserCredentials(new UserCredentials(Username, Password))
                .WithConnectionTimeoutOf(TimeSpan.FromSeconds(30))
                .WithHttpClientProxy(_httpClientProxyMock.Object);

            return cs;
        }
    }
}