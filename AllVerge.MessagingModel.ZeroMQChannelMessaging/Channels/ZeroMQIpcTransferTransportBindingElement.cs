using AllVerge.Core.Resource;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;
using System;
using System.Reflection.Metadata.Ecma335;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public class ZeroMQIpcTransferTransportBindingElement : ZeroMQTransferTransportBindingElementBase
    {
        public ZeroMQIpcTransferTransportBindingElement() : base()
        {
        }

        public ZeroMQIpcTransferTransportBindingElement(ZeroMQIpcTransferTransportBindingElement elementToBeCloned) :
            base(elementToBeCloned)
        {
        }

        public override string Scheme => ResourceProtocolSchemes.ZEROMQ_IPC;

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

            //UpdateAuthenticationSchemes(context);

            ZeroMQIpcTransferTransportBindingElement clone = (ZeroMQIpcTransferTransportBindingElement)this.Clone();

            clone.ManualAddressing = true;

            TransportChannelListener listener = new ZeroMQIpcRequestChannelListener<TChannel>(clone, context);

            AspNetEnvironment.Current.ApplyHostedContext(listener, context);

            return (IChannelListener<TChannel>)(object)listener;
        }

        public override BindingElement Clone()
        {
            return new ZeroMQIpcTransferTransportBindingElement(this);
        }
    }
}