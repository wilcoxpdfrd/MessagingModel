using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    internal class ServiceProviderBehavior : IServiceBehavior
    {
        private IServiceProvider serviceProvider;

        public ServiceProviderBehavior(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
            bindingParameters.Add(this.serviceProvider);
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }
    }
}