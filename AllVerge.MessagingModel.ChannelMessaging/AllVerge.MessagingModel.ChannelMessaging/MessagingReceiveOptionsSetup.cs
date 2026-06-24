using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public class MessagingReceiveOptionsSetup : IConfigureOptions<MessagingReceiveOptions>
    {
        private IConfiguration _configuration;

        public MessagingReceiveOptionsSetup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(MessagingReceiveOptions options)
        {
            options.MaxAcceptedChannels = _configuration.GetValue<Int32>($"{nameof(MessagingReceiveOptions)}:{nameof(MessagingReceiveOptions.MaxAcceptedChannels)}", 100);

            options.MaxMessagesQueueDepth = _configuration.GetValue<Int32>($"{nameof(MessagingReceiveOptions)}:{nameof(MessagingReceiveOptions.MaxMessagesQueueDepth)}", 1000);

            options.ReceiveTimeout = _configuration.GetValue<TimeSpan>($"{nameof(MessagingReceiveOptions)}:{nameof(MessagingReceiveOptions.ReceiveTimeout)}", TimeSpan.Parse("00:02:00"));

            options.CloseChannelTimeout = _configuration.GetValue<TimeSpan>($"{nameof(MessagingReceiveOptions)}:{nameof(MessagingReceiveOptions.CloseChannelTimeout)}", TimeSpan.Parse("00:02:00"));
        }
    }
}