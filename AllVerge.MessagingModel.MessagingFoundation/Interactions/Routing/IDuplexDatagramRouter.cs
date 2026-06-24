using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Routing
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;

    [ResourceContract(SessionMode = SessionMode.NotAllowed)]
    [ServiceContract(SessionMode = SessionMode.NotAllowed, CallbackContract = typeof(ISimplexDatagramRouter))]
    public interface IDuplexDatagramRouter
    {
        [OperationContract(IsOneWay = true, Action = "*")]
        [PostMessageAction("*", "*", IsOneWay = true, CallbackContractType = typeof(ISimplexDatagramRouter))]
        [WaitForReplyMessageAsyncMethod("WaitForReplyMessageAsync", ForReceivePostMethod = "RouteMessage")]
        void RouteMessage(Message message);
        Task<Message> WaitForReplyMessageAsync();
    }
}
