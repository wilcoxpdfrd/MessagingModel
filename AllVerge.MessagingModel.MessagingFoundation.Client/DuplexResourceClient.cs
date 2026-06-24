using System;

using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Client
{
    using AllVerge.MessagingModel.MessagingFoundation.Client.Resource;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using System.ServiceModel.Description;

    public class DuplexResourceClient<T> : ResourceClient<T> where T : class
    {
        /// <summary>
        /// Invoke from a derived class constructor to initialize a new instance of the new instance of the <see cref="ResourceClient{T}"/> class
        /// </summary>
        /// <param name="binding">The binding with which to make calls to the service.</param>
        /// <param name="remoteAddress">The address of the service endpoint.</param>
        public DuplexResourceClient(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress, params IEndpointBehavior[] endpointBehaviors) :
            base(typeof(DuplexResourceClientChannel<T>), callbackInstance, binding, remoteAddress, endpointBehaviors)
        {
        }

        public IDuplexContextChannel InnerDuplexChannel
        {
            get
            {
                return (IDuplexContextChannel)InnerChannel;
            }
        }
    }
}
