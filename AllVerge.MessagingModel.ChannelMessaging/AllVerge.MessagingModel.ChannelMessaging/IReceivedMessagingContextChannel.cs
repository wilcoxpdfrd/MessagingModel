using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    /// <summary>
    /// Defines a single-use channel that contains a received (request) message (see <see cref="IReceiveMessagingContextChannel{MessageContext}"/>, 
    /// and that provides for sending the response message.  Note that implementations of this channel should dispose it after sending the response message.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IReceivedMessagingContextChannel<MessageContext> :
        ISendMessagingContextChannel<MessageContext>
    {
        /// <summary>
        /// The received protocol messaging context.
        /// </summary>
        IMessagingContext<MessageContext> ReceivedMessagingContext { get; }
        /// <summary>
        /// Configures <paramref name="messagingContext"/> with any channel level properties used for binding to the messaging middleware pipeline.
        /// </summary>
        /// <param name="receivedMessagingContext"></param>
        void ConfigureChannelProperties(IMessagingContext<MessageContext> messagingContext);
        /// <summary>
        /// A callback invoked after a received message has been handled by the messaging middleware pipeline.
        /// </summary>
        /// <param name="messagingContext"></param>
        /// <returns></returns>
        Task HandledMessagingCallBackAsync(IMessagingContext<MessageContext> messagingContext);
    }
}
