// Microsoft.AspNetCore.Hosting.Internal.HostedServiceExecutor
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
	public class HostedServiceExecutor
	{
		private readonly IEnumerable<IHostedService> _services;

		private readonly ILogger<HostedServiceExecutor> _logger;

		public HostedServiceExecutor(ILogger<HostedServiceExecutor> logger, IEnumerable<IHostedService> services)
		{
			_logger = logger;
			_services = services;
		}

		public async Task StartAsync(CancellationToken token)
		{
			try
			{
				await ExecuteAsync((IHostedService service) => service.StartAsync(token));
			}
			catch (Exception exception)
			{
				_logger.HostedApplicationStartupError(exception);
			}
		}

		public async Task StopAsync(CancellationToken token)
		{
			try
			{
				await ExecuteAsync((IHostedService service) => service.StopAsync(token));
			}
			catch (Exception exception)
			{
				_logger.ApplicationError((EventId)10, "An error occurred stopping the application", exception);
			}
		}

		private async Task ExecuteAsync(Func<IHostedService, Task> callback)
		{
			List<Exception> exceptions = null;
			foreach (IHostedService service in _services)
			{
				try
				{
					await callback(service);
				}
				catch (Exception item)
				{
					if (exceptions == null)
					{
						exceptions = new List<Exception>();
					}
					exceptions.Add(item);
				}
			}
			if (exceptions != null)
			{
				throw new AggregateException(exceptions);
			}
		}
	}
}
