using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

using AllVerge.Core.ServiceModel.Client;

namespace AllVerge.Core.ServiceModel.ZeroMQ.Client
{
    public class ZeroMQClient<T> : ServiceClient<T> where T : class
    {
        public ZeroMQClient(ZeroMQConnectionOrientedBindingBase binding, EndpointAddress endPointAddress) : base(binding, endPointAddress) { }
    }
}
