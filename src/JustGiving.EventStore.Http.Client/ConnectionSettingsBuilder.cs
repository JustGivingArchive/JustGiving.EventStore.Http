using System;

namespace JustGiving.EventStore.Http.Client
{
    public class ConnectionSettingsBuilder
    {
        private UserCredentials _defaultUserCredentials;
        private TimeSpan? _connectionTimeout;
        private Action<IEventStoreHttpConnection, Exception> _errorHandler;

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

        public static implicit operator ConnectionSettings(ConnectionSettingsBuilder builder)
        {
            return new ConnectionSettings(builder._defaultUserCredentials, builder._connectionTimeout, builder._errorHandler);
        }
    }
}