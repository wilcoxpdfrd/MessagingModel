using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.SystemPrimitives.Net;

    using MessagingBindingContext = AllVerge.MessagingModel.MessagingApplication.BindingContext;
    using BindingContext = System.ServiceModel.Channels.BindingContext;
    using System.Net;

    internal class ZeroMQTcpDuplexChannelListener :
        ZeroMQConnectionOrientedChannelListener<IDuplexSessionChannel, InputQueueChannelAcceptor<IDuplexSessionChannel>>, ISessionPreambleHandler
    {
        private InputQueueChannelAcceptor<IDuplexSessionChannel> duplexAcceptor;

        public ZeroMQTcpDuplexChannelListener(ZeroMQTcpConnectionOrientedTransportBindingElement bindingElement, BindingContext context) :
            base(bindingElement, context)
        {
            this.duplexAcceptor = new InputQueueChannelAcceptor<IDuplexSessionChannel>(this);
        }

        public override string Scheme => TransportProtocolSchemes.ZEROMQ_TCP;

        protected override InputQueueChannelAcceptor<IDuplexSessionChannel> ChannelAcceptor
        {
            get { return this.duplexAcceptor; }
        }

        void ISessionPreambleHandler.HandleServerSessionPreamble(ServerSessionPreambleConnectionReader preambleReader,
            ConnectionDemuxer connectionDemuxer)
        {
            MessagingBindingContext messagingBindingContext = new MessagingBindingContext();

            ZeroMQListenerConnection zeroMQListenerConnection = preambleReader.Connection as ZeroMQListenerConnection;

            String connectionId = zeroMQListenerConnection.ConnectionId.ToString();
            String sequenceId = "0";
            String traceIdentifier = $"{connectionId}:${sequenceId}";

            ConnectionContext.Map(messagingBindingContext.ConnectionContext, connectionId, null, zeroMQListenerConnection.LocalIPEndPoint, zeroMQListenerConnection.RemoteIPEndPoint);

            InteractionContext.Map(messagingBindingContext.InteractionContext, null, zeroMQListenerConnection.LocalAddress.Uri.AbsoluteUri, String.Empty, DateTimeOffset.Now, traceIdentifier, null);

            IMessagingContext<ZeroMQProtocolContext> protocolContext = new MessagingContext<ZeroMQProtocolContext>(messagingBindingContext);

            ZeroMQProtocolContext zeroMQProtocolContext = new ZeroMQProtocolContext(zeroMQListenerConnection, this.DefaultReceiveTimeout, this.DefaultSendTimeout, sequenceId);
            
            protocolContext.Input(zeroMQProtocolContext);

            base.GetProperty<IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>>>().
                SetProtocolContextAsync(protocolContext);

            IDuplexSessionChannel channel = preambleReader.CreateDuplexSessionChannel(
                this, new EndpointAddress(this.Uri), base.ExposeConnectionProperty, connectionDemuxer, zeroMQProtocolContext);

            duplexAcceptor.EnqueueAndDispatch(channel, preambleReader.ConnectionDequeuedCallback);
        }
    }
}