using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQIpcTransportChannelFactory<TChannel> : ZeroMQConnectionOrientedTransportChannelFactoryBase<TChannel>
    {
        public ZeroMQIpcTransportChannelFactory(ZeroMQIpcConnectionOrientedTransportBindingElement bindingElement, BindingContext context) :
            base(bindingElement, context)
        {
        }
    }
}