using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JustGiving.EventStore.Http.Client;
using JustGiving.EventStore.Http.Client.Common.Utils;
using log4net;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class EventStreamSubscriber : IEventStreamSubscriber
    {
        private readonly IEventStoreHttpConnection _connection;
        private readonly IEventHandlerResolver _eventHandlerResolver;
        private readonly IStreamPositionRepository _streamPositionRepository;
        private readonly ISubscriptionTimerManager _subscriptionTimerManager;
        private readonly IEventTypeResolver _eventTypeResolver;
        private readonly ILog _log;
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
        public static IEventStreamSubscriber Create(IEventStoreHttpConnection connection, IEventHandlerResolver eventHandlerResolver, IStreamPositionRepository streamPositionRepository)
        {
            return new EventStreamSubscriber(EventStreamSubscriberSettings.Default(connection, eventHandlerResolver, streamPositionRepository));
        }

        internal EventStreamSubscriber(EventStreamSubscriberSettings settings)
        {
            _connection = settings.Connection;
            _eventHandlerResolver = settings.EventHandlerResolver;
            _streamPositionRepository = settings.StreamPositionRepository;
            _subscriptionTimerManager = settings.SubscriptionTimerManager;
            _eventTypeResolver = settings.EventTypeResolver;
            _defaultPollingInterval = settings.DefaultPollingInterval;
            _maxConcurrency = settings.MaxConcurrency;
            _sliceSize = settings.SliceSize;
            _log = settings.Log;
        }


        public void SubscribeTo(string stream, TimeSpan? pollInterval = null)
        {
            lock (_synchroot)
            {
                var interval = pollInterval ?? _defaultPollingInterval;
                Log.Info(_log, "Subscribing to {0} with an interval of {1}", stream, interval);
                _subscriptionTimerManager.Add(stream, interval, () => PollAsync(stream));
                Log.Info(_log, "Subscribed to {0} with an interval of {1}", stream, interval);
            }
        }

        public void UnsubscribeFrom(string stream)
        {
            lock (_synchroot)
            {
                Log.Info(_log, "Unsubscribing from {0}", stream);
                _subscriptionTimerManager.Remove(stream);
                Log.Info(_log, "Unsubscribed from {0}", stream);
            }
        }

        public async Task PollAsync(string stream)
        {
            Log.Info(_log, "Begin polling {0}", stream);
            lock (_synchroot)
            {
                _subscriptionTimerManager.Pause(stream);
                //we want to be able to cane a stream if we are not up to date, without reading it twice
            }
            var lastPosition = _streamPositionRepository.GetPositionFor(stream) ?? 0;

            Log.Info(_log, "Last position for stream {0} was {1}", stream, lastPosition);

            Log.Info(_log, "Begin reading event metadata for {0}", stream);
            var processingBatch = await _connection.ReadStreamEventsForwardAsync(stream, lastPosition + 1, _sliceSize);
            Log.Info(_log, "Finished reading event metadata for {0}", stream);

            Log.Info(_log, "Processing {0} events for {1}", processingBatch.Entries.Count, stream);
            foreach (var message in processingBatch.Entries)
            {
                Log.Info(_log, "Processing event {0} from {1}", message.Id);
                await InvokeMessageHandlersForStreamMessageAsync(stream, message);
                Log.Info(_log, "Processed event {0} from {1}", message.Id);
            }

            if (processingBatch.Entries.Any())
            {
                Log.Info(_log, "New items in stream {0} were found; repolling", stream);
                await PollAsync(stream);
                return;
            }

            lock (_synchroot)
            {
                _subscriptionTimerManager.Resume(stream);
            }
            Log.Info(_log, "Finished polling {0}", stream);
        }

        public async Task InvokeMessageHandlersForStreamMessageAsync(string stream, BasicEventInfo eventInfo)
        {
            var @event = await _connection.ReadEventAsync(stream, eventInfo.SequenceNumber);

            await InvokeMessageHandlersForEventMessageAsync(stream, @event);
        }

        public async Task InvokeMessageHandlersForEventMessageAsync(string stream, EventReadResult eventReadResult)
        {
            var eventType = _eventTypeResolver.Resolve(eventReadResult.EventInfo.Summary);
            var handlers = _eventHandlerResolver.GetHandlersFor(eventType);

            foreach (var handler in handlers)
            {
                var handlerType = handler.GetType();

                var handleMethod = GetMethodFromHandler(handlerType, eventType, "Handle");

                if (handleMethod == null)
                {
                    continue;
                }

                try
                {
                    var eventJObject = eventReadResult.EventInfo.Content.Data;
                    var @event = eventJObject == null ? null : @eventJObject.ToObject(eventType);

                    try
                    {
                        await (Task) handleMethod.Invoke(handler, new[] {@event});
                    }
                    catch (Exception invokeException)
                    {
                        var errorMessage = string.Format("{0} thrown processing event {1}", invokeException.GetType().FullName, @eventReadResult.EventInfo.Id);
                        Log.Error(_log, errorMessage, invokeException);

                        var errorMethod = GetMethodFromHandler(handlerType, eventType, "OnError");
                        errorMethod.Invoke(handler, new[] { invokeException, @event });
                    }
                }
                catch (Exception deserialisationException)
                {
                    var errorMessage = string.Format("{0} thrown rehydrating event {1}", deserialisationException.GetType().FullName, @eventReadResult.EventInfo.Id);
                    Log.Error(_log, errorMessage, deserialisationException);
                }
            }

            lock (_synchroot)
            {
                Log.Info(_log, "Storing last read event for {0} as {1}", stream, eventReadResult.SequenceNumber);
                _streamPositionRepository.SetPositionFor(stream, eventReadResult.SequenceNumber);
            }
        }


        Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();

        public MethodInfo GetMethodFromHandler(Type concreteHandlerType, Type eventType, string methodName)
        {
            MethodInfo result;
            if (methodCache.TryGetValue(concreteHandlerType + methodName, out result))
            {
                return result;
            }
            
            var @interface = concreteHandlerType.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandleEventsOf<>) && x.GetGenericArguments()[0] == eventType); //a type can explicitly implement two IHandle<> interfaces (which would be insane, but will now at least work)

            if (@interface == null)
            {
                Log.Warning(_log, "{0}, which handles {1} did not contain a suitable method named {2}", concreteHandlerType.FullName, eventType.FullName, methodName);
                methodCache.Add(concreteHandlerType + methodName, result);
            }


            result = @interface.GetMethod(methodName);

            methodCache.Add(concreteHandlerType + methodName, result);

            return result;
        }
    }
}