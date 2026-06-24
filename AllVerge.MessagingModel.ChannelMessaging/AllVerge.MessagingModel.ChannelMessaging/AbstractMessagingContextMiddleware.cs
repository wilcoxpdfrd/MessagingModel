using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting.Server.Features;

using AllVerge.MessagingModel.MessagingApplication;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public delegate Task<bool> OnHandleMessagingContextDelegate<MessageContext>(MessagingContext<MessageContext> messagingContext) where MessageContext : IMessageContext;

    public abstract class AbstractMessagingContextMiddleware<MessageContext> :
        IMessagingApplicationContextMiddleware<MessageContext> where MessageContext : IMessageContext
    {
        private CancellationToken cancellationToken;
        private MessagingContextMiddlewareDelegate<MessageContext> next;

        protected AbstractMessagingContextMiddleware(MessagingContextMiddlewareDelegate<MessageContext> next, IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken)
        {
            this.next = next;

            this.Init(serviceProvider, loggerFactory, hostEnvironment, cancellationToken);
        }

        protected CancellationToken CancellationToken { get => this.cancellationToken; }

        public void Init(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken)
        {
            OnInit(serviceProvider, loggerFactory, hostEnvironment, cancellationToken);

            this.cancellationToken = cancellationToken;
        }

        protected abstract void OnInit(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken);

        public virtual Task<bool> ReadyAsync()
        {
            return Task.FromResult<bool>(!cancellationToken.IsCancellationRequested);
        }

        public async Task InvokeAsync(IMessagingContext<MessageContext> messagingContext)
        {
            bool ready = await ReadyAsync();

            if (ready)
            {
                messagingContext.EnteringMiddleware();

                await OnInvokeAsync(messagingContext);

                await next(messagingContext);
            }
        }

        /// <summary>
        /// Override to return a Task<bool> whose AsyncState is the <paramref name="messagingContext"/>.
        /// </summary>
        /// <param name="messagingContext"></param>
        /// <returns></returns>
        protected abstract Task OnInvokeAsync(IMessagingContext<MessageContext> messagingContext);
    }
}
