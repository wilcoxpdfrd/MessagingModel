using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Description
{
    public interface IMapMessageFaultResult
    {
        Object Map(MessageFault messageFault, MessageVersion messageVersion);
    }
}
