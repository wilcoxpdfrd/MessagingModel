// Microsoft.AspNetCore.Hosting.WebHostBuilderContext
using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
	public class MessagingHostBuilderContext
	{
		static Dictionary<Type, MessagingHostBuilderContext.MessagingHostBuilderContextPropertyInfo> messagingHostBuilderContextInfos =
			new Dictionary<Type, MessagingHostBuilderContextPropertyInfo>();

		struct MessagingHostBuilderContextPropertyInfo
		{
			public Type HostingEnvironmentPropertyType;
			public PropertyInfo HostingEnvironmentPI;
			public PropertyInfo ApplicationNamePI;
			public PropertyInfo EnvironmentNamePI;
			public PropertyInfo ContentRootPathPI;
			public PropertyInfo ContentRootPathFPPI;
			public PropertyInfo HostRootPathPI;
			public PropertyInfo HostRootPathFPPI;
			public PropertyInfo ConfigurationPI;
		}

		IConfiguration configuration;
		IApplicationHostEnvironment hostEnvironment;

		public IConfiguration Configuration
		{
			get => this.configuration;
			set => this.configuration = value;
		}

		public IApplicationHostEnvironment HostEnvironment
		{
			get => this.hostEnvironment;
			set => this.hostEnvironment = value;
		}

		public Object GetMessagingHostBuilderContext()
		{
			Type type = this.GetType();

			//PropertyInfo hostingEnvironmentPropertyInfo = type.GetProperty("HostingEnvironment");

			//if (hostingEnvironmentPropertyInfo.PropertyType.FullName == "Microsoft.AspNetCore.Hosting.IWebHostEnvironment")
			//{
			//	this.environmentNamePI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("EnvironmentName");
			//	this.applicationNamePI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("ApplicationName");
			//	this.contentRootPathPI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("ContentRootPath");
			//	this.contentRootPathFPPI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("ContentRootFileProvider");
			//	this.hostRootPathPI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("WebRootPath");
			//	this.webRootPathFPPI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("WebRootFileProvider");
			//}

			//this.environmentNamePI.SetValue(base.HostingEnvironment, value.EnvironmentName);
			//this.applicationNamePI.SetValue(base.HostingEnvironment, value.ApplicationName);
			//this.contentRootPathPI.SetValue(base.HostingEnvironment, value.ContentRootPath);
			//this.contentRootPathFPPI.SetValue(base.HostingEnvironment, value.ContentRootFileProvider);
			//this.hostRootPathPI.SetValue(base.HostingEnvironment, value.HostRootPath);
			//this.webRootPathFPPI.SetValue(base.HostingEnvironment, value.HostRootFileProvider);

			return null;
		}

		internal static void Initialize(object ctx, MessagingHostBuilderContext context)
		{
			Type ctxType = ctx.GetType();

			MessagingHostBuilderContextPropertyInfo messagingHostBuilderContextPropertyInfo;

			if (!MessagingHostBuilderContext.messagingHostBuilderContextInfos.ContainsKey(ctxType))
			{
				PropertyInfo hostingEnvironmentPropertyInfo = ctxType.GetProperty("HostingEnvironment");
				PropertyInfo cofigurationPropertyInfo = ctxType.GetProperty("Configuration");

				messagingHostBuilderContextPropertyInfo = new MessagingHostBuilderContextPropertyInfo();

				messagingHostBuilderContextPropertyInfo.HostingEnvironmentPropertyType = hostingEnvironmentPropertyInfo.PropertyType;
				messagingHostBuilderContextPropertyInfo.HostingEnvironmentPI = hostingEnvironmentPropertyInfo;
				messagingHostBuilderContextPropertyInfo.ApplicationNamePI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("ApplicationName");
				messagingHostBuilderContextPropertyInfo.EnvironmentNamePI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("EnvironmentName");
				messagingHostBuilderContextPropertyInfo.ContentRootPathPI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("ContentRootPath");
				messagingHostBuilderContextPropertyInfo.ContentRootPathFPPI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("ContentRootFileProvider");
				messagingHostBuilderContextPropertyInfo.HostRootPathPI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("WebRootPath");
				messagingHostBuilderContextPropertyInfo.HostRootPathFPPI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("WebRootFileProvider");
				messagingHostBuilderContextPropertyInfo.ConfigurationPI = hostingEnvironmentPropertyInfo.PropertyType.GetProperty("Configuration");

				MessagingHostBuilderContext.messagingHostBuilderContextInfos.Add(ctxType, messagingHostBuilderContextPropertyInfo);
			}
			else

				messagingHostBuilderContextPropertyInfo = MessagingHostBuilderContext.messagingHostBuilderContextInfos[ctxType];

			Type hostingEnvironmentType;

			if (messagingHostBuilderContextPropertyInfo.HostingEnvironmentPropertyType.FullName == "Microsoft.AspNetCore.Hosting.IWebHostEnvironment")

				hostingEnvironmentType = Type.GetType("Microsoft.AspNetCore.Hosting.HostingEnvironment");

			else

				hostingEnvironmentType = Type.GetType("Microsoft.AspNetCore.Hosting.Internal.HostingEnvironment");

			Object hostingEnvironment = Activator.CreateInstance(hostingEnvironmentType);

			messagingHostBuilderContextPropertyInfo.HostingEnvironmentPI.SetValue(ctx, hostingEnvironment);
			messagingHostBuilderContextPropertyInfo.ApplicationNamePI.SetValue(hostingEnvironment, context.HostEnvironment.ApplicationName);
			messagingHostBuilderContextPropertyInfo.EnvironmentNamePI.SetValue(hostingEnvironment, context.HostEnvironment.EnvironmentName);
			messagingHostBuilderContextPropertyInfo.ContentRootPathPI.SetValue(hostingEnvironment, context.HostEnvironment.ContentRootPath);
			messagingHostBuilderContextPropertyInfo.ContentRootPathFPPI.SetValue(hostingEnvironment, context.HostEnvironment.ContentRootFileProvider);
			messagingHostBuilderContextPropertyInfo.HostRootPathPI.SetValue(hostingEnvironment, context.HostEnvironment.HostRootPath);
			messagingHostBuilderContextPropertyInfo.HostRootPathFPPI.SetValue(hostingEnvironment, context.HostEnvironment.HostRootFileProvider);
			messagingHostBuilderContextPropertyInfo.ConfigurationPI.SetValue(ctx, context.Configuration);
		}
	}
}