using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public interface IMessagingContextChannel<MessageContext> :
        IMessagingChannel<MessageContext>, IDisposable
    {
        /// <summary>
        /// Indicates that the channel is open and ready to be used.
        /// </summary>
        bool IsOpen { get; }
        /// <summary>
        /// Indicates the messaging interactions implemented by the channel.
        /// </summary>
        MessagingChannelInteractions Interactions { get; }
        /// <summary>
        /// Maps the connection context to the channel
        /// </summary>
        /// <param name="connectionContext"></param>
        void MapConnection(ConnectionContext connectionContext);
        /// <summary>
        /// Opens the channel with a timeout.
        /// </summary>
        void Open(TimeSpan timeout);
        /// <summary>
        /// Asynchronously opens the channel with a timeout.
        /// </summary>
        Task OpenAsync(TimeSpan timeout);
        /// <summary>
        /// Closes the channel with a timeout.
        /// </summary>
        void Close(TimeSpan timeout);
        /// <summary>
        /// Asynchronously closes the channel with a timeout.
        /// </summary>
        Task CloseAsync(TimeSpan timeout);
        /// <summary>
        /// Aborts the channel.
        /// </summary>
        void Abort();
        /// <summary>
        /// Gets or sets a delegate invoked when the channel is closed.
        /// </summary>
        Action<IMessagingContextChannel<MessageContext>> Closed { get; set; }
    }
}