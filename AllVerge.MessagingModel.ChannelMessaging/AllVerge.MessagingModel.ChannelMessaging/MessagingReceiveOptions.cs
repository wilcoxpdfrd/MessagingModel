using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public class MessagingReceiveOptions
    {
        public int MaxAcceptedChannels { get; set; }
        public int MaxMessagesQueueDepth { get; set; }
        public TimeSpan ReceiveTimeout { get; set; }
        public TimeSpan CloseChannelTimeout { get; set; }
    }
}
