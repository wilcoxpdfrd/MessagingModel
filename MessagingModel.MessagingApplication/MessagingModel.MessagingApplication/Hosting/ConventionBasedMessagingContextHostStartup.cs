using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.DependencyInjection;
using AllVerge.MessagingModel.MessagingApplication.Builder;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
	internal class ConventionBasedMessagingContextHostStartup<MessageContext> :
		IMessagingApplicationStartup<MessageContext>
	{
		private MessagingContextHostStartupLoader<MessageContext>.StartupMethods methods;

		public ConventionBasedMessagingContextHostStartup(MessagingContextHostStartupLoader<MessageContext>.StartupMethods methods)
		{
			this.methods = methods;
		}

		public void Configure(IMessagingApplicationBuilder<MessageContext> app, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment env, ILoggerFactory loggerFactory)
		{
			try
			{
				this.methods.ConfigureDelegate(app);
			}
			catch (Exception ex)
			{
				if (ex is TargetInvocationException)
				{
					ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				}
				throw;
			}
		}

		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			try
			{
				return this.methods.ConfigureServicesDelegate(services);
			}
			catch (Exception ex)
			{
				if (ex is TargetInvocationException)
				{
					ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				}
				throw;
			}
		}
	}

	internal class ConventionBasedMessagingContextHostStartup<ProtocolContext, MessageContext> :
		IMessagingApplicationStartup<ProtocolContext, MessageContext>
	{
        private MessagingContextHostStartupLoader<ProtocolContext, MessageContext>.StartupMethods methods;

        public ConventionBasedMessagingContextHostStartup(MessagingContextHostStartupLoader<ProtocolContext, MessageContext>.StartupMethods methods)
        {
            this.methods = methods;
        }

		public void Configure(IMessagingApplicationBuilder<ProtocolContext, MessageContext> app, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment env, ILoggerFactory loggerFactory)
		{
			try
			{
				this.methods.ConfigureDelegate(app);
			}
			catch (Exception ex)
			{
				if (ex is TargetInvocationException)
				{
					ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				}
				throw;
			}
		}

		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			try
			{
				return this.methods.ConfigureServicesDelegate(services);
			}
			catch (Exception ex)
			{
				if (ex is TargetInvocationException)
				{
					ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				}
				throw;
			}
		}
	}

    internal class ConventionBasedMessagingContextHostStartup<ProtocolContextHost, ProtocolContext, MessageContext> :
        IMessagingApplicationStartup<ProtocolContextHost, ProtocolContext, MessageContext>
		where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
    {
        private MessagingContextHostStartupLoader<ProtocolContextHost, ProtocolContext, MessageContext>.StartupMethods methods;

        public ConventionBasedMessagingContextHostStartup(MessagingContextHostStartupLoader<ProtocolContextHost, ProtocolContext, MessageContext>.StartupMethods methods)
        {
            this.methods = methods;
        }

        public void Configure(IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> app, IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment env, ILoggerFactory loggerFactory)
        {
            try
            {
                this.methods.ConfigureDelegate(app);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                throw;
            }
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            try
            {
                return this.methods.ConfigureServicesDelegate(services);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                throw;
            }
        }
    }
}