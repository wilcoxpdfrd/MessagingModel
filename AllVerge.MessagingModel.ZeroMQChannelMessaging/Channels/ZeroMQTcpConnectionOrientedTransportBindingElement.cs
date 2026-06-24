using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.SystemPrimitives.Net;

    public class ZeroMQTcpConnectionOrientedTransportBindingElement : 
        ZeroMQConnectionOrientedTransportBindingElementBase
    {
        public ZeroMQTcpConnectionOrientedTransportBindingElement() : base()
        {
        }

        public ZeroMQTcpConnectionOrientedTransportBindingElement(ZeroMQTcpConnectionOrientedTransportBindingElement elementToBeCloned) : 
            base(elementToBeCloned)
        {
        }

        public override string Scheme => TransportProtocolSchemes.ZEROMQ_TCP;

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(context));
            }

            if (!CanBuildChannelFactory<TChannel>(context))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("TChannel", PublicSR.Format(PublicSR.ChannelTypeNotSupported, typeof(TChannel)));
            }

            return (IChannelFactory<TChannel>)(object)new ZeroMQTcpTransportChannelFactory<TChannel>(this, context);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
            }

            if (!this.CanBuildChannelListener<TChannel>(context))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("TChannel", PublicSR.Format(PublicSR.ChannelTypeNotSupported, typeof(TChannel)));
            }

            TransportChannelListener listener;

            if (typeof(TChannel) == typeof(IDuplexSessionChannel))
            {
                listener = new ZeroMQTcpDuplexChannelListener(this, context);
            }
            //else if(typeof(TChannel) == typeof(IReplyChannel))
            //{
            //    listener = new ZeroMQTcpReplyChannelListener(this, context);
            //}
            else 
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("TChannel", PublicSR.Format(PublicSR.ChannelTypeNotSupported, typeof(TChannel)));
            }

            // AspNetEnvironment.Current.ApplyHostedContext(listener, context);

            return (IChannelListener<TChannel>)(object)listener;
        }

        public override BindingElement Clone()
        {
            return new ZeroMQTcpConnectionOrientedTransportBindingElement(this);
        }
    }
}
