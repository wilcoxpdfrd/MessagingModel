using System;
using System.Collections.Generic;
using System.Text;

using System.ServiceModel.Channels;

using AllVerge.Core.ServiceModel.Channels;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;

namespace AllVerge.Core.ServiceModel.ZeroMQ.Client
{
    public static class ZeroMQCustomBindingHelper
    {
        public static Binding CreateBinding(this MessageVersion messageVersion, ZeroMQMessageEncoding messageEncoding, ZeroMQConnectionOrientedTransportBindingElementBase transportBindingElement)
        {
            return ZeroMQBindings.CreateBinding(messageVersion, messageEncoding, transportBindingElement);
        }
    }
}
