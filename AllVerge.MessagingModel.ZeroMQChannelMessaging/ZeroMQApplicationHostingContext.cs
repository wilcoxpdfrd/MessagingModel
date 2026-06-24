using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public struct ZeroMQApplicationHostingContext
    {
        public ZeroMQProtocolContext ProtocolContext { get; internal set; }
        public IDisposable Scope { get; internal set; }
    }
}
