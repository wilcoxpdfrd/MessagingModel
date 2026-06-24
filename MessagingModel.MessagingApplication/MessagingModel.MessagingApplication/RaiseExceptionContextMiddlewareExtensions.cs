using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public static class RaiseExceptionContextMiddlewareExtensions
    {
        class DelegateCollection : KeyedCollection<Type, Delegate>
        {
            protected override Type GetKeyForItem(Delegate item)
            {
                return item.GetType();
            }
        }

        private static readonly DelegateCollection raiseExceptionDelegates = new DelegateCollection();

        public static void Register<TContext>(Func<Exception, Task<ContextMiddlewareDelegate<TContext>>> raiseExceptionDelegate)
        {
            raiseExceptionDelegates.Add(raiseExceptionDelegate);
        }

        public static void Register(Func<Exception, Delegate> raiseExceptionDelegate)
        {
            raiseExceptionDelegates.Add(raiseExceptionDelegate);
        }

        public static Task<ContextMiddlewareDelegate<TContext>> RaiseExceptionContextMiddlewareDelegateAsync<TContext>(this Exception exception)
        {
            return (raiseExceptionDelegates[typeof(TContext)] as Func<Exception, Task<ContextMiddlewareDelegate<TContext>>>)?.Invoke(exception);
        }
    }
}
