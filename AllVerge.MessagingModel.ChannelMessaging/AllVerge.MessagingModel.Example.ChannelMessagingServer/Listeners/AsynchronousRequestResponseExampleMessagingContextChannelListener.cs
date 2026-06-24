using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using AllVerge.MessagingModel.ChannelMessaging;
using AllVerge.MessagingModel.ChannelMessaging.Channels;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer.Listeners
{
    internal class AsynchronousRequestResponseExampleMessagingContextChannelListener : 
        MessagingContextChannelListener<ExampleMessageContext>
    {
        private class ExampleMessagingContextAsyncChannel :
            AbstractAsynchronousRequestResponseMessagingContextChannel<ExampleMessageContext>,
            IBindingContextMapper<ExampleMessageContext>
        {
            private ILogger logger;
            private CancellationToken cancellationToken;
            private int request;
            private IBindingContextMapper<ExampleMessageContext> protocolContextMapper;

            public ExampleMessagingContextAsyncChannel(AsynchronousRequestResponseExampleMessagingContextChannelListener contextChannelListener)
            {
                this.ListenUri = new Uri("uri:exampleasyncmessaging");
                this.logger = contextChannelListener.logger;
                this.cancellationToken = contextChannelListener.cancellationToken;
                this.protocolContextMapper = contextChannelListener;
                this.Open(contextChannelListener.OpenTimeout);
            }

            public override async Task<IMessagingContext<ExampleMessageContext>> ReceiveMessagingContextAsync()
            {
                await Task.Delay(10);

                ExampleMessageContext receivedMessagingContext = new ExampleMessageContext("Connection-3", this.ListenUri.AbsoluteUri, request++);

                BindingContext bindingContext = new BindingContext();

                MapToBindingContext(receivedMessagingContext, bindingContext);

                IMessagingContext<ExampleMessageContext> messagingContext = 
                    new MessagingContext<ExampleMessageContext>(bindingContext);

                messagingContext.Input(receivedMessagingContext);

                return messagingContext;
            }

            public override Task<IMessagingContext<ExampleMessageContext>> TryReceiveMessagingContextAsync(TimeSpan timeout)
            {
                return this.ReceiveMessagingContextAsync();
            }

            public override Task HandledMessagingCallBackAsync(IMessagingContext<ExampleMessageContext> messagingContext)
            {
                Console.WriteLine($"Handled: {messagingContext.BindingContext.InteractionContext.TraceIdentifier} from {messagingContext.BindingContext.InteractionContext.InputLocation} with {messagingContext.Result}");

                return Task.CompletedTask;
            }

            public override Task SendMessagingContextAsync(ExampleMessageContext messagingContext)
            {
                Console.WriteLine($"Sent: {messagingContext}");

                return Task.CompletedTask;
            }

            public override Task TrySendMessagingContextAsync(ExampleMessageContext messagingContext, TimeSpan timeout)
            {
                return SendMessagingContextAsync(messagingContext);
            }

            public bool MapToBindingContext(ExampleMessageContext context, BindingContext bindingContext)
            {
                return protocolContextMapper.MapToBindingContext(context, bindingContext);
            }

            protected override Task OnOpenAsync(TimeSpan timeSpan)
            {
                return Task.CompletedTask;
            }

            protected override Task OnCloseAsync(TimeSpan timeSpan)
            {
                return Task.CompletedTask;
            }

            protected override void OnAbort()
            {
            }

            protected override void MapConnection(ConnectionContext connectionContext)
            {
            }
        }

        private CancellationToken cancellationToken;
        private ILogger logger;
        private bool channel = false;

        public AsynchronousRequestResponseExampleMessagingContextChannelListener(TimeSpan openTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, TimeSpan closeTimeout) : base()
        {
            this.OpenTimeout = openTimeout;
            this.ReceiveTimeout = receiveTimeout;
            this.SendTimeout = sendTimeout;
            this.CloseTimeout = closeTimeout;
        }

        public TimeSpan OpenTimeout { get; }
        public TimeSpan ReceiveTimeout { get; }
        public TimeSpan SendTimeout { get; }
        public TimeSpan CloseTimeout { get; }

        protected override Task OnStartListeningAsync(IApplicationHostEnvironment hostEnvironment, IList<string> listenAddresses, IServiceProvider services, CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;

            this.logger = services.GetService<ILoggerFactory>().CreateLogger(this.GetType());

            return Task.CompletedTask;
        }

        public override Task<(bool success, IReceiveMessagingContextChannel<ExampleMessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync()
        {
            if (!channel)
            {
                if (this.ListenAddresses.Contains("uri:exampleasyncmessaging"))
                {
                    channel = true;

                    return Task.FromResult((true, (IReceiveMessagingContextChannel<ExampleMessageContext>)new ExampleMessagingContextAsyncChannel(this)));
                }
            }

            return Task.FromResult((false, (IReceiveMessagingContextChannel<ExampleMessageContext>)null));
        }

        public override bool MapToBindingContext(ExampleMessageContext context, BindingContext bindingContext)
        {
            ExampleMessageContext.MapToBindingContext(context, bindingContext);

            return true;
        }
    }
}