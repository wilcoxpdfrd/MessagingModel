using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using Microsoft.Extensions.Logging;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public static class HostingLoggerExtensions
    {
        public static void HostedApplicationStartupError(this ILogger logger, Exception exception)
        {
            logger.ApplicationError((EventId)7, "Hosted application startup exception", exception);
        }

        public static void HostingStartupAssemblyError(this ILogger logger, Exception exception)
        {
            logger.ApplicationError((EventId)11, "Hosting startup assembly exception", exception);
        }

        public static void ServerStartupError(this ILogger logger, Exception exception)
        {
            logger.ApplicationError((EventId)12, "Server startup exception", exception);
        }

        public static void ServerShutdownError(this ILogger logger, Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                LoggerExtensions.LogDebug(logger, (EventId)13, ex, "Server shutdown exception", Array.Empty<object>());
            }
        }

        public static void ApplicationError(this ILogger logger, EventId eventId, string message, Exception exception)
        {
            ReflectionTypeLoadException ex = exception as ReflectionTypeLoadException;
            if (ex != null)
            {
                Exception[] loaderExceptions = ex.LoaderExceptions;
                foreach (Exception ex2 in loaderExceptions)
                {
                    message = message + Environment.NewLine + ex2.Message;
                }
            }
            string text = message;
            LoggerExtensions.LogCritical(logger, eventId, exception, text, Array.Empty<object>());
        }

        public static void HostStarting(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                LoggerExtensions.LogDebug(logger, (EventId)3, "Host starting", Array.Empty<object>());
            }
        }

        public static void HostStarted(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                LoggerExtensions.LogDebug(logger, (EventId)4, "Host started", Array.Empty<object>());
            }
        }

        public static void HostShuttingdown(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                LoggerExtensions.LogDebug(logger, (EventId)5, "Host shutting down", Array.Empty<object>());
            }
        }

        public static void HostShutdown(this ILogger logger)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                LoggerExtensions.LogDebug(logger, (EventId)6, "Host shutdown", Array.Empty<object>());
            }
        }
    }
}
