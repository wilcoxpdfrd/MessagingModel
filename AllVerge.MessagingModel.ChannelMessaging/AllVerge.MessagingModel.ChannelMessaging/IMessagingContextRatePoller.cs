using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using AllVerge.MessagingModel.MessagingApplication;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public delegate Task<IMessagingContext<MessageContext>[]> ReceiveMessagesAsync<MessageContext>(int poll, TimeSpan timeout, CancellationToken cancellationToken);

    public delegate void CallBack<MessageContext>(MessagingContext<MessageContext> messagingContext);

    /// <summary>
    /// This interfaces define the contract for messaging rate pollers that will be used by the 
    /// <see cref="MessagingContextPollerServer{MessageContext}"/> to listen for messaging contexts on a 
    /// communication channel.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    internal interface IMessagingContextRatePoller<MessageContext> :
        IBindingContextMapper<MessageContext>,
        IDisposable
        where MessageContext : IMessageContext
    {
        int PollSize { get; }
        int PollTimeoutMS { get; }
        int HandleIntervalMS { get; set; }
        int PollIntervalMS { get; set; }

        ReceiveMessagesAsync<MessageContext> ReceiveMessagesAsync { get; }
        Task CallBackAsync(IMessagingContext<MessageContext> messagingContext);
    }
}