using System;
using log4net;

namespace JustGiving.EventStore.Http.Client
{
    public sealed class ConnectionSettings
    {
        public ConnectionSettings(UserCredentials defaultUserCredentials, TimeSpan? connectionTimeout, Action<IEventStoreHttpConnection, Exception> errorHandler, ILog log, IHttpClientProxy httpClientProxy, string connectionName)
        {
            DefaultUserCredentials = defaultUserCredentials;
            ConnectionTimeout = connectionTimeout;
            ErrorHandler = errorHandler;
            Log = log;
            HttpClientProxy = httpClientProxy;
            ConnectionName = connectionName;
        }

        //An implicit cast is going on....
        private static readonly Lazy<ConnectionSettings> DefaultSettings = new Lazy<ConnectionSettings>(()=>Create(), true);

        /// <summary>
        /// The default <see cref="ConnectionSettings"></see>
        /// </summary>
        public static ConnectionSettings Default
        {
            get
            {
                return DefaultSettings.Value;
            } 
        }

        /// <summary>
        /// Creates a new set of <see cref="ConnectionSettings"/>
        /// </summary>
        /// <returns>A <see cref="ConnectionSettingsBuilder"/> that can be used to build up an <see cref="EventStoreHttpConnection"/></returns>
        public static ConnectionSettingsBuilder Create()
        {
            return new ConnectionSettingsBuilder();
        }

        public UserCredentials DefaultUserCredentials { get;}
        public TimeSpan? ConnectionTimeout { get;}
        public Action<IEventStoreHttpConnection, Exception> ErrorHandler { get;}
        public ILog Log { get; private set; }
        public IHttpClientProxy HttpClientProxy { get;}
        public string ConnectionName { get;}
    }
}