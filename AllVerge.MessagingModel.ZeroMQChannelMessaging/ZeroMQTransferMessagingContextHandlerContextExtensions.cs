using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;

using ServiceModel.MessagingApplication;

using AllVerge.Core.ServiceModel.Http;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;
using AllVerge.Core.Resource;
using AllVerge.Core.ServiceModel.Channels;
using System.Xml;
using System.Security.Principal;
using ServiceModel.ChannelMessaging;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    internal static class ZeroMQTransferMessagingContextHandlerContextExtensions
    {
        //public static String GetReceivedContentType(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        //{
        //    ZeroMQMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    return receivedContext.Request.ContentType;
        //}

        //public static long? GetReceivedContentLength(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        //{
        //    ZeroMQMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    return receivedContext.Headers.ContentLength;
        //}

        //public static void CopyQueryParameters(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, NameValueCollection queryParameters)
        //{
        //    ZeroMQMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    IReadOnlyDictionary<String, StringValues> receivedQueryParameters = HttpKeyValuesCollection.CopyKeysAndValuesSetsFrom(receivedContext.Request.Query);

        //    receivedQueryParameters.Keys.OfType<String>().Aggregate(queryParameters, (map, k) => { map.Add(k, receivedQueryParameters[k]); return map; });
        //}

        //public static bool HasFormParameters(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        //{
        //    ZeroMQMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    return receivedContext.Request.HasFormContentType;
        //}

        //public static void CopyFormParameters(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, NameValueCollection formParameters)
        //{
        //    ZeroMQMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    if (receivedContext.Request.HasFormContentType)
        //    {
        //        IReadOnlyDictionary<String, StringValues> receivedFormParameters = HttpKeyValuesCollection.CopyKeysAndValuesSetsFrom(receivedContext.Request.Form);

        //        receivedFormParameters.Keys.OfType<String>().Aggregate(formParameters, (map, k) => { map.Add(k, receivedFormParameters[k]); return map; });
        //    }
        //}

        public static bool HasBody(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            ZeroMQTransferMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.ReceivedContext;

            return receivedContext.Request.ContentLength > 0 && !receivedContext.Request.ContentType.IsFormMessageFormat;
        }

        public static Stream GetBody(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            ZeroMQTransferMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.ReceivedContext;

            return receivedContext.Request.GetZeroMQTransferMessagingInput(true).GetInputStream(true);
        }

        //public static void CopyReceivedHeaders(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, WebHeaderCollection headers)
        //{
        //    ZeroMQMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    IHeaderDictionary receivedHeaders = receivedContext.Request.Headers;

        //    receivedHeaders.Keys.OfType<String>().Aggregate(headers, (h, k) =>
        //    {
        //        h.Add(k, receivedHeaders[k]);

        //        return h;
        //    });
        //}

        //public static bool TryGetReceivedHeader(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string name, out string[] values)
        //{
        //    ZeroMQMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    IHeaderDictionary receivedHeaders = receivedContext.Request.Headers;

        //    values = receivedHeaders[name];

        //    return values.Length > 0;
        //}

        //public static void CopyReceivedCookies(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, CookieCollection cookies)
        //{
        //    ZeroMQMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    receivedContext.Request.Cookies.Aggregate(cookies, (d, c) =>
        //    {
        //        d.Add(new Cookie(c.Key, c.Value));

        //        return d;
        //    });
        //}

        //public static IDictionary<String, String> ReceivedCookies(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        //{
        //    ZeroMQMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    if (receivedContext?.Request != null)

        //        return receivedContext.Request.Cookies.Aggregate(new Dictionary<String, String>(), (d, c) =>
        //        {
        //            d.Add(c.Key, c.Value);

        //            return d;
        //        });

        //    return new Dictionary<String, String>();
        //}

        //public static Uri ReceivedRequestBaseUri(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        //{
        //    ZeroMQTransferMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    Uri requestUri = receivedContext?.Request.RequestUri;

        //    if (requestUri != null && requestUri.IsAbsoluteUri)
        //    {
        //        String serverAddress = ZeroMQProtocolSchemesHelper.DenormalizeServerAddress(requestUri.AbsoluteUri, true);

        //        if (serverAddress.StartsWith(ResourceProtocolSchemes.ZEROMQ_IPC_DELIMITED))

        //            return new Uri(new Uri(serverAddress).GetLeftPart(UriPartial.Authority));

        //        return new Uri(serverAddress);
        //    }

        //    return null;
        //}

        //public static String ReceivedRequestUriPath(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        //{
        //    ZeroMQTransferMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.Received;

        //    Uri requestUri = receivedContext?.Request.RequestUri;

        //    if (requestUri != null && requestUri.IsAbsoluteUri)
        //    {
        //        String serverAddress = ZeroMQProtocolSchemesHelper.DenormalizeServerAddress(requestUri.AbsoluteUri, true);

        //        return new Uri(serverAddress).AbsolutePath;
        //    }

        //    return null;
        //}

        public static Uri ReceivedRequestUri(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            ZeroMQTransferMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.ReceivedContext;

            return receivedContext?.Request.RequestUri;
        }

        public static string ReceivedRequestBaseUriAndPath(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            ZeroMQTransferMessagingContext receivedContext = zeroMQMessagingContextHandlerContext.ReceivedContext;

            Uri requestUri = receivedContext?.Request.RequestUri;

            if (requestUri != null && requestUri.IsAbsoluteUri)

                return requestUri.GetLeftPart(UriPartial.Path);

            return String.Empty;
        }

        //private static Uri GetRequestUri(this HttpRequest httpRequest)
        //{
        //    UriBuilder b = new UriBuilder();

        //    b.Scheme = httpRequest.Scheme;

        //    b.Host = httpRequest.Host.Host;

        //    if (httpRequest.Host.Port.HasValue)

        //        b.Port = httpRequest.Host.Port.Value;

        //    else if (httpRequest.IsHttps)

        //        b.Port = 443;

        //    else

        //        b.Port = 80;

        //    b.Path = httpRequest.Path;

        //    if (httpRequest.QueryString.HasValue)
        //    {
        //        b.Query = httpRequest.QueryString.Value.Substring(1);
        //    }

        //    return b.Uri;
        //}

        //private static String GetUrlEncodedString(this IEnumerable<KeyValuePair<string, StringValues>> query)
        //{
        //    if (query != null)
        //    {
        //        StringBuilder sb = new StringBuilder();

        //        foreach (var q in query)
        //        {
        //            if (q.Value.Count == 0)
        //            {
        //                if (sb.Length > 0)
        //                    sb.Append('&');
        //                sb.Append(Uri.EscapeDataString(q.Key).Replace(' ', '+'));
        //            }
        //            else
        //            {
        //                foreach (String value in q.Value)
        //                {
        //                    if (sb.Length > 0)
        //                        sb.Append('&');
        //                    sb.Append(Uri.EscapeDataString(q.Key).Replace(' ', '+'));
        //                    sb.Append('=');
        //                    sb.Append(Uri.EscapeDataString(value).Replace(' ', '+'));
        //                }
        //            }
        //        }

        //        return sb.ToString();
        //    }
        //    return null;
        //}

        //public static bool TryAddResponseHeader(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string name, string value)
        //{
        //    ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.Response;

        //    IHeaderDictionary headers = httpResponseContext.Response.Headers;

        //    if (!headers.ContainsKey(name))
        //    {
        //        if (!headers[name].Contains(value))
        //        {
        //            headers.Add(name, value);

        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //public static bool TryAddResponseHeader(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string name, string[] values)
        //{
        //    ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.Response;

        //    IHeaderDictionary headers = httpResponseContext.Response.Headers;

        //    if (!headers.ContainsKey(name))
        //    {
        //        headers.Add(name, values);

        //        return true;
        //    }

        //    return false;
        //}

        //public static bool TryAppendResponseHeaderValue(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string name, string value)
        //{
        //    ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.Response;

        //    IHeaderDictionary headers = httpResponseContext.Response.Headers;

        //    if (!headers.ContainsKey(name))
        //    {
        //        if (!headers[name].Contains(value))
        //        {
        //            headers.Append(name, value);

        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //public static bool TryGetResponseHeader(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string name, out string[] values)
        //{
        //    ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.Response;

        //    IHeaderDictionary headers = httpResponseContext.Response.Headers;

        //    values = headers[name];

        //    return values.Length > 0;
        //}

        //public static bool TryRemoveResponseHeader(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string name, out string[] values)
        //{
        //    ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.Response;

        //    IHeaderDictionary headers = httpResponseContext.Response.Headers;

        //    values = headers[name];

        //    if (headers.Remove(name))
        //    {
        //        return true;
        //    }

        //    values = Array.Empty<String>();

        //    return false;
        //}

        //public static HttpStatusCode GetStatusCode(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        //{
        //    ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.Response;

        //    return (HttpStatusCode)httpResponseContext.Response.StatusCode;
        //}

        //public static void SetStatusCode(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, HttpStatusCode httpStatusCode)
        //{
        //    ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.Response;

        //    httpResponseContext.Response.StatusCode = (int)httpStatusCode;

        //    zeroMQMessagingContextHandlerContext.SetStatusDescription(httpStatusCode.SupplyStatusCodeDescription());
        //}

        //public static String GetStatusDescription(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        //{
        //    return zeroMQMessagingContextHandlerContext.Items["StatusDescription"]?.ToString();
        //}

        //public static void SetStatusDescription(this MessagingContextHandlerContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string statusDescription)
        //{
        //    zeroMQMessagingContextHandlerContext.Items["StatusDescription"] = statusDescription;
        //}

        public static String GetResponseContentType(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.SendContext;

            return responseContext.ResponseContentType.NormalizedMediaType;
        }

        public static void SetResponseContentType(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string contentType)
        {
            ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.SendContext;

            responseContext.ResponseContentType = new MediaContentType(contentType);
        }

        public static void SetResponseContentTypeParameter(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string parameterName, string value)
        {
            throw new NotImplementedException();
        }

        public static string GetContentEncoding(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            throw new NotImplementedException();
        }

        public static void SetContentEncoding(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, IEnumerable<string> contentEncoding)
        {
            throw new NotImplementedException();
        }

        public static long? GetResponseContentLength(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.SendContext;

            return responseContext.ResponseContentLength;
        }

        public static void SetResponseContentLength(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, long contentLength)
        {
            ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.SendContext;

            responseContext.ResponseContentLength = contentLength;
        }

        public static void SetResponseRelatesTo(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, UniqueId relatesTo)
        {
            ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.SendContext;

            responseContext.ResponseRelatesTo = relatesTo;
        }

        public static void SetResponseTo(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, Uri to)
        {
            ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.SendContext;

            responseContext.ResponseTo = to;
        }

        public static void SetResponseAction(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, string action)
        {
            ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.SendContext;

            responseContext.ResponseAction = action;
        }

        public static void SetChunked(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, bool sendChunked)
        {
            zeroMQMessagingContextHandlerContext.Items["SendChunked"] = sendChunked;
        }

        public static bool GetChunked(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            if (zeroMQMessagingContextHandlerContext.Items.ContainsKey("SendChunked"))

                return (bool)zeroMQMessagingContextHandlerContext.Items["SendChunked"];

            return false;
        }

        public static void SetKeepAlive(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, bool keepAlive)
        {
            zeroMQMessagingContextHandlerContext.Items["KeepAlive"] = keepAlive;
        }

        public static bool GetKeepAlive(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            if (zeroMQMessagingContextHandlerContext.Items.ContainsKey("KeepAlive"))

                return (bool)zeroMQMessagingContextHandlerContext.Items["KeepAlive"];

            return false;
        }

        public static void SetRedirect(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext, Uri url)
        {
            ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.SendContext;

            throw new NotImplementedException();
        }

        public static Stream GetResponseStream(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            ZeroMQTransferMessagingContext responseContext = zeroMQMessagingContextHandlerContext.SendContext;

            return responseContext.ResponseBody;
        }

        internal static void AddAuthenticationChangeListener(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> messagingHandlerContext, Action<IPrincipal> authenticationChangeListener)
        {
            ((ZeroMQTransferMessagingContextHandlerContext)messagingHandlerContext).AddAuthenticationChangeListener(authenticationChangeListener);
        }


        internal static void Abort(this ProtocolMessagingContext<ZeroMQTransferMessagingContext> zeroMQMessagingContextHandlerContext)
        {
            zeroMQMessagingContextHandlerContext.ApplicationContext.MessagingChannelAccessor.Get<IMessagingContextChannel<ZeroMQTransferMessagingContext>>().Abort();
        }
    }
}
