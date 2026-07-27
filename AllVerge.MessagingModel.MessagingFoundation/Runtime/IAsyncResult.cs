using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation.Runtime
{
    /// <summary>
    /// Represents the status of an asynchronous operation optimized for a specifc <typeparamref name="AsyncResultType"/>.
    /// </summary>
    /// <typeparam name="AsyncResultType"></typeparam>
    public interface IAsyncResult<AsyncResultType> : IAsyncResult, IDisposable
    {
        /// <summary>
        /// The result type exposing status for the specific asynchronous operation associated with <typeparamref name="AsyncResultType"/>.
        /// </summary>
        AsyncResultType AsyncResult { get;  }
    }
}
