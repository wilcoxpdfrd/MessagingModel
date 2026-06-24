using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

using AllVerge.Core.Resource;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQIpcDuplexChannelListener :
        ZeroMQConnectionOrientedChannelListener<
            IDuplexSessionChannel, 
            InputQueueChannelAcceptor<IDuplexSessionChannel>>, 
        ISessionPreambleHandler
    {
        InputQueueChannelAcceptor<IDuplexSessionChannel> duplexAcceptor;

        public ZeroMQIpcDuplexChannelListener(ZeroMQIpcConnectionOrientedTransportBindingElement bindingElement, BindingContext context) :
            base(bindingElement, context)
        {
            this.duplexAcceptor = new InputQueueChannelAcceptor<IDuplexSessionChannel>(this);
        }

        public override string Scheme => ResourceProtocolSchemes.ZEROMQ_IPC;

        protected override InputQueueChannelAcceptor<IDuplexSessionChannel> ChannelAcceptor
        {
            get { return this.duplexAcceptor; }
        }

        void ISessionPreambleHandler.HandleServerSessionPreamble(
            ServerSessionPreambleConnectionReader preambleReader,
            ConnectionDemuxer connectionDemuxer)
        {
            IDuplexSessionChannel channel = preambleReader.CreateDuplexSessionChannel(
                this, new EndpointAddress(this.Uri), true, connectionDemuxer);

            duplexAcceptor.EnqueueAndDispatch(channel, preambleReader.ConnectionDequeuedCallback);
        }
    }
}