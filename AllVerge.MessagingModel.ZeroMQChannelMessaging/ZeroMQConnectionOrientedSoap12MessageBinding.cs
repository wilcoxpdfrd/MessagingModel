using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration;

    public class ZeroMQConnectionOrientedSoap12MessageBinding : ZeroMQConnectionOrientedBindingBase
    {
        public ZeroMQConnectionOrientedSoap12MessageBinding(ZeroMQConnectionOrientedBindingElement bindingElement) : 
            base(bindingElement)
        {
        }

        protected override void OnInitialize()
        {
            base.Encoding.MessageVersion = MessageVersion.Soap12WSAddressing10;
        }

        internal override EnvelopeVersion GetEnvelopeVersion()
        {
            return EnvelopeVersion.Soap12;
        }
    }
}