using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AllVerge.SystemPrimitives.Collections;

using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using AllVerge.MessagingModel.ChannelMessaging;
using AllVerge.MessagingModel.ChannelMessaging.Channels;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer.Listeners
{
    internal class RequestResponseExampleMessagingContextChannelListener : 
        MessagingContextChannelListener<ExampleMessageContext>
    {
        private class ExampleRequestResponseMessagingContextChannel :
            AbstractRequestResponseMessagingContextChannel<ExampleMessageContext>
        {
            private class ExampleReceivedMessagingContextChannel :
                AbstractMessagingContextChannel<ExampleMessageContext>, 
                IReceivedMessagingContextChannel<ExampleMessageContext>,
                IBindingContextMapper<ExampleMessageContext>
            {
                private IMessagingContext<ExampleMessageContext> receivedMessagingContext;
                private ILogger logger;
                private CancellationToken cancellationToken;
                private RequestResponseExampleMessagingContextChannelListener contextChannelListener;
                private Func<IMessagingContext<ExampleMessageContext>, Task> handledMessagingCallBackAsync;

                public ExampleReceivedMessagingContextChannel(Uri listenUri, RequestResponseExampleMessagingContextChannelListener messagingContextChannelListener, Func<IMessagingContext<ExampleMessageContext>, Task> handledMessagingCallBackAsync, int request) : 
                    base(MessagingChannelInteractions.Received)
                {
                    this.contextChannelListener = messagingContextChannelListener;
                    this.handledMessagingCallBackAsync = handledMessagingCallBackAsync;
                    this.ListenUri = listenUri;
                    this.receivedMessagingContext = CreateReceivedMessagingContext(request);
                    this.logger = messagingContextChannelListener.logger;
                    this.cancellationToken = messagingContextChannelListener.cancellationToken;
                }

                public Uri ListenUri { get; }

                private IMessagingContext<ExampleMessageContext> CreateReceivedMessagingContext(int request)
                {
                    BindingContext bindingContext = new BindingContext();

                    ExampleMessageContext receivedMessagingContext =
                        new ExampleMessageContext("connection-2", this.ListenUri.AbsoluteUri, request);

                    MapToBindingContext(receivedMessagingContext, bindingContext);

                    IMessagingContext<ExampleMessageContext> messagingContext = 
                        new MessagingContext<ExampleMessageContext>(bindingContext);

                    messagingContext.Input(receivedMessagingContext);

                    return messagingContext;
                }

                public IMessagingContext<ExampleMessageContext> ReceivedMessagingContext => this.receivedMessagingContext;

                public void ConfigureChannelProperties(IMessagingContext<ExampleMessageContext> messagingContext)
                {
                    messagingContext.BindingContext.ConnectionContext.Items.Add<IReceivedMessagingContextChannel<ExampleMessageContext>>(this);
                }

                public Task HandledMessagingCallBackAsync(IMessagingContext<ExampleMessageContext> messagingContext)
                {
                    return handledMessagingCallBackAsync(messagingContext);
                }

                public bool MapToBindingContext(ExampleMessageContext context, BindingContext bindingContext)
                {
                    return ((IBindingContextMapper<ExampleMessageContext>)contextChannelListener).MapToBindingContext(context, bindingContext);
                }

                public Task SendMessagingContextAsync(ExampleMessageContext messagingContext)
                {
                    Console.WriteLine($"Sent {messagingContext}");
                    
                    base.Dispose();
                    
                    return Task.CompletedTask;
                }

                public Task TrySendMessagingContextAsync(ExampleMessageContext messagingContext, TimeSpan timeout)
                {
                    return SendMessagingContextAsync(messagingContext);
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

            private RequestResponseExampleMessagingContextChannelListener contextChannelListener;
            private int request;

            public ExampleRequestResponseMessagingContextChannel(Uri listenUri, RequestResponseExampleMessagingContextChannelListener contextChannelListener)
            {
                this.ListenUri = listenUri;
                this.contextChannelListener = contextChannelListener;
                this.Open(contextChannelListener.OpenTimeout);
            }


            public override Task<IReceivedMessagingContextChannel<ExampleMessageContext>> ReceiveMessagingContextChannelAsync(long received)
            {
                return this.TryReceiveMessagingContextChannelAsync(received, this.contextChannelListener.ReceiveTimeout);
            }


            public override async Task<IReceivedMessagingContextChannel<ExampleMessageContext>> TryReceiveMessagingContextChannelAsync(long received, TimeSpan timeout)
            {
                await Task.Delay(10);

                IReceivedMessagingContextChannel<ExampleMessageContext> receivedMessagingContextChannel =
                    new ExampleReceivedMessagingContextChannel(this.ListenUri, this.contextChannelListener, this.HandledMessagingCallBackAsync, request++);

                await receivedMessagingContextChannel.OpenAsync(this.contextChannelListener.OpenTimeout);

                Console.WriteLine($"Received {receivedMessagingContextChannel.ReceivedMessagingContext}");

                return receivedMessagingContextChannel;
            }

            protected override void MapConnection(ConnectionContext connectionContext)
            {
            }

            public override Task HandledMessagingCallBackAsync(IMessagingContext<ExampleMessageContext> messagingContext)
            {
                Console.WriteLine($"Handled {messagingContext.BindingContext.InteractionContext.TraceIdentifier} from {messagingContext.BindingContext.InteractionContext.InputLocation} with {messagingContext.Result}");

                return Task.CompletedTask;
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
        }

        private CancellationToken cancellationToken;
        private ILogger logger;
        private bool channel = false;

        public RequestResponseExampleMessagingContextChannelListener(TimeSpan openTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, TimeSpan closeTimeout)
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
                if (this.ListenAddresses.Contains("uri:examplemessaging"))
                {
                    channel = true;

                    return Task.FromResult((true, (IReceiveMessagingContextChannel<ExampleMessageContext>)new ExampleRequestResponseMessagingContextChannel(new Uri("uri:examplemessaging"), this)));
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