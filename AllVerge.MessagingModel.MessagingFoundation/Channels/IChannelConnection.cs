using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    public enum ChannelState
    {
        Open,
        Aborting,
        Closing,
        Closed,
    }

    public interface IChannelConnection : IConnection
    {
        string ConnectionId { get; }

        IPEndPoint LocalIPEndPoint { get; }

        EndpointAddress RemoteAddress { get; }

        EndpointAddress LocalAddress { get; }

        ChannelState State { get; }
    }
}
