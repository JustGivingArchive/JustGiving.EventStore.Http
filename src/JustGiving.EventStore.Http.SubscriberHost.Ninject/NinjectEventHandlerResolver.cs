using System;
using System.Collections;
using Ninject;

namespace JustGiving.EventStore.Http.SubscriberHost.Ninject
{
    public class NinjectEventHandlerResolver : IEventHandlerResolver
    {
        private readonly IKernel _kernel;

        public NinjectEventHandlerResolver(IKernel kernel)
        {
            _kernel = kernel;
        }

        public IEnumerable GetHandlersOf(Type eventType)
        {
            return _kernel.GetAll(eventType);
        }
    }
}