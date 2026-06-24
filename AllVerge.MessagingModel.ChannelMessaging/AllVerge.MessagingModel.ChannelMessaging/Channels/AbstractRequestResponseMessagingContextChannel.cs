using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Channels
{
    /// <summary>
    /// Abstract <see cref="MessagingChannelInteractions.RequestResponse"/> (half-duplex) channel.
    /// Note that upon receiving a request message, this channel provides a new 
    /// <see cref="IReceivedMessagingContextChannel{MessageContext}"/> channel which 
    /// is a single-use channel with which to send the response message.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public abstract class AbstractRequestResponseMessagingContextChannel<MessageContext>:
        AbstractReceiveMessagingContextChannel<MessageContext>
    {
        public AbstractRequestResponseMessagingContextChannel() : base(MessagingChannelInteractions.RequestResponse) { }
        public abstract Task<IReceivedMessagingContextChannel<MessageContext>> ReceiveMessagingContextChannelAsync(long received);
        public abstract Task<IReceivedMessagingContextChannel<MessageContext>> TryReceiveMessagingContextChannelAsync(long received, TimeSpan timeout);
    }
}
