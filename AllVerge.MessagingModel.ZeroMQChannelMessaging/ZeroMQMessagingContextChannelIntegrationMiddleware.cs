using AllVerge.Core.ServiceModel.Channels;
using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public class ZeroMQMessagingContextChannelIntegrationMiddleware<ChannelMessageDispatcher> :
        MessagingContextChannelIntegrationMiddleware<ZeroMQTransferMessagingContext, ChannelMessageDispatcher>
        where ChannelMessageDispatcher: MessageDispatcher<ZeroMQTransferMessagingContext, Message>, new()
    {
    }
}
