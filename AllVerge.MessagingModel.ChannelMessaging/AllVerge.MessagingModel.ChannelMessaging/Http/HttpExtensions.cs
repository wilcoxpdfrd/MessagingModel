using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.ChannelMessaging.Http
{
    public static class HttpExtensions
    {
        internal static Uri GetRequestUri(this HttpRequest httpRequest)
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
    }
}
