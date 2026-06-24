using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.Core.ServiceModel.ZeroMQ.Configuration;

    public class ZeroMQConnectionOrientedSoap11MessageBinding : ZeroMQConnectionOrientedBindingBase
    {
        public ZeroMQConnectionOrientedSoap11MessageBinding(ZeroMQConnectionOrientedBindingElement bindingElement) : 
            base(bindingElement)
        {
        }

        protected override void OnInitialize()
        {
            base.Encoding.MessageVersion = MessageVersion.Soap11;
        }

        internal override EnvelopeVersion GetEnvelopeVersion()
        {
            return EnvelopeVersion.Soap11;
        }
    }
}