using System;
using System.Collections.Generic;
using System.Text;

using AllVerge.MessagingModel.MessagingApplication;

using AllVerge.MessagingModel.ChannelMessaging;
using AllVerge.MessagingModel.ChannelMessaging.Listeners;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using AllVerge.MessagingModel.MessagingApplication.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer
{
    using AllVerge.MessagingModel.Example.ChannelMessagingServer.Listeners;
    using Microsoft.Extensions.Hosting;
    using System.Threading;

    public class ExampleListenerStartup : BaseMessagingApplicationWithMiddlewareStartup<ExampleMessagingContextMiddleware, ExampleMessageContext> 
    {
        public ExampleListenerStartup(IConfiguration configuration) : base(configuration) { }

        protected override void OnConfigureServices(IServiceCollection services)
        {
            TimeSpan openTimeout = base.Configuration.GetValue<TimeSpan>("OpenTimeout", TimeSpan.Parse("00:01:00"));
            TimeSpan receiveTimeout = base.Configuration.GetValue<TimeSpan>("ReceiveTimeout", TimeSpan.Parse("00:10:00"));
            TimeSpan sendTimeout = base.Configuration.GetValue<TimeSpan>("SendTimeout", TimeSpan.Parse("00:01:00"));
            TimeSpan closeTimeout = base.Configuration.GetValue<TimeSpan>("CloseTimeout", TimeSpan.Parse("00:01:00"));

            services.AddSingleton<IMessagingContextReceiver<ExampleMessageContext>, ExampleMessagingContextReceiver>();

            services.AddSingleton<IMessagingContextChannelListener<ExampleMessageContext>>(new RequestResponseExampleMessagingContextChannelListener(openTimeout, receiveTimeout, sendTimeout, closeTimeout));
            services.AddSingleton<IMessagingContextChannelListener<ExampleMessageContext>>(new AsynchronousRequestResponseExampleMessagingContextChannelListener(openTimeout, receiveTimeout, sendTimeout, closeTimeout));
            services.AddSingleton<IMessagingContextChannelListener<ExampleMessageContext>>(new PollExampleMessagingContextChannelListener(openTimeout, receiveTimeout, sendTimeout, closeTimeout));

            services.AddSingleton<IConfigureOptions<MessagingPollOptions>, MessagingPollOptionsSetup>();
            services.AddSingleton<IConfigureOptions<MessagingReceiveOptions>, MessagingReceiveOptionsSetup>();

            services.AddScoped<ExampleScoped>();
        }

        protected override void OnConfigureMessagingApp(IMessagingApplicationBuilder<ExampleMessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
            base.OnConfigureMessagingApp(messagingApp, loggerFactory, hostEnvironment, applicationStopping);

            messagingApp.UseMessagingApplication<ExampleMessagingMiddleware, ExampleMessageContext>();
        }
    }
}
