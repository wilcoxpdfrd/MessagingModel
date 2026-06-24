using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.Core.ServiceModel.ZeroMQ.Configuration;

    public class ZeroMQConnectionOrientedTextMessageBinding : ZeroMQConnectionOrientedBindingBase
    {
        public ZeroMQConnectionOrientedTextMessageBinding(ZeroMQConnectionOrientedBindingElement bindingElement) : 
            base(bindingElement)
        {
        }

        protected override void OnInitialize()
        {
            base.Encoding.MessageVersion = MessageVersion.None;
        }

        internal override EnvelopeVersion GetEnvelopeVersion()
        {
            return EnvelopeVersion.None;
        }
    }
}