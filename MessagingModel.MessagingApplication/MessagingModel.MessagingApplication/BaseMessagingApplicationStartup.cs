using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

using AllVerge.MessagingModel.MessagingApplication.Builder;
using AllVerge.MessagingModel.MessagingApplication.Hosting;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public abstract class AbstractMessagingApplicationStartup :
        IMessagingApplicationStartup
    {
        protected AbstractMessagingApplicationStartup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        protected IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();

            OnConfigureServices(services);

            return services.BuildServiceProvider();
        }

        protected abstract void OnConfigureServices(IServiceCollection services);
    }

    public abstract class BaseMessagingApplicationStartup<MessageContext> :
        AbstractMessagingApplicationStartup,
        IMessagingApplicationStartup<MessageContext>
    {
        protected BaseMessagingApplicationStartup(IConfiguration configuration) : base(configuration) { }

        /// <param name="messagingApp">The application builder to set up the pipeline with</param>
        /// <param name="hostApplicationLifetime">Provides access to the application lifetime events</param>
        /// <param name="hostEnvironment">object containing environment information</param>
        /// <param name="loggerFactory">logger configuration object</param>
        public void Configure(IMessagingApplicationBuilder<MessageContext> messagingApp, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
        {
            DisposalExtensions.SetApplicationStoppingToken(hostApplicationLifetime.ApplicationStopping);

            ConfigureMessagingApp(messagingApp, loggerFactory, hostEnvironment, hostApplicationLifetime.ApplicationStopping);
        }

        /// <summary>
        /// Called by the runtime; override to configure the messaging pipeline.
        /// </summary>
        /// <param name="messagingApp">The application builder to set up the pipeline with</param>
        /// <param name="hostApplicationLifetime">Provides access to the application lifetime events</param>
        /// <param name="hostEnvironment">object containing environment information</param>
        /// <param name="loggerFactory">logger configuration object</param>
        /// <param name="applicationStopping">Application stopping <see cref="CancellationToken"/></param>
        protected abstract void ConfigureMessagingApp(IMessagingApplicationBuilder<MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping);
    }

    public abstract class BaseMessagingApplicationStartup<ProtocolContext, MessageContext> :
        AbstractMessagingApplicationStartup,
        IMessagingApplicationStartup<ProtocolContext, MessageContext>
    {
        protected BaseMessagingApplicationStartup(IConfiguration configuration) : base(configuration) { }

        /// <param name="messagingApp">The application builder to set up the pipeline with</param>
        /// <param name="hostApplicationLifetime">Provides access to the application lifetime events</param>
        /// <param name="hostEnvironment">object containing environment information</param>
        /// <param name="loggerFactory">logger configuration object</param>
        public void Configure(IMessagingApplicationBuilder<ProtocolContext, MessageContext> messagingApp, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
        {
            DisposalExtensions.SetApplicationStoppingToken(hostApplicationLifetime.ApplicationStopping);

            ConfigureMessagingApp(messagingApp, loggerFactory, hostEnvironment, hostApplicationLifetime.ApplicationStopping);
        }

        /// <summary>
        /// Called by the runtime; override to configure the messaging pipeline.
        /// </summary>
        /// <param name="messagingApp">The application builder to set up the pipeline with</param>
        /// <param name="loggerFactory">logger configuration object</param>
        /// <param name="hostEnvironment">object containing environment information</param>
        /// <param name="applicationStopping">Application stopping <see cref="CancellationToken"/></param>
        protected abstract void ConfigureMessagingApp(IMessagingApplicationBuilder<ProtocolContext, MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping);
    }

    public abstract class BaseMessagingApplicationWithProtocolContextHostStartup<ProtocolContext, MessageContext> :
        AbstractMessagingApplicationStartup,
        IMessagingApplicationStartup<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext>
    {
        protected BaseMessagingApplicationWithProtocolContextHostStartup(IConfiguration configuration) : base(configuration) { }

        /// <param name="messagingApp">The application builder to set up the pipeline with</param>
        /// <param name="loggerFactory">logger configuration object</param>
        /// <param name="hostEnvironment">object containing environment information</param>
        /// <param name="applicationStopping">Application stopping <see cref="CancellationToken"/></param>
        public void Configure(IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> messagingApp, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
        {
            DisposalExtensions.SetApplicationStoppingToken(hostApplicationLifetime.ApplicationStopping);

            ConfigureMessagingApp(messagingApp, loggerFactory, hostEnvironment, hostApplicationLifetime.ApplicationStopping);
        }

        /// <summary>
        /// Called by the runtime; override to configure the messaging pipeline.
        /// </summary>
        /// <param name="messagingApp">The application builder to set up the pipeline with</param>
        /// <param name="loggerFactory">logger configuration object</param>
        /// <param name="hostEnvironment">object containing environment information</param>
        /// <param name="applicationStopping">Application stopping <see cref="CancellationToken"/></param>
        protected abstract void ConfigureMessagingApp(IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping);
    }
}
