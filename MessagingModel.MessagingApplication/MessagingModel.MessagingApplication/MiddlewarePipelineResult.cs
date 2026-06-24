using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication
{
    /// <summary>
    /// Defines the Middleware Pipeline Result Output Headers that can be specified by 
    /// <see cref="IMessagingContext{MessageContext}.Output(MessageContext, MiddlewarePipelineResult, IDictionary{MiddlewarePipelineResultHeaders, Microsoft.Extensions.Primitives.StringValues})"/>. 
    /// </summary>
    /// <remarks>The response headers defined in rf2616 may be used to consider addtional values to be supported here.</remarks>
    /// <seealso cref="https://www.rfc-editor.org/rfc/rfc2616#page-41"/>
    public enum MiddlewarePipelineResultHeaders
    {
        /// <summary>
        /// Indicates the message level authentication scheme that must be used.
        /// </summary>
        Authenticate,
        /// <summary>
        /// Indicates the location at which to find or produce a result.
        /// </summary>
        Location,
        /// <summary>
        /// Indicates the length of time to wait before retrying to obtain a result.
        /// </summary>
        RetryAfter
    }

    public enum MiddlewarePipelineResult
    {
        // The input message was not received in the alloted time
        InputCancelled,
        // There was no middleware pipeline found to handle the input message
        Unreachable,
        // The input message has been received, but not yet handled by the middleware pipeline,
        // or there was no middleware component that handled the message
        NotHandled,
        // The middleware pipeline was too busy to handle the input message
        TooBusy,
        // The middleware pipeline did not complete processing the input message within the alloted time
        Timeout,
        // The middleware pipeline did not complete processing the input message due to being cancelled
        Canceled,
        // The input message was rejected as it was not authorized
        NotAuthorized,
        // The middleware component handling the message in the pipeline faulted
        Faulted,
        // The input message was handled by a components in the the middleware pipeline, and a result was produced
        Completed,
        // The input message was routed by the middleware pipeline to multiple destinations for processing, 
        // each produced a result and the results were reduced into an aggregate result
        DistributedResult,
        // The input message was handled by a component in the middleware pipeline, which produced a result;
        // the result can be obtained by following the specified redirection location header
        RedirectToResult,
        // The input message was handled by a component in the middleware pipeline, which did not produce a result;
        // a result can be produced by following the specified redirection location header, and respecting any specified retry-after header
        RedirectForResult
    }

    public static class MiddlewarePipelineResultExtensions
    {
        public static IDictionary<MiddlewarePipelineResultHeaders, StringValues> TryMapResultHeaders(this IDictionary<RejectHeaders, StringValues> notHandledHeaders)
        {
            if (notHandledHeaders == null)

                return null;

            Dictionary<MiddlewarePipelineResultHeaders, StringValues> resultHeaders = new Dictionary<MiddlewarePipelineResultHeaders, StringValues>();

            foreach (KeyValuePair<RejectHeaders, StringValues> notHandledHeader in notHandledHeaders)
            {
                switch (notHandledHeader.Key)
                {
                    case RejectHeaders.Authenticate:
                        resultHeaders.Add(MiddlewarePipelineResultHeaders.Authenticate, notHandledHeader.Value);
                        break;
                    case RejectHeaders.RetryAfter:
                        resultHeaders.Add(MiddlewarePipelineResultHeaders.RetryAfter, notHandledHeader.Value);
                        break;
                }
            }

            return resultHeaders;
        }

        public static bool TryGetHeader(this IDictionary<MiddlewarePipelineResultHeaders, StringValues> headers, MiddlewarePipelineResultHeaders header, out StringValues value)
        {
            if (headers == null || !headers.ContainsKey(header))
            {
                value = default(StringValues);

                return false;
            }

            value = headers[header];

            return true;
        }
    }
}
