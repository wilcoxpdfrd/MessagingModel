using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using AllVerge.MessagingModel.MessagingApplication;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Controllers
{
    public abstract class BaseController<MessageContext> : IDisposable
    {
        private Func<IMessagingContext<MessageContext>, RejectCode, IDictionary<RejectHeaders, StringValues>, Task> prepareRejectedMessagingContext;
        private Func<IMessagingContext<MessageContext>, Action, Action, Task> receivedMessagingContext;
        private bool disposedValue;

        protected BaseController(ILogger logger, Func<IMessagingContext<MessageContext>, RejectCode, IDictionary<RejectHeaders, StringValues>, Task> prepareRejectedMessagingContext, Func<IMessagingContext<MessageContext>, Action, Action, Task> receivedMessagingContext, CancellationToken cancellationToken)
        {
            this.Logger = logger;
            this.CancellationToken = cancellationToken;
            this.prepareRejectedMessagingContext = prepareRejectedMessagingContext;
            this.receivedMessagingContext = receivedMessagingContext;
        }

        protected ILogger Logger { get; }
        protected CancellationToken CancellationToken { get; }
        protected Func<IMessagingContext<MessageContext>, RejectCode, IDictionary<RejectHeaders, StringValues>, Task> PrepareRejectedMessagingContext => this.prepareRejectedMessagingContext;
        protected Func<IMessagingContext<MessageContext>, Action, Action, Task> ReceivedMessagingContext => this.receivedMessagingContext;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    OnDispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        protected abstract void OnDispose();

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BaseController()
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
