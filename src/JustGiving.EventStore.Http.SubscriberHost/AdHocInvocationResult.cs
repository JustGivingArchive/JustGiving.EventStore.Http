using System;
using System.Collections.Generic;

namespace JustGiving.EventStore.Http.SubscriberHost
{
    public class AdHocInvocationResult
    {
        public AdHocInvocationResultCode ResultCode { get; private set; }

        public IDictionary<Type, Exception> Errors { get; private set; }

        public enum AdHocInvocationResultCode
        {
            Success,
            CouldNotFindEvent,
            NoHandlersFound,
            HandlerThrewException
        }

        public AdHocInvocationResult(AdHocInvocationResultCode resultCode)
        {
            ResultCode = resultCode;
        }

        public AdHocInvocationResult(IDictionary<Type, Exception> errors)
        {
            Errors = errors;
            ResultCode = AdHocInvocationResultCode.HandlerThrewException;
        }
    }
}