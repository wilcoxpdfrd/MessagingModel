using System;
using System.Collections.Generic;
using System.Text;

using System.ServiceModel.Channels;

using AllVerge.MessagingModel.ChannelMessaging;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    public interface IChannelMessageMapper<MessageContext> :
        IMessageContext where MessageContext : IMessageContext
    {
        bool TryGetIncomingMessage(out Message incomingMessage, out DateTime received);
        MessageContext GetOutgoingMessagingContext(Message outgoingMessage);
    }
}
