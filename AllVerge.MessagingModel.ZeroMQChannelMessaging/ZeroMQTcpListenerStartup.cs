using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Description;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Hosting;

    using AllVerge.MessagingModel.MessagingApplication.Builder;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.ChannelMessaging.Listeners;
    using Microsoft.Extensions.Options;
    using AllVerge.MessagingModel.ChannelMessaging;

    public class ZeroMQTcpListenerStartup : BaseMessagingApplicationWithMiddlewareStartup<ChannelMessagingContextDispatcherMiddleware, ZeroMQProtocolContext, ChannelMessageContext>
    {
        public ZeroMQTcpListenerStartup(IConfiguration configuration) : base(configuration) { }

        protected override void OnConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(s =>
            {
                IMessagingContextReceiver<ZeroMQProtocolContext, ChannelMessageContext> protocolMessagingContextReceiver = s.GetService<IMessagingContextReceiver<ZeroMQProtocolContext, ChannelMessageContext>>();

                return protocolMessagingContextReceiver as IProtocolContextAccessorFactory<IMessagingContext<ZeroMQProtocolContext>>;
            });

            services.AddSingleton(s => 
            {
                IProtocolContextAccessorFactory<IMessagingContext<ZeroMQProtocolContext>> requiredService = s.GetRequiredService<IProtocolContextAccessorFactory<IMessagingContext<ZeroMQProtocolContext>>>();

                requiredService.GetProtocolContextAccessor(out var protocolContextAccessor);

                return protocolContextAccessor;
            });

            services.AddSingleton<IMessagingContextReceiver<ZeroMQProtocolContext, ChannelMessageContext>, ZeroMQMessagingContextReceiver>();

            services.AddSingleton<IMessagingContextChannelListener<ZeroMQProtocolContext, ChannelMessageContext>>(new ZeroMQAsynchronousMessagingContextChannelListener());

            services.AddSingleton<IConfigureOptions<MessagingReceiveOptions>, MessagingReceiveOptionsSetup>();
        }
    }
}
