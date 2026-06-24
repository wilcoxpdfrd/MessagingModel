using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.MessagingApplication
{
    /// <summary>
    /// Defines the protocol context dispatch reject codes
    /// </summary>
    public enum RejectCode
    {
        /// <summary>
        /// No channel binding was found for the protocol context
        /// </summary>
        BindingUnreachable,
        /// <summary>
        /// The channel was too busy to dispatch the protocol context
        /// </summary>
        TooBusy,
        /// <summary>
        /// The protocol context was not authorized
        /// </summary>
        NotAuthorized,
        /// <summary>
        /// The protocol context dispatch operation did not complete in the alloted time
        /// </summary>
        Timeout,
        /// <summary>
        /// A fault occured in the protocol context dispatch operation
        /// </summary>
        Faulted,
        /// <summary>
        /// The dispatch operation did not handle the protocol context
        /// </summary>
        NotHandled
    }

    /// <summary>
    /// Defines the protocol context not rejected headers that can be specified by 
    /// <see cref="IProtocolContextFactory{TContext}.RejectAsync(TContext, RejectCode, IDictionary{RejectHeaders, StringValues})"/>.
    /// </summary>
    /// <remarks>The response headers defined in rf2616 may be considered additionally here.</remarks>
    /// <seealso cref="https://www.rfc-editor.org/rfc/rfc2616#page-41"/>
    public enum RejectHeaders
    {
        /// <summary>
        /// Indicates the protocol level authentication scheme that must be used.
        /// </summary>
        Authenticate,
        /// <summary>
        /// Indicates the length of time to wait before retrying.
        /// </summary>
        RetryAfter
    }

    /// <summary>
    /// Abstract interface underlying <see cref="IMessagingContextReceiver{MessageContext}"/> 
    /// or <see cref="IMessagingContextReceiver{ProtocolContext, MessageContext}"/> 
    /// or <see cref="IMessagingContextReceiver{ProtocolContextHost, ProtocolContext, MessageContext}"/>.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IAbstractMessagingContextReceiver<MessageContext> : IDisposable
    {
        /// <summary>
        /// Starts channel listeners configured for the <paramref name="serverAddresses"/>.
        /// </summary>
        /// <param name="serverAddresses"></param>
        /// <returns></returns>
        Task StartAsync(IServerAddressesFeature serverAddresses);

        /// <summary>
        /// Prepares a rejected messagingContext given the <paramref name="rejectionCode"/> and <paramref name="rejectionHeaders"/> by creating and setting an appropriate Output in the context.
        /// </summary>
        /// <param name="messagingContext"></param>
        /// <param name="rejectionCode"></param>
        /// <param name="rejectionHeaders"></param>
        /// <returns></returns>
        Task PrepareRejectedMessagingContextAsync(IMessagingContext<MessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders = null);

        /// <summary>
        /// Invoke the <paramref name="messagingContext"/> call back.
        /// </summary>
        /// <param name="messagingContext"></param>
        /// <returns></returns>
        Task InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext);
    }

    /// <summary>
    /// Implements a messaging context receiver.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IMessagingContextReceiver<MessageContext> :
        IProtocolContextFactory<IMessagingContext<MessageContext>>,
        IAbstractMessagingContextReceiver<MessageContext>
    {
    }

    /// <summary>
    /// Implements a messaging context receiver that binds a protocol context to a channel.
    /// </summary>
    /// <typeparam name="ProtocolContext"></typeparam>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IMessagingContextReceiver<ProtocolContext, MessageContext> :
        IProtocolContextFactory<IMessagingContext<ProtocolContext>>,
        IProtocolContextAccessorFactory<IMessagingContext<ProtocolContext>>,
        IAbstractMessagingContextReceiver<MessageContext>
    {
        /// <summary>
        /// Asynchronously tries to bind <paramref name="protocolContext"/> to a receiver channel.
        /// </summary>
        /// <param name="protocolContext"></param>
        /// <returns>
        /// true if <paramref name="protocolContext"/> binds to a receiver channel.  Otherwise false.
        /// </returns>
        Task<bool> TryBindToChannelAsync(IMessagingContext<ProtocolContext> protocolContext);

        /// <summary>
        /// Asynchronously receives an <see cref="IMessagingContext{MessageContext}"/> instance from the bound channel.
        /// </summary>
        /// <returns></returns>
        Task<IMessagingContext<MessageContext>> ReceiveMessagingContextAsync();
    }

    /// <summary>
    /// Implements a messaging context receiver that binds a protocol hosting context to a channel.
    /// </summary>
    /// <typeparam name="ProtocolContextHost"></typeparam>
    /// <typeparam name="ProtocolContext"></typeparam>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext> :
        IProtocolContextFactory<ProtocolContextHost>,
        IProtocolContextAccessorFactory<ProtocolContextHost>,
        IAbstractMessagingContextReceiver<MessageContext>
        where ProtocolContextHost : IProtocolContextHost<ProtocolContext>
    {
        /// <summary>
        /// Asynchronously tries to bind <paramref name="protocolContextHost"/> to a receiver channel.
        /// Returns true if <paramref name="protocolContextHost"/> binds to a receiver channel.  Otherwise false.
        /// </summary>
        /// <param name="protocolContextHost"></param>
        /// <returns>
        /// true if <paramref name="protocolContextHost"/> binds to a receiver channel.  Otherwise false.
        /// </returns>
        Task<bool> TryBindToChannelAsync(ProtocolContextHost protocolContextHost);

        /// <summary>
        /// Asynchronously receives an <see cref="IMessagingContext{MessageContext}"/> instance from the bound channel.
        /// </summary>
        /// <returns>The produced <see cref="IMessagingContext{MessageContext}"/> instance.</returns>
        Task<IMessagingContext<MessageContext>> ReceiveMessagingContextAsync(TimeSpan timeout);
    }
}
