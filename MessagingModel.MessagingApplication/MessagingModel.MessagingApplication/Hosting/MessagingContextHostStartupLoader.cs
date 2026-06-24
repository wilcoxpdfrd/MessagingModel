
namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    using Microsoft.AspNetCore.Hosting;

    using AllVerge.MessagingModel.MessagingApplication.Builder;

    public class MessagingContextHostStartupLoader<MessageContext> : 
        StartupLoader<IMessagingApplicationBuilder<MessageContext>>
    {

    }

    public class MessagingContextHostStartupLoader<ProtocolContext, MessageContext> :
        StartupLoader<IMessagingApplicationBuilder<ProtocolContext, MessageContext>>
    {

    }

    public class MessagingContextHostStartupLoader<ProtocolContextHost, ProtocolContext, MessageContext> :
        StartupLoader<IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>>
        where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
    {

    }
}