using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public class MessagingContextAccessor<MessageContext> :
        IMessagingContextAccessor<MessageContext>
    {
        private static AsyncLocal<IMessagingContext<MessageContext>> _protocolMessagingContextCurrent = 
            new AsyncLocal<IMessagingContext<MessageContext>>();

        public IMessagingContext<MessageContext> MessagingContext { set => _protocolMessagingContextCurrent.Value = value; }

        public Task<IMessagingContext<MessageContext>> ReceivedMessagingContextAsync()
        {
            if (_protocolMessagingContextCurrent.Value == null)
            {
                System.Diagnostics.Debugger.Break();
            }

            return Task.FromResult(_protocolMessagingContextCurrent.Value);
        }
    }
}
