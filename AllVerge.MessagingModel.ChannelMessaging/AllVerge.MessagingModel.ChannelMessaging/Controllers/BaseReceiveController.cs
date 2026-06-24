using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using AllVerge.MessagingModel.MessagingApplication;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Controllers
{
    public abstract class BaseReceiveController<MessageContext> :
        BaseController<MessageContext>
    {
        private MessagingReceiveOptions messagingReceiveOptions;

        protected BaseReceiveController(ILogger logger, MessagingReceiveOptions messagingReceiveOptions, Func<IMessagingContext<MessageContext>, RejectCode, IDictionary<RejectHeaders, StringValues>, Task> prepareRejectedMessagingContext, Func<IMessagingContext<MessageContext>, Action, Action, Task> receivedMessagingContext, CancellationToken cancellationToken) :
            base(logger, prepareRejectedMessagingContext, receivedMessagingContext, cancellationToken)
        {
            this.messagingReceiveOptions = messagingReceiveOptions;
        }

        protected MessagingReceiveOptions MessagingReceiveOptions => messagingReceiveOptions;

        protected IDictionary<RejectHeaders, StringValues> CalculateRetryAfter(long rejected)
        {

            Dictionary<RejectHeaders, StringValues> rejectionHeaders =
                new Dictionary<RejectHeaders, StringValues>();

            // https://en.wikipedia.org/wiki/Exponential_backoff (binary exponential backoff)

            var c = Math.DivRem(rejected, (long)this.messagingReceiveOptions.MaxMessagesQueueDepth, out long remainder);

            var t = 2 ^ c;

            rejectionHeaders.Add(RejectHeaders.RetryAfter, t.ToString());

            return rejectionHeaders;
        }

    }
}
