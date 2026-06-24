using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public interface IMessagingContextAccessor<MessageContext>
    {
        Task<IMessagingContext<MessageContext>> ReceivedMessagingContextAsync();

        IMessagingContext<MessageContext> MessagingContext { set; }
    }
}
