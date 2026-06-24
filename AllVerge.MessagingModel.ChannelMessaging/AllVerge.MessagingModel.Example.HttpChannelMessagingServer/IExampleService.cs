using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.Example.HttpChannelMessagingServer
{
    using AllVerge.MessagingModel.MessagingFoundation.Client;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;

    [ServiceContract(Namespace = "http://tempuri.org/", Name = "IExampleService")]
    [ResourceContract]
    public interface IExampleService
    {
        [OperationContract()]
        [PostMessageAction()]
        Message Message(Message message);
    }

    public class ExampleServiceClient : ResourceClient<IExampleService>, IExampleService
    {
        public ExampleServiceClient(Binding binding, EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        {
        }

        public Message Message(Message content)
        {
            using (OperationContextScope scope = new OperationContextScope((IContextChannel)base.Channel))
            {
                return base.Channel.Message(content);
            }
        }
    }
}
