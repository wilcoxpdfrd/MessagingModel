using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    using AllVerge.MessagingModel.MessagingApplication;

    public interface IMessageDispatcher<MessagingContext, MessageType> where MessageType : class
    {
        void Init(IDispatcherRuntime dispatchRuntime);
        /// <summary>
        /// Dispatches the <paramref name="messageType"/>; 
        /// the dispatcher should not fault; rather it should prepare a message reflecting any exception and return that instead.
        /// </summary>
        /// <param name="messagingContext"></param>
        /// <param name="messageType"></param>
        /// <returns></returns>
        Task<MessageType> DispatchMessageAsync(IMessagingContext<MessagingContext> messagingContext, MessageType messageType);
    }
}