using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Channels
{
    /// <summary>
    /// The run-time typically calls <see cref="ReceiveMessagesAsync(int, TimeSpan, CancellationToken)"/> periodically using a rate calculation 
    /// based on <see cref="PollSize"/> and <see cref="PollTimeoutMS"/> and other configured values (see <see cref="MessagingPollOptions"/>).
    /// Depending on the internal protocol binding, implementations may use the underlying SolicitResponse methods to solicit a mult-part respose 
    /// that is depcomposed into the individual messges returned from <see cref="ReceiveMessagesAsync(int, TimeSpan, CancellationToken)"/> and use
    /// <see cref="AcknowledgeReceivedMessagingContextAsync(MessageContext)"/> to acknowledge those messages, or they may bypass those underlying methods.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public abstract class AbstractPollMessagingContextChannel<MessageContext>:
        AbstractSolicitResponseMessagingContextChannel<MessageContext>
    {
        protected AbstractPollMessagingContextChannel() : base(MessagingChannelInteractions.Poll) { }

        public abstract int PollSize { get; }
        public abstract int PollTimeoutMS { get; }
        public abstract Task<IMessagingContext<MessageContext>[]> ReceiveMessagesAsync(int pollSize, TimeSpan timeout, CancellationToken cancellationToken);
        public abstract Task AcknowledgeReceivedMessagingContextAsync(MessageContext messagingContext);
    }
}
