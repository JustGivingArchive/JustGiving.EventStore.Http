using System;
using System.Collections.Generic;
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
        private readonly string _endpoint;
        private readonly string _connectionName;

        public HttpClientHandler _TestingClientHandler { private get; set; }

        /// <summary>
        /// Creates a new <see cref="IEventStoreHttpConnection"/> to single node using default <see cref="ConnectionSettings"/>
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <returns>a new <see cref="IEventStoreHttpConnection"/></returns>
        public static IEventStoreHttpConnection Create(string endpoint, string connectionName = null)
        {
            return Create(ConnectionSettings.Default, endpoint, connectionName);
        }

        /// <summary>
        /// Creates a new <see cref="IEventStoreHttpConnection"/> to single node using specific <see cref="ConnectionSettings"/>
        /// </summary>
        /// <param name="settings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="connectionName">Optional name of connection (will be generated automatically, if not provided)</param>
        /// <returns>a new <see cref="IEventStoreHttpConnection"/></returns>
        public static IEventStoreHttpConnection Create(ConnectionSettings settings, string endpoint, string connectionName = null)
        {
            Ensure.NotNull(settings, "settings");
            Ensure.NotNull(endpoint, "endpoint");
            return new EventStoreHttpConnection(settings, endpoint, connectionName);
        }

        internal EventStoreHttpConnection(ConnectionSettings settings, string endpoint, string connectionName)
        {
            _connectionName = connectionName ?? string.Format("ES-{0}", Guid.NewGuid());
            _settings = settings;
            _endpoint = endpoint;
        }

        public string ConnectionName { get { return _connectionName; } }

        public async Task DeleteStreamAsync(string stream, int expectedVersion, UserCredentials userCredentials = null)
        {
            await DeleteStreamAsync(stream, expectedVersion, false, userCredentials);
        }

        public async Task DeleteStreamAsync(string stream, int expectedVersion, bool hardDelete, UserCredentials userCredentials = null)
        {
            using (var client = GetClient(userCredentials))
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, _endpoint + "/streams/" + stream);
                request.Headers.Add("Content-Type", "application/json");
                request.Headers.Add("ES-ExpectedVersion", expectedVersion.ToString());

                if (hardDelete)
                {
                    request.Headers.Add("ES-HardDelete", "true");
                }

                var result = await client.SendAsync(request);

                if (!result.IsSuccessStatusCode)
                {
                    throw new EventStoreHttpException(result.Content.ToString(), result.ReasonPhrase, result.StatusCode);
                }
            }
        }

        public async Task AppendToStreamAsync(string stream, int expectedVersion, params NewEventData[] events)
        {
            await AppendToStreamAsync(stream, expectedVersion, events, null);
        }

        public async Task AppendToStreamAsync(string stream, int expectedVersion, UserCredentials userCredentials, params NewEventData[] events)
        {
            await AppendToStreamAsync(stream, expectedVersion, events, userCredentials);
        }

        public async Task AppendToStreamAsync(string stream, int expectedVersion, IEnumerable<NewEventData> events, UserCredentials userCredentials = null)
        {

            using (var client = GetClient(userCredentials))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _endpoint + "/streams/" + stream);

                request.Content = new StringContent(JsonConvert.SerializeObject(events), Encoding.UTF8, "application/json");
                request.Headers.Add("ES-ExpectedVersion", expectedVersion.ToString());
                var result = await client.SendAsync(request);

                if (!result.IsSuccessStatusCode)
                {
                    throw new EventStoreHttpException(result.Content.ToString(), result.ReasonPhrase, result.StatusCode);
                }
            }
        }

        public async Task<EventReadResult> ReadEventAsync(string stream, int position, UserCredentials userCredentials = null)
        {
            using (var client = GetClient(userCredentials))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(_endpoint, "/streams/", stream, "/", position== StreamPosition.End ? "head" : position.ToString()));
                
                var result = await client.SendAsync(request);

                if (!result.IsSuccessStatusCode)
                {
                    throw new EventStoreHttpException(result.Content.ToString(), result.ReasonPhrase, result.StatusCode);
                }

                var content = await result.Content.ReadAsStringAsync();
                var eventInfo = JsonConvert.DeserializeObject<EventInfo>(content);

                return new EventReadResult(EventReadStatus.Success, stream, position, eventInfo);
            }
        }

        public async Task<StreamEventsSlice> ReadStreamEventsForwardAsync(string stream, int start, int count, UserCredentials userCredentials = null)
        {
            using (var client = GetClient(userCredentials))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, string.Concat(_endpoint, "/streams/", stream, "/", start, "/forward/", count));

                var result = await client.SendAsync(request);

                if (!result.IsSuccessStatusCode)
                {
                    throw new EventStoreHttpException(await result.Content.ReadAsStringAsync(), result.ReasonPhrase, result.StatusCode);
                }

                var content = await result.Content.ReadAsStringAsync();
                var eventInfo = JsonConvert.DeserializeObject<StreamEventsSlice>(content);

                eventInfo.Entries.Reverse();//atom lists things backward

                return eventInfo;
            }
        }

        public HttpClient GetClient(UserCredentials userCredentials)
        {
            var client = new HttpClient(GetHandlerForCredentials(userCredentials), true);

            if (_settings.ConnectionTimeout.HasValue)
            {
                client.Timeout = _settings.ConnectionTimeout.Value;
            }

            return client;
        }

        public HttpClientHandler GetHandlerForCredentials(UserCredentials credentials)
        {
            var handler = _TestingClientHandler ?? new HttpClientHandler();
            if (credentials != null)
            {
                handler.Credentials = new NetworkCredential(credentials.Username, credentials.Password);
            }
            else if (_settings.DefaultUserCredentials != null)
            {
                var defaultCredentials = _settings.DefaultUserCredentials;
                handler.Credentials = new NetworkCredential(defaultCredentials.Username, defaultCredentials.Password);
            }
            return handler;
        }
    }
}