using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace AllVerge.MessagingModel.MessagingFoundation.Description
{
    internal class DuplexCallbackMethodBehavior : IOperationBehavior
    {
        private string waitForReplyAsyncMethod;
        private string callbackOutputOperationName;

        public DuplexCallbackMethodBehavior(string waitForReplyAsyncMethod, string callbackOutputOperationName)
        {
            this.waitForReplyAsyncMethod = waitForReplyAsyncMethod;
            this.callbackOutputOperationName = callbackOutputOperationName;
        }

        void IOperationBehavior.AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        void IOperationBehavior.ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }

        void IOperationBehavior.ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Parent.CallbackOperationMap.Add(dispatchOperation.Name, (this.waitForReplyAsyncMethod, this.callbackOutputOperationName));
        }

        void IOperationBehavior.Validate(OperationDescription operationDescription)
        {
        }
    }
}