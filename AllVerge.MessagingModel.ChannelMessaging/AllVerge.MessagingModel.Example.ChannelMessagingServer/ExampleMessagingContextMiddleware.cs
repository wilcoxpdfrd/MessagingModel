using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using AllVerge.MessagingModel.ChannelMessaging;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer
{
    public class ExampleMessagingContextMiddleware : 
        AbstractMessagingContextMiddleware<ExampleMessageContext>
    {
        static long counter = 0;
        static object tooBusyEncountered = null;

        IHostEnvironment hostEnvironment;

        public ExampleMessagingContextMiddleware(MessagingContextMiddlewareDelegate<ExampleMessageContext> next, IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken) : 
            base(next, serviceProvider, loggerFactory, hostEnvironment, cancellationToken) { }

        protected override void OnInit(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken)
        {
            this.hostEnvironment = hostEnvironment;
        }

        protected override Task OnInvokeAsync(IMessagingContext<ExampleMessageContext> messagingContext)
        {
            if (!this.CancellationToken.IsCancellationRequested)
            {
                long last = Interlocked.Increment(ref counter);

                Console.WriteLine($"Handling {messagingContext.InputContext}");

                if (last % 27 == 0 && Interlocked.CompareExchange(ref tooBusyEncountered, new Object(), null) == null)

                    // only send too busy once, so we can test the timer based throttle up logic ...

                    messagingContext.Output(messagingContext.InputContext, MiddlewarePipelineResult.TooBusy); // throttle

                else if (last % 9 == 0)

                    messagingContext.Output(messagingContext.InputContext, MiddlewarePipelineResult.NotHandled); // acknowledge

                else if (last % 18 == 0)

                    messagingContext.Output(new ExampleMessageContext(new Exception("This is a fault.")), MiddlewarePipelineResult.Faulted); // retry

                else

                    messagingContext.Output(messagingContext.InputContext, MiddlewarePipelineResult.Completed); // acknowledge
            }

            return Task.CompletedTask;
        }
    }
}
