using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

using AllVerge.MessagingModel.MessagingFoundation.Client;
using AllVerge.MessagingModel.ZeroMQChannelMessaging;

namespace AllVerge.Core.ServiceModel.ZeroMQ.Client
{
    public class ZeroMQClient<T> : ResourceClient<T> where T : class
    {
        public ZeroMQClient(ZeroMQConnectionOrientedBindingBase binding, EndpointAddress endPointAddress) : base(binding, endPointAddress) { }
    }
}
