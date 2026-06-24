
//using Microsoft.Extensions.Options;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace AllVerge.MessagingModel.Example.HttpChannelMessagingServer
{
    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    internal class ExampleMessagingContextServer : 
        MessagingContextServer<ChannelMessageContext>
    {
        public ExampleMessagingContextServer(IOptions<MessagingServerOptions> options, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory) :
            base(options, applicationLifetime, loggerFactory)
        {
        }
    }
}