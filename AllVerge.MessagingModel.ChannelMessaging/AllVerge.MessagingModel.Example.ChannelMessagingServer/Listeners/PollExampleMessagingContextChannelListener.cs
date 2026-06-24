using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using AllVerge.MessagingModel.ChannelMessaging;
using AllVerge.MessagingModel.ChannelMessaging.Channels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using System.Linq;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer.Listeners
{
    using AllVerge.SystemPrimitives.Runtime;
    using Microsoft.Extensions.Hosting;

    internal class PollExampleMessagingContextChannelListener :
        SolicitResponseExampleMessagingContextChannelListener
    {
        private class PollExampleMessagingContextChannel :
            AbstractPollMessagingContextChannel<ExampleMessageContext>,
            IBindingContextMapper<ExampleMessageContext>
        {
            private ILogger logger;
            private CancellationToken cancellationToken;
            private BlockingCollection<ExampleMessageContext> exampleMessagingContexts;
            private int polled = 0;
            private IBindingContextMapper<ExampleMessageContext> bindingContextMapper;

            public PollExampleMessagingContextChannel(PollExampleMessagingContextChannelListener contextChannelListener)
            {
                this.ListenUri = new Uri("uri:examplepollmessaging");
                this.logger = contextChannelListener.logger;
                this.cancellationToken = contextChannelListener.cancellationToken;
                this.bindingContextMapper = contextChannelListener;
                this.exampleMessagingContexts = new BlockingCollection<ExampleMessageContext>();
                this.Open(contextChannelListener.OpenTimeout);
            }

            public override int PollSize => 100;

            public override int PollTimeoutMS => 1000;

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

            public override async Task SolicitMessagingContextAsync(ExampleMessageContext pollmessagingContext)
            {
                this.cancellationToken.ThrowIfCancellationRequested();

                Console.WriteLine($"Sent {pollmessagingContext}");

                pollmessagingContext.Items.TryGetValue("poll", out object o1);
                pollmessagingContext.Items.TryGetValue("pollSize", out object o2);

                int poll = (int)o1;
                int pollSize = (int)o2;

                for (int i = 0; i < pollSize; i++)
                {
                    await Task.Delay(10);

                    ExampleMessageContext messagingContext = new ExampleMessageContext("connection-1", this.ListenUri.AbsoluteUri, poll, i, pollSize);

                    this.exampleMessagingContexts.Add(messagingContext);
                }
            }

            public override Task TrySolicitMessagingContextAsync(ExampleMessageContext messagingContext, TimeSpan timeout)
            {
                return SolicitMessagingContextAsync(messagingContext);
            }

            public override Task<IMessagingContext<ExampleMessageContext>> ReceiveMessagingContextAsync()
            {
                return Task.Run(() =>
                {
                    this.cancellationToken.ThrowIfCancellationRequested();

                    ExampleMessageContext receivedMessagingContext = this.exampleMessagingContexts.Take(this.cancellationToken);

                    Console.WriteLine($"Received {receivedMessagingContext}");

                    BindingContext bindingContext = new BindingContext();

                    this.MapToBindingContext(receivedMessagingContext, bindingContext);

                    IMessagingContext<ExampleMessageContext> messagingContext =
                        new MessagingContext<ExampleMessageContext>(bindingContext);

                    messagingContext.Input(receivedMessagingContext);

                    return messagingContext;
                });
            }

            public override Task<IMessagingContext<ExampleMessageContext>> TryReceiveMessagingContextAsync(TimeSpan timeout)
            {
                return this.ReceiveMessagingContextAsync();
            }

            public override async Task<IMessagingContext<ExampleMessageContext>[]> ReceiveMessagesAsync(int pollSize, TimeSpan timeout, CancellationToken cancellationToken)
            {
                this.cancellationToken.ThrowIfCancellationRequested();

                if (!this.IsOpen)

                    throw new ObjectDisposedException(this.GetType().FullName);
                    
                await this.SolicitMessagingContextAsync(new ExampleMessageContext("connection-1", this.ListenUri.AbsoluteUri, polled, pollSize));

                polled++;

                ICountdownTimer timeoutHelper = timeout.StartCountdown();

                List<IMessagingContext<ExampleMessageContext>> received = new List<IMessagingContext<ExampleMessageContext>>();

                TimeSpan remainingTime;

                while (received.Count < pollSize && (remainingTime = timeoutHelper.RemainingTime()) > TimeSpan.Zero)
                {
                    received.Add(await this.TryReceiveMessagingContextAsync(remainingTime));
                }

                return received.ToArray();
            }

            public override Task HandledMessagingCallBackAsync(IMessagingContext<ExampleMessageContext> messagingContext)
            {
                Console.WriteLine($"Handled {messagingContext.BindingContext.InteractionContext.TraceIdentifier} from {messagingContext.BindingContext.InteractionContext.InputLocation} with {messagingContext.Result}");

                return Task.CompletedTask;
            }

            public override Task AcknowledgeReceivedMessagingContextAsync(ExampleMessageContext messagingContext)
            {
                Console.WriteLine($"Acknowledged {messagingContext}");

                return Task.CompletedTask;
            }

            public bool MapToBindingContext(ExampleMessageContext context, BindingContext bindingContext)
            {
                return this.bindingContextMapper.MapToBindingContext(context, bindingContext);
            }

            protected override void MapConnection(ConnectionContext connectionContext)
            {
            }
        }

        CancellationToken cancellationToken;
        private ILogger logger;
        private bool channel = false;

        public PollExampleMessagingContextChannelListener(TimeSpan openTimeout, TimeSpan receiveTimeout, TimeSpan sendTimeout, TimeSpan closeTimeout) : 
            base(openTimeout, receiveTimeout, sendTimeout, closeTimeout)
        {
        }

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
                if (this.ListenAddresses.Contains("uri:examplepollmessaging"))
                {
                    channel = true;

                    return Task.FromResult((true, (IReceiveMessagingContextChannel<ExampleMessageContext>)new PollExampleMessagingContextChannel(this)));
                }
            }

            return Task.FromResult((false, (IReceiveMessagingContextChannel<ExampleMessageContext>)null));
        }
    }
}