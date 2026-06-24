using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Http
{
    public class HttpConnectionContextHeaders : InteractionContext.Headers
    {
        public HttpConnectionContextHeaders(RequestHeaders requestHeaders) :
            base(requestHeaders.Headers, requestHeaders.Host.ToUriComponent(), requestHeaders.Referer)
        {
        }

        public static implicit operator HttpConnectionContextHeaders(RequestHeaders requestHeaders)
        {
            return new HttpConnectionContextHeaders(requestHeaders);
        }
    }
}
