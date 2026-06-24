using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Builder
{
    /// <summary>
    /// Extension methods for adding terminal middleware.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds a terminal middleware delegate to the application's request pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IMessagingApplicationBuilder{ProtocolContext, MessageContext}" /> instance.</param>
        /// <param name="handler">A delegate that handles the request.</param>
        public static void Run<MessageContext>(this IMessagingApplicationBuilder<MessageContext> app, MessagingContextMiddlewareDelegate<MessageContext> handler)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            app.Use(_ => handler);
        }

        /// <summary>
        /// Adds a terminal middleware delegate to the application's request pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IMessagingApplicationBuilder{ProtocolContext, MessageContext}" /> instance.</param>
        /// <param name="handler">A delegate that handles the request.</param>
        public static void Run<ProtocolContext, MessageContext>(this IMessagingApplicationBuilder<ProtocolContext, MessageContext> app, MessagingContextMiddlewareDelegate<MessageContext> handler)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            app.Use(_ => handler);
        }

        /// <summary>
        /// Adds a terminal middleware delegate to the application's request pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IMessagingApplicationBuilder{ProtocolContextHost, ProtocolContext, MessageContext}" /> instance.</param>
        /// <param name="handler">A delegate that handles the request.</param>
        public static void Run<ProtocolContextHost, ProtocolContext, MessageContext>(this IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> app, MessagingContextMiddlewareDelegate<MessageContext> handler)
            where ProtocolContextHost : IProtocolContextHost<ProtocolContext>
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            app.Use(_ => handler);
        }
    }
}
