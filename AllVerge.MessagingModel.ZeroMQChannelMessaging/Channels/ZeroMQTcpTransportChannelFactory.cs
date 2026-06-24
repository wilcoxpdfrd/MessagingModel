using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQTcpTransportChannelFactory<TChannel> : ZeroMQConnectionOrientedTransportChannelFactoryBase<TChannel>
    {
        public ZeroMQTcpTransportChannelFactory(ZeroMQTcpConnectionOrientedTransportBindingElement bindingElement, BindingContext context)
            : base(bindingElement, context)
        {
        }
    }
}