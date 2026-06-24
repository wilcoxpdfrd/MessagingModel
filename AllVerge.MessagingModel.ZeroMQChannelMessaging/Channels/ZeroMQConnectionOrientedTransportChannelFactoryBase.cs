using System;
using System.Collections.Generic;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal abstract class ZeroMQConnectionOrientedTransportChannelFactoryBase<TChannel> : 
        ConnectionOrientedTransportChannelFactory<TChannel>, IZeroMQTransportFactorySettings
    {
        private static ZeroMQConnectionPoolRegistry s_connectionPoolRegistry = new ZeroMQConnectionPoolRegistry();

        protected ZeroMQConnectionOrientedTransportChannelFactoryBase(ZeroMQConnectionOrientedTransportBindingElementBase bindingElement, BindingContext context)
            : base(bindingElement, context, bindingElement.ConnectionPoolSettings.GroupName,
                    bindingElement.ConnectionPoolSettings.IdleTimeout,
                    bindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint,
                    true)
        {
            this.LeaseTimeout = bindingElement.ConnectionPoolSettings.LeaseTimeout;
            this.Scheme = bindingElement.Scheme;
        }

        public TimeSpan LeaseTimeout { get; }

        public override string Scheme { get; }

        public new int MaxBufferPoolSize => (int)base.MaxBufferPoolSize;

        public new int MaxReceivedMessageSize => (int)base.MaxReceivedMessageSize;

        protected override IConnectionInitiator GetConnectionInitiator()
        {
            IConnectionInitiator socketConnectionInitiator = 
                new ZeroMQConnectionInitiator(ConnectionBufferSize);

            return socketConnectionInitiator.CreateBufferedConnectionInitiator(MaxOutputDelay, ConnectionBufferSize);
        }

        protected override ConnectionPool GetConnectionPool()
        {
            return s_connectionPoolRegistry.Lookup(this);
        }

        protected override void ReleaseConnectionPool(ConnectionPool pool, TimeSpan timeout)
        {
            s_connectionPoolRegistry.Release(pool, timeout);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            base.OnClose(timeout);
        }
    }
}
