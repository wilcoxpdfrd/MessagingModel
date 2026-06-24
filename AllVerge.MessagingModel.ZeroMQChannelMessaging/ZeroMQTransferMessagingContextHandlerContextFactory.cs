using Microsoft.AspNetCore.Http.Features;
using ServiceModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public class ZeroMQTransferMessagingContextHandlerContextFactory :
        IProtocolMessagingContextFactory<ZeroMQTransferMessagingContext>
    {
        public IMessagingContext<ZeroMQTransferMessagingContext> Create(IFeatureCollection features, IApplicationMessagingContext<ZeroMQTransferMessagingContext> applicationMessagingContext)
        {
            return new ZeroMQTransferMessagingContextHandlerContext(features, applicationMessagingContext);
        }

        public void Dispose(IMessagingContext<ZeroMQTransferMessagingContext> messagingContext)
        {
            messagingContext.ReceivedContext.Dispose();
            messagingContext.Dispose();
        }
    }
}
