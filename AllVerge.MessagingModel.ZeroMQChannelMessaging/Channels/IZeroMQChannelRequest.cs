using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal interface IZeroMQChannelRequest
    {
        ZeroMQRequestChannel Channel { get; }
        BufferManager BufferManager { get; }
        bool ManualAddressing { get; }
        EndpointAddress To { get; }
        Uri Via { get; }
    }
}