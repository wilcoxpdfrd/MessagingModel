using System;
using System.Collections.Generic;
using System.Text;

using System.ServiceModel;
using System.ServiceModel.Channels;

using AllVerge.Core.ServiceModel.ZeroMQ.Configuration;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public class ZeroMQTransferSoap12MessageBinding : ZeroMQTransferBindingBase
    {
        public ZeroMQTransferSoap12MessageBinding() : base()
        {
        }

        public ZeroMQTransferSoap12MessageBinding(string configurationName) :
            base(configurationName)
        {
        }

        public ZeroMQTransferSoap12MessageBinding(ZeroMQTransferBindingElement configurationElement) :
            base(configurationElement)
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
