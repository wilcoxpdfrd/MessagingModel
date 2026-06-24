using System;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQConnectionPoolRegistry : ConnectionPoolRegistry
    {
        public ZeroMQConnectionPoolRegistry()
            : base()
        {
        }

        protected override ConnectionPool CreatePool(IConnectionOrientedTransportChannelFactorySettings settings)
        {
            IZeroMQTransportFactorySettings zeroMqSettings = (IZeroMQTransportFactorySettings)settings;

            return new ZeroMQConnectionPool(zeroMqSettings);
        }

        private class ZeroMQConnectionPool : ConnectionPool
        {
            public ZeroMQConnectionPool(IZeroMQTransportFactorySettings settings)
                : base(settings, settings.LeaseTimeout)
            {
            }

            protected override string GetPoolKey(EndpointAddress address, Uri via)
            {
                int port = via.Port;
                //if (port == -1)
                //{
                //    port = TcpUri.DefaultPort;
                //}

                string normalizedHost = via.DnsSafeHost.ToUpperInvariant();

                return string.Format(CultureInfo.InvariantCulture, @"[{0}, {1}]", normalizedHost, port);
            }

            public override bool IsCompatible(IConnectionOrientedTransportChannelFactorySettings settings)
            {
                IZeroMQTransportFactorySettings zeroMqSettings = (IZeroMQTransportFactorySettings)settings;
                return (
                    (LeaseTimeout == zeroMqSettings.LeaseTimeout) &&
                    base.IsCompatible(settings)
                    );
            }
        }
    }
}