using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Controllers
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    using AllVerge.SystemPrimitives.Collections;

    using AllVerge.MessagingModel.MessagingApplication;

    using AllVerge.MessagingModel.ChannelMessaging.Channels;
    using System.Xml.Linq;

    internal class AsynchronousRequestResponseController<MessageContext>:
        BaseReceiveController<MessageContext>
        where MessageContext : IMessageContext
    {
        private IList<AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext>> channels;

        public AsynchronousRequestResponseController(ILogger logger, MessagingReceiveOptions messagingReceiveOptions, Func<IMessagingContext<MessageContext>, RejectCode, IDictionary<RejectHeaders, StringValues>, Task> prepareRejectedMessagingContext, Func<IMessagingContext<MessageContext>, Action, Action, Task> receivedMessagingContext, CancellationToken cancellationToken) :
            base(logger, messagingReceiveOptions, prepareRejectedMessagingContext, receivedMessagingContext, cancellationToken)
        {
            this.channels = new List<AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext>>();
        }

        internal Task AcceptChannelAsync(Func<IMessagingContext<MessageContext>, bool> tryMapBindingContext, AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> asyncRequestResponseMessagingContextChannel) 
        {
            return AcceptChannelAndReceiveMessagesAsync(tryMapBindingContext, asyncRequestResponseMessagingContextChannel);
        }

        private Task AcceptChannelAndReceiveMessagesAsync(Func<IMessagingContext<MessageContext>, bool> tryMapBindingContext, AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> asyncRequestResponseMessagingContextChannel)
        {
            _ = Task.Run(async () =>
            {
                long queueDepth = 0;
                long rejected = 0;

                if (!this.channels.Contains(asyncRequestResponseMessagingContextChannel)) {

                    this.channels.Add(asyncRequestResponseMessagingContextChannel);

                    while (!CancellationToken.IsCancellationRequested && asyncRequestResponseMessagingContextChannel.IsOpen)
                    {
                        try
                        {
                            IMessagingContext<MessageContext> receivedMessagingContext =
                                await asyncRequestResponseMessagingContextChannel.ReceiveMessagingContextAsync();

                            if (receivedMessagingContext != null)
                            {
                                receivedMessagingContext.BindingContext.ConnectionContext.Items.Add<IMessagingContextChannel<MessageContext>>(asyncRequestResponseMessagingContextChannel);

                                if (Interlocked.Read(ref queueDepth) > this.MessagingReceiveOptions.MaxMessagesQueueDepth)
                                {
                                    Interlocked.Increment(ref rejected);
                                    Interlocked.Increment(ref queueDepth);

                                    IDictionary<RejectHeaders, StringValues> rejectionHeaders = CalculateRetryAfter(rejected);

                                    await this.PrepareRejectedMessagingContext(receivedMessagingContext, RejectCode.TooBusy, rejectionHeaders);

                                    await this.ReceivedMessagingContext(receivedMessagingContext, () => Interlocked.Decrement(ref queueDepth), () => Interlocked.Decrement(ref queueDepth));
                                }
                                else
                                {
                                    Interlocked.Exchange(ref rejected, 0);
                                    Interlocked.Increment(ref queueDepth);

                                    if (receivedMessagingContext.CanBind && tryMapBindingContext(receivedMessagingContext))
                                    {
                                        if (asyncRequestResponseMessagingContextChannel is IReceiveMessagingContextChannel<MessageContext>)

                                            (asyncRequestResponseMessagingContextChannel as IReceiveMessagingContextChannel<MessageContext>).ConfigureChannelProperties(receivedMessagingContext);

                                        await this.ReceivedMessagingContext(receivedMessagingContext, () => Interlocked.Decrement(ref queueDepth), () => Interlocked.Decrement(ref queueDepth));
                                    }
                                    else
                                    {

                                        // If bindingContext is null, the request is closing the upgraded channel ...

                                        this.Logger.LogTrace($"Closing connection {receivedMessagingContext.BindingContext.ConnectionContext.ConnectionId}");

                                        await asyncRequestResponseMessagingContextChannel.CloseAsync(MessagingReceiveOptions.CloseChannelTimeout);
                                    }
                                }
                            }

                            await Task.Yield();
                        }
                        catch (TimeoutException)
                        {
                            // no-op
                        }
                        catch (Exception e)
                        {
                            if (asyncRequestResponseMessagingContextChannel.IsOpen)
                                this.Logger.LogError(e, $"Unhandled exception while receiving a message in {nameof(AcceptChannelAndReceiveMessagesAsync)}.");
                        }
                    }

                    this.channels.Remove(asyncRequestResponseMessagingContextChannel);

                    if (asyncRequestResponseMessagingContextChannel.IsOpen)

                        await asyncRequestResponseMessagingContextChannel.CloseAsync(this.MessagingReceiveOptions.CloseChannelTimeout);
                }
            });

            return Task.CompletedTask;
        }

        protected override void OnDispose()
        {
            int i = this.channels.Count;

            while (i > 0)
            {
                var asyncRequestResponseMessagingContextChannel = RemoveChannel(i - 1);

                if (asyncRequestResponseMessagingContextChannel.IsOpen)

                    asyncRequestResponseMessagingContextChannel.Dispose();

                i = this.channels.Count;
            }

            AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> RemoveChannel(int index)
            {
                AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> asyncRequestResponseMessagingContextChannel = this.channels[index];

                this.channels.RemoveAt(index);

                return asyncRequestResponseMessagingContextChannel;
            }
        }
    }
}