using System;
using System.Collections.Generic;
using System.Text;

using AllVerge.MessagingModel.MessagingApplication.Builder;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    public interface IMessagingApplicationStartup
    {
        /// <summary>
        /// Must be implicitely implemented on the Startup class.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        IServiceProvider ConfigureServices(IServiceCollection services);
    }

    public interface IMessagingApplicationStartup<MessageContext> :
        IMessagingApplicationStartup
    {
        /// <summary>
        /// Must be implicitely implemented on the Startup class.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="hostApplicationLifetime"></param>
        /// <param name="hostEnvironment"></param>
        /// <param name="loggerFactory"></param>
        void Configure(IMessagingApplicationBuilder<MessageContext> app, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory);
    }

    public interface IMessagingApplicationStartup<ProtocolContext, MessageContext> :
        IMessagingApplicationStartup
    {
        /// <summary>
        /// Must be implicitely implemented on the Startup class.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="hostApplicationLifetime"></param>
        /// <param name="hostEnvironment"></param>
        /// <param name="loggerFactory"></param>
        void Configure(IMessagingApplicationBuilder<ProtocolContext, MessageContext> app, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory);
    }

    public interface IMessagingApplicationStartup<ProtocolContextHost, ProtocolContext, MessageContext> :
        IMessagingApplicationStartup
        where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
    {
        /// <summary>
        /// Must be implicitely implemented on the Startup class.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="hostApplicationLifetime"></param>
        /// <param name="hostEnvironment"></param>
        /// <param name="loggerFactory"></param>
        void Configure(IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> app, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory);
    }
}
