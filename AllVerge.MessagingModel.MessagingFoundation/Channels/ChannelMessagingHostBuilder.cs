using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;

    public class ChannelMessagingHostBuilder<ProtocolContext> :
        DefaultMessagingContextHostBuilder<ProtocolContext, ChannelMessageContext>
    {
        protected override IMessagingHost CreateMessagingHost(IConfiguration configuration, HostOptions hostOptions, IServiceProvider hostingServiceProvider, IServiceCollection applicationServices, AggregateException hostingStartupAssemblyErrors)
        {
            return new DefaultMessagingContextHost<ProtocolContext, ChannelMessageContext>(configuration, hostOptions, hostingServiceProvider, applicationServices, hostingStartupAssemblyErrors);
        }
    }

    public class ChannelMessagingHostBuilder<ProtocolContextHost, ProtocolContext> : 
        DefaultMessagingContextHostBuilder<ProtocolContextHost, ProtocolContext, ChannelMessageContext>
        where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
    {
        protected override IMessagingHost CreateMessagingHost(IConfiguration configuration, HostOptions hostOptions, IServiceProvider hostingServiceProvider, IServiceCollection applicationServices, AggregateException hostingStartupAssemblyErrors)
        {
            return new DefaultMessagingContextHost<ProtocolContextHost, ProtocolContext, ChannelMessageContext>(configuration, hostOptions, hostingServiceProvider, applicationServices, hostingStartupAssemblyErrors);
        }
    }
}
