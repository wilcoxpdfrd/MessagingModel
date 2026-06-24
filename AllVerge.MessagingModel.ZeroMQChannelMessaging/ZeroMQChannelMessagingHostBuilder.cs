using System;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using AllVerge.MessagingModel.MessagingApplication.Hosting.Builder;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    using AllVerge.SystemPrimitives.Reflection;

    public class ZeroMQChannelMessagingHostBuilder :
        ChannelMessagingHostBuilder<ZeroMQProtocolContext>
    {
        protected override Type GetMessagingApplicationBuilderFactoryServiceType(out Type concreteMessagingApplicationBuilderFactoryType)
        {
            var protocolMessagingType = ImplementationTypeInfo<IMessagingApplicationBuilderFactory<ZeroMQProtocolContext, ChannelMessageContext>>.GetImplementationTypeInfo<ZeroMQChannelMessagingApplicationBuilderFactory>();
            concreteMessagingApplicationBuilderFactoryType = protocolMessagingType.ImplementationType;
            return protocolMessagingType.AbstractType;
        }
    }
}