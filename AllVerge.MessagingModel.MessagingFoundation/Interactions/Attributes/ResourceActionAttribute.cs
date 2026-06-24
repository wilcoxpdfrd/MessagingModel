using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    using AllVerge.SystemPrimitives.Net;

    /// <summary>Indicates that the decorated method defines the action of a resource endpoint in a service contract.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class ResourceActionAttribute : ResourceEndpointAttribute
    {
        /// <summary>Initializes a new instance of the <see cref="ResourceActionAttribute" /> class with the given <paramref name="methodName"/> and <paramref name="resourceAction"/>.</summary>
        protected ResourceActionAttribute(String methodName, string resourceAction) : base(methodName)
        {
            this.ResourceAction = resourceAction;
        }

        /// <summary>Initializes a new instance of the <see cref="ResourceActionAttribute" /> class with the given <paramref name="resourceAction"/>.</summary>
        protected ResourceActionAttribute(string resourceAction) : base()
        {
            this.ResourceAction = resourceAction;
        }

        /// <summary>Initializes a new instance of the <see cref="ResourceActionAttribute" />.</summary>
        protected ResourceActionAttribute() : base()
        {
        }

        /// <summary>
        /// Gets the resource endpoint action, a.k.a Web Method when attribute is decorating a web resource.
        /// </summary>
        public String ResourceAction { get; }

        protected abstract MessageFilter GetMessageFilter(Uri baseAddress, OperationDescription operationDescription);

        protected override void AddBindingParameters(ServiceEndpoint endpoint, OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
            if (bindingParameters.Contains(typeof(ThreadSafeMessageFilterTable<EndpointAddress>)))
            {
                MessageFilter messageFilter = GetMessageFilter(endpoint.Address.Uri.ToBaseAddressUri(), operationDescription);

                ThreadSafeMessageFilterTable<EndpointAddress> messageFilterTable = bindingParameters.Find<ThreadSafeMessageFilterTable<EndpointAddress>>();

                messageFilterTable.Add(messageFilter, endpoint.Address);
            }
        }
    }
}
