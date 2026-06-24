using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
	public class HostOptions
	{
		public string ApplicationName
		{
			get;
			set;
		}

		public bool PreventHostingStartup
		{
			get;
			set;
		}

		public bool SuppressStatusMessages
		{
			get;
			set;
		}

		public IReadOnlyList<string> HostingStartupAssemblies
		{
			get;
			set;
		}

		public IReadOnlyList<string> HostingStartupExcludeAssemblies
		{
			get;
			set;
		}

		public bool DetailedErrors
		{
			get;
			set;
		}

		public bool CaptureStartupErrors
		{
			get;
			set;
		}

		public string Environment
		{
			get;
			set;
		}

		public string StartupAssembly
		{
			get;
			set;
		}

		public string HostRoot
		{
			get;
			set;
		}

		public string ContentRootPath
		{
			get;
			set;
		}

		public TimeSpan ShutdownTimeout
		{
			get;
			set;
		} = TimeSpan.FromSeconds(5.0);


		public HostOptions()
		{
		}

		public HostOptions(IConfiguration configuration)
			: this(configuration, string.Empty)
		{
		}

		public HostOptions(IConfiguration configuration, string applicationNameFallback)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException("configuration");
			}
			ApplicationName = (configuration[WebHostDefaults.ApplicationKey] ?? applicationNameFallback);
			StartupAssembly = configuration[WebHostDefaults.StartupAssemblyKey];
			DetailedErrors = HostUtilities.ParseBool(configuration, WebHostDefaults.DetailedErrorsKey);
			CaptureStartupErrors = HostUtilities.ParseBool(configuration, WebHostDefaults.CaptureStartupErrorsKey);
			Environment = configuration[WebHostDefaults.EnvironmentKey];
			HostRoot = configuration[WebHostDefaults.WebRootKey];
			ContentRootPath = configuration[WebHostDefaults.ContentRootKey];
			PreventHostingStartup = HostUtilities.ParseBool(configuration, WebHostDefaults.PreventHostingStartupKey);
			SuppressStatusMessages = HostUtilities.ParseBool(configuration, WebHostDefaults.SuppressStatusMessagesKey);
			HostingStartupAssemblies = Split(ApplicationName + ";" + configuration[WebHostDefaults.HostingStartupAssembliesKey]);
			HostingStartupExcludeAssemblies = Split(configuration[WebHostDefaults.HostingStartupExcludeAssembliesKey]);
			string text = configuration[WebHostDefaults.ShutdownTimeoutKey];
			if (!string.IsNullOrEmpty(text) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int result))
			{
				ShutdownTimeout = TimeSpan.FromSeconds(result);
			}
		}

		public IEnumerable<string> GetFinalHostingStartupAssemblies()
		{
			return HostingStartupAssemblies.Except(HostingStartupExcludeAssemblies, StringComparer.OrdinalIgnoreCase);
		}

		private IReadOnlyList<string> Split(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return Array.Empty<string>();
			}
			List<string> list = new List<string>();
			string[] array = value.Split(new char[1]
			{
			';'
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(text);
				}
			}
			return list;
		}
	}
}
