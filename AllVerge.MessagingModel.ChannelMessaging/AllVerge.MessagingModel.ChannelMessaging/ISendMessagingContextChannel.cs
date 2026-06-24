using System;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public interface ISendMessagingContextChannel<MessageContext> :
        IMessagingContextChannel<MessageContext>
    {
        /// <summary>
        /// Sends <paramref name="messagingContext"/>.
        /// </summary>
        /// <param name="messagingContext"></param>
        /// <returns></returns>
        Task SendMessagingContextAsync(MessageContext messagingContext);
        /// <summary>
        /// Asynchronously tries to send <paramref name="messagingContext"/>; if not completed within <paramref name="timeout"/>, throws a <see cref="TimeoutException"/>.
        /// </summary>
        /// <param name="messagingContext"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Task TrySendMessagingContextAsync(MessageContext messagingContext, TimeSpan timeout);
    }
}