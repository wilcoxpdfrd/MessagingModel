using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
	internal interface ITransferTransportFactorySettings : ITransportFactorySettings
	{
        bool KeepAliveEnabled { get; }
        int MaxBufferSize { get; }
        TransferMode TransferMode { get; }
    }
}