using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingApplication.Builder;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using AllVerge.MessagingModel.MessagingApplication.Hosting.Builder;
using AllVerge.MessagingModel.MessagingFoundation.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace AllVerge.MessagingModel.HttpChannelMessaging
{
    public class HttpChannelMessagingApplicationBuilderFactory :
        DefaultMessagingApplicationBuilderFactory<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext>
    {
        public HttpChannelMessagingApplicationBuilderFactory(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override IMessagingApplicationBuilder<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext> CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener)
        {
            return new HttpChannelMessagingApplicationBuilder((Object)serverFeatures, this.ServiceProvider, protocolMessagingContextReceiver, logger, diagnosticListener);
        }
    }
}
