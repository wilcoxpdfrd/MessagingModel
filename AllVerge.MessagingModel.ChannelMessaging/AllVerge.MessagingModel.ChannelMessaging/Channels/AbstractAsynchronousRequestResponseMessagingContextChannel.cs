using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Channels
{
    public abstract class AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> :
        AbstractReceiveMessagingContextChannel<MessageContext>
    {
        public AbstractAsynchronousRequestResponseMessagingContextChannel() : base(MessagingChannelInteractions.AsynchronousRequestResponse) { }
        public abstract Task<IMessagingContext<MessageContext>> ReceiveMessagingContextAsync();
        public abstract Task<IMessagingContext<MessageContext>> TryReceiveMessagingContextAsync(TimeSpan timeout);
        public abstract Task SendMessagingContextAsync(MessageContext messagingContext);
        public abstract Task TrySendMessagingContextAsync(MessageContext messagingContext, TimeSpan timeout);
    }
}
