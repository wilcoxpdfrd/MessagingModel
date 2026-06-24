using System;
using System.Collections.Generic;
using System.Text;

using System.ServiceModel;
using System.ServiceModel.Channels;

using AllVerge.Core.ServiceModel.ZeroMQ.Configuration;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public class ZeroMTransferQSoap11MessageBinding : ZeroMQTransferBindingBase
    {
        public ZeroMTransferQSoap11MessageBinding() : base()
        {
        }

        public ZeroMTransferQSoap11MessageBinding(string configurationName) : 
            base(configurationName)
        {
        }

        public ZeroMTransferQSoap11MessageBinding(ZeroMQTransferBindingElement configurationElement) : 
            base(configurationElement)
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
