
using System;
using System.Runtime;

namespace AllVerge.MessagingModel.MessagingFoundation.Runtime
{
    //An AsyncResult that completes as soon as it is instantiated.
    internal class AsyncCompletedResult : AsyncResult
    {
        public AsyncCompletedResult() : this(null, null) { }

        public AsyncCompletedResult(AsyncCallback callback, object state)
            : base(callback, state)
        {
            Complete(true);
        }

        public static void End(IAsyncResult result)
        {
            AsyncResult.End<AsyncCompletedResult>(result);
        }
    }

    internal class AsyncCompletedResult<T> : AsyncResult, IAsyncResult<T>
    {
        T asyncresult;

        public AsyncCompletedResult(T asyncresult, AsyncCallback callback, object state)
            : base(callback, state)
        {
            this.asyncresult = asyncresult;
            Complete(true);
        }

        public T AsyncResult
        {
            get
            {
                base.ThrowIfException();

                return this.asyncresult;
            }
        }

        public static T End(IAsyncResult result)
        {
            AsyncCompletedResult<T> completedResult = global::System.Runtime.AsyncResult.End<AsyncCompletedResult<T>>(result);

            return completedResult.AsyncResult;
        }

        public void Dispose()
        {
        }
    }
}
