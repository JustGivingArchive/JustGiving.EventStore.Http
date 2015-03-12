using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class EventTypeResolver : IEventTypeResolver
    {
        private Dictionary<string, Type> cache = new Dictionary<string,Type>();

        public Type Resolve(string eventType)
        {
            Type result;
            if (cache.TryGetValue(eventType, out result))
            {
                return result;
            }

            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var match = assembly.GetTypes().FirstOrDefault(x => x.FullName == eventType || x.GetCustomAttributes<BindsToAttribute>().Any(a=>a.EventType==eventType));
                    if (match != null)
                    {
                        cache[eventType] = match;
                        return match;
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                return null;
            }

            cache[eventType] = null;
            return null;
        }
    }
}