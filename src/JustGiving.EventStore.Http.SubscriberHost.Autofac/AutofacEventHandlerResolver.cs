using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace JustGiving.EventStore.Http.SubscriberHost.Autofac
{
    public class AutofacEventHandlerResolver : IEventHandlerResolver
    {
        private readonly ILifetimeScope _scope;

        public AutofacEventHandlerResolver(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public IEnumerable GetHandlersOf(Type handlerType)
        {
            var resolveType = typeof (IEnumerable<>).MakeGenericType(handlerType);

            return (IEnumerable) _scope.Resolve(resolveType);
        }
    }
}
