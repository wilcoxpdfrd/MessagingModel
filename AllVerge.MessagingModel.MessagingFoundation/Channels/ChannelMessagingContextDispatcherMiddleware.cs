using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    using AllVerge.MessagingModel.ChannelMessaging;

    using AllVerge.MessagingModel.MessagingApplication;

    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.SystemPrimitives.Collections;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System.Diagnostics;

    public class ChannelMessagingContextDispatcherMiddleware : 
        AbstractMessagingContextMiddleware<ChannelMessageContext>
    {
        TaskCompletionSource<bool> completionSource =
            new TaskCompletionSource<bool>();

        public ChannelMessagingContextDispatcherMiddleware(MessagingContextMiddlewareDelegate<ChannelMessageContext> next, IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken) : 
            base(next, serviceProvider, loggerFactory, hostEnvironment, cancellationToken)
        {
        }

        protected override void OnInit(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken)
        {
        }

        public override Task<bool> ReadyAsync()
        {
            return base.ReadyAsync().ContinueWith(t => t.Result && !completionSource.Task.IsCompleted);
        }

        protected override async Task OnInvokeAsync(IMessagingContext<ChannelMessageContext> messagingContext)
        {
            IMessageDispatcher<ChannelMessageContext, Message> messageDispatcher =
                new ChannelMessagingContextChannelMessageDispatcher();

            if (messagingContext.Items.TryGetValue(out IChannelDispatcherManager channelDispatcherManager))
            {
                Message receivedMessage = messagingContext.InputContext.Message;

                if (channelDispatcherManager.TryMatchDispatcherOperation(ref receivedMessage, out IDispatcherRuntime dispatcherRuntime))
                {
                    messageDispatcher.Init(dispatcherRuntime);

                    await messageDispatcher.DispatchMessageAsync(messagingContext, receivedMessage).ContinueWith(t =>
                    {
                        messagingContext.Output(ChannelMessageContext.Create(messagingContext.InputContext, t.Result));
                    });
                }
            }
        }
    }
}
