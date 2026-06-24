using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public class MessagingPollOptionsSetup : IConfigureOptions<MessagingPollOptions>
    {
        private IConfiguration _configuration;

        public MessagingPollOptionsSetup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(MessagingPollOptions options)
        {
            options.MaxMessagesHandledPerSecond = _configuration.GetValue<Int32>($"{nameof(MessagingPollOptions)}:{nameof(MessagingPollOptions.MaxMessagesHandledPerSecond)}", 100);
            options.MaxMessagesThrottleByPercent = _configuration.GetValue<Double>($"{nameof(MessagingPollOptions)}:{nameof(MessagingPollOptions.MaxMessagesThrottleByPercent)}", 0.9);
            options.MaxMessagesRecalculatePeriodSeconds = _configuration.GetValue<Int32>($"{nameof(MessagingPollOptions)}:{nameof(MessagingPollOptions.MaxMessagesRecalculatePeriodSeconds)}", 30); ;
        }
    }
}