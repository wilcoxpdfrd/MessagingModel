using System;

using AllVerge.Core.ServiceModel.Channels;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Internal;
using AllVerge.Core.Reflection;
using ServiceModel.MessagingApplication.Hosting.Builder;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public class ZeroMQTransferChannelMessagingHostBuilder :
        ChannelMessagingHostBuilder<ZeroMQApplicationHostingContext, ZeroMQTransferProtocolContext>
    {
        protected override ImplementationTypeInfo<IMessagingApplicationBuilderFactory<ChannelMessageContext>> GetMessagingApplicationBuilderType()
        {
            return ImplementationTypeInfo<IMessagingApplicationBuilderFactory<ChannelMessageContext>>.GetConcreteImplementationTypeInfo<ChannelMessagingApplicationBuilderFactory>();
        }
    }
}