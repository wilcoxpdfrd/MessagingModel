using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Builder
{
    public interface IProtocolApplicationBuilder<ProtocolContext>
    {
        /// <summary>
        /// Gets or sets the System.IServiceProvider that provides access to the application's
        /// service container.
        /// </summary>
        IServiceProvider ApplicationServices { get; set; }
        /// <summary>
        /// Gets the set of HTTP features the application's server provides.
        /// </summary>
        IFeatureCollection ServerFeatures { get; }
        /// <summary>
        /// Gets a key/value collection that can be used to share data between middleware.
        /// </summary>
        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Builds the delegate used by this application to process HTTP requests.
        /// </summary>
        /// <returns>The request handling delegate.</returns>
        ContextMiddlewareDelegate<ProtocolContext> Build();
        /// <summary>
        /// Creates a new Microsoft.AspNetCore.Builder.IApplicationBuilder that shares the
        /// Microsoft.AspNetCore.Builder.IApplicationBuilder.Properties of this Microsoft.AspNetCore.Builder.IApplicationBuilder.
        /// </summary>
        /// <returns>
        /// The new <see cref="IProtocolApplicationBuilder<ProtocolContext>"/>.
        /// </returns>
        IProtocolApplicationBuilder<ProtocolContext> New();
        /// <summary>
        /// Adds a middleware delegate to the application's request pipeline.
        /// </summary>
        /// <param name="middleware">
        /// The middleware delegate.
        /// </param>
        /// <returns>
        /// The <see cref="IProtocolApplicationBuilder<ProtocolContext>"/>.
        /// </returns>
        IProtocolApplicationBuilder<ProtocolContext> Use(Func<ContextMiddlewareDelegate<ProtocolContext>, ContextMiddlewareDelegate<ProtocolContext>> middleware);
    }
}
