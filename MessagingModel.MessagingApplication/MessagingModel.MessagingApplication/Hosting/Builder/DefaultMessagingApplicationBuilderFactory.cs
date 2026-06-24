using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

using AllVerge.MessagingModel.MessagingApplication.Builder;
using AllVerge.MessagingModel.MessagingApplication.Builder.Default;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Builder
{
    public class DefaultMessagingApplicationBuilderFactory<MessageContext> :
        IMessagingApplicationBuilderFactory<MessageContext>
    {
        private IServiceProvider serviceProvider;

        public DefaultMessagingApplicationBuilderFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected IServiceProvider ServiceProvider { get => serviceProvider; }


        IMessagingApplicationBuilder<MessageContext> IMessagingApplicationBuilderFactory<MessageContext>.CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener)
        {
            return CreateMessagingApplicationBuilder(serverFeatures, protocolMessagingContextReceiver, logger, diagnosticListener);
        }

        protected virtual IMessagingApplicationBuilder<MessageContext> CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener)
        {
            return new DefaultMessagingApplicationBuilder<MessageContext>((Object)serverFeatures, this.serviceProvider, protocolMessagingContextReceiver, logger, diagnosticListener);
        }
    }

    public class DefaultMessagingApplicationBuilderFactory<ProtocolContext, MessageContext> : 
        IMessagingApplicationBuilderFactory<ProtocolContext, MessageContext>
    {
        private IServiceProvider serviceProvider;

        public DefaultMessagingApplicationBuilderFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected IServiceProvider ServiceProvider { get => serviceProvider; }


        IMessagingApplicationBuilder<ProtocolContext, MessageContext> IMessagingApplicationBuilderFactory<ProtocolContext, MessageContext>.CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<ProtocolContext, MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener)
        {
            return CreateMessagingApplicationBuilder(serverFeatures, protocolMessagingContextReceiver, logger, diagnosticListener);
        }

        protected virtual IMessagingApplicationBuilder<ProtocolContext, MessageContext> CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<ProtocolContext, MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener)
        {
            return new DefaultMessagingApplicationBuilder<ProtocolContext, MessageContext>((Object)serverFeatures, this.serviceProvider, protocolMessagingContextReceiver, logger, diagnosticListener);
        }
    }

    public class DefaultMessagingApplicationBuilderFactory<ProtocolContextHost, ProtocolContext, MessageContext> :
        IMessagingApplicationBuilderFactory<ProtocolContextHost, ProtocolContext, MessageContext>
        where ProtocolContextHost : IProtocolContextHost<ProtocolContext>
    {
        private IServiceProvider serviceProvider;

        public DefaultMessagingApplicationBuilderFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected IServiceProvider ServiceProvider { get => serviceProvider; }


        IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> IMessagingApplicationBuilderFactory<ProtocolContextHost, ProtocolContext, MessageContext>.CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener)
        {
            return CreateMessagingApplicationBuilder(serverFeatures, protocolMessagingContextReceiver, logger, diagnosticListener);
        }

        protected virtual IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> CreateMessagingApplicationBuilder(IFeatureCollection serverFeatures, IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener)
        {
            return new DefaultMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>((Object)serverFeatures, this.ServiceProvider, protocolMessagingContextReceiver, logger, diagnosticListener);
        }
    }
}
