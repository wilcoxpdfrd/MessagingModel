using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    internal class ResourceDispatcherRuntime : IDispatcherRuntime
    {
        private ResourceChannelDispatcherManager resourceChannelDispatcherManager;
        private DispatchRuntime dispatchRuntime;
        private DispatchOperationRuntime dispatchOperationRuntime;
        private DispatchOperation callbackResponseOperation;
        private ProxyOperationRuntime callbackOperationRuntime;
        private ServiceChannel serviceChannel;
        private TaskFactory taskFactory;

        internal ResourceDispatcherRuntime(ResourceChannelDispatcherManager resourceChannelDispatcherManager, DispatchRuntime dispatchRuntime, DispatchOperationRuntime dispatchOperationRuntime)
        {
            this.resourceChannelDispatcherManager = resourceChannelDispatcherManager;
            this.dispatchRuntime = dispatchRuntime;
            this.dispatchOperationRuntime = dispatchOperationRuntime;
            this.taskFactory = new TaskFactory();
        }

        internal ResourceDispatcherRuntime(ResourceChannelDispatcherManager resourceChannelDispatcherManager, DispatchRuntime dispatchRuntime, DispatchOperationRuntime dispatchOperationRuntime, DispatchOperation callbackResponseOperation, ProxyOperationRuntime callbackOperationRuntime)
        {
            this.resourceChannelDispatcherManager = resourceChannelDispatcherManager;
            this.dispatchRuntime = dispatchRuntime;
            this.dispatchOperationRuntime = dispatchOperationRuntime;
            this.callbackResponseOperation = callbackResponseOperation;
            this.callbackOperationRuntime = callbackOperationRuntime;
            this.InitializeServiceChannel();
            this.taskFactory = new TaskFactory();
        }

        private void InitializeServiceChannel()
        {
            ServiceEndpoint serviceEndpoint = this.resourceChannelDispatcherManager.FindServiceEndpoint(this.dispatchRuntime.EndpointDispatcher.EndpointAddress.Uri);

            ServiceChannelFactory serviceChannelFactory = ServiceChannelFactory.BuildChannelFactory(serviceEndpoint);

            serviceChannelFactory.Open();

            this.serviceChannel = serviceChannelFactory.CreateServiceChannel(serviceEndpoint.Address, serviceEndpoint.Address.Uri);
        }

        public String DispatchOperationName => this.dispatchOperationRuntime.Name;

        public object[] AllocateInvokerInputs()
        {
            return this.dispatchOperationRuntime.Invoker.AllocateInputs();
        }

        public void DeserializeRequest(Message message, object[] parameters) => this.dispatchOperationRuntime.Formatter.DeserializeRequest(message, parameters);

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result) => this.dispatchOperationRuntime.Formatter.SerializeReply(messageVersion, parameters, result);

        public MessageFault SerializeFaultReply(FaultException faultException, out string action) => this.dispatchOperationRuntime.FaultFormatter.Serialize(faultException, out action);

        public IParameterInspector[] ParameterInspectors => this.dispatchOperationRuntime.ParameterInspectors;

        public bool ShouldSerializeReply => this.dispatchOperationRuntime.SerializeReply;

        public Object GetInstance(Message message)
        {
            return this.dispatchRuntime.GetRuntime().InstanceBehavior.GetInstance(this.resourceChannelDispatcherManager.InstanceContext, message);
        }

        public Task<Object> InvokeAsync(Object dispatcher, Object[] inputs)
        {
            return taskFactory.FromAsync(
                this.dispatchOperationRuntime.Invoker.InvokeBegin,
                result =>
                {
                    Object output = this.dispatchOperationRuntime.Invoker.InvokeEnd(dispatcher, out Object[] outputs, result);
                    outputs.Aggregate(result.AsyncState as IList<Object>, (l, o) => { l.Add(o); return l; });
                    return output;
                }, dispatcher, inputs, new List<Object>());
        }

        public bool TryFormatCallbackMessage(Object[] outputs, out Message message)
        {
            if (this.callbackOperationRuntime != null)
            {
                ProxyRpc proxyRpc = new ProxyRpc(this.serviceChannel, this.callbackOperationRuntime, null, outputs, default(TimeSpan));

                this.callbackOperationRuntime?.BeforeRequest(ref proxyRpc);

                message = proxyRpc.Request;
            }
            else

                message = null;

            return message != null;
        }

        [Obsolete]
        internal bool TryFormatCallbackMessage(IChannel channel, Object[] outputs, out Message message)
        {
            if (this.callbackOperationRuntime != null)
            {
                ProxyRpc proxyRpc = new ProxyRpc(this.serviceChannel, this.callbackOperationRuntime, null, outputs, default(TimeSpan));

                this.callbackOperationRuntime?.BeforeRequest(ref proxyRpc);

                message = proxyRpc.Request;
            }
            else

                message = null;

            return message != null;
        }

        public Task<object> GetCallbackResonseAsync(object dispatcher)
        {
            Object[] inputs = this.callbackResponseOperation.Invoker.AllocateInputs();

            return taskFactory.FromAsync(this.callbackResponseOperation.Invoker.InvokeBegin(dispatcher, inputs, null, null), r => this.callbackResponseOperation.Invoker.InvokeEnd(dispatcher, out Object[] outputs, r));
        }
    }
}