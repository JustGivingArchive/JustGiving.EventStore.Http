using System;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    /// <summary>
    /// Resolves a string representing a Type's FullName into the correcsponding type (Since Type.GetType(string) doesn't work across assemblies)
    /// </summary>
    /// <remarks>It is unlikely you will need to use anything other than <see cref="EventTypeResolver"/></remarks>
    public interface IEventTypeResolver
    {
        /// <summary>
        /// Find a Type corresponding to the FullName
        /// </summary>
        /// <param name="fullName">The Fully qualified (not assembly qualified) name of the type</param>
        /// <returns>The represented type</returns>
        Type Resolve(string fullName);
    }
}