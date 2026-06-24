using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Example.HttpChannelMessagingServer
{
    using AllVerge.MessagingModel.MessagingApplication;

    using AllVerge.MessagingModel.ChannelMessaging;

    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Faults;

    using ChannelMessage = System.ServiceModel.Channels.Message;

    public class ExampleMessagingContextMiddleware : 
        AbstractMessagingContextMiddleware<ChannelMessageContext>
    {
        static long counter = 0;
        static object? tooBusyEncountered = null;

        public ExampleMessagingContextMiddleware(MessagingContextMiddlewareDelegate<ChannelMessageContext> next, IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken) : 
            base(next, serviceProvider, loggerFactory, hostEnvironment, cancellationToken) { }

        protected override void OnInit(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken)
        {
        }

        protected override Task OnInvokeAsync(IMessagingContext<ChannelMessageContext> protocolMessagingContext)
        {
            if (!CancellationToken.IsCancellationRequested)
            {
                long last = Interlocked.Increment(ref counter);

                Console.WriteLine($"Handling {protocolMessagingContext.InputContext}");

                if (last % 27 == 0 && Interlocked.CompareExchange(ref tooBusyEncountered, new Object(), null) == null)

                    // only send too busy once, so we can test the timer based throttle up logic ...

                    protocolMessagingContext.Output(protocolMessagingContext.InputContext, MiddlewarePipelineResult.TooBusy); // throttle

                else if (last % 9 == 0)

                    protocolMessagingContext.Output(protocolMessagingContext.InputContext, MiddlewarePipelineResult.NotHandled); // acknowledge

                else if (last % 18 == 0)

                    protocolMessagingContext.Output(
                        ChannelMessageContext.Create(
                            protocolMessagingContext.InputContext,
                            ChannelMessage.CreateMessage(
                                protocolMessagingContext.InputContext.Message.Version,
                                new Exception("This is a fault.").CreateFault(
                                    FaultCodes.ServerErrorCode.InternalServerError, 
                                    protocolMessagingContext.InputContext.Message.Version),
                                protocolMessagingContext.InputContext.Message.Version.Addressing.FaultAction)
                            ), 
                        MiddlewarePipelineResult.Faulted); // retry

                else

                    protocolMessagingContext.Output(protocolMessagingContext.InputContext, MiddlewarePipelineResult.Completed); // acknowledge
            }

            return Task.CompletedTask;
        }
    }
}
