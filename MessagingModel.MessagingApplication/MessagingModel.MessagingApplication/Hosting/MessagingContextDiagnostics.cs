using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;

#if NETSTANDARD2_0
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http.Internal;
#elif NET6_0_OR_GREATER
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
#endif

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    class MessagingContextDiagnostics<MessageContext>
    {
        private static readonly double TimestampToTicks = 10000000.0 / (double)Stopwatch.Frequency;

        private ILogger _logger;
        private DiagnosticListener _diagnosticListener;

        public MessagingContextDiagnostics(ILogger logger, DiagnosticListener diagnosticListener)
        {
            this._logger = logger;
            this._diagnosticListener = diagnosticListener;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BeginRequest(IMessagingContext<MessageContext> messagingContext)
        {
            long num = 0L;
            if (HostingEventSource.Log.IsEnabled())
            {
                messagingContext.ApplicationContext.EventLogEnabled = true;
                RecordRequestStartEventLog(messagingContext);
            }
            bool num2 = _diagnosticListener.IsEnabled();
            bool flag = _logger.IsEnabled(LogLevel.Critical);
            StringValues value = default(StringValues);
            if (num2 | flag)
            {
                if (messagingContext.BindingContext.InteractionContext.InputHeaders != null)

                    _ = 
                        messagingContext.BindingContext.InteractionContext.InputHeaders.RawHeaders.TryGetValue("Request-Id", out value) ||
                        messagingContext.BindingContext.InteractionContext.InputHeaders.RawHeaders.TryGetValue("X-Request-ID", out value);
            }
            if (num2)
            {
                if (_diagnosticListener.IsEnabled("Microsoft.AspNetCore.Hosting.HttpRequestIn", messagingContext))
                {
                    messagingContext.ApplicationContext.Activity = StartActivity(messagingContext, value);
                }
                if (_diagnosticListener.IsEnabled("Microsoft.AspNetCore.Hosting.BeginRequest"))
                {
                    num = Stopwatch.GetTimestamp();
                    RecordBeginRequestDiagnostics(messagingContext, num);
                }
            }
            if (flag)
            {
                messagingContext.ApplicationContext.Scope = _logger.RequestScope(messagingContext, value);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    if (num == 0L)
                    {
                        num = Stopwatch.GetTimestamp();
                    }
                    LogRequestStarting(messagingContext, num);
                }
            }
            messagingContext.ApplicationContext.StartTimestamp = num;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RecordRequestStartEventLog(IMessagingContext<MessageContext> messagingContext)
        {
            HostingEventSource.Log.RequestStart(messagingContext.BindingContext.InteractionContext.InputVerb, messagingContext.BindingContext.InteractionContext.InputLocation);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Activity StartActivity(IMessagingContext<MessageContext> messagingContext, StringValues requestId)
        {
            Activity activity = new Activity("Microsoft.AspNetCore.Hosting.HttpRequestIn");
            if (!StringValues.IsNullOrEmpty(requestId))
            {
                activity.SetParentId(requestId);

                if (messagingContext.BindingContext.InteractionContext.InputHeaders.RawHeaders.TryGetValue("Correlation-Context", out StringValues correlationContextValues))
                {
                    correlationContextValues = new StringValues(GetCommaSeparatedValues(correlationContextValues).ToArray());

                    string[] array = correlationContextValues;
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (NameValueHeaderValue.TryParse(array[i], out NameValueHeaderValue val))
                        {
                            Activity activity2 = activity;
                            StringSegment val2 = val.Name;
                            string key = ((object)val2).ToString();
                            val2 = val.Value;
                            activity2.AddBaggage(key, ((object)val2).ToString());
                        }
                    }
                }
            }
            if (_diagnosticListener.IsEnabled("Microsoft.AspNetCore.Hosting.HttpRequestIn.Start"))
            {
                _diagnosticListener.StartActivity(activity, new
                {
                    Context = messagingContext
                });
            }
            else
            {
                activity.Start();
            }
            return activity;
        }

        private static IEnumerable<String> GetCommaSeparatedValues(StringValues values)
        {
            foreach (HeaderSegment item in new HeaderSegmentCollection(values))
            {
                if (!StringSegment.IsNullOrEmpty(item.Data))
                {
                    string text = item.Data.Value?.Trim('"');
                    if (!string.IsNullOrEmpty(text))
                    {
                        yield return text;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RecordBeginRequestDiagnostics(IMessagingContext<MessageContext> messagingContext, long startTimestamp)
        {
            _diagnosticListener.Write("Microsoft.AspNetCore.Hosting.BeginRequest", new
            {
                Context = messagingContext,
                timestamp = startTimestamp
            });
        }

        (IMessagingContext<MessageContext> Context, long Timestamp) RequestStartingLog((IMessagingContext<MessageContext> Context, long Timestamp) args) { return (args.Context, args.Timestamp); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRequestStarting(IMessagingContext<MessageContext> messagingContext, long startTimestamp)
        {
            _logger.Log<(IMessagingContext<MessageContext> Context, long Timestamp)>(LogLevel.Information, (EventId)1, RequestStartingLog((messagingContext, startTimestamp)), (Exception)null, (s, x) => $"Context:{s.Context};StartTime:{s.Timestamp}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EndRequest(IMessagingContext<MessageContext> messagingContext, Exception exception)
        {
            long startTimestamp = messagingContext.ApplicationContext.StartTimestamp;
            long num = 0L;
            if (startTimestamp != 0L)
            {
                num = Stopwatch.GetTimestamp();
                LogRequestFinished(messagingContext, startTimestamp, num);
            }
            if (_diagnosticListener.IsEnabled())
            {
                if (num == 0L)
                {
                    num = Stopwatch.GetTimestamp();
                }
                if (exception == null)
                {
                    if (_diagnosticListener.IsEnabled("Microsoft.AspNetCore.Hosting.EndRequest"))
                    {
                        RecordEndRequestDiagnostics(messagingContext, num);
                    }
                }
                else if (_diagnosticListener.IsEnabled("Microsoft.AspNetCore.Hosting.UnhandledException"))
                {
                    RecordUnhandledExceptionDiagnostics(messagingContext, num, exception);
                }
                Activity activity = messagingContext.ApplicationContext.Activity;
                if (activity != null)
                {
                    StopActivity(messagingContext, activity);
                }
            }
            if (messagingContext.ApplicationContext.EventLogEnabled && exception != null)
            {
                HostingEventSource.Log.UnhandledException();
            }

            messagingContext.ApplicationContext.Scope?.Dispose();

            ContextDisposed(messagingContext.ApplicationContext);
        }


        (IMessagingContext<MessageContext> Context, TimeSpan Timespan) RequstFinishedLog((IMessagingContext<MessageContext> Context, TimeSpan Timespan)args) { return (args.Context, args.Timespan); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LogRequestFinished(IMessagingContext<MessageContext> messagingContext, long startTimestamp, long currentTimestamp)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                TimeSpan elapsed = new TimeSpan((long)(TimestampToTicks * (double)(currentTimestamp - startTimestamp)));

                _logger.Log<(IMessagingContext<MessageContext> Context, TimeSpan Timespan)>(LogLevel.Information, (EventId)2, RequstFinishedLog((messagingContext, elapsed)), (Exception)null, (s, x) => $"Context:{s.Context};Timespan:{s.Timespan}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RecordEndRequestDiagnostics(IMessagingContext<MessageContext> messagingContext, long currentTimestamp)
        {
            _diagnosticListener.Write("Microsoft.AspNetCore.Hosting.EndRequest", new
            {
                Context = messagingContext,
                Timestamp = currentTimestamp
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RecordUnhandledExceptionDiagnostics(IMessagingContext<MessageContext> messagingContext, long currentTimestamp, Exception exception)
        {
            _diagnosticListener.Write("Microsoft.AspNetCore.Hosting.UnhandledException", new
            {
                Context = messagingContext,
                Timestamp = currentTimestamp,
                Exception = exception
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void StopActivity(IMessagingContext<MessageContext> messagingContext, Activity activity)
        {
            _diagnosticListener.StopActivity(activity, new
            {
                Context = messagingContext
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ContextDisposed(IApplicationMessagingContext context)
        {
            if (context.EventLogEnabled)
            {
                HostingEventSource.Log.RequestStop();
            }
        }
    }
}
