using AllVerge.Core.Resource;
using AllVerge.SystemPrimitives.Net;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public class ZeroMQIpcConnectionOrientedTransportBindingElement : 
        ZeroMQConnectionOrientedTransportBindingElementBase
    {
        public ZeroMQIpcConnectionOrientedTransportBindingElement() : base()
        {

        }

        public ZeroMQIpcConnectionOrientedTransportBindingElement(ZeroMQIpcConnectionOrientedTransportBindingElement elementToBeCloned) :
            base(elementToBeCloned)
        {
        }

        public override string Scheme => TransportProtocolSchemes.ZEROMQ_IPC;

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(context));
            }

            if (!CanBuildChannelFactory<TChannel>(context))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("TChannel", SSR.Format(SSR.ChannelTypeNotSupported, typeof(TChannel)));
            }

            return (IChannelFactory<TChannel>)(object)new ZeroMQIpcTransportChannelFactory<TChannel>(this, context);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
            }

            if (!this.CanBuildChannelListener<TChannel>(context))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("TChannel", SSR.Format(SSR.ChannelTypeNotSupported, typeof(TChannel)));
            }

            TransportChannelListener listener;

            if (typeof(TChannel) == typeof(IReplyChannel))
            {
                listener = new ZeroMQIpcReplyChannelListener(this, context);
            }
            else if (typeof(TChannel) == typeof(IDuplexSessionChannel))
            {
                listener = new ZeroMQIpcDuplexChannelListener(this, context);
            }
            else
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("TChannel", SSR.Format(SSR.ChannelTypeNotSupported, typeof(TChannel)));
            }

            AspNetEnvironment.Current.ApplyHostedContext(listener, context);

            return (IChannelListener<TChannel>)(object)listener;
        }

        public override BindingElement Clone()
        {
            return new ZeroMQIpcConnectionOrientedTransportBindingElement(this);
        }
    }
}
