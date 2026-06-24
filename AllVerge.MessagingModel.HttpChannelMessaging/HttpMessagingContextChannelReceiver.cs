using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace AllVerge.MessagingModel.HttpChannelMessaging
{
    using AllVerge.SystemPrimitives.Threading.Tasks;

    using AllVerge.MessagingModel.ChannelMessaging.Http;
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Http;
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using Microsoft.Extensions.Primitives;
    using System.Security.AccessControl;
    using System.Xml;
    using System.Xml.Linq;
    using AllVerge.SystemPrimitives.Reflection;
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.MessagingModel.MarkupPrimitives.Xml;
    using System.Net;
    using System.Text;
    using AllVerge.MessagingModel.MessagingFoundation;

    public class HttpChannelMessagingContextChannelReceiver :
        BaseHttpMessagingContextChannelReceiver<ChannelMessageContext>
    {
        private static ConcurrentDictionary<String, SlidingDelayTask<String>> _connections;
        private static TransportManager<BlockingCollection<ProtocolContextHost<HttpContext>>> _protocolContextQueues;
        private IProtocolContextAccessor<ProtocolContextHost<HttpContext>> protocolContextAccessor;

        private class HostingApplicationHttpContextAccessor : 
            IProtocolContextAccessor<ProtocolContextHost<HttpContext>>
        {
            static HostingApplicationHttpContextAccessor()
            {
                ProtocolMessagingContext.RegisterMapper((ProtocolContextHost<HttpContext> protocolContextHost) => 
                    protocolContextHost.ProtocolContext.Items.TryGetValue(typeof(ITransportProtocolContext), out Object transportProtocolContext) ? (ITransportProtocolContext)transportProtocolContext : null);
            }

            public HostingApplicationHttpContextAccessor()
            {
            }

            public event Action<string> OnConnectionClosed;

            public void SetProtocolContextAsync(ProtocolContextHost<HttpContext> protocolContextHost)
            {
                Console.WriteLine(protocolContextHost.ProtocolContext.Request.Path);

                var slidingDelayTask = _connections.GetOrAdd(protocolContextHost.ProtocolContext.Connection.Id, _connectionId =>
                {
                    var _slidingDelayTask = new SlidingDelayTask<String>(() => _connectionId, TimeSpan.FromMinutes(2), default(CancellationToken));

                    _slidingDelayTask.CompletionTask.ContinueWith(t =>
                    {
                        if (OnConnectionClosed != null)

                            OnConnectionClosed.Invoke(_connectionId);

                        _connections.TryRemove(_connectionId, out _);
                    });

                    return _slidingDelayTask;
                });

                if (protocolContextHost.ProtocolContext.Request.Headers.HasHeaderValue(HttpHeaderNames.Connection, HttpHeaderValues.Connection.Close))
                    slidingDelayTask.CompleteWithoutDelay();
                else
                    slidingDelayTask.ExtendDelay(TimeSpan.FromMinutes(2));

                if (_protocolContextQueues.TryLookupUri(protocolContextHost.ProtocolContext.Request.GetRequestUri(), System.ServiceModel.HostNameComparisonMode.StrongWildcard, out BlockingCollection<ProtocolContextHost<HttpContext>> c))

                    c.Add(protocolContextHost);
            }

            public Task<ProtocolContextHost<HttpContext>> GetProtocolContextAsync(Uri listenUri, CancellationToken cancellationToken)
            {
                return Task<ProtocolContextHost<HttpContext>>.Run(() => 
                {
                    if (_protocolContextQueues.TryLookupUri(
                        listenUri,
                        System.ServiceModel.HostNameComparisonMode.StrongWildcard,
                        out BlockingCollection<ProtocolContextHost<HttpContext>> protocolContextHostQueue))

                        return protocolContextHostQueue.Take(cancellationToken);

                    throw new InvalidOperationException($"There is no protocol context host queue registered for {listenUri}.");
                }
                );
            }

            public Task<String> WaitForConnectionTimeoutAsync(string connectionId)
            {
                if (_connections.TryGetValue(connectionId, out SlidingDelayTask<String> t))

                    return t.CompletionTask;

                return Task.FromResult(connectionId);
            }
        }

        public HttpChannelMessagingContextChannelReceiver(IApplicationHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider services) :
            base(hostEnvironment, hostApplicationLifetime, services)
        {
            _connections = new ConcurrentDictionary<string, SlidingDelayTask<string>>();
            _protocolContextQueues = new TransportManager<BlockingCollection<ProtocolContextHost<HttpContext>>>(HostNameComparisonMode.StrongWildcard);
        }

        protected override void RegisterListenAddresses(IEnumerable<string> serverAddresses)
        {
            serverAddresses.Aggregate(_protocolContextQueues as ITransportManager<BlockingCollection<ProtocolContextHost<HttpContext>>>, (q, a) => { q.Register(new Uri(a), new BlockingCollection<ProtocolContextHost<HttpContext>>()); return q; });
        }

        protected override void UnregisterListenAddresses(IEnumerable<string> serverAddresses)
        {
            serverAddresses.Aggregate(_protocolContextQueues as ITransportManager<BlockingCollection<ProtocolContextHost<HttpContext>>>, (q, a) => { q.Unregister(new Uri(a)); return q; });
        }

        protected override ChannelMessageContext GetNullMessageContext(IMessagingContext<ChannelMessageContext> messagingContext)
        {
            return ChannelMessageContext.Create(messagingContext.InputContext, new NullMessage());
        }

        protected override ChannelMessageContext GetRejectedMessageContext(IMessagingContext<ChannelMessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders, string faultAction = null)
        {
            return messagingContext.GetRejectedMessageContext(rejectionCode, rejectionHeaders, faultAction);
        }

        protected override async Task InvokeProtocolMessagingContextCallbackAsync(IMessagingContext<ChannelMessageContext> messagingContext)
        {
            if (messagingContext.Items.TryGetValue<ProtocolContextHost<HttpContext>>(out ProtocolContextHost<HttpContext> protocolContextHost))
            {
                if (messagingContext.OutputContext == null)

                    messagingContext.Output(GetNullMessageContext(messagingContext));

                HttpContext httpContext = protocolContextHost.ProtocolContext;

                switch (messagingContext.Result)
                {
                    case MiddlewarePipelineResult.Unreachable:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;
                    case MiddlewarePipelineResult.NotHandled:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                        break;
                    case MiddlewarePipelineResult.TooBusy:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                        messagingContext.ResultHeaders.Aggregate(httpContext.Response.Headers, (h, r) =>
                        {
                            switch (r.Key)
                            {
                                case MiddlewarePipelineResultHeaders.RetryAfter:
                                    h.Add(HttpHeaderNames.RetryAfter, r.Value);
                                    break;
                            }
                            return h;
                        });
                        break;
                    case MiddlewarePipelineResult.Timeout:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
                        break;
                    case MiddlewarePipelineResult.Canceled:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                        httpContext.Response.Headers.Add(HttpHeaderNames.Connection, HttpHeaderValues.Connection.Close);
                        break;
                    case MiddlewarePipelineResult.NotAuthorized:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        messagingContext.ResultHeaders.Aggregate(httpContext.Response.Headers, (h, r) =>
                        {
                            switch (r.Key)
                            {
                                case MiddlewarePipelineResultHeaders.Authenticate:
                                    h.Add(HttpHeaderNames.WWWAuthenticate, r.Value);
                                    break;
                            }
                            return h;
                        });
                        break;
                    case MiddlewarePipelineResult.Faulted:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        break;
                    case MiddlewarePipelineResult.Completed:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                        break;
                    case MiddlewarePipelineResult.DistributedResult:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                        break;
                    case MiddlewarePipelineResult.RedirectToResult:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.Redirect;
                        messagingContext.ResultHeaders.Aggregate(httpContext.Response.Headers, (h, r) =>
                        {
                            switch (r.Key)
                            {
                                case MiddlewarePipelineResultHeaders.Location:
                                    h.Add(HttpHeaderNames.Location, r.Value);
                                    break;
                            }
                            return h;
                        });
                        break;
                    case MiddlewarePipelineResult.RedirectForResult:
                        httpContext.Response.StatusCode = (int)HttpStatusCode.RedirectMethod;
                        messagingContext.ResultHeaders.Aggregate(httpContext.Response.Headers, (h, r) =>
                        {
                            switch (r.Key)
                            {
                                case MiddlewarePipelineResultHeaders.Location:
                                    h.Add(HttpHeaderNames.Location, r.Value);
                                    break;
                                case MiddlewarePipelineResultHeaders.RetryAfter:
                                    h.Add(HttpHeaderNames.RetryAfter, r.Value);
                                    break;
                            }
                            return h;
                        });
                        break;
                }

                StringBuilder sb = new StringBuilder();

                using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateDictionaryWriter(XmlWriter.Create(sb)))
                {
                    await httpContext.Response.WriteAsync(sb.ToString());
                }
            }
        }

        protected override IProtocolContextAccessor<ProtocolContextHost<HttpContext>> ProtocolContextAccessor
        {
            get
            { 
                if (this.protocolContextAccessor == null)
                {
                    this.protocolContextAccessor = new HostingApplicationHttpContextAccessor();
                }

                return this.protocolContextAccessor;
            }
        }
    }
}
