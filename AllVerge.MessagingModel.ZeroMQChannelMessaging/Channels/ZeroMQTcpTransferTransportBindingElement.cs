using AllVerge.Core.Resource;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public class ZeroMQTcpTransferTransportBindingElement : ZeroMQTransferTransportBindingElementBase
    {
        public ZeroMQTcpTransferTransportBindingElement() : base()
        {
        }

        public ZeroMQTcpTransferTransportBindingElement(ZeroMQTcpTransferTransportBindingElement elementToBeCloned) :
            base(elementToBeCloned)
        {
        }

        public override string Scheme => ResourceProtocolSchemes.ZEROMQ_TCP;

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

            ZeroMQTcpTransferTransportBindingElement clone = (ZeroMQTcpTransferTransportBindingElement)this.Clone();

            clone.ManualAddressing = true;

            TransportChannelListener listener = new ZeroMQTcpRequestChannelListener<TChannel>(clone, context);

            AspNetEnvironment.Current.ApplyHostedContext(listener, context);

            return (IChannelListener<TChannel>)(object)listener;
        }


        public override BindingElement Clone()
        {
            return new ZeroMQTcpTransferTransportBindingElement(this);
        }
    }
}