using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
#if NET6_0_OR_GREATER
using System.Reflection;
#endif
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebSockets;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.ChannelMessaging.Http
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;

    using AllVerge.SystemPrimitives.Collections;
    using System.Threading;

    public abstract class BaseHttpMessagingContextChannelReceiver<MessageContext> :
        BaseMessagingContextReceiver<ProtocolContextHost<HttpContext>, HttpContext, MessageContext> 
        where MessageContext : IMessageContext
    {
        IHttpContextFactory _httpContextFactory;
#if NET6_0_OR_GREATER
        MethodInfo _disposeMethodInfo;
#endif
        protected BaseHttpMessagingContextChannelReceiver(IApplicationHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider services) : 
            base(hostEnvironment, hostApplicationLifetime, services)
        {
#if NET6_0_OR_GREATER
            _httpContextFactory = new DefaultHttpContextFactory(services);
            _disposeMethodInfo = typeof(DefaultHttpContextFactory).GetMethod("Dispose", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
#elif NETSTANDARD2_0
            _httpContextFactory = new HttpContextFactory(Options.Create<FormOptions>(new FormOptions()));
#endif
        }

        protected override ProtocolContextHost<HttpContext> OnCreate(IFeatureCollection contextFeatures)
        {
            HttpContext httpContext;

            httpContext = CreateHttpContext(contextFeatures);

            ProtocolContextHost<HttpContext> context = new ProtocolContextHost<HttpContext>();

            context.ProtocolContext = httpContext;

#if NET6_0_OR_GREATER
            context.InitScope(context => _disposeMethodInfo.Invoke(_httpContextFactory, new object[] { context }));
#elif NETSTANDARD2_0
                    context.InitScope(_httpContextFactory.Dispose);
#endif

            context.StartTimestamp = DateTimeOffset.UtcNow.Ticks;

            httpContext.Items.Add(context);

            return context;
        }

        private static void EnsureItems(HttpContext httpContext)
        {
            if (httpContext.Items == null)

                httpContext.Items = new Dictionary<Object, Object>();
        }

        private HttpContext CreateHttpContext(IFeatureCollection contextFeatures)
        {
            HttpContext httpContext = _httpContextFactory.Create(contextFeatures);

            EnsureItems(httpContext);

            return httpContext;
        }

        protected override void Dispose(ProtocolContextHost<HttpContext> protocolContextHost)
        {
            protocolContextHost.Dispose();
        }

        protected override void Dispose(ProtocolContextHost<HttpContext> protocolContextHost, Exception e)
        {
            this.Logger.LogError(e, $"Disposing {nameof(protocolContextHost)} reason.");

            protocolContextHost.Dispose();
        }
    }
}
