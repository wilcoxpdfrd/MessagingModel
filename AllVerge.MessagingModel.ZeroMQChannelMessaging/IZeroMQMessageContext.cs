using System;
using System.Collections.Generic;
using System.Text;

using ServiceModel.ChannelMessaging;

using AllVerge.Core.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public interface IZeroMQMessageContext<MessagingContext> : 
        IChannelMessageMapper<MessagingContext> 
        where MessagingContext : IMessagingContext
    {
    }
}
