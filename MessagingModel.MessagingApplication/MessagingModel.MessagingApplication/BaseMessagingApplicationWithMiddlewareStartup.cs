using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AllVerge.MessagingModel.MessagingApplication.Builder;

using Microsoft.Extensions.Configuration;

using System.Threading;
using Microsoft.Extensions.Hosting;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using Microsoft.Extensions.Logging;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public abstract class BaseMessagingApplicationWithMiddlewareStartup<ProtocolMessagingContextMiddleware, MessageContext> :
        BaseMessagingApplicationStartup<MessageContext>
        where ProtocolMessagingContextMiddleware : IMessagingApplicationContextMiddleware<MessageContext>
    {
        protected BaseMessagingApplicationWithMiddlewareStartup(IConfiguration configuration) : base(configuration) { }

        protected override sealed void ConfigureMessagingApp(IMessagingApplicationBuilder<MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
            messagingApp.UseMessagingApplication<ProtocolMessagingContextMiddleware, MessageContext>(loggerFactory, hostEnvironment, applicationStopping);

            OnConfigureMessagingApp(messagingApp, loggerFactory, hostEnvironment, applicationStopping);
        }

        /// <summary>
        /// Called by the runtime; override to configure the messaging pipeline.
        /// </summary>
        /// <param name="messagingApp">The application builder to set up the pipeline with</param>
        /// <param name="loggerFactory">logger configuration object</param>
        /// <param name="hostEnvironment">object containing environment information</param>
        /// <param name="applicationStopping">Application stopping <see cref="CancellationToken"/></param>
        protected virtual void OnConfigureMessagingApp(IMessagingApplicationBuilder<MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
        }
    }

    public abstract class BaseMessagingApplicationWithMiddlewareStartup<ProtocolMessagingContextMiddleware, ProtocolContext, MessageContext> :
        BaseMessagingApplicationStartup<ProtocolContext, MessageContext>
        where ProtocolMessagingContextMiddleware : IMessagingApplicationContextMiddleware<MessageContext>
    {
        protected BaseMessagingApplicationWithMiddlewareStartup(IConfiguration configuration) : base(configuration) { }

        protected override sealed void ConfigureMessagingApp(IMessagingApplicationBuilder<ProtocolContext, MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
            messagingApp.UseMessagingApplication<ProtocolMessagingContextMiddleware, ProtocolContext, MessageContext>(loggerFactory, hostEnvironment, applicationStopping);

            OnConfigureMessagingApp(messagingApp, loggerFactory, hostEnvironment, applicationStopping);
        }

        /// <summary>
        /// Called by the runtime; override to configure the messaging pipeline.
        /// </summary>
        /// <param name="messagingApp">The application builder to set up the pipeline with</param>
        /// <param name="loggerFactory">logger configuration object</param>
        /// <param name="hostEnvironment">object containing environment information</param>
        /// <param name="applicationStopping">Application stopping <see cref="CancellationToken"/></param>
        protected virtual void OnConfigureMessagingApp(IMessagingApplicationBuilder<ProtocolContext, MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
        }
    }

    public abstract class BaseMessagingApplicationWithHostingApplicationContextAndMiddlewareStartup<ProtocolMessagingContextMiddleware, ProtocolContext, MessageContext> :
        BaseMessagingApplicationWithProtocolContextHostStartup<ProtocolContext, MessageContext>
        where ProtocolMessagingContextMiddleware : IMessagingApplicationContextMiddleware<MessageContext>
    {
        protected BaseMessagingApplicationWithHostingApplicationContextAndMiddlewareStartup(IConfiguration configuration) : base(configuration) { }

        protected override sealed void ConfigureMessagingApp(IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
            messagingApp.UseMessagingApplication<ProtocolMessagingContextMiddleware, ProtocolContext, MessageContext>(loggerFactory, hostEnvironment, applicationStopping);

            OnConfigureMessagingApp(messagingApp, loggerFactory, hostEnvironment, applicationStopping);
        }

        /// <summary>
        /// Called by the runtime; override to configure the messaging pipeline.
        /// </summary>
        /// <param name="messagingApp">The application builder to set up the pipeline with</param>
        /// <param name="loggerFactory">logger configuration object</param>
        /// <param name="hostEnvironment">object containing environment information</param>
        /// <param name="applicationStopping">Application stopping <see cref="CancellationToken"/></param>
        protected virtual void OnConfigureMessagingApp(IMessagingApplicationBuilder<ProtocolContextHost<ProtocolContext>, ProtocolContext, MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
        }
    }
}
