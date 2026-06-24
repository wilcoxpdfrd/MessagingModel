using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public class MessagingServerOptions
    {
        public IServiceProvider ApplicationServices { get; set; }
        public UrlPrefixCollection UrlPrefixes { get; set; }
    }
}
