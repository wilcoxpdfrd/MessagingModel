using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

using AllVerge.MessagingModel.MessagingApplication.Builder;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Builder
{
    public interface IMessagingApplicationBuilderFactory<MessageContext> 
    {
        IMessagingApplicationBuilder<MessageContext> CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener);
    }

    public interface IMessagingApplicationBuilderFactory<ProtocolContext, MessageContext> 
    {
        IMessagingApplicationBuilder<ProtocolContext, MessageContext> CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<ProtocolContext, MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener);
    }

    public interface IMessagingApplicationBuilderFactory<ProtocolContextHost, ProtocolContext, MessageContext>
        where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
    {
        IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener);
    }
}
