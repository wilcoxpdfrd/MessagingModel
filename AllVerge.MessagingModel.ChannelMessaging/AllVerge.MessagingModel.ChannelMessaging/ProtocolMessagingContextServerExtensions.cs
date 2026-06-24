using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public static class ProtocolMessagingContextServerExtensions
    {
        public static IWebHostBuilder UseMessagingContextServer<MessagingContextServer, MessageContext>(this IWebHostBuilder hostBuilder) 
            where MessageContext : IMessageContext where MessagingContextServer : MessagingContextServer<MessageContext>
        {
            return hostBuilder.ConfigureServices(services => 
            {
                services.AddTransient<IConfigureOptions<MessagingServerOptions>, MessagingServerOptionsSetup>();
                services.AddSingleton<IServer, MessagingContextServer>();
            });
        }

        public static IWebHostBuilder UseMessagingContextServer<MessagingContextServer, MessageContext>(this IWebHostBuilder hostBuilder, Action<MessagingServerOptions> options)
            where MessageContext : IMessageContext where MessagingContextServer : MessagingContextServer<MessageContext>
        {
            return hostBuilder.UseMessagingContextServer<MessagingContextServer, MessageContext>().ConfigureServices(services => 
            {
                OptionsServiceCollectionExtensions.Configure<MessagingServerOptions>(services, options);
            });
        }

        public static IWebHostBuilder UseProtocolMessagingContextServer<ProtocolMessagingContextServer, ProtocolContext, MessageContext>(this IWebHostBuilder hostBuilder)
            where MessageContext : IMessageContext where ProtocolMessagingContextServer : MessagingContextServer<ProtocolContext, MessageContext>
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddTransient<IConfigureOptions<MessagingServerOptions>, MessagingServerOptionsSetup>(); 
                services.AddSingleton<IServer, ProtocolMessagingContextServer>();
            });
        }

        public static IWebHostBuilder UseProtocolMessagingContextServer<ProtocolMessagingContextServer, ProtocolContext, MessageContext>(this IWebHostBuilder hostBuilder, Action<MessagingServerOptions> options)
            where MessageContext : IMessageContext where ProtocolMessagingContextServer : MessagingContextServer<ProtocolContext, MessageContext>
        {
            return hostBuilder.UseProtocolMessagingContextServer<ProtocolMessagingContextServer, ProtocolContext, MessageContext>().ConfigureServices(services =>
            {
                OptionsServiceCollectionExtensions.Configure<MessagingServerOptions>(services, options);
            });
        }
    }
}
