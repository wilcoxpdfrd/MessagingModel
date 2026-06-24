using AllVerge.MessagingModel.MessagingApplication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    /// <summary>
    /// Allows consumers to perform cleanup during a graceful shutdown.
    /// </summary>
    public class HostApplicationLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource _startedSource = new CancellationTokenSource();

        private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();

        private readonly CancellationTokenSource _stoppedSource = new CancellationTokenSource();

        private readonly ILogger<HostApplicationLifetime> _logger;

        public HostApplicationLifetime(ILogger<HostApplicationLifetime> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Triggered when the application host has fully started and is about to wait
        /// for a graceful shutdown.
        /// </summary>
        public CancellationToken ApplicationStarted => _startedSource.Token;

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// Request may still be in flight. Shutdown will block until this event completes.
        /// </summary>
        public CancellationToken ApplicationStopping => _stoppingSource.Token;

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// All requests should be complete at this point. Shutdown will block
        /// until this event completes.
        /// </summary>
        public CancellationToken ApplicationStopped => _stoppedSource.Token;

        public void StopApplication()
        {
            lock (_stoppingSource)
            {
                try
                {
                    ExecuteHandlers(_stoppingSource);
                }
                catch (Exception exception)
                {
                    _logger.ApplicationError(7, "An error occurred stopping the application", exception);
                }
            }
        }

        /// <summary>
        /// Signals the ApplicationStarted event and blocks until it completes.
        /// </summary>
        public void NotifyStarted()
        {
            try
            {
                ExecuteHandlers(_startedSource);
            }
            catch (Exception exception)
            {
                _logger.ApplicationError(6, "An error occurred starting the application", exception);
            }
        }

        /// <summary>
        /// Signals the ApplicationStopped event and blocks until it completes.
        /// </summary>
        public void NotifyStopped()
        {
            try
            {
                ExecuteHandlers(_stoppedSource);
            }
            catch (Exception exception)
            {
                _logger.ApplicationError(8, "An error occurred stopping the application", exception);
            }
        }

        private void ExecuteHandlers(CancellationTokenSource cancel)
        {
            if (!cancel.IsCancellationRequested)
            {
                cancel.Cancel(throwOnFirstException: false);
            }
        }
    }
}