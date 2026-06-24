using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    public delegate IAsyncResult BeginParameterizedOperationDelegate(object[] args, TimeSpan timeout, AsyncCallback asyncCallback, object state);
    public delegate object[] EndOperationWithResultsDelegate(IAsyncResult asyncResult);

    public class AsyncOperationCompletedEventArgs : AsyncCompletedEventArgs
    {
        private object[] results;

        /// <summary>Gets the results from an asynchronous operation.</summary>
        /// <returns>An array of <see cref="T:System.Object" /> that contains the results from an asynchronous operation.</returns>
        public object[] Results
        {
            get
            {
                return this.results;
            }
        }

        internal AsyncOperationCompletedEventArgs(object[] results, Exception error, bool cancelled, object userState) : base(error, cancelled, userState)
        {
            this.results = results;
        }
    }

    class ResourceClientChannel<T> : ResourceClientBase<T>
        where T : class
    {
        public ResourceClientChannel(Binding binding, EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        {
        }

        protected ResourceClientChannel(InstanceContext instanceContext, Binding binding, EndpointAddress remoteAddress) :
            base(instanceContext, binding, remoteAddress)
        {
        }

        public void InvokeServiceAsync(BeginParameterizedOperationDelegate beginOperation, object[] args, TimeSpan timeout, Action<AsyncOperationCompletedEventArgs> completedCallBack, object userState, EndOperationWithResultsDelegate endOperation)
        {
            if (beginOperation == null)

                throw new ArgumentNullException("beginOperation");

            if (endOperation == null)

                throw new ArgumentNullException("endOperation");

            args = new object[] { args, timeout };

            BeginOperationDelegate beginOperationDelegate =
                (object[] _args, AsyncCallback _callback, Object _state) => { return beginOperation((object[])_args[0], (TimeSpan)_args[1], _callback, _state); };

            EndOperationDelegate endOperationDelegate = (IAsyncResult r) => { return endOperation(r); };

            SendOrPostCallback callBack = (Object state) =>
            {
                if (state is InvokeAsyncCompletedEventArgs)
                {
                    InvokeAsyncCompletedEventArgs a =
                        (InvokeAsyncCompletedEventArgs)state;

                    if (a.UserState == null)

                        throw new NullReferenceException("state member UserState.");

                    Tuple<Action<AsyncOperationCompletedEventArgs>, Object> t =
                        (Tuple<Action<AsyncOperationCompletedEventArgs>, Object>)a.UserState;

                    if (t == null)

                        throw new InvalidOperationException(String.Format("Unexpected type '{0}' of state member UserState.", a.UserState.GetType()));

                    t.Item1(new AsyncOperationCompletedEventArgs(a.Results, a.Error, a.Cancelled, t.Item2));
                }
                else

                    throw new InvalidOperationException(String.Format("Unexpected state type '{0}'.", state.GetType()));
            };

            if (completedCallBack == null)

                throw new ArgumentNullException("completedCallBack");

            base.InvokeAsync(beginOperationDelegate, args, endOperationDelegate, callBack, new Tuple<Action<AsyncOperationCompletedEventArgs>, Object>(completedCallBack, userState));
        }

        internal new T Channel
        {
            get { return base.Channel; }
        }
    }
}
