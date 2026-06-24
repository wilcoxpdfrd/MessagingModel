using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
	public static class WebHostBuilderExtensions
	{
        /// <summary>
        /// Specify the startup type to be used by the web host.
        /// </summary>
        /// <typeparam name="MessageContext">The messaging context type.</typeparam>
        /// <param name="hostBuilder">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> to configure.</param>
        /// <param name="startupType">The <see cref="T:System.Type" /> to be used.</param>
        /// <returns>The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" />.</returns>
        public static IWebHostBuilder UseStartup<MessageContext>(this IWebHostBuilder hostBuilder, Type startupType)
		{
			string name = startupType.GetTypeInfo().Assembly.GetName().Name;
			return hostBuilder.UseSetting(WebHostDefaults.ApplicationKey, name).ConfigureServices((Action<IServiceCollection>)delegate (IServiceCollection services)
			{
				if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
				{
					ServiceCollectionServiceExtensions.AddSingleton(services, typeof(IMessagingApplicationStartup<MessageContext>), startupType);
				}
				else
				{
					ServiceCollectionServiceExtensions.AddSingleton(services, typeof(IMessagingApplicationStartup<MessageContext>), (Func<IServiceProvider, object>)delegate (IServiceProvider sp)
					{
                        IHostEnvironment requiredService = ServiceProviderServiceExtensions.GetRequiredService<IHostEnvironment>(sp);
						return new ConventionBasedMessagingContextHostStartup<MessageContext>(MessagingContextHostStartupLoader<MessageContext>.LoadMethods(sp, startupType, requiredService.EnvironmentName));
					});
				}
			});
		}

        /// <summary>
        /// Specify the startup type to be used by the web host.
        /// </summary>
        /// <typeparam name="ProtocolContext">The protocol context type.</typeparam>
        /// <typeparam name="MessageContext">The messaging context type.</typeparam>
        /// <param name="hostBuilder">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> to configure.</param>
        /// <param name="startupType">The <see cref="T:System.Type" /> to be used.</param>
        /// <returns>The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" />.</returns>
        public static IWebHostBuilder UseStartup<ProtocolContext, MessageContext>(this IWebHostBuilder hostBuilder, Type startupType)
		{
			string name = startupType.GetTypeInfo().Assembly.GetName().Name;
			return hostBuilder.UseSetting(WebHostDefaults.ApplicationKey, name).ConfigureServices((Action<IServiceCollection>)delegate (IServiceCollection services)
			{
				if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
				{
					ServiceCollectionServiceExtensions.AddSingleton(services, typeof(IMessagingApplicationStartup<ProtocolContext, MessageContext>), startupType);
				}
				else
				{
					ServiceCollectionServiceExtensions.AddSingleton(services, typeof(IMessagingApplicationStartup<ProtocolContext, MessageContext>), (Func<IServiceProvider, object>)delegate (IServiceProvider sp)
					{
                        IHostEnvironment requiredService = ServiceProviderServiceExtensions.GetRequiredService<IHostEnvironment>(sp);

                        return new ConventionBasedMessagingContextHostStartup<ProtocolContext, MessageContext>(MessagingContextHostStartupLoader<ProtocolContext, MessageContext>.LoadMethods(sp, startupType, requiredService.EnvironmentName));
					});
				}
			});
		}

        /// <summary>
        /// Specify the startup type to be used by the web host.
        /// </summary>
        /// <typeparam name="ProtocolContextHost">The hosting context type for the protocol host.</typeparam>
        /// <typeparam name="ProtocolContext">The protocol context type.</typeparam>
        /// <typeparam name="MessageContext">The messaging context type.</typeparam>
        /// <param name="hostBuilder">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> to configure.</param>
        /// <param name="startupType">The <see cref="T:System.Type" /> to be used.</param>
        /// <returns>The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" />.</returns>
        public static IWebHostBuilder UseStartup<ProtocolContextHost, ProtocolContext, MessageContext>(this IWebHostBuilder hostBuilder, Type startupType)
            where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
        {
            string name = startupType.GetTypeInfo().Assembly.GetName().Name;
            return hostBuilder.UseSetting(WebHostDefaults.ApplicationKey, name).ConfigureServices((Action<IServiceCollection>)delegate (IServiceCollection services)
            {
                if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                {
                    ServiceCollectionServiceExtensions.AddSingleton(services, typeof(IMessagingApplicationStartup<ProtocolContextHost, ProtocolContext, MessageContext>), startupType);
                }
                else
                {
                    ServiceCollectionServiceExtensions.AddSingleton(services, typeof(IMessagingApplicationStartup<ProtocolContextHost, ProtocolContext, MessageContext>), (Func<IServiceProvider, object>)delegate (IServiceProvider sp)
                    {
                        IHostEnvironment requiredService = ServiceProviderServiceExtensions.GetRequiredService<IHostEnvironment>(sp);
                        return new ConventionBasedMessagingContextHostStartup<ProtocolContextHost, ProtocolContext, MessageContext>(MessagingContextHostStartupLoader<ProtocolContextHost, ProtocolContext, MessageContext>.LoadMethods(sp, startupType, requiredService.EnvironmentName));
                    });
                }
            });
        }

        /// <summary>
        /// Specify the startup type to be used by the web host.
        /// </summary>
        /// <typeparam name="ProtocolContext">The protocol context type.</typeparam>
        /// <typeparam name="MessageContext">The messaging context type.</typeparam>
        /// <typeparam name="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <param name="hostBuilder">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> to configure.</param>
        /// <returns>The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" />.</returns>
        public static IWebHostBuilder UseStartup<MessageContext, TStartup>(this IWebHostBuilder hostBuilder) where TStartup : class
		{
			return hostBuilder.UseStartup<MessageContext>(typeof(TStartup));
		}

        /// <summary>
        /// Specify the startup type to be used by the web host.
        /// </summary>
        /// <typeparam name="ProtocolContext">The protocol context type.</typeparam>
        /// <typeparam name="MessageContext">The messaging context type.</typeparam>
        /// <typeparam name="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <param name="hostBuilder">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> to configure.</param>
        /// <returns>The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" />.</returns>
        public static IWebHostBuilder UseStartup<ProtocolContext, MessageContext, TStartup>(this IWebHostBuilder hostBuilder) where TStartup : class
		{
			return hostBuilder.UseStartup<ProtocolContext, MessageContext>(typeof(TStartup));
		}

        /// <summary>
        /// Specify the startup type to be used by the web host.
        /// </summary>
		/// <typeparam name="ProtocolContextHost">The hosting type for the protocol context.</typeparam>
		/// <typeparam name="ProtocolContext">The protocol context type.</typeparam>
		/// <typeparam name="MessageContext">The messaging context type.</typeparam>
        /// <param name="hostBuilder">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> to configure.</param>
        /// <typeparam name="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <returns>The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" />.</returns>
        public static IWebHostBuilder UseStartup<ProtocolContextHost, ProtocolContext, MessageContext, TStartup>(this IWebHostBuilder hostBuilder) 
            where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
            where TStartup : class
        {
            return hostBuilder.UseStartup<ProtocolContextHost, ProtocolContext, MessageContext>(typeof(TStartup));
        }
    }
}
