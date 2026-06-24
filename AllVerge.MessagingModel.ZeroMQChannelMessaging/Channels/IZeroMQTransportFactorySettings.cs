using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public interface IZeroMQTransportFactorySettings : IConnectionOrientedTransportChannelFactorySettings
    {
        TimeSpan LeaseTimeout { get; }
        int MaxBufferPoolSize { get; }
        new int MaxReceivedMessageSize { get; }
    }
}
