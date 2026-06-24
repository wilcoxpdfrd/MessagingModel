using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    public interface IDispatcherRuntime
    {
        string DispatchOperationName { get; }
        IParameterInspector[] ParameterInspectors { get; }
        bool ShouldSerializeReply { get; }
        void DeserializeRequest(Message message, object[] parameters);
        Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result);
        MessageFault SerializeFaultReply(FaultException faultException, out string action);
        object[] AllocateInvokerInputs();
        object GetInstance(Message incomingMessage);
        Task<object> InvokeAsync(object dispatcher, object[] inputs);
        Task<object> GetCallbackResonseAsync(object dispatcher);
        bool TryFormatCallbackMessage(object[] vs, out Message outgoingMessage);
    }
}