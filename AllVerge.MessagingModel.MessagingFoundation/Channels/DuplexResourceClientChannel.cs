using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Threading;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{

    internal class DuplexResourceClientChannel<T> : ResourceClientChannel<T>// DuplexClientBase<T>
        where T : class
    {
        public DuplexResourceClientChannel(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress) :
            base(callbackInstance, binding, remoteAddress)
        {
        }

        //internal void InvokeServiceAsync(BeginAsyncOperationDelegate beginOperation, object[] args, TimeSpan timeout, Action<AsyncOperationCompletedEventArgs> completedCallBack, object userState, EndAsyncOperationDelegate endOperation)
        //{
        //    if (beginOperation == null)

        //        throw new ArgumentNullException("beginOperation");

        //    if (endOperation == null)

        //        throw new ArgumentNullException("endOperation");

        //    args = new object[] { args, timeout };

        //    BeginOperationDelegate beginOperationDelegate =
        //        (object[] a, AsyncCallback b, Object c) => { return beginOperation((object[])a[0], (TimeSpan)a[1], b, c); };

        //    EndOperationDelegate endOperationDelegate = (IAsyncResult r) => { return endOperation(r); };

        //    SendOrPostCallback callBack = (Object state) =>
        //    {
        //        if (state is InvokeAsyncCompletedEventArgs)
        //        {
        //            InvokeAsyncCompletedEventArgs a =
        //                (InvokeAsyncCompletedEventArgs)state;

        //            if (a.UserState == null)

        //                throw new NullReferenceException("state member UserState.");

        //            Tuple<Action<AsyncOperationCompletedEventArgs>, Object> t =
        //                (Tuple<Action<AsyncOperationCompletedEventArgs>, Object>)a.UserState;

        //            if (t == null)

        //                throw new InvalidOperationException(String.Format("Unexpected type '{0}' of state member UserState.", a.UserState.GetType()));

        //            t.Item1(new AsyncOperationCompletedEventArgs(a.Results, a.Error, a.Cancelled, t.Item2));
        //        }
        //        else

        //            throw new InvalidOperationException(String.Format("Unexpected state type '{0}'.", state.GetType()));
        //    };

        //    if (completedCallBack == null)

        //        throw new ArgumentNullException("completedCallBack");

        //    base.InvokeAsync(beginOperationDelegate, args, endOperationDelegate, callBack, new Tuple<Action<AsyncOperationCompletedEventArgs>, Object>(completedCallBack, userState));
        //}

        internal new T Channel
        {
            get { return base.Channel; }
        }
    }
}
