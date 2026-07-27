using System;
using System.Reflection;
using System.Threading;

using AllVerge.SystemPrimitives.Runtime;

namespace AllVerge.MessagingModel.MessagingFoundation.Runtime
{
    // internal class AsyncFunctionInResult<T, ResultType> : AsyncWorkResult, IAsyncResult<ResultType>
    public class AsyncFunctionInResult<T, ResultType> : AsyncWorkResult, IAsyncResult<ResultType>
    {
        private Func<ResultType> currentAction;
        private ResultType asyncResult;
        private bool disposedValue;

        public AsyncFunctionInResult(Func<T, TimeSpan, ResultType> action, T arg, TimeSpan timeout) : this(timeout, null, null)
        {
            BeginNextAction(action, arg, null);
        }

        public AsyncFunctionInResult(Func<T, TimeSpan, ResultType> action, T arg, TimeSpan timeout, AsyncCallback callback, object state) : this(timeout, callback, state)
        {
            BeginNextAction(action, arg, null);
        }

        protected AsyncFunctionInResult(Func<T, TimeSpan, ResultType> action, T arg, TimeSpan timeout, AsyncCallback callback, object state, AsyncCompletion completionCallback) : this(timeout, callback, state)
        {
            if (completionCallback == null)

                throw new ArgumentNullException("completionCallback", "Asynchronous completion call back must be provided.");

            BeginNextAction(action, arg, completionCallback);
        }

        protected AsyncFunctionInResult(TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
        {
        }

        /// <summary>
        /// Use for AsyncCompletion path
        /// </summary>
        /// <param name="result"></param>
        /// <param name="callback"></param>
        /// <param name="asyncFunctionResult"></param>
        private AsyncFunctionInResult(ResultType result, AsyncCallback callback, Object state) : base(TimeSpan.MaxValue, callback, state)
        {
            this.asyncResult = result;

            this.Complete(false);
        }

        public ResultType AsyncResult { get { base.ThrowIfException(); return this.asyncResult; } }

        protected static void BeginNextAction(IAsyncResult result, Func<T, TimeSpan, ResultType> action, T arg, AsyncCompletion completionCallback)
        {
            AsyncFunctionInResult<T, ResultType> actionResult = (result.AsyncState as AsyncFunctionInResult<T, ResultType>);

            if (actionResult == null)

                throw ExceptionFactory.InvalidAsyncResultException("result");

            actionResult.BeginNextAction(action, arg, completionCallback);
        }

        private void BeginNextAction(Func<T, TimeSpan, ResultType> action, T arg, AsyncCompletion completionCallback)
        {
            if (action == null)

                throw new ArgumentNullException("action", "Action must be provided.");

            this.CurrentWorkCompleted = false;
            this.CurrentWorkEnded = false;

            this.currentAction = delegate ()
            {
                try
                {
                    ResultType result = action.Invoke(arg, this.RemainingTime);

                    this.CurrentWorkCompleted = true;

                    return result;
                }
                catch (Exception e)
                {
                    this.Complete(false, new TargetInvocationException(action.Method.Name, e));
                }

                return default(ResultType);
            };

            if (completionCallback == null)

                completionCallback = new AsyncCompletion(DefaultCompletionCallback);

            if (RuntimeInfo.Framework.IsDotNetCore)
            {
                AsyncCallback callback = PrepareAsyncCompletion(completionCallback);

                ThreadPool.QueueUserWorkItem((a) =>
                {
                    Func<ResultType> _currentAction = (Func<ResultType>)a;

                    this.asyncResult = _currentAction.Invoke();

                    if (!this.IsCompleted)

                        new AsyncFunctionInResult<T, ResultType>(this.asyncResult, callback, this);
                },
                currentAction);
            }
            else

                currentAction.BeginInvoke(PrepareAsyncCompletion(completionCallback), this);
        }

        protected override void WaitForCompletion()
        {
            if (this.AsyncWaitHandle != null)

                this.AsyncWaitHandle.WaitOne(this.RemainingTime);
        }

        protected override bool OnEnd(IAsyncResult result)
        {
            if (result is AsyncFunctionInResult<T, ResultType>)
            {
                base.OnEnd(result);

                return true;
            }

            if (this.CurrentWorkEnded)
            {
                throw ExceptionFactory.AsyncResultAlreadyEndedException();
            }

            if (!this.CurrentWorkCompleted)
            {
                this.InternalWaitHandle.WaitOne(this.RemainingTime);
            }

            if (this.CurrentWorkCompleted)
            {
                if (!RuntimeInfo.Framework.IsDotNetCore)
    
                    this.asyncResult = this.currentAction.EndInvoke(result);

                this.CurrentWorkEnded = true;
            }

            return false;
        }

        public static ResultType End(IAsyncResult result)
        {
            AsyncFunctionInResult<T, ResultType> endedResult = AsyncWorkResult.End<AsyncFunctionInResult<T, ResultType>>(result);

            return endedResult.asyncResult;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AsyncFunctionResult()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
