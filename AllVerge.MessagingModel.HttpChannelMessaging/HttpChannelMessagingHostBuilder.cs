using System;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Internal;

namespace AllVerge.MessagingModel.HttpChannelMessaging
{
    using AllVerge.SystemPrimitives.Reflection;
    using AllVerge.MessagingModel.MessagingApplication.Hosting.Builder;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using System.Net;
    using Microsoft.AspNetCore.Builder;

    public class HttpChannelMessagingHostBuilder :
        ChannelMessagingHostBuilder<ProtocolContextHost<HttpContext>, HttpContext>
    {
        protected override Type GetMessagingApplicationBuilderFactoryServiceType(out Type concreteMessagingApplicationBuilderFactoryType)
        {
            var protocolMessagingType = ImplementationTypeInfo<IMessagingApplicationBuilderFactory<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext>>.GetImplementationTypeInfo<HttpChannelMessagingApplicationBuilderFactory>();
            concreteMessagingApplicationBuilderFactoryType = protocolMessagingType.ImplementationType;
            return protocolMessagingType.AbstractType;
        }
    }
}