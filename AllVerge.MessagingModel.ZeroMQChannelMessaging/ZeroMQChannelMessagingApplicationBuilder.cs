using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Builder.Default;
    using AllVerge.MessagingModel.MessagingApplication.Hosting.Builder;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    using Microsoft.AspNetCore.Http.Features;

    using Microsoft.Extensions.Logging;

    internal class ZeroMQChannelMessagingApplicationBuilder : 
        DefaultMessagingApplicationBuilder<ZeroMQProtocolContext, ChannelMessageContext>,
        IProtocolApplicationBuilder<IMessagingContext<ZeroMQProtocolContext>>
    {
        public ZeroMQChannelMessagingApplicationBuilder(object serverFeatures, IServiceProvider applicationServices, IMessagingContextReceiver<ZeroMQProtocolContext, ChannelMessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener) :
            base(serverFeatures, applicationServices, protocolMessagingContextReceiver, logger, diagnosticListener)
        {
        }

        protected ZeroMQChannelMessagingApplicationBuilder(ZeroMQChannelMessagingApplicationBuilder builder) :
            base(builder)
        {
        }

        IServiceProvider IProtocolApplicationBuilder<IMessagingContext<ZeroMQProtocolContext>>.ApplicationServices { get => base.ApplicationServices; set => base.ApplicationServices = value; }

        IFeatureCollection IProtocolApplicationBuilder<IMessagingContext<ZeroMQProtocolContext>>.ServerFeatures => base.ServerFeatures;

        IDictionary<string, object> IProtocolApplicationBuilder<IMessagingContext<ZeroMQProtocolContext>>.Properties => base.Properties;

        ContextMiddlewareDelegate<IMessagingContext<ZeroMQProtocolContext>> IProtocolApplicationBuilder<IMessagingContext<ZeroMQProtocolContext>>.Build()
        {
            return Build();
        }

        IProtocolApplicationBuilder<IMessagingContext<ZeroMQProtocolContext>> IProtocolApplicationBuilder<IMessagingContext<ZeroMQProtocolContext>>.New()
        {
            return new ZeroMQChannelMessagingApplicationBuilder(this.ServerFeatures, this.ApplicationServices, this.ProtocolMessagingContextReceiver, this.DiagnosticLogger, this.DiagnosticListener);
        }

        IProtocolApplicationBuilder<IMessagingContext<ZeroMQProtocolContext>> IProtocolApplicationBuilder<IMessagingContext<ZeroMQProtocolContext>>.Use(Func<ContextMiddlewareDelegate<IMessagingContext<ZeroMQProtocolContext>>, ContextMiddlewareDelegate<IMessagingContext<ZeroMQProtocolContext>>> middleware)
        {
            Use(middleware);

            return this;
        }

        protected virtual ContextMiddlewareDelegate<IMessagingContext<ZeroMQProtocolContext>> Build()
        {
            return protocolContext => {
                if (protocolContext.Items.TryGetValue<IMessagingContext<ZeroMQProtocolContext>>(out IMessagingContext<ZeroMQProtocolContext> hostingApplicationContext))
                    return base.BuildContextMiddlewareAsync().ContinueWith(t => t.Result(hostingApplicationContext)).Result;
                throw new InvalidOperationException($"{nameof(protocolContext)}.Items did not contain a value of type {nameof(ZeroMQProtocolContext)}.");
            };
        }

        //protected virtual void Use(Func<ContextMiddlewareDelegate<ZeroMQProtocolContext>, ContextMiddlewareDelegate<ZeroMQProtocolContext>> middleware)
        //{
        //    Func<ContextMiddlewareDelegate<ZeroMQProtocolContext>, ContextMiddlewareDelegate<ZeroMQProtocolContext>> middlewareComponent =
        //        middlewareDelegate =>
        //        {
        //            return
        //                async protocolContext =>
        //                {
        //                    ContextMiddlewareDelegate<ZeroMQProtocolContext> requestDelegate = middleware(async _protocolContext => await middlewareDelegate(protocolContext));

        //                    await requestDelegate(protocolContext);
        //                };
        //        };

        //    base.Use(middlewareComponent);
        //}
    }
}