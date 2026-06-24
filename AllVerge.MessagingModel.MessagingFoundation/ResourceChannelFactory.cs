// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using AllVerge.MessagingModel.MessagingFoundation.Description;
using System;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Diagnostics;
using System.ServiceModel.Security;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    public class ResourceChannelFactory<TChannel> : 
        ChannelFactory<TChannel>
    {
        //Overload for activation DuplexChannelFactory
        protected ResourceChannelFactory(Type channelType)
            : base(channelType)
        {
        }

        // TChannel provides ContractDescription
        public ResourceChannelFactory()
            : base()
        {
        }

        // TChannel provides ContractDescription, attr/config [TChannel,name] provides Address,Binding
        public ResourceChannelFactory(string endpointConfigurationName)
            : this(endpointConfigurationName, null)
        {
        }

        // TChannel provides ContractDescription, attr/config [TChannel,name] provides Binding, provide Address explicitly
        public ResourceChannelFactory(string endpointConfigurationName, EndpointAddress remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {
        }

        // TChannel provides ContractDescription, attr/config [TChannel,name] provides Address,Binding
        public ResourceChannelFactory(Binding binding)
            : this(binding, (EndpointAddress)null)
        {
        }

        public ResourceChannelFactory(Binding binding, String remoteAddress)
            : this(binding, new EndpointAddress(remoteAddress))
        {
        }

        // TChannel provides ContractDescription, provide Address,Binding explicitly
        public ResourceChannelFactory(Binding binding, EndpointAddress remoteAddress)
            : base(binding, remoteAddress)
        {
        }

        // provide ContractDescription,Address,Binding explicitly
        public ResourceChannelFactory(ServiceEndpoint endpoint)
            : base(endpoint)
        {
        }

        protected override ITypeLoader GetTypeLoader()
        {
            return new ResourceTypeLoader(true);
        }
    }
}
