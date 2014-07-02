using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class SubscriptionTimerManager : ISubscriptionTimerManager
    {
        private readonly Dictionary<string, Timer> _subscriptions = new Dictionary<string, Timer>(StringComparer.InvariantCultureIgnoreCase);

        public void Add(string stream, TimeSpan pollInterval, Func<Task> handler, Action tickMonitor)
        {
            var actualPollInterval = pollInterval.TotalMilliseconds;

            Timer current;
            if (_subscriptions.TryGetValue(stream, out current))
            {
                current.Interval = actualPollInterval;
            }
            else
            {
                current = new Timer(actualPollInterval);
                current.Start();
                _subscriptions.Add(stream, current);
                current.Elapsed += (s, e) =>
                {
                    handler();
                    tickMonitor();
                };

                Task.Run(handler);
            }
        }

        public void Remove(string stream)
        {
            Timer current;
            if (_subscriptions.TryGetValue(stream, out current))
            {
                current.Stop();
                current.Dispose();
                _subscriptions.Remove(stream);
            }
        }

        public void Pause(string stream)
        {
            Timer current;
            if (_subscriptions.TryGetValue(stream, out current))
            {
                current.Stop();
            }
        }

        public void Resume(string stream)
        {
            Timer current;
            if (_subscriptions.TryGetValue(stream, out current))
            {
                current.Start();
            }
        }
    }
}