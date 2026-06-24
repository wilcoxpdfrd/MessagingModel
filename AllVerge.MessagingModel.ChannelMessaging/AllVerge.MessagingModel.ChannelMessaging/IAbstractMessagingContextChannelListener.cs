using System;
using System.Collections.Generic;
using System.Threading;

using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace AllVerge.MessagingModel.ChannelMessaging.Listeners
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;

    /// <summary>
    /// Abstract interface supporting one of <see cref="IMessagingContextChannelListener{MessageContext}"/> or <see cref="IMessagingContextChannelListener{ProtocolContext, MessageContext}"/>.
    /// </summary>
    public interface IAbstractMessagingContextChannelListener : IDisposable
    {
        /// <summary>
        /// The listen addresses after <see cref="StartListeningAsync()"/> completes.
        /// </summary>
        IEnumerable<String> ListenAddresses { get; }

        /// <summary>
        /// Use to configure the messaging context channel listener.
        /// </summary>
        /// <param name="hostEnvironment"></param>
        /// <param name="listenAddresses"></param>
        /// <param name="services"></param>
        /// <param name="cancellationToken"></param>
        void Init(IApplicationHostEnvironment hostEnvironment, IEnumerable<string> listenAddresses, IServiceProvider services, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously starts listening on the configured channel.
        /// </summary>
        /// <returns></returns>
        Task StartListeningAsync();
    }

    /// <summary>
    /// Implements a messaging context channel listener behavior where the message context is received by the listener.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IMessagingContextChannelListener<MessageContext> :
        IAbstractMessagingContextChannelListener, 
        IBindingContextMapper<MessageContext> 
        where MessageContext : IMessageContext
    {
        /// <summary>
        /// Tries to accept a messaging context channnel from one of the <see cref="IAbstractMessagingContextChannelListener.ListenAddresses"/>.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="searchUri"></param>
        /// <returns>
        /// A two-tuple; the succcess item will be false if no channel was accepted within an implementation provided timeout, 
        /// otherwise the success item will be true, and the messagingContextChannel item will be valued.
        /// </returns>
        Task<(bool success, IReceiveMessagingContextChannel<MessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync();
    }

    /// <summary>
    /// Implements a messaging context channel listener behavior where the protocol binding context is received by the listener.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IMessagingContextChannelListener<ProtocolContext, MessageContext> : 
        IAbstractMessagingContextChannelListener,
        IBindingContextMapper<IMessagingContext<ProtocolContext>>
        where MessageContext : IMessageContext
    {
        /// <summary>
        /// Receives and returns an incoming protocol context if one is available within an implementation provided time.
        /// </summary>
        /// <returns>A two tuple; if success is true protocolContext will have the received value.  Otherwise protocolContext will be null.</returns>
        Task<(bool success, ProtocolContext protocolContext)> TryReceiveContext();

        /// <summary>
        /// Tries to accept a messaging context channnel for the given <paramref name="protocolContext"/>; 
        /// to succeed, the context must have a received location that binds to one of the <see cref="IAbstractMessagingContextChannelListener.ListenAddresses"/>.
        /// </summary>
        /// <param name="protocolContext"></param>
        /// <returns>
        /// A two-tuple; if success is null or false messagingContextChannel will be null;
        /// success will by null if <paramref name="protocolContext"/> did not bind to one of the listeners ListenAddresses;
        /// succcess will be false if <typeparamref name="ProtocolContext"/> bound to one of the listeners ListenAddresses but no channel was accepted within an implementation provided timeout; 
        /// otherwise success will be true and the messagingContextChannel will be valued.
        /// </returns>
        Task<(bool? success, IReceiveMessagingContextChannel<MessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync(ProtocolContext protocolContext);
    }

    /// <summary>
    /// Implements a messaging context channel listener behavior where the hosting/protocol binding context is received by the host and passed to the listener.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IMessagingContextChannelListener<ProtocolContextHost, ProtocolContext, MessageContext> : 
        IAbstractMessagingContextChannelListener,
        IBindingContextMapper<ProtocolContextHost>
        where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
        where MessageContext : IMessageContext
    {
        /// <summary>
        /// Tries to accept a messaging context channnel for the given <paramref name="protocolContext"/>; 
        /// to succeed, the context must have a received location that binds to one of the <see cref="IAbstractMessagingContextChannelListener.ListenAddresses"/>.
        /// </summary>
        /// <param name="protocolContextHost"></param>
        /// <returns>
        /// A two-tuple; the success item will by null if <paramref name="protocolContextHost"/> did not have a received location that bound to one of the listener's ListenAddresses, 
        /// or the succcess item will be false if no channel was accepted within an implementation provided timeout, otherwise the success item will be true, and the messagingContextChannel item will be valued.
        /// </returns>
        Task<(bool? success, IReceiveMessagingContextChannel<MessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync(ProtocolContextHost protocolContextHost);
    }
}