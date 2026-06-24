using System;
using System.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.MessagingModel.ChannelMessaging.Channels;
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    using Microsoft.Extensions.Logging;

    internal class ZeroMQChannelMessagingContextAsyncChannel :
        AbstractAsynchronousRequestResponseMessagingContextChannel<ChannelMessageContext>,
        IBindingContextMapper<ZeroMQProtocolContext>
    {
        private ZeroMQAsynchronousMessagingContextChannelListener contextChannelListener;
        private ZeroMQProtocolContext protocolContext;
        private ILogger logger;
        private CancellationToken cancellationToken;
        private int request;

        public ZeroMQChannelMessagingContextAsyncChannel(ZeroMQAsynchronousMessagingContextChannelListener contextChannelListener, ZeroMQProtocolContext protocolContext) : base()
        {
            this.contextChannelListener = contextChannelListener;
            this.protocolContext = protocolContext;
            this.ListenUri = new Uri(this.contextChannelListener.ListenAddresses.First());
            this.logger = contextChannelListener.Logger;
            this.cancellationToken = contextChannelListener.CancellationToken;
            this.Open();
        }

        public override async Task<IMessagingContext<ChannelMessageContext>> ReceiveMessagingContextAsync()
        {
            ZeroMQProtocolContext context = null;

            Message receivedMessage = await Task.FromResult((Message)null);

            ChannelMessageContext receivedMessagingContext = ChannelMessageContext.Create(context, receivedMessage, DateTime.Now);

            BindingContext bindingContext = new BindingContext();

            MapToBindingContext(context, bindingContext);

            IMessagingContext<ChannelMessageContext> messagingContext =
                new MessagingContext<ChannelMessageContext>(bindingContext);

            messagingContext.Input(receivedMessagingContext);

            return messagingContext;
        }

        public override Task<IMessagingContext<ChannelMessageContext>> TryReceiveMessagingContextAsync(TimeSpan timeout)
        {
            return this.ReceiveMessagingContextAsync();
        }

        public override Task HandledMessagingCallBackAsync(IMessagingContext<ChannelMessageContext> messagingContext)
        {
            Console.WriteLine($"Handled: {messagingContext.BindingContext.InteractionContext.TraceIdentifier} from {messagingContext.BindingContext.InteractionContext.InputLocation} with {messagingContext.Result}");

            return Task.CompletedTask;
        }

        public override Task SendMessagingContextAsync(ChannelMessageContext messagingContext)
        {
            Console.WriteLine($"Sent: {messagingContext}");

            return Task.CompletedTask;
        }

        public override Task TrySendMessagingContextAsync(ChannelMessageContext messagingContext, TimeSpan timeout)
        {
            return SendMessagingContextAsync(messagingContext);
        }

        public bool MapToBindingContext(ZeroMQProtocolContext context, BindingContext bindingContext)
        {
            return this.contextChannelListener.MapToBindingContext(context, bindingContext);
        }

        protected override Task OnCloseAsync(TimeSpan timeSpan)
        {
            return Task.CompletedTask;
        }

        protected override void OnAbort()
        {
        }
    }
}