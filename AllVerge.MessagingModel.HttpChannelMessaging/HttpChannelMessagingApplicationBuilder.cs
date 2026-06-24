using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.HttpChannelMessaging
{
    using AllVerge.SystemPrimitives.Collections;

    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Builder.Default;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    internal class HttpChannelMessagingApplicationBuilder :
        DefaultMessagingApplicationBuilder<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext>,
        IApplicationBuilder
    {
        public HttpChannelMessagingApplicationBuilder(object serverFeatures, IServiceProvider applicationServices, IMessagingContextReceiver<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener) : 
            base(serverFeatures, applicationServices, protocolMessagingContextReceiver, logger, diagnosticListener)
        {
        }

        protected HttpChannelMessagingApplicationBuilder(HttpChannelMessagingApplicationBuilder builder) :
            base(builder)
        {
        }

        IServiceProvider IApplicationBuilder.ApplicationServices {  get => base.ApplicationServices; set => base.ApplicationServices = value; }

        IFeatureCollection IApplicationBuilder.ServerFeatures => base.ServerFeatures;

        IDictionary<string, object> IApplicationBuilder.Properties => base.Properties;

        RequestDelegate IApplicationBuilder.Build()
        {
            return Build();
        }

        IApplicationBuilder IApplicationBuilder.New()
        {
            return new HttpChannelMessagingApplicationBuilder(this.ServerFeatures, this.ApplicationServices, this.ProtocolMessagingContextReceiver, this.DiagnosticLogger, this.DiagnosticListener);
        }

        IApplicationBuilder IApplicationBuilder.Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            Use(middleware);

            return this;
        }

        protected virtual RequestDelegate Build()
        {
            return httpContext => {
                if (httpContext.Items.TryGetValue<ProtocolContextHost<HttpContext>>(out ProtocolContextHost<HttpContext> hostingApplicationContext))
                    return base.BuildContextMiddlewareAsync().ContinueWith(t => t.Result(hostingApplicationContext)).Result;
                throw new InvalidOperationException($"{nameof(httpContext)}.Items did not contain a value of type {typeof(ProtocolContextHost<HttpContext>).Name}.");
            };
        }

        protected virtual void Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            Func<ContextMiddlewareDelegate<ProtocolContextHost<HttpContext>>, ContextMiddlewareDelegate<ProtocolContextHost<HttpContext>>> middlewareComponent =
                middlewareDelegate =>
                {
                    return
                        async protocolContextHost =>
                        {
                            RequestDelegate requestDelegate = middleware(async httpContext => await middlewareDelegate(protocolContextHost));

                            await requestDelegate(protocolContextHost.ProtocolContext); 
                        };
                };

            base.Use(middlewareComponent);
        }
    }
}
