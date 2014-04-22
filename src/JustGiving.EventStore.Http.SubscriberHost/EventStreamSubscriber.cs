using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JustGiving.EventStore.Http.Client;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class EventStreamSubscriber : IEventStreamSubscriber
    {
        private readonly IEventStoreHttpConnection _connection;
        private readonly IEventHandlerResolver _eventHandlerResolver;
        private readonly IStreamPositionRepository _streamPositionRepository;
        private readonly ISubscriptionTimerManager _subscriptionTimerManager;
        private readonly TimeSpan _defaultPollingInterval;
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
        public static IEventStreamSubscriber Create(IEventStoreHttpConnection connection, IEventHandlerResolver eventHandlerResolver, IStreamPositionRepository streamPositionRepository, ISubscriptionTimerManager subscriptionTimerManager)
        {
            return new EventStreamSubscriber(EventStreamSubscriberSettings.Default(connection, eventHandlerResolver, streamPositionRepository, subscriptionTimerManager));
        }

        internal EventStreamSubscriber(EventStreamSubscriberSettings settings)
        {
            _connection = settings.Connection;
            _eventHandlerResolver = settings.EventHandlerResolver;
            _streamPositionRepository = settings.StreamPositionRepository;
            _subscriptionTimerManager = settings.SubscriptionTimerManager;
            _defaultPollingInterval = settings.DefaultPollingInterval;
            _maxConcurrency = settings.MaxConcurrency;
            _sliceSize = settings.SliceSize;
        }


        public void SubscribeTo(string stream, TimeSpan? pollInterval = null)
        {
            lock (_synchroot)
            {
                _subscriptionTimerManager.Add(stream, pollInterval ?? _defaultPollingInterval, () => PollAsync(stream));
            }
        }

        public void UnsubscribeFrom(string stream)
        {
            lock (_synchroot)
            {
                _subscriptionTimerManager.Remove(stream);
            }
        }

        public async Task PollAsync(string stream)
        {
            lock (_synchroot)
            {
                _subscriptionTimerManager.Pause(stream);
                //we want to be able to cane a stream if we are not up to date, without reading it twice
            }
            var lastPosition = _streamPositionRepository.GetPositionFor(stream) ?? 0;

            var processingBatch = await _connection.ReadStreamEventsForwardAsync(stream, lastPosition + 1, _sliceSize);

            foreach (var message in processingBatch.Entries)
            {
                await InvokeMessageHandlersForMessageAsync(stream, message);
            }

            if (processingBatch.Entries.Any())
            {
                await PollAsync(stream);
            }

            lock (_synchroot)
            {
                _subscriptionTimerManager.Resume(stream);
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

                var handleMethod = GetMethodFromHandler(handlerType, eventType, "Handle");

                try
                {
                    await (Task) handleMethod.Invoke(handler, new[] {@event});
                }
                catch (Exception ex)
                {
                    var errorMethod = GetMethodFromHandler(handlerType, eventType, "OnError");
                    errorMethod.Invoke(handler, new[] { ex });
                }
            }

            lock (_synchroot)
            {
                _streamPositionRepository.SetPositionFor(stream, @eventInfo.SequenceNumber);
            }
        }

        Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();

        public MethodInfo GetMethodFromHandler(Type concreteHandlerType, Type eventType, string methodName)
        {
            MethodInfo result;
            if (methodCache.TryGetValue(concreteHandlerType + "_" + methodName, out result))
            {
                return result;
            }
            
            var @interface = concreteHandlerType.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandleEventsOf<>) && x.GetGenericArguments()[0] == eventType); //a type can explicitly implement two IHandle<> interfaces (which would be insane, but will now at least work)
            result = @interface.GetMethod(methodName);

            methodCache.Add(concreteHandlerType + "_" + methodName, result);

            return result;
        }
    }
}