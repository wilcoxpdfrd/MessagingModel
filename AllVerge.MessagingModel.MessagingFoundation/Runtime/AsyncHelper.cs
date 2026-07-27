using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Runtime
{
    public class AsyncHelper
    {
        public static IAsyncResult BeginAsyncActionWithResult<T, ResultType>(Func<T, TimeSpan, ResultType> action, T arg, TimeSpan timeout)
        {
            return new AsyncFunctionInResult<T, ResultType>(action, arg, timeout);
        }

        public static IAsyncResult BeginAsyncActionWithResult<T, ResultType>(Func<T, TimeSpan, ResultType> action, T arg, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new AsyncFunctionInResult<T, ResultType>(action, arg, timeout, callback, state);
        }

        public static ResultType EndAsyncActionWithResult<T, ResultType>(IAsyncResult result)
        {
            return AsyncFunctionInResult<T, ResultType>.End(result);
        }

        public static IAsyncResult BeginCompletedResult(AsyncCallback callback, object state)
        {
            return new AsyncCompletedResult(callback, state);
        }

        public static void EndCompletedResult(IAsyncResult result)
        {
            AsyncCompletedResult.End(result);
        }

        public static IAsyncResult BeginCompletedResult<ResultType>(ResultType data, AsyncCallback callback, object state)
        {
            return new AsyncCompletedResult<ResultType>(data, callback, state);
        }

        public static ResultType EndCompletedResult<ResultType>(IAsyncResult result)
        {
            return AsyncCompletedResult<ResultType>.End(result);
        }
    }
}
