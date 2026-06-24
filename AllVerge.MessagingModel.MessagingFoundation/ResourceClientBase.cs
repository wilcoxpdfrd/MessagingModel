// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    public abstract class ResourceClientBase<TChannel> : 
        ClientBase<TChannel, ResourceChannelFactory<TChannel>, DuplexResourceChannelFactory<TChannel>>
        where TChannel : class
    {
        protected ResourceClientBase(Binding binding, EndpointAddress remoteAddress) : 
            base(binding, remoteAddress)
        {
        }

        protected ResourceClientBase(ServiceEndpoint endpoint): base(endpoint)
        {
        }

        protected ResourceClientBase(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress) :
            base(callbackInstance, binding, remoteAddress)
        {
        }
    }
}
