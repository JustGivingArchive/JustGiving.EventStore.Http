using System;
using log4net;

namespace JustGiving.EventStore.Http.Client
{
    public sealed class ConnectionSettings
    {
        public ConnectionSettings(UserCredentials defaultUserCredentials, TimeSpan? connectionTimeout, Action<IEventStoreHttpConnection, Exception> errorHandler, ILog log)
        {
            DefaultUserCredentials = defaultUserCredentials;
            ConnectionTimeout = connectionTimeout;
            ErrorHandler = errorHandler;
            Log = log;
        }

        //An implicit cast is going on....
        private static readonly Lazy<ConnectionSettings> DefaultSettings = new Lazy<ConnectionSettings>(()=>Create(), true);

        /// <summary>
        /// The default <see cref="ConnectionSettings"></see>
        /// </summary>
        public static ConnectionSettings Default { get { return DefaultSettings.Value; } }

        /// <summary>
        /// Creates a new set of <see cref="ConnectionSettings"/>
        /// </summary>
        /// <returns>A <see cref="ConnectionSettingsBuilder"/> that can be used to build up an <see cref="EventStoreHttpConnection"/></returns>
        public static ConnectionSettingsBuilder Create()
        {
            return new ConnectionSettingsBuilder().WithConnectionTimeoutOf(TimeSpan.FromSeconds(100));
        }

        public UserCredentials DefaultUserCredentials { get; private set; }
        public TimeSpan? ConnectionTimeout { get; private set; }
        public Action<IEventStoreHttpConnection, Exception> ErrorHandler { get; private set; }
        public ILog Log { get; private set; }
    }
}