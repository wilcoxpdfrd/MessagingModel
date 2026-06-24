using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using AllVerge.MessagingModel.MessagingApplication.Builder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Builder.Http
{
    public class HttpMessagingApplicationBuilderFactory<MessageContext> :
        IApplicationBuilderFactory
    {
        private IMessagingApplicationBuilderFactory<HttpContext, MessageContext> messagingApplicationBuilderFactory;
        private IMessagingContextReceiver<HttpContext, MessageContext> protocolMessagingContextReceiver;
        private ILogger logger;
        private DiagnosticListener diagnosticListener;

        public HttpMessagingApplicationBuilderFactory(IMessagingApplicationBuilderFactory<HttpContext, MessageContext> messagingApplicationBuilderFactory, IMessagingContextReceiver<HttpContext, MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener)
        {
            this.messagingApplicationBuilderFactory = messagingApplicationBuilderFactory;
            this.protocolMessagingContextReceiver = protocolMessagingContextReceiver;
            this.logger = logger;
            this.diagnosticListener = diagnosticListener;
        }

        IApplicationBuilder IApplicationBuilderFactory.CreateBuilder(IFeatureCollection serverFeatures)
        {
            IMessagingApplicationBuilder<HttpContext, MessageContext>  messagingApplicationBuilder =
                messagingApplicationBuilderFactory.CreateMessagingApplicationBuilder(serverFeatures, this.protocolMessagingContextReceiver, this.logger, this.diagnosticListener);

            return new ApplicationBuilder(messagingApplicationBuilder);
        }

        private class ApplicationBuilder : IApplicationBuilder
        {
            private IMessagingApplicationBuilder<HttpContext, MessageContext> messagingApplicationBuilder;

            public ApplicationBuilder(IMessagingApplicationBuilder<HttpContext, MessageContext> messagingApplicationBuilder)
            {
                this.messagingApplicationBuilder = messagingApplicationBuilder;
            }

            public IServiceProvider ApplicationServices { get => messagingApplicationBuilder.ApplicationServices; set => messagingApplicationBuilder.ApplicationServices = value; }

            public IFeatureCollection ServerFeatures => messagingApplicationBuilder.ServerFeatures;

            public IDictionary<string, object> Properties => messagingApplicationBuilder.Properties;

            public RequestDelegate Build()
            {
                Task<ContextMiddlewareDelegate<HttpContext>> requestDelegateTask = messagingApplicationBuilder.BuildContextMiddlewareAsync();

                return httpContext => requestDelegateTask.ContinueWith(t => t.Result(httpContext)).Result;
            }

            public IApplicationBuilder New()
            {
                return this;
            }

            public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
            {
                Func<ContextMiddlewareDelegate<HttpContext>, ContextMiddlewareDelegate<HttpContext>> middlewareComponent =
                    middlewareDelegate1 =>
                    {
                        RequestDelegate requestDelegate1 = context1 => middlewareDelegate1(context1);
                        var requestDelegate2 = middleware(requestDelegate1);
                        ContextMiddlewareDelegate<HttpContext> middlewareDelegate2 = context2 => requestDelegate2(context2);
                        return middlewareDelegate2;
                    };

                messagingApplicationBuilder.Use(middlewareComponent);

                return this;
            }
        }
    }
}
