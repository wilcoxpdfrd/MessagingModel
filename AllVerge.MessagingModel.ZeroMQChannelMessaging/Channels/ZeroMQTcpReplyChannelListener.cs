using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Diagnostics;

using AllVerge.Core.Resource;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQTcpReplyChannelListener :
        ZeroMQConnectionOrientedChannelListener<IReplyChannel, ReplyChannelAcceptor>, ISingletonChannelListener
    {
        ReplyChannelAcceptor replyAcceptor;

        public ZeroMQTcpReplyChannelListener(ZeroMQTcpConnectionOrientedTransportBindingElement bindingElement, BindingContext context) :
            base(bindingElement, context)
        {
            this.replyAcceptor = new ZeroMQTransportReplyChannelAcceptor(this);
        }

        public override string Scheme => ResourceProtocolSchemes.ZEROMQ_TCP;

        protected override ReplyChannelAcceptor ChannelAcceptor
        {
            get { return this.replyAcceptor; }
        }

        TimeSpan ISingletonChannelListener.ReceiveTimeout
        {
            get { return this.InternalReceiveTimeout; }
        }

        //internal override UriPrefixTable<ITransportManagerRegistration> TransportManagerTable => throw new NotImplementedException();

        void ISingletonChannelListener.ReceiveRequest(RequestContext requestContext, Action callback, bool canDispatchOnThisThread)
        {
            if (DiagnosticUtility.ShouldTraceVerbose)
            {
                TraceUtility.TraceEvent(TraceEventType.Verbose, TraceCode.TcpChannelMessageReceived,
                    SSR.TraceCodeTcpChannelMessageReceived, requestContext.RequestMessage);
            }
            replyAcceptor.Enqueue(requestContext, callback, canDispatchOnThisThread);
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            throw new NotImplementedException();
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        internal override ITransportManagerRegistration CreateTransportManagerRegistration(Uri listenUri)
        {
            return base.CreateTransportManagerRegistration(listenUri);
        }
    }
}