using AllVerge.MessagingModel.MessagingApplication.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    public interface IMessagingApplicationStartupFilter<MessageContext>
    {
        Action<IMessagingApplicationBuilder<MessageContext>, IHostApplicationLifetime, IHostEnvironment, ILoggerFactory> Configure(Action<IMessagingApplicationBuilder<MessageContext>, IHostApplicationLifetime, IHostEnvironment, ILoggerFactory> next);
    }

    public interface IMessagingApplicationStartupFilter<ProtocolContext, MessageContext>
    {
        Action<IMessagingApplicationBuilder<ProtocolContext, MessageContext>, IHostApplicationLifetime, IHostEnvironment, ILoggerFactory> Configure(Action<IMessagingApplicationBuilder<ProtocolContext, MessageContext>, IHostApplicationLifetime, IHostEnvironment, ILoggerFactory> next);
    }

    public interface IMessagingApplicationStartupFilter<ProtocolContextHost, ProtocolContext, MessageContext>
        where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
    {
        Action<IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>, IHostApplicationLifetime, IHostEnvironment, ILoggerFactory> Configure(Action<IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>, IHostApplicationLifetime, IHostEnvironment, ILoggerFactory> next);
    }
}
