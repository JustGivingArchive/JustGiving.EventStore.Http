using System;
using System.Threading.Tasks;
using log4net;

namespace JustGiving.EventStore.Http.Client
{
    public class ConnectionSettingsBuilder
    {
        private UserCredentials _defaultUserCredentials;
        private TimeSpan? _connectionTimeout = TimeSpan.FromSeconds(100);
        private Action<IEventStoreHttpConnection, Exception> _errorHandler;
        private ILog _log;
        private IHttpClientProxy _httpClientProxy = new HttpClientProxy();
        private string _connectionName;

        public ConnectionSettingsBuilder SetDefaultUserCredentials(UserCredentials credentials)
        {
            _defaultUserCredentials = credentials;
            return this;
        }

        public ConnectionSettingsBuilder WithConnectionTimeoutOf(TimeSpan timeout)
        {
            _connectionTimeout = timeout;
            return this;
        }

        public ConnectionSettingsBuilder OnErrorOccured(Action<IEventStoreHttpConnection, Exception> handler)
        {
            _errorHandler = handler;
            return this;
        }

        public ConnectionSettingsBuilder WithLog(ILog log)
        {
            _log = log;
            return this;
        }

        public ConnectionSettingsBuilder WithHttpClientProxy(IHttpClientProxy httpClientProxy)
        {
            _httpClientProxy = httpClientProxy;
            return this;
        }

        public ConnectionSettingsBuilder WithConnectionName(string connectionName)
        {
            _connectionName = connectionName;
            return this;
        }

        public static implicit operator ConnectionSettings(ConnectionSettingsBuilder builder)
        {
            return new ConnectionSettings(builder._defaultUserCredentials, builder._connectionTimeout, builder._errorHandler, builder._log, builder._httpClientProxy, builder._connectionName ?? $"ES-{Guid.NewGuid()}");
        }
    }
}