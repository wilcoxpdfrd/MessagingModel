using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System.Net;
using System.Diagnostics;

using AllVerge.MessagingModel.MessagingApplication.Builder.Default;

namespace AllVerge.MessagingModel.MessagingApplication.Builder.Http
{
    public class HttpContextMessagingApplicationBuilder<MessageContext> :
        DefaultMessagingApplicationBuilder<HttpContext, MessageContext>, 
        IApplicationBuilder
    {
        public HttpContextMessagingApplicationBuilder(object server, IServiceProvider serviceProvider, IMessagingContextReceiver<HttpContext, MessageContext> protocolMessagingContextFactory, ILogger logger, DiagnosticListener diagnosticListener)
            : base(server, serviceProvider, protocolMessagingContextFactory, logger, diagnosticListener)
        {
        }

        protected HttpContextMessagingApplicationBuilder(HttpContextMessagingApplicationBuilder<MessageContext> builder) :
            base(builder)
        {
        }

        IServiceProvider IApplicationBuilder.ApplicationServices { get => base.ApplicationServices; set => base.ApplicationServices = value; }

        RequestDelegate IApplicationBuilder.Build()
        {
            return Build();
        }

        IApplicationBuilder IApplicationBuilder.New()
        {
            return new HttpContextMessagingApplicationBuilder<MessageContext>(this);
        }

        IApplicationBuilder IApplicationBuilder.Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            Use(middleware);

            return this;
        }

        protected virtual RequestDelegate Build()
        {
            return httpContext => base.BuildContextMiddlewareAsync().ContinueWith(t => t.Result(httpContext)).Result;
        }

        protected virtual void Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            Func<ContextMiddlewareDelegate<HttpContext>, ContextMiddlewareDelegate<HttpContext>> protocolContextMiddlewareComponent =
                middlewareDelegate =>
                {
                    RequestDelegate requestDelegate = middleware(httpContext => middlewareDelegate(httpContext));

                    return httpContext => requestDelegate(httpContext);
                };

            base.Use(protocolContextMiddlewareComponent);
        }

        protected override Task NotBoundAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            return Task.CompletedTask;
        }
    }
}