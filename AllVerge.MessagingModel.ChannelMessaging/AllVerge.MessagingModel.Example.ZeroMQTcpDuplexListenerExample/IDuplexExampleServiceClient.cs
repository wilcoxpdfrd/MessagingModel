using AllVerge.MessagingModel.MessagingFoundation.Client;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace AllVerge.MessageProcessor.ZeroMQ.TcpDuplexListenerExample
{
    [ServiceContract(Namespace = TcpDuplexListenerExample.Message.NS, Name = "IDuplexExampleService", CallbackContract = typeof(IDuplexExampleServiceCallback))]
    [ResourceContract(Name = "IDuplexExampleService")]
    public interface IDuplexExampleServiceClient
    {
        [OperationContract(IsOneWay = true)]
        [PostMessageAction(
            IsOneWay = true,
            CallbackContractType = typeof(IDuplexExampleServiceCallback)
        )]
        void Message(Message message);
    }

    public class DuplexExampleServiceClient : DuplexResourceClient<IDuplexExampleServiceClient>, IDuplexExampleServiceClient
    {
        public delegate void OnHandleCallbackMessage(Message message);
        public delegate void OnHandleCallbackMessageFault(MessageFault fault, MessageVersion messageVersion, UniqueId relatesTo, out System.ServiceModel.Channels.Message faultMessage);

        public class DuplexExampleServiceCallback : IDuplexExampleServiceCallback
        {
            private OnHandleCallbackMessage onHandleCallbackMessage;
            private OnHandleCallbackMessageFault onHandleCallbackMessageFault;

            public DuplexExampleServiceCallback(OnHandleCallbackMessage onHandleCallbackMessage, OnHandleCallbackMessageFault onHandleCallbackMessageFault)
            {
                this.onHandleCallbackMessage = onHandleCallbackMessage;
                this.onHandleCallbackMessageFault = onHandleCallbackMessageFault;
            }

            public void Message(Message message)
            {
                this.onHandleCallbackMessage(message);
            }

            public static DuplexExampleServiceCallback Create(OnHandleCallbackMessage onHandleCallbackMessage, OnHandleCallbackMessageFault onHandleCallbackMessageFault)
            {
                return new DuplexExampleServiceCallback(onHandleCallbackMessage, onHandleCallbackMessageFault);
            }
        }
        public DuplexExampleServiceClient(OnHandleCallbackMessage onHandleCallbackMessage, OnHandleCallbackMessageFault onHandleCallbackMessageFault, Binding binding, EndpointAddress remoteAddress) : 
            base(new InstanceContext(DuplexExampleServiceCallback.Create(onHandleCallbackMessage, onHandleCallbackMessageFault)), binding, remoteAddress)
        {
        }

        public void Message(Message content)
        {
            using (OperationContextScope scope = new OperationContextScope((IContextChannel)base.Channel))
            {
                base.Channel.Message(content);
            }
        }
    }
}
