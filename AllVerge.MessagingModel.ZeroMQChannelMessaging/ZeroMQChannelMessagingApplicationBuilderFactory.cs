using System;
using System.Diagnostics;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Builder;
    using AllVerge.MessagingModel.MessagingApplication.Hosting.Builder;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.Extensions.Logging;

    internal class ZeroMQChannelMessagingApplicationBuilderFactory : DefaultMessagingApplicationBuilderFactory<ZeroMQProtocolContext, ChannelMessageContext>
    {
        public ZeroMQChannelMessagingApplicationBuilderFactory(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override IMessagingApplicationBuilder<ZeroMQProtocolContext, ChannelMessageContext> CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<ZeroMQProtocolContext, ChannelMessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener)
        {
            return new ZeroMQChannelMessagingApplicationBuilder((Object)serverFeatures, this.ServiceProvider, protocolMessagingContextReceiver, logger, diagnosticListener);
        }
    }
}