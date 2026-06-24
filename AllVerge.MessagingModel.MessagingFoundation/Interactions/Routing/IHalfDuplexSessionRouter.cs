using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Routing
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

    [ServiceContract(SessionMode = SessionMode.Required)]
    [ResourceContract(SessionMode = SessionMode.Required)]
    public interface IHalfDuplexSessionRouter
    {
        [OperationContract(IsOneWay = false, Action = "*", ReplyAction = "*")]
        [PostMessageAction("*", "*", ReplyAction = "*", IsOneWay = false)]
        [PostResourceAction("/*")]
        [PutResourceAction("/*")]
        [PatchResourceAction("/*")]
        Message RouteMessage(Message message);
    }
}
