using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Routing
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(ISimplexSessionRouter))]
    [ResourceContract(SessionMode = SessionMode.Required)]
    public interface IDuplexSessionRouter
    {
        [OperationContract(IsOneWay = true, Action = "*")]
        [PostMessageAction("*", "*", IsOneWay = true, CallbackContractType = typeof(ISimplexDatagramRouter))]
        [WaitForReplyMessageAsyncMethod("WaitForReplyMessageAsync", ForReceivePostMethod = "RouteMessage")]
        void RouteMessage(Message message);
        Task<Message> WaitForReplyMessageAsync();
    }
}
