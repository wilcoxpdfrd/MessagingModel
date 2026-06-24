using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Routing
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

    [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    [ResourceContract(SessionMode = SessionMode.NotAllowed)]
    public interface ISimplexDatagramRouter
    {
        [OperationContract(Action = "*", IsOneWay = true)]
        [PostMessageAction("*", "*", IsOneWay = true)]
        [PostResourceAction("/*")]
        [PutResourceAction("/*")]
        [PatchResourceAction("/*")]
        void RouteMessage(Message message);
    }
}
