using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AllVerge.MessagingModel.Description
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;

    [ResourceContract(Namespace = DescriptionConstants.Namespace, Name = DescriptionConstants.ProtocolServiceName)]
    public interface IProtocolDescriptionService
    {
        [GetResourceAction("?description")]
        Stream GetProtocolDescription();
    }
}
