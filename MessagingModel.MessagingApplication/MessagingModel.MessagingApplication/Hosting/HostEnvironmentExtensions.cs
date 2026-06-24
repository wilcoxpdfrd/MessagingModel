using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
	/// <summary>
	/// Extension methods for <see cref="IHostEnvironment" />.
	/// </summary>
	public static class HostEnvironmentExtensions
	{
		internal static void Initialize(this IApplicationHostEnvironment applicationHostEnvironment, string contentRootPath, HostOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException("options");
			}
			if (string.IsNullOrEmpty(contentRootPath))
			{
				throw new ArgumentException("A valid non-empty content root must be provided.", "contentRootPath");
			}
			if (!Directory.Exists(contentRootPath))
			{
				throw new ArgumentException($"The content root '{contentRootPath}' does not exist.", "contentRootPath");
			}
			applicationHostEnvironment.ApplicationName = options.ApplicationName;
			applicationHostEnvironment.ContentRootPath = contentRootPath;
			applicationHostEnvironment.ContentRootFileProvider = new PhysicalFileProvider(applicationHostEnvironment.ContentRootPath);
			string hostRoot = options.HostRoot;
			if (hostRoot == null)
			{
				string text = Path.Combine(applicationHostEnvironment.ContentRootPath, "wwwroot");
				if (Directory.Exists(text))
				{
					applicationHostEnvironment.HostRootPath = text;
				}
			}
			else
			{
				applicationHostEnvironment.HostRootPath = Path.Combine(applicationHostEnvironment.ContentRootPath, hostRoot);
			}
			if (!string.IsNullOrEmpty(applicationHostEnvironment.HostRootPath))
			{
				applicationHostEnvironment.HostRootPath = Path.GetFullPath(applicationHostEnvironment.HostRootPath);
				if (!Directory.Exists(applicationHostEnvironment.HostRootPath))
				{
					Directory.CreateDirectory(applicationHostEnvironment.HostRootPath);
				}
				applicationHostEnvironment.HostRootFileProvider = new PhysicalFileProvider(applicationHostEnvironment.HostRootPath);
			}
			else
			{
				applicationHostEnvironment.HostRootFileProvider = new NullFileProvider();
			}
			applicationHostEnvironment.EnvironmentName = (options.Environment ?? applicationHostEnvironment.EnvironmentName);
		}

		/// <summary>
		/// Checks if the current hosting environment name is <see cref="EnvironmentName.Staging" />.
		/// </summary>
		/// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment" />.</param>
		/// <returns>True if the environment name is <see cref="EnvironmentName.Staging" />, otherwise false.</returns>
		public static bool IsStaging(this IHostEnvironment hostEnvironment)
		{
			if (hostEnvironment == null)
			{
				throw new ArgumentNullException("hostingEnvironment");
			}
			return hostEnvironment.IsEnvironment(EnvironmentName.Staging);
		}

		/// <summary>
		/// Checks if the current hosting environment name is <see cref="EnvironmentName.Production" />.
		/// </summary>
		/// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment" />.</param>
		/// <returns>True if the environment name is <see cref="EnvironmentName.Production" />, otherwise false.</returns>
		public static bool IsProduction(this IHostEnvironment hostEnvironment)
		{
			if (hostEnvironment == null)
			{
				throw new ArgumentNullException("hostingEnvironment");
			}
			return hostEnvironment.IsEnvironment(EnvironmentName.Production);
		}

		/// <summary>
		/// Compares the current hosting environment name against the specified value.
		/// </summary>
		/// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment" />.</param>
		/// <param name="environmentName">Environment name to validate against.</param>
		/// <returns>True if the specified name is the same as the current environment, otherwise false.</returns>
		public static bool IsEnvironment(this IHostEnvironment hostEnvironment, string environmentName)
		{
			if (hostEnvironment == null)
			{
				throw new ArgumentNullException("hostingEnvironment");
			}
			return string.Equals(hostEnvironment.EnvironmentName, environmentName, StringComparison.OrdinalIgnoreCase);
		}
	}
}