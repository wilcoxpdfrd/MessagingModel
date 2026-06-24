using static AllVerge.MessagingModel.Example.HttpChannelMessagingServer.DuplexExampleResourceClient;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Xml.Linq;
using AllVerge.MessagingModel.MessagingFoundation.Client;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;

namespace AllVerge.MessagingModel.Example.HttpChannelMessagingServer
{
    [ResourceContract(Namespace = HttpChannelMessagingServer.Message.NS, Name = "IDuplexExampleService")]
    public interface IDuplexExampleServiceClient
    {
        [PostMessageAction(
            IsOneWay = true,
            CallbackContractType = typeof(IDuplexExampleServiceCallback)
        )]
        void Message(Message message);
    }

    public class DuplexExampleResourceClient : DuplexResourceClient<IDuplexExampleServiceClient>, IDuplexExampleServiceClient
    {
        public DuplexExampleServiceCallback CallbackHandler { get; private set; }

        public delegate void OnHandleCallbackMessage(Message message);

        public class DuplexExampleServiceCallback : IDuplexExampleServiceCallback
        {
            private OnHandleCallbackMessage onHandleCallbackMessage;

            public DuplexExampleServiceCallback(OnHandleCallbackMessage onHandleCallbackMessage)
            {
                this.onHandleCallbackMessage = onHandleCallbackMessage;
            }
            public void Message(Message message)
            {
                this.onHandleCallbackMessage(message);
            }

            public static DuplexExampleServiceCallback Create(OnHandleCallbackMessage onHandleCallbackMessage, out DuplexExampleServiceCallback callbackHandler)
            {
                callbackHandler = new DuplexExampleServiceCallback(onHandleCallbackMessage);
                return callbackHandler;
            }
        }
        public DuplexExampleResourceClient(OnHandleCallbackMessage onHandleCallbackMessage, Binding binding, EndpointAddress remoteAddress) :
            base(new InstanceContext(DuplexExampleServiceCallback.Create(onHandleCallbackMessage, out DuplexExampleServiceCallback callbackHandler)), binding, remoteAddress)
        {
            this.CallbackHandler = callbackHandler;
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
