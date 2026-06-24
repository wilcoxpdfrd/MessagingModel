using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using AllVerge.MessagingModel.ChannelMessaging;
using AllVerge.MessagingModel.ChannelMessaging.Channels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer.Listeners
{
    internal class SolicitResponseExampleMessagingContextChannelListener :
        MessagingContextChannelListener<ExampleMessageContext>
    {
        private class SolicitResponseExampleMessagingContextChannel :
            AbstractSolicitResponseMessagingContextChannel<ExampleMessageContext>
        {
            private ILogger logger;
            CancellationToken cancellationToken;
            Queue<ExampleMessageContext> exampleMessagingContexts;
            private IBindingContextMapper<ExampleMessageContext> bindingContextMapper;

            public SolicitResponseExampleMessagingContextChannel(SolicitResponseExampleMessagingContextChannelListener messagingContextChannelListener)
            {
                this.ListenUri = new Uri(messagingContextChannelListener.ListenAddresses.First());
                this.logger = messagingContextChannelListener.logger;
                this.cancellationToken = messagingContextChannelListener.cancellationToken;
                this.bindingContextMapper = messagingContextChannelListener;
                this.exampleMessagingContexts = new Queue<ExampleMessageContext>();
                this.Open(messagingContextChannelListener.OpenTimeout);
            }

            public override Task SolicitMessagingContextAsync(ExampleMessageContext messagingContext)
            {
                this.exampleMessagingContexts.Enqueue(messagingContext);

                Console.WriteLine($"Sent {messagingContext}");

                return Task.CompletedTask;
            }

            public override Task TrySolicitMessagingContextAsync(ExampleMessageContext messagingContext, TimeSpan timeout)
            {
                return SolicitMessagingContextAsync(messagingContext);
            }

            public override Task<IMessagingContext<ExampleMessageContext>> ReceiveMessagingContextAsync()
            {
                ExampleMessageContext receivedMessagingContext = this.exampleMessagingContexts.Dequeue();

                Console.WriteLine($"Received {receivedMessagingContext}");

                BindingContext bindingContext = new BindingContext();

                this.bindingContextMapper.MapToBindingContext(receivedMessagingContext, bindingContext);

                IMessagingContext<ExampleMessageContext> messagingContext =
                    new MessagingContext<ExampleMessageContext>(bindingContext);

                messagingContext.Input(receivedMessagingContext);

                return Task.FromResult(messagingContext);
            }

            public override Task<IMessagingContext<ExampleMessageContext>> TryReceiveMessagingContextAsync(TimeSpan timeout)
            {
                return this.ReceiveMessagingContextAsync();
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

            protected override void MapConnection(ConnectionContext connectionContext)
            {
            }
        }

        CancellationToken cancellationToken;
        private ILogger logger;

        public SolicitResponseExampleMessagingContextChannelListener(TimeSpan openTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, TimeSpan closeTimeout) : base()
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
            return Task.FromResult((true, (IReceiveMessagingContextChannel<ExampleMessageContext>)new SolicitResponseExampleMessagingContextChannel(this)));
        }

        public override bool MapToBindingContext(ExampleMessageContext context, BindingContext bindingContext)
        {
            ExampleMessageContext.MapToBindingContext(context, bindingContext);

            return true;
        }
    }
}