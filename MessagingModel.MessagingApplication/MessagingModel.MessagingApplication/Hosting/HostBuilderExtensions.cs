using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    public static class HostBuilderExtensions
    {
		/// <summary>
		/// Adds a delegate for configuring the provided <see cref="LoggerFactory" />. This may be called multiple times.
		/// </summary>
		/// <param name="hostBuilder">The <see cref="IMessagingHostBuilder" /> to configure.</param>
		/// <param name="configureLogging">The delegate that configures the <see cref="LoggerFactory" />.</param>
		/// <returns>The <see cref="IMessagingHostBuilder" />.</returns>
		public static IMessagingHostBuilder ConfigureLogging(this IMessagingHostBuilder hostBuilder, Action<MessagingHostBuilderContext, ILoggingBuilder> configureLogging)
		{
			return hostBuilder.ConfigureServices(delegate (MessagingHostBuilderContext context, IServiceCollection collection)
			{
				collection.AddLogging(delegate (ILoggingBuilder builder)
				{
					configureLogging(context, builder);
				});
			});
		}

		/// <summary>
		/// Configures the default service provider
		/// </summary>
		/// <param name="hostBuilder">The <see cref="IMessagingHostBuilder" /> to configure.</param>
		/// <param name="configure">A callback used to configure the <see cref="ServiceProviderOptions" /> for the default <see cref="IServiceProvider" />.</param>
		/// <returns>The <see cref="IMessagingHostBuilder" />.</returns>
		public static IMessagingHostBuilder UseDefaultServiceProvider(this IMessagingHostBuilder hostBuilder, Action<MessagingHostBuilderContext, ServiceProviderOptions> configure)
		{
			return hostBuilder.ConfigureServices(delegate (MessagingHostBuilderContext context, IServiceCollection services)
			{
				ServiceProviderOptions serviceProviderOptions = new ServiceProviderOptions();
				configure(context, serviceProviderOptions);
				services.Replace(ServiceDescriptor.Singleton((IServiceProviderFactory<IServiceCollection>)new DefaultServiceProviderFactory(serviceProviderOptions)));
			});
		}
	}
}
