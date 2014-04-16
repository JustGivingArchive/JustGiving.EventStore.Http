using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JustGiving.EventStore.Http.Client.Common.Utils;
using JustGiving.EventStore.Http.Client.Exceptions;
using Newtonsoft.Json;

namespace JustGiving.EventStore.Http.Client
{
    public class EventStoreHttpConnection : IEventStoreHttpConnection
    {
        private readonly ConnectionSettings _settings;
        private readonly IHttpClientProxy _httpClientProxy;
        private readonly string _endpoint;
        private readonly string _connectionName;

        /// <summary>
        /// Creates a new <see cref="IEventStoreHttpConnection"/> to single node using default <see cref="ConnectionSettings"/>
        /// </summary>
        /// <param name="connectionSettings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
        /// <param name="httpClientProxy">Shim to abstract non-mockable httpclient</param>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <returns>a new <see cref="IEventStoreHttpConnection"/></returns>
        public static IEventStoreHttpConnection Create(ConnectionSettings connectionSettings, IHttpClientProxy httpClientProxy, string endpoint, string connectionName = null)
        {
            return new EventStoreHttpConnection(connectionSettings, httpClientProxy, endpoint, connectionName);
        }

        /// <summary>
        /// Creates a new <see cref="IEventStoreHttpConnection"/> to single node using default <see cref="ConnectionSettings"/>
        /// </summary>
        /// <param name="httpClientProxy">Shim to abstract non-mockable httpclient</param>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <returns>a new <see cref="IEventStoreHttpConnection"/></returns>
        public static IEventStoreHttpConnection Create(IHttpClientProxy httpClientProxy, string endpoint, string connectionName = null)
        {
            return new EventStoreHttpConnection(ConnectionSettings.Default, httpClientProxy, endpoint, connectionName);
        }

        /// <summary>
        /// Creates a new <see cref="IEventStoreHttpConnection"/> to single node using specific <see cref="ConnectionSettings"/>
        /// </summary>
        /// <param name="settings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
        /// <param name="httpClientProxy">Shim to abstract non-mockable httpclient</param>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <returns>a new <see cref="IEventStoreHttpConnection"/></returns>
        internal EventStoreHttpConnection(ConnectionSettings settings, IHttpClientProxy httpClientProxy, string endpoint, string connectionName = null)
        {
            Ensure.NotNull(settings, "settings");
            Ensure.NotNull(endpoint, "endpoint");

            _connectionName = connectionName ?? string.Format("ES-{0}", Guid.NewGuid());
            _httpClientProxy = httpClientProxy;
            _settings = settings;
            _endpoint = endpoint;
        }

        public string ConnectionName { get { return _connectionName; } }

        public async Task DeleteStreamAsync(string stream, int expectedVersion)
        {
            await DeleteStreamAsync(stream, expectedVersion, false);
        }

        public async Task DeleteStreamAsync(string stream, int expectedVersion, bool hardDelete)
        {
            using (var client = GetClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, _endpoint + "/streams/" + stream);
                request.Headers.Add("ES-ExpectedVersion", expectedVersion.ToString());

                if (hardDelete)
                {
                    request.Headers.Add("ES-HardDelete", "true");
                }

                var result = await _httpClientProxy.SendAsync(client, request);

                if (!result.IsSuccessStatusCode)
                {
                    throw new EventStoreHttpException(result.Content.ToString(), result.ReasonPhrase, result.StatusCode);
                }
            }
        }

        public async Task AppendToStreamAsync(string stream, int expectedVersion, params NewEventData[] events)
        {
            using (var client = GetClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _endpoint + "/streams/" + stream);

                request.Content = new StringContent(JsonConvert.SerializeObject(events), Encoding.UTF8, "application/json");
                request.Headers.Add("ES-ExpectedVersion", expectedVersion.ToString());
                var result = await _httpClientProxy.SendAsync(client, request);

                if (!result.IsSuccessStatusCode)
                {
                    throw new EventStoreHttpException(result.Content.ToString(), result.ReasonPhrase, result.StatusCode);
                }
            }
        }

        public async Task<EventReadResult> ReadEventAsync(string stream, int position)
        {
            using (var client = GetClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(_endpoint, "/streams/", stream, "/", position== StreamPosition.End ? "head" : position.ToString()));
                var result = await _httpClientProxy.SendAsync(client, request);

                if (result.StatusCode == HttpStatusCode.NotFound)
                {
                    return new EventReadResult(EventReadStatus.NotFound, stream, position, null);
                }

                if (result.StatusCode == HttpStatusCode.Gone)
                {
                    return new EventReadResult(EventReadStatus.StreamDeleted, stream, position, null);
                }

                if (!result.IsSuccessStatusCode)
                {
                    throw new EventStoreHttpException(result.Content.ToString(), result.ReasonPhrase, result.StatusCode);
                }

                var content = await result.Content.ReadAsStringAsync();
                var eventInfo = JsonConvert.DeserializeObject<EventInfo>(content);

                return new EventReadResult(EventReadStatus.Success, stream, position, eventInfo);
            }
        }

        public async Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, int start, int count)
        {
            using (var client = GetClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(_endpoint, "/streams/", stream, "/", start, "/forward/", count));

                var result = await _httpClientProxy.SendAsync(client, request);

                if (result.StatusCode == HttpStatusCode.NotFound)
                {
                    return StreamEventsSlice.StreamNotFound();
                }

                if (result.StatusCode == HttpStatusCode.Gone)
                {
                    return StreamEventsSlice.StreamDeleted();
                }

                if (!result.IsSuccessStatusCode)
                {
                    throw new EventStoreHttpException(await result.Content.ReadAsStringAsync(), result.ReasonPhrase, result.StatusCode);
                }

                var content = await result.Content.ReadAsStringAsync();
                var eventInfo = JsonConvert.DeserializeObject<StreamEventsSlice>(content);
                eventInfo.Status = StreamReadStatus.Success;
                eventInfo.Entries.Reverse();//atom lists things backward

                return eventInfo;
            }
        }

        public HttpClient GetClient()
        {
            var handler = GetHandler();

            var client = new HttpClient(handler, true);

            if (_settings.ConnectionTimeout.HasValue)
            {
                client.Timeout = _settings.ConnectionTimeout.Value;
            }

            return client;
        }

        public HttpClientHandler GetHandler()
        {
            var handler = new HttpClientHandler();

            if (_settings.DefaultUserCredentials != null)
            {
                var defaultCredentials = _settings.DefaultUserCredentials;
                handler.Credentials = new NetworkCredential(defaultCredentials.Username, defaultCredentials.Password);
            }
            return handler;
        }
    }
}