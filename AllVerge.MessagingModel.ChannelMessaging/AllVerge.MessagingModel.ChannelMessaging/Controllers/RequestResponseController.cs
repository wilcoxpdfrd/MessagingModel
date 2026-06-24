using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Controllers
{
    using Microsoft.Extensions.Primitives;

    using AllVerge.SystemPrimitives.Collections;

    using AllVerge.MessagingModel.MessagingApplication;

    using AllVerge.MessagingModel.ChannelMessaging.Channels;
    using Microsoft.Extensions.Logging;

    internal class RequestResponseController<MessageContext>:
        BaseReceiveController<MessageContext>
        where MessageContext : IMessageContext
    {
        private IList<AbstractRequestResponseMessagingContextChannel<MessageContext>> channels;
        public RequestResponseController(ILogger logger, MessagingReceiveOptions messagingReceiveOptions, Func<IMessagingContext<MessageContext>, RejectCode, IDictionary<RejectHeaders, StringValues>, Task> prepareRejectedMessagingContext, Func<IMessagingContext<MessageContext>, Action, Action, Task> receivedMessagingContext, CancellationToken cancellationToken) :
            base(logger, messagingReceiveOptions, prepareRejectedMessagingContext, receivedMessagingContext, cancellationToken)
        {
            this.channels = new List<AbstractRequestResponseMessagingContextChannel<MessageContext>>();
        }

        internal Task AcceptChannelAsync(Func<IMessagingContext<MessageContext>, bool> tryMapBindingContext, AbstractRequestResponseMessagingContextChannel<MessageContext> requestResponseMessagingContextChannel)
        {
            return AcceptChannelAndReceiveMessagesAsync(tryMapBindingContext, requestResponseMessagingContextChannel);
        }

        private Task AcceptChannelAndReceiveMessagesAsync(Func<IMessagingContext<MessageContext>, bool> tryMapBindingContext, AbstractRequestResponseMessagingContextChannel<MessageContext> requestResponseMessagingContextChannel)
        {

            _ = Task.Run(async () =>
            {
                long received = 0;
                long queueDepth = 0;
                long rejected = 0;

                if (!this.channels.Contains(requestResponseMessagingContextChannel))
                {
                    this.channels.Add(requestResponseMessagingContextChannel);

                    while (!CancellationToken.IsCancellationRequested && requestResponseMessagingContextChannel.IsOpen)
                    {
                        try
                        {
                            IReceivedMessagingContextChannel<MessageContext> receivedMessagingContextChannel =
                                await requestResponseMessagingContextChannel.ReceiveMessagingContextChannelAsync(Interlocked.Increment(ref received));

                            if (receivedMessagingContextChannel != null)
                            {
                                if (receivedMessagingContextChannel.ReceivedMessagingContext.CanBind &&
                                    tryMapBindingContext(receivedMessagingContextChannel.ReceivedMessagingContext))
                                {
                                    receivedMessagingContextChannel.ConfigureChannelProperties(receivedMessagingContextChannel.ReceivedMessagingContext);

                                    if (Interlocked.Read(ref queueDepth) > this.MessagingReceiveOptions.MaxMessagesQueueDepth)
                                    {
                                        Interlocked.Increment(ref rejected);

                                        Interlocked.Increment(ref queueDepth);

                                        IDictionary<RejectHeaders, StringValues> rejectionHeaders = CalculateRetryAfter(rejected);

                                        await this.PrepareRejectedMessagingContext(receivedMessagingContextChannel.ReceivedMessagingContext, RejectCode.TooBusy, rejectionHeaders);

                                        await this.ReceivedMessagingContext(receivedMessagingContextChannel.ReceivedMessagingContext, 
                                        () => {
                                            Interlocked.Decrement(ref queueDepth);

                                            receivedMessagingContextChannel.Dispose();
                                        }, 
                                        () => {
                                            Interlocked.Decrement(ref queueDepth);

                                            receivedMessagingContextChannel.Dispose();
                                        });
                                    }
                                    else
                                    {
                                        Interlocked.Exchange(ref rejected, 0);

                                        Interlocked.Increment(ref queueDepth);

                                        await this.ReceivedMessagingContext(receivedMessagingContextChannel.ReceivedMessagingContext, () => Interlocked.Decrement(ref queueDepth), () => Interlocked.Decrement(ref queueDepth));
                                    }
                                }
                                else

                                    // If bindingContext is null, the request is closing the channel ...

                                    await requestResponseMessagingContextChannel.CloseAsync(MessagingReceiveOptions.CloseChannelTimeout);
                            }

                            await Task.Yield();
                        }
                        catch (TimeoutException)
                        {
                            // no-op
                        }
                        catch (Exception e)
                        {
                            if (requestResponseMessagingContextChannel.IsOpen)
                                this.Logger.LogError(e, $"Unhandled exception while receiving a message in {nameof(AcceptChannelAndReceiveMessagesAsync)}.");
                        }
                    }

                    this.channels.Remove(requestResponseMessagingContextChannel);

                    if (requestResponseMessagingContextChannel.IsOpen)

                        requestResponseMessagingContextChannel.Close(this.MessagingReceiveOptions.CloseChannelTimeout);
                }
            });

            return Task.CompletedTask;
        }

        protected override void OnDispose()
        {
            int i = this.channels.Count;

            while (i > 0)
            {
                var requestResponseMessagingContextChannel = RemoveChannel(i - 1);

                if (requestResponseMessagingContextChannel.IsOpen)

                    requestResponseMessagingContextChannel.Dispose();

                i = this.channels.Count;
            }

            AbstractRequestResponseMessagingContextChannel<MessageContext> RemoveChannel(int index)
            {
                AbstractRequestResponseMessagingContextChannel<MessageContext> requestResponseMessagingContextChannel = this.channels[index];

                this.channels.RemoveAt(index);
                
                return requestResponseMessagingContextChannel;
            }
        }
    }
}