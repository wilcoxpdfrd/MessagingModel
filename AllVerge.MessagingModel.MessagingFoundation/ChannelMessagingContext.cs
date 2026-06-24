using AllVerge.MessagingModel.ChannelMessaging.Http;
using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingFoundation.Channels;
using AllVerge.MessagingModel.MessagingFoundation.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    internal class ChannelMessagingContext : MessagingContext<ChannelMessageContext>
    {
        protected override void OnAfterInputReceived(object source, EventArgs eventArgs)
        {
            base.CanBind = ChannelMessageContext.MapContext(this.InputContext, this.BindingContext);
        }

        protected override void OnAfterOutputReceived(object source, EventArgs eventArgs)
        {
            if (this.InputContext.Message.State != MessageState.Closed && this.InputContext.Message.Properties.ContainsKey(HttpRequestMessageProperty.Name))
            {
                if (this.OutputContext.Message.State != MessageState.Closed)
                {
                    if (!this.OutputContext.Message.Properties.TryGetProperty(HttpResponseMessageProperty.Name, out HttpResponseMessageProperty httpResponseMessageProperty))
                    {
                        httpResponseMessageProperty = new HttpResponseMessageProperty();

                        this.OutputContext.Message.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty);

                        switch (this.Result)
                        {
                            case MiddlewarePipelineResult.Unreachable:
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.NotFound;
                                break;
                            case MiddlewarePipelineResult.NotHandled:
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.NotImplemented;
                                break;
                            case MiddlewarePipelineResult.TooBusy:
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.ServiceUnavailable;
                                this.ResultHeaders.Aggregate(httpResponseMessageProperty.Headers, (h, r) =>
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
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.GatewayTimeout;
                                break;
                            case MiddlewarePipelineResult.Canceled:
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.RequestTimeout;
                                httpResponseMessageProperty.Headers.Add(HttpHeaderNames.Connection, HttpHeaderValues.Connection.Close);
                                break;
                            case MiddlewarePipelineResult.NotAuthorized:
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.Unauthorized;
                                this.ResultHeaders.Aggregate(httpResponseMessageProperty.Headers, (h, r) =>
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
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.InternalServerError;
                                break;
                            case MiddlewarePipelineResult.Completed:
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.OK;
                                break;
                            case MiddlewarePipelineResult.DistributedResult:
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.OK;
                                break;
                            case MiddlewarePipelineResult.RedirectToResult:
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.Redirect;
                                this.ResultHeaders.Aggregate(httpResponseMessageProperty.Headers, (h, r) =>
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
                                httpResponseMessageProperty.StatusCode = HttpStatusCode.RedirectMethod;
                                this.ResultHeaders.Aggregate(httpResponseMessageProperty.Headers, (h, r) =>
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
                    }
                }
            }
        }
    }
}