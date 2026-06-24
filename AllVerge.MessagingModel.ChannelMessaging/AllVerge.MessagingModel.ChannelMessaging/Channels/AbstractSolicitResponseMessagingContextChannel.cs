using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Channels
{
    /// <summary>
    /// Note that the <see cref="AbstractReceiveMessagingContextChannel{MessageContext}.ListenUri"/> is generally a virtual address; 
    /// it is used to select the <see cref="MessagingChannelInteractions.SolicitResponse"/> or <see cref="MessagingChannelInteractions.AsynchronousSolicitResponse"/> channel listener only - 
    /// there is no sense of listening for messages in a solicit response channel that originates messaging interactions, 
    /// although some implementations may send the address when soliciting a response, and setup a separate listener for the response as opposed to using the same channel for the response.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public abstract class AbstractSolicitResponseMessagingContextChannel<MessageContext>:
        AbstractReceiveMessagingContextChannel<MessageContext>
    {
        protected AbstractSolicitResponseMessagingContextChannel(MessagingChannelInteractions extendsSolictResponse) : 
            base(MessagingChannelInteractions.ValidateIsExtending(MessagingChannelInteractions.SolicitResponse, extendsSolictResponse)) { }
        public AbstractSolicitResponseMessagingContextChannel() : base(MessagingChannelInteractions.SolicitResponse) { }
        public abstract Task SolicitMessagingContextAsync(MessageContext messagingContext);
        public abstract Task TrySolicitMessagingContextAsync(MessageContext messagingContext, TimeSpan timeout);
        public abstract Task<IMessagingContext<MessageContext>> ReceiveMessagingContextAsync();
        public abstract Task<IMessagingContext<MessageContext>> TryReceiveMessagingContextAsync(TimeSpan timeout);
    }
}
