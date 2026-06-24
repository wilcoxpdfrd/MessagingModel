using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Claims;
using System.Security.Principal;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using AllVerge.MessagingModel.MessagingFoundation.Http;
using AllVerge.MessagingModel.MessagingFoundation.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.HttpChannelMessaging
{
    internal class HttpMessagingContextAccessor : IHttpMessagingContextAccessor
    {
        private class SetResponseStatusAndHeadersAction : IAction
        {
            private IHttpWebSocketFeature httpWebSocketFeature;
            private IHttpResponseFeature httpResponseFeature;
            private HttpResponseMessage httpResponseMessage;
            private Action<IHttpWebSocketFeature, IHttpResponseFeature, HttpResponseMessage> action;

            public SetResponseStatusAndHeadersAction(IHttpWebSocketFeature httpWebSocketFeature, IHttpResponseFeature httpResponseFeature, HttpResponseMessage httpResponseMessage, Action<IHttpWebSocketFeature, IHttpResponseFeature, HttpResponseMessage> action)
            {
                this.httpWebSocketFeature = httpWebSocketFeature;
                this.httpResponseFeature = httpResponseFeature;
                this.httpResponseMessage = httpResponseMessage;
                this.action = action;
            }

            //public SetResponseStatusAndHeadersAction(HttpContext context, HttpResponseMessage httpResponseMessage, Action<HttpContext, HttpResponseMessage> action)
            //{
            //    this.context = context;
            //    this.httpResponseMessage = httpResponseMessage;
            //    this.action = action;
            //}

            public void Invoke()
            {
                this.action(this.httpWebSocketFeature, this.httpResponseFeature, this.httpResponseMessage);
            }
        }

        private class HttpMessagingContext : IHttpMessagingContext
        {
            private HttpRequestMessage httpRequestMessage;
            private bool isWebSocketRequest;
            private string webSocketVersion;
            private IWebSocketManager webSocketManager;
            private IDictionary<object, object> items;
            private IPropertyAccessor<IServiceProvider> requestServicesAccessor;
            private IPropertyAccessor<String> traceIdentifierAccessor;
            private IPEndPoint localEndpoint;
            private IPEndPoint remoteEndpoint;
            private HttpResponseMessageProperty httpResponseMessageProperty;
            private TaskCompletionSource<VoidTaskResult> completeMessagingSource;

            public HttpMessagingContext(HttpRequestMessage httpRequestMessage)
            {
                this.httpRequestMessage = httpRequestMessage;

                this.httpRequestMessage.Properties.Add(typeof(ITransportProtocolContext).Name, this);

                if (this.httpRequestMessage.Properties.TryGetProperty<HttpMessagingContextProperty>(HttpMessagingContextProperty.Name, out HttpMessagingContextProperty httpMessagingContextProperty))
                {
                    this.items = httpMessagingContextProperty.Items;

                    this.requestServicesAccessor = httpMessagingContextProperty.RequestServicesAccessor;

                    this.traceIdentifierAccessor = httpMessagingContextProperty.TraceIdentifierAccessor;
                }
                else
                {
                    this.items = new Dictionary<object, object>();

                    this.requestServicesAccessor = new PropertyAccessor<IServiceProvider>();

                    this.traceIdentifierAccessor = new PropertyAccessor<String>();
                }

                if (this.httpRequestMessage.Properties.TryGetProperty<HttpConnectionInfoMessageProperty>(HttpConnectionInfoMessageProperty.Name, out HttpConnectionInfoMessageProperty httpConnectionInfoMessageProperty))
                {
                    this.ConnectionId = httpConnectionInfoMessageProperty.Id;
                }
                else
                {
                    this.ConnectionId = this.TraceIdentifier.Split(':').FirstOrDefault();
                }

                this.CloseConnection = this.httpRequestMessage.Headers.Connection.Contains(HttpHeaderValues.Connection.Close);

                if (this.httpRequestMessage.Properties.TryGetProperty<LocalEndpointMessageProperty>(LocalEndpointMessageProperty.Name, out LocalEndpointMessageProperty localEndpointMessageproperty))
                {
                    this.localEndpoint = localEndpointMessageproperty.ToEndPoint();
                }

                if (this.httpRequestMessage.Properties.TryGetProperty<RemoteEndpointMessageProperty>(RemoteEndpointMessageProperty.Name, out RemoteEndpointMessageProperty remoteEndpointMessageproperty))
                {
                    this.remoteEndpoint = remoteEndpointMessageproperty.ToEndPoint();
                }

                if (this.httpRequestMessage.Properties.TryGetProperty<HttpWebSocketMessageProperty>(HttpWebSocketMessageProperty.Name, out HttpWebSocketMessageProperty httpWebSocketMessageProperty))
                {
                    this.isWebSocketRequest = httpWebSocketMessageProperty.IsWebSocketRequest;
                    this.webSocketVersion = httpWebSocketMessageProperty.SecWebSocketVersion;
                    this.webSocketManager = httpWebSocketMessageProperty.WebSocketManager;
                }
                else if (this.Request.Headers.Contains(HttpHeaderNames.SecWebSocketVersion))
                {
                    throw new NotImplementedException("TBD: Need to provide implementation of IWebSocketManager (see origonal WebSocketHelper.cs lines ~ 107-151)");
                }

                this.completeMessagingSource = new TaskCompletionSource<VoidTaskResult>();

                if (httpRequestMessage.Properties.TryGetProperty<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name, out HttpResponseMessageProperty httpResponseMessageProperty))
                {
                    this.httpResponseMessageProperty = httpResponseMessageProperty;

                    if (this.httpResponseMessageProperty == null)

                        System.Diagnostics.Debugger.Break();

                    httpResponseMessageProperty.SetResponseCompletedMessagingContextCallback(s =>
                    {
                        this.completeMessagingSource.SetResult(new VoidTaskResult());
                    },
                    null);
                }
            }

            public IDictionary<object, object> Items => this.items;

            public string ConnectionId { get; private set; }

            public bool CloseConnection { get; private set; }

            public string TraceIdentifier
            {
                get
                {
                    return this.traceIdentifierAccessor.Property;
                }
            }

            public IPEndPoint LocalEndPoint => this.localEndpoint;

            public IPEndPoint RemoteEndPoint => this.remoteEndpoint;

            public IPrincipal User
            {
                //set
                //{
                //    this.httpRequestMessage.SetUserPrincipal(value as ClaimsPrincipal);
                //}
                get
                {
                    return this.httpRequestMessage.GetUserPrincipal() as ClaimsPrincipal;
                }
            }

            public HttpRequestMessage Request => this.httpRequestMessage;

            public HttpResponseMessage Response => this.httpResponseMessageProperty.HttpResponseMessage;

            public bool IsWebSocketRequest => this.isWebSocketRequest;

            public string WebSocketVersion => this.webSocketVersion;

            public IWebSocketManager WebSocketManager => this.webSocketManager;

            public bool IsResponseSent => this.completeMessagingSource.Task.IsCompleted;

            Task IHttpMessagingContext.CompleteResponseAsync(bool aborting)
            {
                if (this.IsResponseSent)
                    return Task.CompletedTask;
                return this.httpResponseMessageProperty.CompleteResponseAsync(aborting);
            }

            Task IHttpMessagingContext.ResponseCompletedAsync()
            {
                if (this.IsResponseSent)
                    return Task.CompletedTask;
                return this.httpResponseMessageProperty.ResponseCompletedAsync();
            }
        }

        IProtocolContextAccessor<ProtocolContextHost<HttpContext>> protocolContextAccessor;
        WebSocketMiddleware webSocketMiddleware;
        private ILogger logger;
        private static List<String> traceIds;

        public HttpMessagingContextAccessor(IServiceProvider services)
        {
            ILoggerFactory loggerFactory = services.GetRequiredService<ILoggerFactory>();
            this.protocolContextAccessor = services.GetRequiredService<IProtocolContextAccessor<ProtocolContextHost<HttpContext>>>();
            this.webSocketMiddleware = new WebSocketMiddleware(c => Task.CompletedTask, Options.Create<WebSocketOptions>(new WebSocketOptions()), loggerFactory);
            this.logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(this.GetType());
        }

        public async Task<IHttpMessagingContext> GetHttpMessagingContextAsync(Uri listenUri, CancellationToken cancellationToken)
        {
            ProtocolContextHost<HttpContext> protocolContextHost = await this.protocolContextAccessor.GetProtocolContextAsync(listenUri, cancellationToken);

            IHttpMessagingContext httpMessagingContext = await CreateHttpMessagingContextAsync(this.logger, this.webSocketMiddleware, protocolContextHost.StartTimestamp, protocolContextHost.ProtocolContext);

            return httpMessagingContext;
        }

        public Task<String> WaitForConnectionTimeoutAsync(string connectionId)
        {
            return this.protocolContextAccessor.WaitForConnectionTimeoutAsync(connectionId);
        }
        
        private static async Task<IHttpMessagingContext> CreateHttpMessagingContextAsync(ILogger logger, WebSocketMiddleware webSocketMiddleware, long startTimestamp, HttpContext context)
        {
            IFeatureCollection features = context.Features;
            IItemsFeature itemsFeature = features.Get<IItemsFeature>();
            IHttpRequestFeature httpRequest = features.Get<IHttpRequestFeature>();
            // HttpRequest httpRequest = context.Request;
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(httpRequest.GetMethod(), httpRequest.GetRequestUri());

            httpRequestMessage.Headers.TrySetStandardHeaders(httpRequest.Headers);

            IFormFeature formFeature = features.Get<IFormFeature>();

            bool hasFormContentType = formFeature != null && formFeature.HasFormContentType;
            //bool hasFormContentType = new FormFeature(httpRequest).HasFormContentType;

            if (hasFormContentType)
            {
                if (formFeature.Form.Files != null)
                    httpRequestMessage.Content =
                        formFeature.Form.Files.Aggregate(
                            new MultipartFormDataContent(), (c, f) => { c.Add(GetStreamContent(f), f.Name, f.FileName); return c; });

                else

                    httpRequestMessage.Content =
                        new HttpFormUrlEncodedContent(
                            formFeature.Form.Select(kv => new KeyValuePair<string, StringValues>(kv.Key, kv.Value)));

                // if (httpRequest.Form.Files != null)
                //    httpRequestMessage.Content =
                //        httpRequest.Form.Files.Aggregate(
                //            new MultipartFormDataContent(), (c, f) => { c.Add(GetStreamContent(f), f.Name, f.FileName); return c; });

                //else

                //    httpRequestMessage.Content =
                //        new HttpFormUrlEncodedContent(
                //            httpRequest.Form.Select(kv => new KeyValuePair<string, StringValues>(kv.Key, kv.Value)));
            }
            else

                httpRequestMessage.Content = await GetStreamContentAsync(httpRequest.Body);

            httpRequestMessage.Content.Headers.TrySetContentHeaders(httpRequest);

            IHttpResponseFeature httpResponseFeature = features.Get<IHttpResponseFeature>();

            HttpResponseMessage httpResponseMessage;

            IHttpWebSocketFeature httpWebSocketFeature = features.Get<IHttpWebSocketFeature>();

            if (context.Features.Get<IHttpUpgradeFeature>().IsUpgradableRequest)
            {
                await webSocketMiddleware.Invoke(context);

                WebSocketManager webSockets = context.WebSockets;

                IHeaderDictionary headers = httpRequest.Headers;
                //IHeaderDictionary headers = context.Request.Headers;

                if (headers.ContainsKey(HttpHeaderNames.Upgrade))

                    httpRequestMessage.Headers.TrySetUpgradeHeaders(headers);

                httpRequestMessage.Properties.Add(
                        HttpWebSocketMessageProperty.Name,
                        new HttpWebSocketMessageProperty(
                            headers[HttpHeaderNames.SecWebSocketVersion],
                            new WebSocketManagerWrapper(
                                webSockets.IsWebSocketRequest,
                                webSockets.WebSocketRequestedProtocols,
                                webSockets.AcceptWebSocketAsync,
                                webSockets.AcceptWebSocketAsync, 
                                (s) => { })));
                
                httpResponseMessage =
                    httpResponseFeature.Body == null ?
                    new HttpResponseMessage()
                    {
                        RequestMessage = httpRequestMessage
                    } :
                    new HttpResponseMessage()
                    {
                        RequestMessage = httpRequestMessage,
                        Content = new StreamContent(httpResponseFeature.Body)
                    };

                httpRequestMessage.Properties.Add(
                    HttpMessagingContextProperty.Name,
                    new HttpMessagingContextProperty(
                        startTimestamp,
                        context,
                        new PropertyAccessor<IItemsFeature, IDictionary<object, object>>(itemsFeature, f => f.Items),
                        new PropertyAccessor<HttpContext, IServiceProvider>(context, c => c.RequestServices),
                        new SetResponseStatusAndHeadersAction(httpWebSocketFeature, httpResponseFeature, httpResponseMessage, (w, c, r) =>
                        {
                            if (w != null && !w.IsWebSocketRequest)
                                logger.LogWarning("Unexpected attempt to set http status for a non-web socket request.");
                        }),
                        new PropertyAccessor<IHttpResponseFeature, Stream>(httpResponseFeature, f => f.Body),
                        new PropertyAccessor<HttpContext, String>(context, c => c.TraceIdentifier)
                    )
                );
            }
            else
            {
                httpResponseMessage =
                    httpResponseFeature.Body == null ?
                    new HttpResponseMessage()
                    {
                        RequestMessage = httpRequestMessage
                    } :
                    new HttpResponseMessage()
                    {
                        RequestMessage = httpRequestMessage,
                        Content = new StreamContent(httpResponseFeature.Body)
                    };

                IHttpRequestIdentifierFeature httpRequestIdentifierFeature = features.Get<IHttpRequestIdentifierFeature>();

                httpRequestMessage.Properties.Add(
                    HttpMessagingContextProperty.Name,
                    new HttpMessagingContextProperty(
                        startTimestamp,
                        context,
                        new PropertyAccessor<IItemsFeature, IDictionary<object, object>>(itemsFeature, f => f.Items),
                        new PropertyAccessor<HttpContext, IServiceProvider>(context, c => c.RequestServices),
                        new SetResponseStatusAndHeadersAction(httpWebSocketFeature, httpResponseFeature, httpResponseMessage, (w, f, r) =>
                        {
                            if (w != null && w.IsWebSocketRequest)
                            {
                                logger.LogWarning("Unexpected attempt to set http status for a web socket request.");
                            }
                            else if (!f.HasStarted)
                            {
                                if (httpResponseMessage.RequestMessage.Properties.TryGetProperty<HttpMessagingContext>(nameof(HttpMessagingContext), out HttpMessagingContext _httpMessagingContext))
                                {
                                    string traceIdentifier = _httpMessagingContext.TraceIdentifier;
                                    if (HttpMessagingContextAccessor.traceIds.Contains(traceIdentifier))
                                        System.Diagnostics.Debugger.Break();
                                    HttpMessagingContextAccessor.traceIds.Add(traceIdentifier);
                                }
                                f.StatusCode = (int)r.StatusCode;
                                if (httpResponseMessage.Content == null)
                                    f.Headers.TrySetHeaders(r.Headers);
                                else
                                    f.Headers.TrySetHeaders(r.Headers.Union(r.Content.Headers));
                            }
                        }),
                        new PropertyAccessor<IHttpResponseFeature, Stream>(httpResponseFeature, f => f.Body),
                        new PropertyAccessor<IHttpRequestIdentifierFeature, String>(httpRequestIdentifierFeature, f => f.TraceIdentifier)
                    )
                );

                //httpRequestMessage.Properties.Add(
                //    HttpMessagingContextProperty.Name,
                //    new HttpMessagingContextProperty(
                //        startTimestamp,
                //        context,
                //        context.Items,
                //        new PropertyAccessor<HttpContext, IServiceProvider>(context, c => c.RequestServices),
                //        new SetResponseStatusAndHeadersAction(context, httpResponseMessage, (c, r) =>
                //        {
                //            if (c.WebSockets.IsWebSocketRequest)
                //            {
                //                logger.LogWarning("Unexpected attempt to set http status for a web socket request.");
                //            }
                //            else if (!c.Response.HasStarted)
                //            {
                //                c.Response.StatusCode = (int)r.StatusCode;
                //                if (httpResponseMessage.Content == null)
                //                    c.Response.Headers.TrySetHeaders(r.Headers);
                //                else
                //                    c.Response.Headers.TrySetHeaders(r.Headers.Union(r.Content.Headers));
                //            }
                //        }),
                //        httpResponseFeature.Body,
                //        new PropertyAccessor<HttpContext, String>(context, c => c.TraceIdentifier)
                //    )
                // );
            }

            IHttpConnectionFeature httpConnectionFeature = features.Get<IHttpConnectionFeature>();

            ITlsConnectionFeature tlsConnectionFeature = features.Get<ITlsConnectionFeature>();

            // ConnectionInfo connectionInfo = httpRequest.HttpContext.Connection;

            HttpConnectionInfo httpConnectionInfo =
                new HttpConnectionInfo(
                    httpConnectionFeature.ConnectionId,
                    () => tlsConnectionFeature?.ClientCertificate,
                    (cancellationToken) => tlsConnectionFeature?.GetClientCertificateAsync(cancellationToken));

            httpRequestMessage.Properties.Add(
                HttpConnectionInfoMessageProperty.Name,
                new HttpConnectionInfoMessageProperty(
                    httpConnectionInfo)
            );

            httpRequestMessage.Properties.Add(
                LocalEndpointMessageProperty.Name,
                new LocalEndpointMessageProperty(
                    new IPEndPoint(
                        httpConnectionFeature.LocalIpAddress,
                        httpConnectionFeature.LocalPort)
                    )
                );

            httpRequestMessage.Properties.Add(
                RemoteEndpointMessageProperty.Name,
                new RemoteEndpointMessageProperty(
                    new IPEndPoint(
                        httpConnectionFeature.RemoteIpAddress,
                        httpConnectionFeature.RemotePort)
                    )
            );

            if (tlsConnectionFeature.TryGetChannelBinding(out ChannelBinding channelBinding))

                httpRequestMessage.Properties.Add(
                    ChannelBindingMessageProperty.Name,
                    new ChannelBindingMessageProperty(
                        channelBinding,
                        false));

            //if (httpRequest.TryGetChannelBinding(out ChannelBinding channelBinding))

            //    httpRequestMessage.Properties.Add(
            //        ChannelBindingMessageProperty.Name,
            //        new ChannelBindingMessageProperty(
            //            channelBinding,
            //            false));

            HttpResponseMessageProperty httpResponseMessageProperty =
                new HttpResponseMessageProperty(httpResponseMessage);

            httpRequestMessage.Properties.Add(
                HttpResponseMessageProperty.Name,
                httpResponseMessageProperty);

            HttpMessagingContext httpMessagingContext = new HttpMessagingContext(httpRequestMessage);

            lock (itemsFeature.Items)
            {
                itemsFeature.Items.Add(typeof(ITransportProtocolContext), httpMessagingContext);
            }

            return httpMessagingContext;
        }

        private static StreamContent GetStreamContent(IFormFile f)
        {
            MemoryStream ms = new MemoryStream();

            f.CopyTo(ms);

            StreamContent streamContent = new StreamContent(ms);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(f.ContentType);
            streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue(f.ContentDisposition);
            streamContent.Headers.ContentLength = f.Length;

            f.Headers.Aggregate(streamContent.Headers, (c, h) =>
            {
                switch (h.Key)
                {
                    case HttpHeaderNames.ContentType:
                    case HttpHeaderNames.ContentLength:
                    case HttpHeaderNames.ContentDisposition:
                        break;
                    default:
                        c.Add(h.Key, h.Value.ToArray());
                        break;
                }

                return c;
            });

            return streamContent;
        }

        private static async Task<StreamContent> GetStreamContentAsync(Stream stream)
        {
            MemoryStream ms = new MemoryStream();

            await stream.CopyToAsync(ms);

            ms.Seek(0, SeekOrigin.Begin);

            return new StreamContent(ms);
        }
    }

    internal static class HttpExtensions
    {
        public static HttpMethod GetMethod(this IHttpRequestFeature httpRequest)
        {
            return new HttpMethod(httpRequest.Method);
        }

        public static Uri GetRequestUri(this IHttpRequestFeature httpRequest)
        {
            UriBuilder b = new UriBuilder();

            b.Scheme = httpRequest.Scheme;

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.Host, out StringValues host))
            {
                HostString hostString = HostString.FromUriComponent((string)host);
                b.Host = hostString.Host;
                if (hostString.Port.HasValue)
                    b.Port = hostString.Port.Value;
                else if (string.Equals("https", b.Scheme, StringComparison.OrdinalIgnoreCase))
                    b.Port = 443;
                else if (string.Equals("http", b.Scheme, StringComparison.OrdinalIgnoreCase))
                    b.Port = 80;
            }
            b.Path = httpRequest.Path;
            if (!String.IsNullOrEmpty(httpRequest.QueryString))
                b.Query = httpRequest.QueryString.Substring(1);
            return b.Uri;
        }

        public static void TrySetContentHeaders(this HttpContentHeaders target, IHttpRequestFeature httpRequest)
        {
            MediaTypeHeaderValue contentTypeHeaderValue;

            StringValues values;

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentType, out values) && MediaTypeHeaderValue.TryParse(values, out contentTypeHeaderValue))

                target.ContentType = contentTypeHeaderValue;

            target.ContentLength = httpRequest.Headers.ContentLength;

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.Allow, out values))

                values.Aggregate(target.Allow, (c, v) => { c.Add(v); return c; });

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentDisposition, out values))

                target.ContentDisposition = new ContentDispositionHeaderValue(values.First());

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentEncoding, out values))

                values.Aggregate(target.ContentEncoding, (c, v) => { c.Add(v); return c; });

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentLanguage, out values))

                values.Aggregate(target.ContentLanguage, (c, v) => { c.Add(v); return c; });

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentLocation, out values))

                target.ContentLocation = new Uri(values.First());

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentMD5, out values))

                target.ContentMD5 = System.Net.Http.WebHeaders.HeaderEncoding.BinaryString2ByteArray(values.ToString());

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentRange, out values))

                target.ContentRange = new ContentRangeHeaderValue(long.Parse(values.First()));

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.Expires, out values))

                target.Expires = DateTimeOffset.Parse(values.First());

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.LastModified, out values))

                target.LastModified = DateTimeOffset.Parse(values.First());
        }

        public static bool TryGetChannelBinding(this ITlsConnectionFeature tlsConnectionFeature, out ChannelBinding channelBinding)
        {
            if (tlsConnectionFeature?.ClientCertificate != null)
            {
                byte[] bytes = tlsConnectionFeature.ClientCertificate.RawData;

                IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);

                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);

                // Call unmanaged code
                //Marshal.FreeHGlobal(unmanagedPointer);

                channelBinding = new System.Web.HttpChannelBindingToken(unmanagedPointer, bytes.Length);

                return true;
            }

            channelBinding = null;

            return false;
        }

        public static HttpMethod GetMethod(this HttpRequest httpRequest)
        {
            return new HttpMethod(httpRequest.Method);
        }

        public static bool TryGetChannelBinding(this HttpRequest httpRequest, out ChannelBinding channelBinding)
        {
            if (httpRequest.HttpContext.Connection.ClientCertificate != null)
            {
                byte[] bytes = httpRequest.HttpContext.Connection.ClientCertificate.RawData;

                IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);

                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);

                // Call unmanaged code
                //Marshal.FreeHGlobal(unmanagedPointer);

                channelBinding = new System.Web.HttpChannelBindingToken(unmanagedPointer, bytes.Length);

                return true;
            }

            channelBinding = null;

            return false;
        }

        public static Uri GetRequestUri(this HttpRequest httpRequest)
        {
            UriBuilder b = new UriBuilder();

            b.Scheme = httpRequest.Scheme;
            b.Host = httpRequest.Host.Host;
            if (httpRequest.Host.Port.HasValue)
                b.Port = httpRequest.Host.Port.Value;
            else if (httpRequest.IsHttps)
                b.Port = 443;
            else
                b.Port = 80;
            b.Path = httpRequest.Path;
            if (httpRequest.QueryString.HasValue)
                b.Query = httpRequest.QueryString.Value.Substring(1);
            return b.Uri;
        }

        public static void TrySetStandardHeaders(this HttpRequestHeaders targetHeaders, IHeaderDictionary sourceHeaders)
        {
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Accept, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.AcceptCharset, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.AcceptEncoding, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.AcceptLanguage, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Authorization, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.CacheControl, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Connection, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Date, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Expect, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.From, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Host, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.IfMatch, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.IfModifiedSince, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.IfNoneMatch, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.IfRange, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.IfUnmodifiedSince, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.MaxForwards, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Pragma, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.ProxyAuthorization, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Range, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Referer, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.TE, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Trailer, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.TransferEncoding, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Upgrade, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.UserAgent, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Via, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Warning, sourceHeaders);
        }

        public static void TrySetUpgradeHeaders(this HttpRequestHeaders targetHeaders, IHeaderDictionary sourceHeaders)
        {
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.MSBinaryTransferMode, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.Origin, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.SecWebSocketKey, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.SecWebSocketProtocol, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.SecWebSocketVersion, sourceHeaders);
            targetHeaders.TrySetHeaderValues(HttpHeaderNames.SoapContentType, sourceHeaders);
        }

        private static void TrySetHeaderValues(this HttpRequestHeaders targetHeaders, String headerKey, IHeaderDictionary sourceHeaders)
        {
            StringValues values;

            if (sourceHeaders.TryGetValue(headerKey, out values))
            {
                if (values.Count > 1)

                    targetHeaders.Add(headerKey, (IEnumerable<String>)values);

                else if (values.Count > 0)

                    targetHeaders.Add(headerKey, values[0]);
            }
        }

        public static void TrySetContentHeaders(this HttpContentHeaders target, HttpRequest httpRequest)
        {
            MediaTypeHeaderValue contentTypeHeaderValue;

            if (MediaTypeHeaderValue.TryParse(httpRequest.ContentType, out contentTypeHeaderValue))

                target.ContentType = contentTypeHeaderValue;

            target.ContentLength = httpRequest.ContentLength;

            StringValues values;

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.Allow, out values))

                values.Aggregate(target.Allow, (c, v) => { c.Add(v); return c; });

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentDisposition, out values))

                target.ContentDisposition = new ContentDispositionHeaderValue(values.First());

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentEncoding, out values))

                values.Aggregate(target.ContentEncoding, (c, v) => { c.Add(v); return c; });

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentLanguage, out values))

                values.Aggregate(target.ContentLanguage, (c, v) => { c.Add(v); return c; });

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentLocation, out values))

                target.ContentLocation = new Uri(values.First());

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentMD5, out values))

                target.ContentMD5 = System.Net.Http.WebHeaders.HeaderEncoding.BinaryString2ByteArray(values.ToString());

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.ContentRange, out values))

                target.ContentRange = new ContentRangeHeaderValue(long.Parse(values.First()));

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.Expires, out values))

                target.Expires = DateTimeOffset.Parse(values.First());

            if (httpRequest.Headers.TryGetValue(HttpHeaderNames.LastModified, out values))

                target.LastModified = DateTimeOffset.Parse(values.First());
        }

        public static void TrySetHeaders(this IHeaderDictionary target, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                try
                {
                    target.Add(header.Key, new StringValues(header.Value.ToArray()));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public static bool HasHeaderValue(this IHeaderDictionary target, string headerName, string value)
        {
            if (target.TryGetValue(headerName, out StringValues values))
            {
                return values.Contains(value);
            }
            return false;
        }
    }
}