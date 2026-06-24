using System;
using System.Collections.Generic;
using System.Text;

using System.ServiceModel;
using System.ServiceModel.Configuration;
using System.ServiceModel.Channels;

using AllVerge.Core.ServiceModel.Channels;
using AllVerge.Core.ServiceModel.ZeroMQ.Configuration;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public class ZeroMQTransferTextMessageBinding : ZeroMQTransferBindingBase
    {
        public ZeroMQTransferTextMessageBinding() : base()
        {
        }

        public ZeroMQTransferTextMessageBinding(string configurationName) :
            base(configurationName)
        {
        }

        public ZeroMQTransferTextMessageBinding(ZeroMQTransferBindingElement configurationElement) :
            base(configurationElement)
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
