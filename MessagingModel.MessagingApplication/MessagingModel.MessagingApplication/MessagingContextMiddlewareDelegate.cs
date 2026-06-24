using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public delegate Task ContextMiddlewareDelegate<TContext>(TContext context);

    public delegate Task MessagingContextMiddlewareDelegate<MessageContext>(IMessagingContext<MessageContext> messagingContext);

    public delegate Task MessagingContextMiddlewareRejectionDelegate<MessageContext>(IMessagingContext<MessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders = null);
}
