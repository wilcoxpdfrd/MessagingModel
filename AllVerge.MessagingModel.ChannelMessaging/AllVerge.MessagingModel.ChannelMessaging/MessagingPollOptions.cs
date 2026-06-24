using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public class MessagingPollOptions
    {
        public int MaxMessagesHandledPerSecond { get; set; }
        public double MaxMessagesThrottleByPercent { get; set; }
        public int MaxMessagesRecalculatePeriodSeconds { get; set; }
    }
}
