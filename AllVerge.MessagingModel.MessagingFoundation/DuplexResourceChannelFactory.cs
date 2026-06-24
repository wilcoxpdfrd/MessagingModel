// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Diagnostics;
using System;
using System.ServiceModel;
using AllVerge.MessagingModel.MessagingFoundation.Description;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    public class DuplexResourceChannelFactory<TChannel> : DuplexChannelFactory<TChannel>
    {
        //Type overloads
        public DuplexResourceChannelFactory(Type callbackInstanceType)
            : this((object)callbackInstanceType)
        { }
        public DuplexResourceChannelFactory(Type callbackInstanceType, Binding binding, String remoteAddress)
            : this((object)callbackInstanceType, binding, new EndpointAddress(remoteAddress))
        { }
        public DuplexResourceChannelFactory(Type callbackInstanceType, Binding binding, EndpointAddress remoteAddress)
            : this((object)callbackInstanceType, binding, remoteAddress)
        { }
        public DuplexResourceChannelFactory(Type callbackInstanceType, Binding binding)
            : this((object)callbackInstanceType, binding)
        { }
        public DuplexResourceChannelFactory(Type callbackInstanceType, string endpointConfigurationName, EndpointAddress remoteAddress)
            : this((object)callbackInstanceType, endpointConfigurationName, remoteAddress)
        { }
        public DuplexResourceChannelFactory(Type callbackInstanceType, string endpointConfigurationName)
            : this((object)callbackInstanceType, endpointConfigurationName)
        { }
        public DuplexResourceChannelFactory(Type callbackInstanceType, ServiceEndpoint endpoint)
            : this((object)callbackInstanceType, endpoint)
        { }

        //InstanceContext overloads
        public DuplexResourceChannelFactory(InstanceContext callbackInstance)
            : this((object)callbackInstance)
        { }
        public DuplexResourceChannelFactory(InstanceContext callbackInstance, Binding binding, String remoteAddress)
            : this((object)callbackInstance, binding, new EndpointAddress(remoteAddress))
        { }
        public DuplexResourceChannelFactory(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress)
            : this((object)callbackInstance, binding, remoteAddress)
        { }
        public DuplexResourceChannelFactory(InstanceContext callbackInstance, Binding binding)
            : this((object)callbackInstance, binding)
        { }
        public DuplexResourceChannelFactory(InstanceContext callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress)
            : this((object)callbackInstance, endpointConfigurationName, remoteAddress)
        { }
        public DuplexResourceChannelFactory(InstanceContext callbackInstance, string endpointConfigurationName)
            : this((object)callbackInstance, endpointConfigurationName)
        { }
        public DuplexResourceChannelFactory(InstanceContext callbackInstance, ServiceEndpoint endpoint)
            : this((object)callbackInstance, endpoint)
        { }

        // TChannel provides ContractDescription
        public DuplexResourceChannelFactory(object callbackObject)
            : base(callbackObject)
        {
        }

        // TChannel provides ContractDescription, attr/config [TChannel,name] provides Address,Binding
        public DuplexResourceChannelFactory(object callbackObject, string endpointConfigurationName)
            : this(callbackObject, endpointConfigurationName, null)
        {
        }

        // TChannel provides ContractDescription, attr/config [TChannel,name] provides Binding, provide Address explicitly
        public DuplexResourceChannelFactory(object callbackObject, string endpointConfigurationName, EndpointAddress remoteAddress)
            : base(callbackObject, endpointConfigurationName, remoteAddress)
        {
        }

        // TChannel provides ContractDescription, attr/config [TChannel,name] provides Address,Binding
        public DuplexResourceChannelFactory(object callbackObject, Binding binding)
            : this(callbackObject, binding, (EndpointAddress)null)
        {
        }

        // TChannel provides ContractDescription, provide Address,Binding explicitly
        public DuplexResourceChannelFactory(object callbackObject, Binding binding, String remoteAddress)
            : this(callbackObject, binding, new EndpointAddress(remoteAddress))
        {
        }
        // TChannel provides ContractDescription, provide Address,Binding explicitly
        public DuplexResourceChannelFactory(object callbackObject, Binding binding, EndpointAddress remoteAddress)
            : base(callbackObject, binding, remoteAddress)
        {
        }

        // provide ContractDescription,Address,Binding explicitly
        public DuplexResourceChannelFactory(object callbackObject, ServiceEndpoint endpoint)
            : base(callbackObject, endpoint)
        {
        }

        protected override ITypeLoader GetTypeLoader()
        {
            return new ResourceTypeLoader(true);
        }
    }
}
