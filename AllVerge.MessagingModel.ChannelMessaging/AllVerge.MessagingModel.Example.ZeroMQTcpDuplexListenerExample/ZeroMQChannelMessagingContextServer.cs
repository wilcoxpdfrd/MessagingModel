using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AllVerge.MessageProcessor.ZeroMQ.TcpDuplexListenerExample
{
    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.ZeroMQChannelMessaging;

    internal class ZeroMQChannelMessagingContextServer : MessagingContextServer<ZeroMQProtocolContext, ChannelMessageContext>
    {
        public ZeroMQChannelMessagingContextServer(IOptions<MessagingServerOptions> options, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory) : base(options, applicationLifetime, loggerFactory)
        {
        }
    }
}