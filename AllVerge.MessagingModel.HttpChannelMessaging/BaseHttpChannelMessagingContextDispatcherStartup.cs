using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.ServiceModel.Channels;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AllVerge.SystemPrimitives.Reflection;

namespace AllVerge.MessagingModel.HttpChannelMessaging
{
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Builder;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using AllVerge.MessagingModel.MessagingApplication.Http;

    public abstract class BaseHttpChannelMessagingContextDispatcherStartup :
        BaseHttpMessagingContextStartup<ChannelMessagingContextDispatcherMiddleware, ChannelMessageContext>
    {
        protected BaseHttpChannelMessagingContextDispatcherStartup(IConfiguration configuration) : base(configuration)
        {

        }

        protected override void OnConfigureHttpServices(IServiceCollection services)
        {
            services.AddSingleton(s =>
            {
                IMessagingContextReceiver<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext> protocolMessagingContextReceiver =
                    s.GetService<IMessagingContextReceiver<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext>>();

                return protocolMessagingContextReceiver as IProtocolContextAccessorFactory<ProtocolContextHost<HttpContext>>;
            });

            services.AddSingleton(s =>
            {
                IProtocolContextAccessorFactory<ProtocolContextHost<HttpContext>> protocolContextAccessorFactory = s.GetRequiredService<IProtocolContextAccessorFactory<ProtocolContextHost<HttpContext>>>();
                
                protocolContextAccessorFactory.GetProtocolContextAccessor(out IProtocolContextAccessor<ProtocolContextHost<HttpContext>> protocolContextAccessor);
                
                return protocolContextAccessor;
            });

            services.AddSingleton(s =>
            {
                IHttpMessagingContextAccessor httpMessagingContextAccessor = new HttpMessagingContextAccessor(s);

                return httpMessagingContextAccessor;
            });
        }
    }
}
