using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class SubscriptionTimerManager : ISubscriptionTimerManager
    {
        private readonly Dictionary<string, Timer> _subscriptions = new Dictionary<string, Timer>();

        public void Add(string stream, string subscriberId, TimeSpan pollInterval, Func<Task> handler, Action streamIntervalMonitor)
        {
            var actualPollInterval = pollInterval.TotalMilliseconds;

            var timerKey = TimerKeyFor(stream, subscriberId);
            Timer current;
            if (_subscriptions.TryGetValue(timerKey, out current))
            {
                current.Interval = actualPollInterval;
            }
            else
            {
                current = new Timer(actualPollInterval);
                current.Start();
                _subscriptions.Add(timerKey, current);
                current.Elapsed += (s, e) =>
                {
                    handler();
                    streamIntervalMonitor();
                };

                Task.Run(handler);
            }
        }

        public void Remove(string stream, string subscriberId)
        {
            var timerKey = TimerKeyFor(stream, subscriberId);
            Timer current;
            if (_subscriptions.TryGetValue(timerKey, out current))
            {
                current.Stop();
                current.Dispose();
                _subscriptions.Remove(timerKey);
            }
        }

        public void Pause(string stream, string subscriberId)
        {
            var timerKey = TimerKeyFor(stream, subscriberId);
            Timer current;
            if (_subscriptions.TryGetValue(timerKey, out current))
            {
                current.Stop();
            }
        }

        public void Resume(string stream, string subscriberId)
        {
            var timerKey = TimerKeyFor(stream, subscriberId);
            Timer current;
            if (_subscriptions.TryGetValue(timerKey, out current))
            {
                current.Start();
            }
        }

        private string TimerKeyFor(string stream, string subscriberId)
        {
            return string.IsNullOrEmpty(subscriberId) ? stream : string.Concat(stream, EventStreamSubscriber.StreamIdentifierSeparator, subscriberId);
        }

        public IEnumerable<StreamSubscription> GetSubscriptions()
        {
            foreach (var subscription in _subscriptions)
            {
                var interval = TimeSpan.FromMilliseconds(subscription.Value.Interval);
                var subscriptionParts = subscription.Key.Split(new[] {EventStreamSubscriber.StreamIdentifierSeparator}, StringSplitOptions.RemoveEmptyEntries);

                yield return new StreamSubscription(subscriptionParts[0], subscriptionParts.Length>1?subscriptionParts[1]:null, interval, subscription.Value.Enabled);
            }
        }
    }
}