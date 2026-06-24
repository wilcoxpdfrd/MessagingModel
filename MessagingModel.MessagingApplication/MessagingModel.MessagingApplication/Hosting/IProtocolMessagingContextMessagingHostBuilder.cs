using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    public interface IProtocolMessagingContextMessagingHostBuilder<MessageContext> :
        IMessagingHostBuilder
    {

    }

    public interface IProtocolMessagingContextMessagingHostBuilder<ProtocolContext, MessageContext> : 
        IMessagingHostBuilder
    {

    }

    public interface IProtocolMessagingContextMessagingHostBuilder<ProtocolContextHost, ProtocolContext, MessageContext> :
        IMessagingHostBuilder
    {

    }

    public interface IMessagingHostBuilder : IWebHostBuilder
    {
        /// <summary>
        /// Adds a delegate for configuring <see cref="IMessagingHostBuilder"/> 
        /// that will construct an <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configureDelegate">
        /// The delegate for configuring the <see cref="IMessagingHostBuilder"/> that will be used to 
        /// construct an <see cref="IConfiguration"/>.
        /// </param>
        /// <remarks>
        /// The <see cref="IConfiguration"/> and <see cref="ILoggerFactory "/>
        /// on the <see cref="MessagingHostBuilderContext"/> are uninitialized at 
        /// this stage. The <see cref="IConfigurationBuilder"/> is pre-populated 
        /// with the settings of the <see cref="IMessagingHostBuilder"/>.
        /// </remarks>
        /// <returns>
        /// The <see cref="IMessagingHostBuilder"/>.
        /// </returns>
        IMessagingHostBuilder ConfigureAppConfiguration(Action<MessagingHostBuilderContext, IConfigurationBuilder> configureDelegate);

        /// Adds a delegate for configuring additional services for the host or messaging application.  
        /// This may be called multiple times.
        /// </summary>
        /// <param name="configureServices">
        /// A delegate for configuring the <see cref="IServiceCollection"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IMessagingHostBuilder"/>.
        /// </returns>
        IMessagingHostBuilder ConfigureServices(Action<MessagingHostBuilderContext, IServiceCollection> configureServices);
    }
}
