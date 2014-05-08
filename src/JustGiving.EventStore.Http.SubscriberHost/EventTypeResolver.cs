using System;
using System.Collections.Generic;
using System.Linq;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class EventTypeResolver : IEventTypeResolver
    {
        private Dictionary<string, Type> cache = new Dictionary<string,Type>();

        public Type Resolve(string fullName)
        {
            Type result;
            if (cache.TryGetValue(fullName, out result))
            {
                return result;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var match = assembly.GetTypes().FirstOrDefault(x => x.FullName == fullName);
                if (match != null)
                {
                    cache[fullName] = match;
                    return match;
                }
            }

            return null;
        }
    }
}