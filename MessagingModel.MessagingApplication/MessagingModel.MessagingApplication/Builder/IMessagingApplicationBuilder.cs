using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.MessagingApplication.Builder
{
    /// <summary>
    /// Deines an interface that provides the mechanisms to configure an application's messaging pipeline.  
    /// Supports one of <see cref="IMessagingApplicationBuilder{MessageContext}"/> or 
    /// <see cref="IMessagingApplicationBuilder{ProtocolContext, MessageContext}"/> or 
    /// <see cref="IMessagingApplicationBuilder{ProtocolContextHost, ProtocolContext, MessageContext}"/>
    /// </summary>
    public interface IAbstractMessagingApplicationBuilder<MessageContext> :
        IAbstractApplicationBuilder
    {
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
    /// Deines an interface that provides the mechanisms to configure an application's messaging pipeline (where messages originate from an applications internal listeners).
    /// </summary>
    public interface IMessagingApplicationBuilder<MessageContext>
        : IAbstractMessagingApplicationBuilder<MessageContext>
    {
        IMessagingApplicationBuilder<MessageContext> New();

        /// <summary>
        /// Builds the delegate used by this application to process <see cref="IMessagingContext{MessageContext>}"/> input messages.
        /// </summary>
        /// <returns>
        /// <see cref="MessagingContextMiddlewareDelegate<{MessageContext}"/>
        /// </returns>
        Task<MessagingContextMiddlewareDelegate<MessageContext>> BuildMessagingContextMiddlewareAsync();

        /// <summary>
        /// Adds a middleware component used by the application to process input messaging contexts.
        /// </summary>
        /// <param name="middlewareComponent"></param>
        /// <returns><see cref="IMessagingApplicationBuilder{MessageContext}"/></returns>
        IMessagingApplicationBuilder<MessageContext> Use(Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>> middlewareComponent);
    }

    /// <summary>
    /// Deines an interface that provides the mechanisms to configure an application's messaging pipeline (where messages originate from listeners external to the application).
    /// </summary>
    public interface IMessagingApplicationBuilder<ProtocolContext, MessageContext>
        : IAbstractMessagingApplicationBuilder<MessageContext>
    {
        IMessagingApplicationBuilder<ProtocolContext, MessageContext> New();

        /// <summary>
        /// Adds a middleware component used by the application to process context objects received from the bound channel.
        /// </summary>
        /// <param name="middlewareComponent"></param>
        /// <returns><see cref="IMessagingApplicationBuilder{ProtocolContext, MessageContext}"/></returns>
        IMessagingApplicationBuilder<ProtocolContext, MessageContext> Use(Func<ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>, ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>> middlewareComponent);

        /// <summary>
        /// Adds a middleware component used by the application to process context objects received from the bound channel.
        /// </summary>
        /// <param name="middlewareComponent"></param>
        /// <returns><see cref="IMessagingApplicationBuilder{ProtocolContext, MessageContext}"/></returns>
        IMessagingApplicationBuilder<ProtocolContext, MessageContext> Use(Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>> middlewareComponent);

        /// <summary>
        /// Builds the delegate used by this application to process <see cref="ProtocolContext"/> input messages.
        /// </summary>
        /// <returns>
        /// <see cref="ContextMiddlewareDelegate{ProtocolContext}"/>
        /// </returns>
        Task<ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>> BuildContextMiddlewareAsync();

        /// <summary>
        /// Asynchronously attempts to bind a <paramref name="protocolContext"/> to a channel.
        /// </summary>
        /// <param name="protocolContext"></param>
        /// <returns></returns>
        Task<bool> TryBindToChannelAsync(IMessagingContext<ProtocolContext> protocolContext);

        /// <summary>
        /// Asynchronously receives an <see cref="IMessagingContext{MessageContext}"/> from the bound channel.
        /// </summary>
        /// <returns></returns>
        Task<IMessagingContext<MessageContext>> ReceiveMessagingContextAsync();
    }

    /// <summary>
    /// Deines an interface that provides the mechanisms to configure an application's messaging pipeline (where messages originate from listeners external to the application).
    /// </summary>
    public interface IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>
        : IAbstractMessagingApplicationBuilder<MessageContext> where ProtocolContextHost : IProtocolContextHost<ProtocolContext>
    {
        IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> New();

        /// <summary>
        /// Adds a middleware component used by the application to process context objects received from the bound channel.
        /// </summary>
        /// <param name="middlewareComponent"></param>
        /// <returns><see cref="IMessagingApplicationBuilder{ProtocolContext, MessageContext}"/></returns>
        IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> Use(Func<ContextMiddlewareDelegate<ProtocolContextHost>, ContextMiddlewareDelegate<ProtocolContextHost>> middlewareComponent);

        /// <summary>
        /// Adds a middleware component used by the application to process context objects received from the bound channel.
        /// </summary>
        /// <param name="middlewareComponent"></param>
        /// <returns><see cref="IMessagingApplicationBuilder{ProtocolContext, MessageContext}"/></returns>
        IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> Use(Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>> middlewareComponent);

        /// <summary>
        /// Builds the delegate used by this application to process <see cref="ProtocolContextHost"/> process context objects received from the bound channel.
        /// </summary>
        /// <returns>
        /// <see cref="ContextMiddlewareDelegate{ProtocolContextHost}"/>
        /// </returns>
        Task<ContextMiddlewareDelegate<ProtocolContextHost>> BuildContextMiddlewareAsync();

        /// <summary>
        /// Asynchronously attempts to bind a <paramref name="protocolContextHost"/> to a channel.
        /// </summary>
        /// <param name="protocolContextHost"></param>
        /// <returns></returns>
        Task<bool> TryBindToChannelAsync(ProtocolContextHost protocolContextHost);

        /// <summary>
        /// Asynchronously receives an <see cref="IMessagingContext{MessageContext}"/> from the bound channel.
        /// </summary>
        /// <returns></returns>
        Task<IMessagingContext<MessageContext>> ReceiveMessagingContextAsync(TimeSpan timeout);
    }
}
