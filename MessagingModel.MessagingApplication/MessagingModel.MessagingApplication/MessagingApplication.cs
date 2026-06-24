using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;

namespace AllVerge.MessagingModel.MessagingApplication
{
    using AllVerge.MessagingModel.MessagingApplication.Builder;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class MessagingApplication<MessagingContextMiddleware, MessageContext>
        where MessagingContextMiddleware : IMessagingApplicationContextMiddleware<MessageContext>, new()
    {
        private readonly MessagingContextMiddlewareDelegate<MessageContext> _next;
        MessagingContextMiddleware messagingContextMiddleware;

        public MessagingApplication(MessagingContextMiddlewareDelegate<MessageContext> nextMessagingContextMiddleware, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IHostEnvironment hostEnvironment, CancellationToken cancellationToken)
        {
            this._next = nextMessagingContextMiddleware;

            this.messagingContextMiddleware = new MessagingContextMiddleware();

            this.messagingContextMiddleware.Init(serviceProvider, loggerFactory, hostEnvironment, cancellationToken);
        }

        public async Task Invoke(IMessagingContext<MessageContext> messagingContext)
        {
            if (messagingContext.Result == MiddlewarePipelineResult.NotHandled)
            {
                bool isReady = await messagingContextMiddleware.ReadyAsync();

                if (isReady)
                    await messagingContextMiddleware.InvokeAsync(messagingContext);
                else
                    messagingContext.Output(messagingContext.InputContext, MiddlewarePipelineResult.TooBusy);
            }

            await _next(messagingContext);
        }
    }

    public class MessagingApplication<ApplicationMessagingContextMiddleware, ProtocolContext, MessageContext> 
        where ApplicationMessagingContextMiddleware : IMessagingApplicationContextMiddleware<MessageContext>, new()
    {
        private readonly MessagingContextMiddlewareDelegate<MessageContext> _next;
        ApplicationMessagingContextMiddleware messagingContextMiddleware;

        public MessagingApplication(MessagingContextMiddlewareDelegate<MessageContext> nextMessagingContextMiddleware, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IHostEnvironment hostEnvironment, CancellationToken cancellationToken)
        {
            this._next = nextMessagingContextMiddleware;

            this.messagingContextMiddleware = new ApplicationMessagingContextMiddleware();

            messagingContextMiddleware.Init(serviceProvider, loggerFactory, hostEnvironment, cancellationToken);
        }

        public async Task Invoke(IMessagingContext<MessageContext> MessageContext)
        {
            if (MessageContext.Result == MiddlewarePipelineResult.NotHandled)
            {
                bool isReady = await messagingContextMiddleware.ReadyAsync();

                if (isReady)
                    await messagingContextMiddleware.InvokeAsync(MessageContext);
                else
                    MessageContext.Output(MessageContext.InputContext, MiddlewarePipelineResult.TooBusy);
            }

            await _next(MessageContext);
        }
    }

    public static class MessagingApplicationExtensions
    {
        public static IMessagingApplicationBuilder<ProtocolContext, MessageContext> Use<ProtocolContext, MessageContext>(this IMessagingApplicationBuilder<ProtocolContext, MessageContext> app, Func<ContextMiddlewareDelegate<ProtocolContext>, ContextMiddlewareDelegate<ProtocolContext>> middlewareComponent)
        {
            return app.Use(middlewareComponent);
        }

        public static IMessagingApplicationBuilder<ProtocolContext, MessageContext> UseMessagingApplication<ApplicationMessagingContextMiddleware, ProtocolContext, MessageContext>(this IMessagingApplicationBuilder<ProtocolContext, MessageContext> app, CancellationToken cancellationToken) where ApplicationMessagingContextMiddleware : IMessagingApplicationContextMiddleware<MessageContext>, new()
        {
            return app.UseMessagingApplication<MessagingApplication<ApplicationMessagingContextMiddleware, ProtocolContext, MessageContext>, ProtocolContext, MessageContext>(app.ServerFeatures.Get<IServerAddressesFeature>(), cancellationToken);
        }

        public static IMessagingApplicationBuilder<MessageContext> UseMessagingApplication<ApplicationMessagingContextMiddleware, MessageContext>(this IMessagingApplicationBuilder<MessageContext> app, CancellationToken cancellationToken) where ApplicationMessagingContextMiddleware : IMessagingApplicationContextMiddleware<MessageContext>, new()
        {
            return app.UseMessagingApplication<MessagingApplication<ApplicationMessagingContextMiddleware, MessageContext>, MessageContext>(app.ServerFeatures.Get<IServerAddressesFeature>(), cancellationToken);
        }
    }
}
