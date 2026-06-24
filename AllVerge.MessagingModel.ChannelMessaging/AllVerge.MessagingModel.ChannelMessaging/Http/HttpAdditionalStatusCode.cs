using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.ChannelMessaging.Http
{
    //
    // Summary:
    //     Contains the values of additional status codes defined for HTTP in http://www.rfc-editor.org/info/rfc6585.
    public enum HttpAdditionalStatusCode
    {
        /// <summary>
        /// Equivalent to HTTP status 428. <see cref="HttpAdditionalStatusCode.PreconditionRequired"/> indicates that the 
        /// origin server requires the request to be conditional.
        /// </summary>
        PreconditionRequired = 428,
        /// <summary>
        /// Equivalent to HTTP status 429. <see cref="HttpAdditionalStatusCode.TooManyRequests"/> indicates that the 
        /// user has sent too many requests in a given amount of time ("rate limiting").
        /// </summary>
        TooManyRequests = 429,
        /// <summary>
        /// Equivalent to HTTP status 431. <see cref="HttpAdditionalStatusCode.RequestHeaderFieldsTooLarge"/> indicates that the 
        /// server is unwilling to process the request because its header fields are too large.
        /// </summary>
        RequestHeaderFieldsTooLarge = 431,
        /// <summary>
        /// Equivalent to HTTP status 511. <see cref="HttpAdditionalStatusCode.NetworkAuthenticationRequired"/> indicates that the
        /// client needs to authenticate to gain network access.
        /// </summary>
        NetworkAuthenticationRequired = 511
    }
}
