using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;

namespace AllVerge.MessagingModel.MessagingFoundation.Runtime
{
    using AllVerge.SystemPrimitives.Runtime;

    // Can be utilized by subclasses to write core completion code for both the sync and async paths
    // NOTE: requires that "this" is passed in as the state object to the asynchronous sub-call being used with a completion routine.
    public delegate void AsyncWorkCompletion(IAsyncResult result);

    // AsyncWorkResult starts acquired; Complete releases.
    // internal abstract class AsyncWorkResult : AsyncResult
    public abstract class AsyncWorkResult : AsyncResult
    {
        private TimeoutHelper timeouthelper;
        private ManualResetEvent manualResetEvent;
        private bool currentWorkCompleted;
        private bool currentWorkEnded;

        bool endCalled;
        Exception exception;
#if DEBUG
        StackTrace endStack;
#endif
        [Fx.Tag.SynchronizationObject(Blocking = false)]
        object thisLock = new object();

        protected AsyncWorkResult(TimeSpan timeout, AsyncCallback callback, object state) : 
            base(callback, state)
        {
            if (timeout <= TimeSpan.Zero)

                throw new ArgumentException("timeout", "Timeout is expired.");

            this.timeouthelper = new TimeoutHelper(timeout);

            this.manualResetEvent = new ManualResetEvent(false);

            this.OnCompleting = (result, exception) =>
            {
                this.exception = exception; 
                this.EndWaitForCompletion();
            };
        }

        object ThisLock
        {
            get
            {
                return this.thisLock;
            }
        }

        public TimeSpan RemainingTime
        {
            get { return timeouthelper.RemainingTime(); }
        }

        // better name - CurrentWorkWaitHandle
        protected ManualResetEvent InternalWaitHandle
        {
            get { return this.manualResetEvent; }
        }

        protected bool CurrentWorkCompleted { get { return this.currentWorkCompleted; } set { this.currentWorkCompleted = value; if (value) this.InternalWaitHandle.Set(); } }

        protected bool CurrentWorkEnded { get { return this.currentWorkEnded; } set { this.currentWorkEnded = value; } }

        protected bool DefaultCompletionCallback(IAsyncResult result)
        {
            // return true, so that AsynResult will "completeSelf".

            return true;
        }

        //protected new AsyncCallback PrepareAsyncCompletion(AsyncCompletion completion)
        //{
        //    AsyncCompletion asyncCompletion = (r) => completion.Invoke((IAsyncResult)r.AsyncState);

        //    AsyncCallback callback = base.PrepareAsyncCompletion(asyncCompletion);

        //    return (r) => callback(new AsyncWorkCompletionResult(r as AsyncResult));
        //}

        [Fx.Tag.Blocking(Conditional = "!asyncResult.isCompleted")]
        protected static new TAsyncResult End<TAsyncResult>(IAsyncResult result)
            where TAsyncResult : AsyncWorkResult
        {
            if (result == null)
            {
                throw Fx.Exception.ArgumentNull("result");
            }

            TAsyncResult asyncResult = result as TAsyncResult;

            if (asyncResult == null)
            {
                throw Fx.Exception.Argument("result", PublicSR.InvalidAsyncResult);
            }

            if (asyncResult.endCalled)
            {
                throw Fx.Exception.AsError(new InvalidOperationException(PublicSR.AsyncResultAlreadyEnded));
            }

#if DEBUG
            //if (!Fx.FastDebug && asyncResult.endStack == null)
            if (false && asyncResult.endStack == null)
            {
                asyncResult.endStack = new StackTrace();
            }
#endif

            lock (asyncResult.ThisLock)
            {
                if (!asyncResult.IsCompleted)
                {
                    asyncResult.WaitForCompletion();
                }
            }

            asyncResult.End(result);

            return asyncResult;
        }

        protected virtual void WaitForCompletion()
        {
            if (manualResetEvent != null)

                manualResetEvent.WaitOne();
        }

        private void EndWaitForCompletion()
        {
            if (manualResetEvent != null)
            {
                manualResetEvent.Set();
            }
        }

        private void End(IAsyncResult result)
        {
            if (this.exception == null)

                try
                {
                    this.endCalled = OnEnd(result);
                }
                catch (Exception e)
                {
                    this.exception = e;
                }

            manualResetEvent?.Close();

            ThrowIfException();
        }

        protected new void ThrowIfException()
        {
            if (this.exception != null)
            {
                throw Fx.Exception.AsError(this.exception);
            }
        }

        protected virtual bool OnEnd(IAsyncResult result)
        {
            if (!this.IsCompleted)

                throw new TimeoutException();

            return true;
        }

        protected static AsyncCompletion GetAsyncCompletion(AsyncWorkCompletion asyncWorkCompletion, bool completeSelf = true)
        {
            return (r) =>
            {
                asyncWorkCompletion.Invoke(r);

                // return true when there is no callback, so that AsynResult will "completeSelf".

                return completeSelf;
            };
        }
    }
}
