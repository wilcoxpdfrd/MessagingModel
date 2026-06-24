using System;

namespace AllVerge.Core.ServiceModel.Channels.ZeroMQ
{
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration;

    /// <summary>
    /// ZeroMQ implementation of <see cref="TransferTransportFactorySettings"/>.  Default constructor specifies <see cref="ZeroMQTransportProtocols.TCP"/> is used.
    /// </summary>
    public class ZeroMQTransferTransportFactorySettings : TransferTransportFactorySettings
    {
        /// <summary>
        /// Constructs an instance of <see cref="ZeroMQTransferTransportFactorySettings"/> specifying <see cref="ZeroMQTransportProtocols.TCP"/> protocol.
        /// </summary>
        public ZeroMQTransferTransportFactorySettings()
        {
            this.Protocol = ZeroMQTransportProtocols.TCP;
        }

        /// <summary>
        /// Constructs an instance of <see cref="ZeroMQTransferTransportFactorySettings"/> specifying <paramref name="protocol"/>.
        /// </summary>
        /// <param name="protocol"></param>
        public ZeroMQTransferTransportFactorySettings(ZeroMQTransportProtocols protocol)
        {
            this.Protocol = protocol;
        }

        TimeSpan MaxTimespan = TimeSpan.FromMilliseconds(Int32.MaxValue);

        public ZeroMQTransportProtocols Protocol { get; }
        public override TimeSpan CloseTimeout => MaxTimespan;
        public override TimeSpan OpenTimeout => MaxTimespan;
        public override TimeSpan ReceiveTimeout => MaxTimespan;
        public override TimeSpan SendTimeout => MaxTimespan;
    }
}
