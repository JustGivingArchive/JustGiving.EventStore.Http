using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using JustGiving.EventStore.Http.Client;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class EventStreamSubscriber : IEventStreamSubscriber
    {
        private Dictionary<string, Timer> _subscriptions = new Dictionary<string, Timer>(StringComparer.InvariantCultureIgnoreCase);

        private readonly IEventStoreHttpConnection _connection;
        private readonly IEventHandlerResolver _eventHandlerResolver;
        private readonly IStreamPositionRepository _streamPositionRepository;
        private readonly TimeSpan _defaultPollingInterval = TimeSpan.FromSeconds(30);
        private readonly int? _maxConcurrency;
        private readonly int _sliceSize;

        private readonly object _synchroot = new object();

        /// <summary>
        /// Creates a new <see cref="IEventStreamSubscriber"/> to single node using default <see cref="ConnectionSettings"/>
        /// </summary>
        /// <param name="settings">The <see cref="ConnectionSettings"/> to apply to the new connection</param>
        /// <returns>a new <see cref="IEventStreamSubscriber"/></returns>
        public static IEventStreamSubscriber Create(EventStreamSubscriberSettings settings)
        {
            return new EventStreamSubscriber(settings);
        }

        /// <summary>
        /// Creates a new <see cref="IEventStreamSubscriber"/> to single node using default <see cref="ConnectionSettings"/>
        /// </summary>
        /// <returns>a new <see cref="IEventStreamSubscriber"/></returns>
        public static IEventStreamSubscriber Create(IEventStoreHttpConnection connection, IEventHandlerResolver eventHandlerResolver, IStreamPositionRepository streamPositionRepository)
        {
            return new EventStreamSubscriber(EventStreamSubscriberSettings.Default(connection, eventHandlerResolver, streamPositionRepository));
        }

        internal EventStreamSubscriber(EventStreamSubscriberSettings settings)
        {
            _connection = settings.Connection;
            _eventHandlerResolver = settings.EventHandlerResolver;
            _streamPositionRepository = settings.StreamPositionRepository;
            _defaultPollingInterval = settings.DefaultPollingInterval;
            _maxConcurrency = settings.MaxConcurrency;
            _sliceSize = settings.SliceSize;
        }


        public void SubscribeTo(string stream, TimeSpan? pollInterval = null)
        {
            var actualPollInterval = (pollInterval ?? _defaultPollingInterval).TotalMilliseconds;

            lock (_synchroot)
            {
                Timer current;
                if (_subscriptions.TryGetValue(stream, out current))
                {
                    current.Interval = actualPollInterval;
                }
                else
                {
                    current = new Timer(actualPollInterval);
                    _subscriptions.Add(stream, current);
                    current.Start();
                    current.Elapsed += (s,e)=> PollAsync(stream);
                }
            }
        }

        public void UnsubscribeFrom(string stream)
        {
            lock (_synchroot)
            {
                Timer current;
                if (_subscriptions.TryGetValue(stream, out current))
                {
                    current.Stop();
                    current.Dispose();
                    _subscriptions.Remove(stream);
                }
            }
        }

        public async Task PollAsync(string stream)
        {
            var lastPosition = _streamPositionRepository.GetPositionFor(stream)??0;

            var processingBatch = await _connection.ReadStreamEventsForwardAsync(stream, lastPosition + 1, _sliceSize);

            foreach (var message in processingBatch.Entries)
            {
                await InvokeMessageHandlersForMessageAsync(stream, message);
            }
        }

        public async Task InvokeMessageHandlersForMessageAsync(string stream, BasicEventInfo eventInfo)
        {
            var @event = await _connection.ReadEventAsync(stream, eventInfo.SequenceNumber);

            var eventType = Type.GetType(@event.EventInfo.Summary);

            var handlers = _eventHandlerResolver.GetHandlersFor(eventType);

            foreach (var handler in handlers)
            {
                var handlerType = handler.GetType();

                var handleMethod = GetHandleMethod(handlerType, eventType);

                await (Task)handleMethod.Invoke(handler, new[] { @event });
            }

            _streamPositionRepository.SetPositionFor(stream, @eventInfo.SequenceNumber);
        }

        public MethodInfo GetHandleMethod(Type handlerType, Type eventType)
        {
            var @interface = handlerType.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandleEventsOf<>) && x.GetGenericArguments()[0] == eventType); //a type can explicitly implement two IHandle<> interfaces (which would be insane, but will now at least work)

            var handleMethod = @interface.GetMethod("Handle");

            return handleMethod;
        }
    }
}