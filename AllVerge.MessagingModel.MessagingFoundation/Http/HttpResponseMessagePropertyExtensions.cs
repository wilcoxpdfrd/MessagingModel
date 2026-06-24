using AllVerge.MessagingModel.MessagingFoundation.Channels;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Http
{
    internal static class HttpResponseMessagePropertyExtensions
    {
        public static bool TryGetMediaContentType(this HttpResponseMessageProperty httpResponseMessageProperty, out MediaContentType mediaContentType)
        {
            if (httpResponseMessageProperty != null && httpResponseMessageProperty.Headers.TryGetHeaderValues(HttpHeaderNames.ContentType, out string[] values))
            {
                mediaContentType = 
                    new MediaContentType(values.First());

                return true;
            }

            mediaContentType = null;

            return false;
        }

        public static HttpResponseMessageProperty SetHeader(this HttpResponseMessageProperty httpResponseMessageProperty, string headerName, string value)
        {
            if (httpResponseMessageProperty != null)
                
                httpResponseMessageProperty.Headers[headerName] = value;

            return httpResponseMessageProperty;
        }
    }
}
