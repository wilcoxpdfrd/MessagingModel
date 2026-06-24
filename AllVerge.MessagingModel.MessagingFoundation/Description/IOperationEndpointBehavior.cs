using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Description
{
    public interface IOperationEndpointBehavior : IOperationBehavior
    {
        void AddBindingParameters(ServiceEndpoint endpoint, OperationDescription operationDescription, BindingParameterCollection bindingParameters);
    }
}
