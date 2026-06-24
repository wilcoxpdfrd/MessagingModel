
using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllVerge.MessagingModel.MessagingApplication.Http
{
    using AllVerge.SystemPrimitives.Reflection;

    using AllVerge.MessagingModel.MessagingApplication.Builder;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using System.Threading;

    public abstract class BaseHttpMessagingContextStartup<HttpContextMessagingMiddleware, MessageContext> :
        BaseMessagingApplicationWithHostingApplicationContextAndMiddlewareStartup<HttpContextMessagingMiddleware, HttpContext, MessageContext>
        where HttpContextMessagingMiddleware : IMessagingApplicationContextMiddleware<MessageContext>
    {
        public BaseHttpMessagingContextStartup(IConfiguration configuration) : base(configuration) { }

        protected abstract ImplementationTypeInfo<IMessagingContextReceiver<ProtocolContextHost<HttpContext>, HttpContext, MessageContext>> GetProtocolMessagingContextReceiverTypeInfo();

        protected sealed override void OnConfigureServices(IServiceCollection services)
        {
            services.AddProtocolMessagingContextReceiver<ProtocolContextHost<HttpContext>, HttpContext, MessageContext>(GetProtocolMessagingContextReceiverTypeInfo().ImplementationType);

            OnConfigureHttpServices(services);
        }

        protected virtual void OnConfigureHttpServices(IServiceCollection services)
        {
        }

        /// <summary>
        /// Configure in StartUp class defines the middleware for the request pipeline.
        /// This method gets called by the runtime after ConfigureServices. Use this method to configure the HTTP request pipeline.
        ///
        /// Because the order of items within the pipeline is important, any child classes
        /// who override this method should not call base but should rather implement this functionality themselves.
        /// </summary>
        /// <param name="app">The application builder to set up the pipeline with</param>
        /// <param name="hostApplicationLifetime">Provides access to the application lifetime events</param>
        /// <param name="env">object containing environment information</param>
        /// <param name="loggerFactory">logger configuration object</param>
        protected override void OnConfigureMessagingApp(IMessagingApplicationBuilder<ProtocolContextHost<HttpContext>, HttpContext, MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
            if (messagingApp is IApplicationBuilder)
            {
                IApplicationBuilder app  =
                    messagingApp as IApplicationBuilder;

                OnConfigureHttpMessagingApp(app, messagingApp, loggerFactory, hostEnvironment, applicationStopping);
            }
            else

                throw new ArgumentException($"Parameter does not implement {nameof(IMessagingApplicationBuilder<HttpContext, MessageContext>)}", nameof(messagingApp));
        }

        protected virtual void OnConfigureHttpMessagingApp(IApplicationBuilder app, IMessagingApplicationBuilder<ProtocolContextHost<HttpContext>, HttpContext, MessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
            messagingApp.UseMessagingApplication<HttpContextMessagingMiddleware, HttpContext, MessageContext>(loggerFactory, hostEnvironment, applicationStopping);
        }
    }
}
