using Microsoft.Extensions.Hosting;
using AllVerge.MessagingModel.MessagingApplication;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer
{
    using AllVerge.MessagingModel.MessagingApplication.Hosting;

    internal class ExampleMessagingMiddleware
    {
        private readonly MessagingContextMiddlewareDelegate<ExampleMessageContext> _next;
        private readonly IHostEnvironment _hostEnvironment;

        public ExampleMessagingMiddleware(MessagingContextMiddlewareDelegate<ExampleMessageContext> next, IHostEnvironment hostEnvironment)
        {
            _next = next;
            _hostEnvironment = hostEnvironment;
        }

        public async Task InvokeAsync(IMessagingContext<ExampleMessageContext> context, ExampleScoped exampleScoped)
        {
            await _next(context);
        }
    }
}