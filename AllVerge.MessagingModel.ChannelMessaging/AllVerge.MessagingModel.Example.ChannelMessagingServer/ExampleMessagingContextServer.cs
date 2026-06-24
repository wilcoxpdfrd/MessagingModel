
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Microsoft.Extensions.Hosting;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer
{
    using AllVerge.MessagingModel.ChannelMessaging;

    internal class ExampleMessagingContextServer : 
        MessagingContextServer<ExampleMessageContext>
    {
        public ExampleMessagingContextServer(IOptions<MessagingServerOptions> options, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory) :
            base(options, applicationLifetime, loggerFactory)
        {
        }
    }
}