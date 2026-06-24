namespace AllVerge.MessagingModel.Example.HttpChannelMessagingServer
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    internal class ExampleChannelMessagingContextMiddleware
    {
        private readonly MessagingContextMiddlewareDelegate<ChannelMessageContext> _next;
        private readonly IHostEnvironment _hostEnvironment;

        public ExampleChannelMessagingContextMiddleware(MessagingContextMiddlewareDelegate<ChannelMessageContext> next, IHostEnvironment hostEnvironment)
        {
            _next = next;
            _hostEnvironment = hostEnvironment;
        }

        public async Task InvokeAsync(IMessagingContext<ChannelMessageContext> context, ExampleScoped exampleScoped)
        {
            await _next(context);
        }
    }
}