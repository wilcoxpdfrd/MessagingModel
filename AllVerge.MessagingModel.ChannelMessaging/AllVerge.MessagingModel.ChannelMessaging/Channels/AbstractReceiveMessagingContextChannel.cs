using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Channels
{
    public abstract class AbstractReceiveMessagingContextChannel<MessageContext> :
        AbstractMessagingContextChannel<MessageContext>, 
        IReceiveMessagingContextChannel<MessageContext>
    {
        protected AbstractReceiveMessagingContextChannel(MessagingChannelInteractions interactions) : base(interactions)
        {
        }

        public Uri ListenUri { get; protected set; }

        public virtual void ConfigureChannelProperties(IMessagingContext<MessageContext> messagingContext) { }

        public abstract Task HandledMessagingCallBackAsync(IMessagingContext<MessageContext> messagingContext);

        protected override string GetStringDetails()
        {
            return $"{nameof(ListenUri)}: {this.ListenUri}";
        }
    }
}
