using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public static class RaiseExceptionMessagingContextMiddlewareExtensions
    {
        class DelegateCollection : KeyedCollection<Type, Delegate>
        {
            protected override Type GetKeyForItem(Delegate item)
            {
                return item.GetType();
            }
        }

        private static readonly DelegateCollection raiseExceptionDelegates = new DelegateCollection();

        public static void Register<MessageContext>(Func<Exception, Task<MessagingContextMiddlewareDelegate<MessageContext>>> raiseExceptionDelegate)
        {
            raiseExceptionDelegates.Add(raiseExceptionDelegate);
        }

        public static void Register(Func<Exception, Delegate> raiseExceptionDelegate)
        {
            raiseExceptionDelegates.Add(raiseExceptionDelegate);
        }

        public static Task<MessagingContextMiddlewareDelegate<MessageContext>> RaiseExceptionMessagingContextMiddlewareDelegateAsync<MessageContext>(this Exception exception)
        {
            return (raiseExceptionDelegates[typeof(MessageContext)] as Func<Exception, Task<MessagingContextMiddlewareDelegate<MessageContext>>>)?.Invoke(exception);
        }
    }
}
