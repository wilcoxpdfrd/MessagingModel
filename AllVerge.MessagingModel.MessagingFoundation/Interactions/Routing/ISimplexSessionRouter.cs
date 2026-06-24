using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Routing
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

    public delegate void SimplexSessionProcessorDelegate(Message message);

    [ServiceContract(SessionMode = SessionMode.Required)]
    [ResourceContract(SessionMode = SessionMode.Required)]
    public interface ISimplexSessionRouter
    {
        [OperationContract(Action = "*", IsOneWay = true)]
        [PostMessageAction("*", "*", IsOneWay = true)]
        [PostResourceAction("/*")]
        [PutResourceAction("/*")]
        [PatchResourceAction("/*")]
        void RouteMessage(Message message);
    }
}
