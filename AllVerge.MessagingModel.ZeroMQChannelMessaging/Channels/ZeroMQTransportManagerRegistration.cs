using AllVerge.MessagingModel.MarkupPrimitives.Xml;
using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQTransportManagerRegistration : TransportManagerRegistration
    {
        private ZeroMQTransportManager transportManager;

        public ZeroMQTransportManagerRegistration(Uri listenUri, ZeroMQConnectionOrientedChannelListener channelListener) : 
            base(listenUri, HostNameComparisonMode.Exact)
        {
            this.ConnectionBufferSize = channelListener.ConnectionBufferSize;
            this.ChannelInitializationTimeout = channelListener.ChannelInitializationTimeout;
            this.MaxOutputDelay = channelListener.MaxOutputDelay;
            this.MaxPendingConnections = channelListener.MaxPendingConnections;
            this.MaxPendingAccepts = channelListener.MaxPendingAccepts;
            this.IdleTimeout = channelListener.IdleTimeout;
            this.MaxPooledConnections = channelListener.MaxPooledConnections;
            this.Scheme = channelListener.Scheme;
            this.ExposeConnectionProperty = channelListener.ExposeConnectionProperty;
        }

        public int ConnectionBufferSize { get; }
        public TimeSpan ChannelInitializationTimeout { get; }
        public TimeSpan MaxOutputDelay { get; }
        public int MaxPendingConnections { get; }
        public int MaxPendingAccepts { get; }
        public TimeSpan IdleTimeout { get; }
        public int MaxPooledConnections { get; }
        public string Scheme { get; }
        internal bool ExposeConnectionProperty { get; }

        public override IList<TransportManager> Select(TransportChannelListener channelListener)
        {
            List<TransportManager> selection = new List<TransportManager>();

            ZeroMQConnectionOrientedChannelListener zeroMQChannelListener = (ZeroMQConnectionOrientedChannelListener)channelListener;

            if (!this.IsCompatible(zeroMQChannelListener))

                return null;

            if (this.transportManager == null)
            {
                this.transportManager = new ZeroMQTransportManager(this, zeroMQChannelListener);
            }

            selection.Add(this.transportManager);

            return selection;
        }

        private bool IsCompatible(ZeroMQConnectionOrientedChannelListener channelListener)
        {
            return (true
                && (this.ChannelInitializationTimeout == channelListener.ChannelInitializationTimeout)
                && (this.IdleTimeout == channelListener.IdleTimeout)
                && (this.MaxPooledConnections == channelListener.MaxPooledConnections)
                && (this.ConnectionBufferSize == channelListener.ConnectionBufferSize)
                && (this.MaxPendingConnections == channelListener.MaxPendingConnections)
                && (this.MaxOutputDelay == channelListener.MaxOutputDelay)
                && (this.MaxPendingAccepts == channelListener.MaxPendingAccepts));
        }

        public void OnClose(ZeroMQTransportManager manager)
        {
            if (manager == this.transportManager)
            {
                this.transportManager = null;
            }
            else
            {
                Fx.Assert("Unknown transport manager passed to OnClose().");
            }

            if ((this.transportManager == null))
            {
                ZeroMQConnectionOrientedChannelListener.StaticTransportManagerTable.UnregisterUri(this.ListenUri, this.HostNameComparisonMode);
            }
        }
    }
}