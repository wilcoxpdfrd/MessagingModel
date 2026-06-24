using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public interface IMessagingChannelAccessor<MessageContext>
    {
        void Set<MessagingChannel>(MessagingChannel messagingChannel) 
            where MessagingChannel : class, IMessagingChannel<MessageContext>;
        MessagingChannel Get<MessagingChannel>() 
            where MessagingChannel : class, IMessagingChannel<MessageContext>;
        Task DisposeMessagingChannelAsync(IMessagingChannel<MessageContext> messagingChannel);
    }
}
