using System;
using System.Collections.Generic;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    /// <summary>
    /// Compares Types resulting in the most derived being at the top
    /// </summary>
    public class TypeInheritanceComparer : IComparer<Type>
    {
        public int Compare(Type x, Type y)
        {
            if (x == y)
            {
                return 0;
            }

            if (x.IsAssignableFrom(y))
            {
                return 1;
            }

            return -1;
        }
    }
}