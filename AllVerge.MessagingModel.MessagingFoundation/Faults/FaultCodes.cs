using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Faults
{
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;

    /// <summary>
    /// Utilities for preparing and formatting service errors.
    /// </summary>
    public static class FaultCodes
    {
        public class ClientRedirectionCode : SenderFaultCode
        {
            public static readonly FaultCode SubCode = new FaultCode("Redirection");

            private ClientRedirectionCode(FaultCode subCode, FaultReason faultReason) : base(SubCode.AttachSubCode(subCode), faultReason)
            {
            }

            public static ClientRedirectionCode Ambiguous = new ClientRedirectionCode(new FaultCode("Ambiguous"), new FaultReason("Ambiguous"));
            public static ClientRedirectionCode MultipleChoices = new ClientRedirectionCode(new FaultCode("MultipleChoices"), new FaultReason("Multiple Choices"));
            public static ClientRedirectionCode Moved = new ClientRedirectionCode(new FaultCode("Moved"), new FaultReason("Moved"));
            public static ClientRedirectionCode MovedPermanently = new ClientRedirectionCode(new FaultCode("MovedPermanently"), new FaultReason("Moved Permanently"));
            public static ClientRedirectionCode Redirect = new ClientRedirectionCode(new FaultCode("Redirect"), new FaultReason("Redirect"));
            public static ClientRedirectionCode Found = new ClientRedirectionCode(new FaultCode("Found"), new FaultReason("Found"));
            public static ClientRedirectionCode RedirectMethod = new ClientRedirectionCode(new FaultCode("RedirectMethod"), new FaultReason("Redirect Method"));
            public static ClientRedirectionCode SeeOther = new ClientRedirectionCode(new FaultCode("SeeOther"), new FaultReason("See Other"));
            public static ClientRedirectionCode NotModified = new ClientRedirectionCode(new FaultCode("NotModified"), new FaultReason("Not Modified"));
            public static ClientRedirectionCode UseProxy = new ClientRedirectionCode(new FaultCode("UseProxy"), new FaultReason("Use Proxy"));
            public static ClientRedirectionCode Unused = new ClientRedirectionCode(new FaultCode("Unused"), new FaultReason("(Unused)"));
            public static ClientRedirectionCode RedirectKeepVerb = new ClientRedirectionCode(new FaultCode("RedirectKeepVerb"), new FaultReason("Redirect Keep Verb"));
            public static ClientRedirectionCode TemporaryRedirect = new ClientRedirectionCode(new FaultCode("TemporaryRedirect"), new FaultReason("Temporary Redirect"));
            public static ClientRedirectionCode PermanentRedirect = new ClientRedirectionCode(new FaultCode("PermanentRedirect"), new FaultReason("Permanent Redirect"));

            public static ClientRedirectionCode Create(FaultCode subCode, FaultReason faultReason) => new ClientRedirectionCode(subCode, faultReason);
        }

        public class ClientErrorCode : SenderFaultCode
        {
            public static readonly FaultCode SubCode = new FaultCode("ClientError");

            private ClientErrorCode(FaultCode subCode, FaultReason faultReason) : base(SubCode.AttachSubCode(subCode), faultReason)
            {
            }

            public static ClientErrorCode BadRequest = new ClientErrorCode(new FaultCode("BadRequest"), new FaultReason("Bad Request"));
            public static ClientErrorCode ClaimNotFound = new ClientErrorCode(new FaultCode("ClaimNotFound"), new FaultReason("Claim Not Found"));
            public static ClientErrorCode ClaimRejected = new ClientErrorCode(new FaultCode("ClaimRejected"), new FaultReason("Claim Rejected"));
            public static ClientErrorCode ClaimConfirmationRejected = new ClientErrorCode(new FaultCode("ClaimConfirmationRejected"), new FaultReason("Claim Confirmation Rejected"));
            public static ClientErrorCode Unauthorized = new ClientErrorCode(new FaultCode("Unauthorized"), new FaultReason("Unauthorized"));
            public static ClientErrorCode PaymentRequired = new ClientErrorCode(new FaultCode("PaymentRequired"), new FaultReason("Payment Required"));
            public static ClientErrorCode Forbidden = new ClientErrorCode(new FaultCode("Forbidden"), new FaultReason("Forbidden"));
            public static ClientErrorCode NotFound = new ClientErrorCode(new FaultCode("NotFound"), new FaultReason("Not Found"));
            public static ClientErrorCode MethodNotAllowed = new ClientErrorCode(new FaultCode("MethodNotAllowed"), new FaultReason("Method Not Allowed"));
            public static ClientErrorCode NotAcceptable = new ClientErrorCode(new FaultCode("NotAcceptable"), new FaultReason("Not Acceptable"));
            public static ClientErrorCode ProxyAuthenticationRequired = new ClientErrorCode(new FaultCode("ProxyAuthenticationRequired"), new FaultReason("Proxy Authentication Required"));
            public static ClientErrorCode RequestTimeout = new ClientErrorCode(new FaultCode("RequestTimeout"), new FaultReason("Request Timeout"));
            public static ClientErrorCode Conflict = new ClientErrorCode(new FaultCode("Conflict"), new FaultReason("Conflict"));
            public static ClientErrorCode Gone = new ClientErrorCode(new FaultCode("Gone"), new FaultReason("Gone"));
            public static ClientErrorCode LengthRequired = new ClientErrorCode(new FaultCode("LengthRequired"), new FaultReason("Length Required"));
            public static ClientErrorCode PreconditionFailed = new ClientErrorCode(new FaultCode("PreconditionFailed"), new FaultReason("Precondition Failed"));
            public static ClientErrorCode RequestEntityTooLarge = new ClientErrorCode(new FaultCode("RequestEntityTooLarge"), new FaultReason("Request Entity Too Large"));
            public static ClientErrorCode RequestUriTooLong = new ClientErrorCode(new FaultCode("RequestUriTooLong"), new FaultReason("Request-URI Too Long"));
            public static ClientErrorCode UnsupportedMediaType = new ClientErrorCode(new FaultCode("UnsupportedMediaType"), new FaultReason("Unsupported Media Type"));
            public static ClientErrorCode RequestedRangeNotSatisfiable = new ClientErrorCode(new FaultCode("RequestedRangeNotSatisfiable"), new FaultReason("Requested Range Not Satisfiable"));
            public static ClientErrorCode ExpectationFailed = new ClientErrorCode(new FaultCode("ExpectationFailed"), new FaultReason("Expectation Failed"));
            public static ClientErrorCode UpgradeRequired = new ClientErrorCode(new FaultCode("UpgradeRequired"), new FaultReason("Upgrade Required"));

            public static ClientErrorCode Create(FaultCode subCode, FaultReason faultReason) => new ClientErrorCode(subCode, faultReason);
        }

        public class ServerErrorCode : ReceiverFaultCode
        {
            public static readonly FaultCode SubCode = new FaultCode("ServerError");

            private ServerErrorCode(FaultCode subCode, FaultReason faultReason) : base(SubCode.AttachSubCode(subCode), faultReason)
            {
            }

            public static readonly ServerErrorCode IntermediaryFaulted = new ServerErrorCode(new FaultCode("IntermediaryFaulted"), new FaultReason("Intermediary Faulted"));
            public static readonly ServerErrorCode IntermediaryThrottled = new ServerErrorCode(new FaultCode("IntermediaryThrottled"), new FaultReason("Intermediary Throttled"));
            public static readonly ServerErrorCode IntermediaryTimeout = new ServerErrorCode(new FaultCode("IntermediaryTimeout"), new FaultReason("Intermediary Timeout"));
            public static readonly ServerErrorCode InternalServerError = new ServerErrorCode(new FaultCode("InternalServerError"), new FaultReason("Internal Server Error"));
            public static readonly ServerErrorCode ServiceFaulted = new ServerErrorCode(new FaultCode("ServiceFaulted"), new FaultReason("Service Faulted"));
            public static readonly ServerErrorCode ServiceNotImplemented = new ServerErrorCode(new FaultCode("ServiceNotImplemented"), new FaultReason("Service Not Implemented"));
            public static readonly ServerErrorCode ServiceUnavailable = new ServerErrorCode(new FaultCode("ServiceUnavailable"), new FaultReason("Service Unavailable"));
            public static readonly ServerErrorCode ServiceThrottled = new ServerErrorCode(new FaultCode("ServiceThrottled"), new FaultReason("Service Throttled"));
            public static readonly ServerErrorCode ServiceTimeout = new ServerErrorCode(new FaultCode("ServiceTimeout"), new FaultReason("Service Timeout"));

            public static ServerErrorCode Create(FaultCode subCode, FaultReason faultReason) => new ServerErrorCode(subCode, faultReason);
        }

        public static readonly FaultReason AGGREGATE_FAULT_EXCEPTION_REASON = new FaultReason("An aggregate exception was thrown.");

        /// <summary>
        /// Gets the Http response factors.  See remarks.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="httpStatusCode"></param>
        /// <param name="httpStatusCodeDescription"></param>
        /// <param name="suppressPreamble"></param>
        /// <param name="suppressEntityBody"></param>
        /// <param name="responseHeaders"></param>
        /// <param name="mapFaultReasonToStatusDescription"></param>
        /// <remarks>
        /// This method may read <paramref name="message"/> (if <see cref="Message.IsFault"/> is true).  
        /// To avoid the <see cref="Message.State"/> of the original message from being changed by the read operation, 
        /// consider creating two copies of the original message, then pass one here; 
        /// the other should replace the original message.
        /// </remarks>
        public static void GetHttpResponseMetadata(this Message message, out HttpStatusCode httpStatusCode, out string httpStatusCodeDescription, out bool suppressPreamble, out bool suppressEntityBody, out KeyValuePair<HttpResponseHeader, string>[] responseHeaders, bool mapFaultReasonToStatusDescription = false)
        {
            Dictionary<HttpResponseHeader, string> headers =
                new Dictionary<HttpResponseHeader, string>();

            if (message.Version == MessageVersion.None)
            {
                if (message.IsFault)
                {
                    MessageFault messageFault = MessageFault.CreateFault(message, ushort.MaxValue);

                    httpStatusCode = GetHttpStatusCodeFromPredefinedFaultCodeAndReason(messageFault.Code, messageFault.Reason, message.Properties, headers, out suppressPreamble, out suppressEntityBody);

                    if (messageFault.Reason != null && mapFaultReasonToStatusDescription)
                    {
                        httpStatusCodeDescription = messageFault.Reason.GetMatchingTranslation().Text;
                    }
                    else
                    {
                        httpStatusCodeDescription = null;
                    }
                }
                else
                {
                    httpStatusCode = HttpStatusCode.OK;

                    httpStatusCodeDescription = null;

                    suppressPreamble = false;
                    suppressEntityBody = message.IsEmpty;
                }
            }
            else
            {
                if (message.IsFault)
                {
                    httpStatusCode = HttpStatusCode.InternalServerError;

                    httpStatusCodeDescription = null;
                }
                else
                {
                    httpStatusCode = HttpStatusCode.OK;

                    httpStatusCodeDescription = null;
                }

                suppressPreamble = false;
                suppressEntityBody = false;
            }

            responseHeaders = headers.ToArray();
        }

        private static HttpStatusCode GetHttpStatusCodeFromPredefinedFaultCodeAndReason(FaultCode faultCode, FaultReason faultReason, MessageProperties messageProperties, Dictionary<HttpResponseHeader, string> headers, out bool suppressPreamble, out bool suppressEntityBody)
        {
            suppressPreamble = false;

            HttpStatusCode httpStatusCode;

            if (faultCode != null && faultCode.IsPredefinedFault)
            {
                if (faultCode.IsSenderFault)
                {
                    suppressEntityBody = false;

                    if (faultCode.SubCode == null)

                        httpStatusCode = HttpStatusCode.BadRequest;

                    else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.SubCode, false))
                    {
                        if (messageProperties != null && messageProperties.TryGetProperty(IncomingMessageEventArgs.DispatchOperationRedirectUriPropertyName, out Uri redirectTo))

                            headers.Add(HttpResponseHeader.Location, redirectTo.AbsoluteUri);

                        if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.Ambiguous.Code))

                            httpStatusCode = HttpStatusCode.Ambiguous;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.MultipleChoices.Code))

                            httpStatusCode = HttpStatusCode.MultipleChoices;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.Moved.Code))

                            httpStatusCode = HttpStatusCode.Moved;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.MovedPermanently.Code))

                            httpStatusCode = HttpStatusCode.MovedPermanently;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.Found.Code))

                            httpStatusCode = HttpStatusCode.Found;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.Redirect.Code))

                            httpStatusCode = HttpStatusCode.Redirect;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.RedirectMethod.Code))

                            httpStatusCode = HttpStatusCode.RedirectMethod;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.RedirectMethod.Code))

                            httpStatusCode = HttpStatusCode.SeeOther;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.NotModified.Code))

                            httpStatusCode = HttpStatusCode.NotModified;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.UseProxy.Code))

                            httpStatusCode = HttpStatusCode.UseProxy;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.Unused.Code))

                            httpStatusCode = HttpStatusCode.Unused;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.RedirectKeepVerb.Code))

                            httpStatusCode = HttpStatusCode.RedirectKeepVerb;

                        else if (faultCode.SubCode.IsEqualTo(ClientRedirectionCode.TemporaryRedirect.Code))

                            httpStatusCode = HttpStatusCode.TemporaryRedirect;

                        else

                            httpStatusCode = HttpStatusCode.Redirect;
                    }
                    else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.SubCode, false))
                    {
                        if (faultCode.SubCode.IsEqualTo(ClientErrorCode.BadRequest.Code))

                            httpStatusCode = HttpStatusCode.BadRequest;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.ClaimNotFound.Code))

                            httpStatusCode = HttpStatusCode.BadRequest;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.ClaimRejected.Code))

                            httpStatusCode = HttpStatusCode.BadRequest;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.ClaimConfirmationRejected.Code))

                            httpStatusCode = HttpStatusCode.BadRequest;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.Unauthorized.Code))

                            httpStatusCode = HttpStatusCode.Unauthorized;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.PaymentRequired.Code))

                            httpStatusCode = HttpStatusCode.PaymentRequired;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.Forbidden.Code))

                            httpStatusCode = HttpStatusCode.Forbidden;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.NotFound.Code))

                            httpStatusCode = HttpStatusCode.NotFound;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.MethodNotAllowed.Code))

                            httpStatusCode = HttpStatusCode.MethodNotAllowed;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.NotAcceptable.Code))

                            httpStatusCode = HttpStatusCode.NotAcceptable;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.ProxyAuthenticationRequired.Code))

                            httpStatusCode = HttpStatusCode.ProxyAuthenticationRequired;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.RequestTimeout.Code))

                            httpStatusCode = HttpStatusCode.RequestTimeout;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.RequestTimeout.Code))

                            httpStatusCode = HttpStatusCode.RequestTimeout;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.Conflict.Code))

                            httpStatusCode = HttpStatusCode.Conflict;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.Gone.Code))

                            httpStatusCode = HttpStatusCode.Gone;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.LengthRequired.Code))

                            httpStatusCode = HttpStatusCode.LengthRequired;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.PreconditionFailed.Code))

                            httpStatusCode = HttpStatusCode.PreconditionFailed;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.RequestEntityTooLarge.Code))

                            httpStatusCode = HttpStatusCode.RequestEntityTooLarge;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.RequestUriTooLong.Code))

                            httpStatusCode = HttpStatusCode.RequestUriTooLong;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.UnsupportedMediaType.Code))

                            httpStatusCode = HttpStatusCode.UnsupportedMediaType;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.RequestedRangeNotSatisfiable.Code))

                            httpStatusCode = HttpStatusCode.RequestedRangeNotSatisfiable;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.ExpectationFailed.Code))

                            httpStatusCode = HttpStatusCode.ExpectationFailed;

                        else if (faultCode.SubCode.IsEqualTo(ClientErrorCode.UpgradeRequired.Code))

                            httpStatusCode = HttpStatusCode.UpgradeRequired;

                        else

                            httpStatusCode = HttpStatusCode.BadRequest;
                    }
                    else

                        httpStatusCode = HttpStatusCode.BadRequest;
                }
                else if (faultCode.IsReceiverFault)
                {
                    suppressEntityBody = true;

                    if (faultCode.SubCode == null)

                        httpStatusCode = HttpStatusCode.InternalServerError;

                    else if (faultCode.SubCode.IsEqualTo(ServerErrorCode.SubCode, false))
                    {
                        if (faultCode.SubCode.IsEqualTo(ServerErrorCode.IntermediaryFaulted.Code))

                            httpStatusCode = HttpStatusCode.BadGateway;

                        else if (faultCode.SubCode.IsEqualTo(ServerErrorCode.IntermediaryThrottled.Code))

                            httpStatusCode = HttpStatusCode.ServiceUnavailable;

                        else if (faultCode.SubCode.IsEqualTo(ServerErrorCode.IntermediaryTimeout.Code))

                            httpStatusCode = HttpStatusCode.GatewayTimeout;

                        else if (faultCode.SubCode.IsEqualTo(ServerErrorCode.InternalServerError.Code))

                            httpStatusCode = HttpStatusCode.InternalServerError;

                        else if (faultCode.SubCode.IsEqualTo(ServerErrorCode.ServiceFaulted.Code))

                            httpStatusCode = HttpStatusCode.InternalServerError;

                        else if (faultCode.SubCode.IsEqualTo(ServerErrorCode.ServiceUnavailable.Code))

                            httpStatusCode = HttpStatusCode.ServiceUnavailable;

                        else if (faultCode.SubCode.IsEqualTo(ServerErrorCode.ServiceThrottled.Code))
                        {
                            httpStatusCode = HttpStatusCode.ServiceUnavailable;

                            headers.Add(HttpResponseHeader.RetryAfter, "5");
                        }
                        else if (faultCode.SubCode.IsEqualTo(ServerErrorCode.ServiceTimeout.Code))

                            httpStatusCode = HttpStatusCode.RequestTimeout;

                        else

                            httpStatusCode = HttpStatusCode.InternalServerError;
                    }
                    else

                        httpStatusCode = HttpStatusCode.InternalServerError;
                }
                else
                {
                    httpStatusCode = HttpStatusCode.InternalServerError;

                    suppressEntityBody = true;
                }
            }
            else
            {
                httpStatusCode = HttpStatusCode.InternalServerError;

                suppressEntityBody = true;
            }

            return httpStatusCode;
        }

        public static FaultCode GetPredefinedFaultCodeFromHttpStatusCode(this HttpStatusCode statusCode, string statusReason, out FaultReason faultReason)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Continue: //100,
                case HttpStatusCode.SwitchingProtocols: //101,
                    break;
                case HttpStatusCode.OK: //200,
                case HttpStatusCode.Created: //201,
                case HttpStatusCode.Accepted: //202,
                case HttpStatusCode.NonAuthoritativeInformation: //203,
                case HttpStatusCode.NoContent: //204,
                case HttpStatusCode.ResetContent: //205,
                case HttpStatusCode.PartialContent: //206,
                    break;
                case HttpStatusCode.MultipleChoices: //300,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.MultipleChoices.Reason;
                    return ClientRedirectionCode.MultipleChoices.Code;
                case HttpStatusCode.MovedPermanently: //301,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.MovedPermanently.Reason;
                    return ClientRedirectionCode.MovedPermanently.Code;
                case HttpStatusCode.Found: //302,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.Found.Reason;
                    return ClientRedirectionCode.Found.Code;
                case HttpStatusCode.SeeOther: //303,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.SeeOther.Reason;
                    return ClientRedirectionCode.SeeOther.Code;
                case HttpStatusCode.NotModified: //304,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.NotModified.Reason;
                    return ClientRedirectionCode.NotModified.Code;
                case HttpStatusCode.UseProxy: //305,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.UseProxy.Reason;
                    return ClientRedirectionCode.UseProxy.Code;
                case HttpStatusCode.Unused: //306,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.Unused.Reason;
                    return ClientRedirectionCode.Unused.Code;
                case HttpStatusCode.TemporaryRedirect: //307,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.TemporaryRedirect.Reason;
                    return ClientRedirectionCode.TemporaryRedirect.Code;
                case HttpStatusCode.BadRequest: //400,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.BadRequest.Reason;
                    return ClientErrorCode.BadRequest.Code;
                case HttpStatusCode.Unauthorized: //401,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.Unauthorized.Reason;
                    return ClientErrorCode.Unauthorized.Code;
                case HttpStatusCode.PaymentRequired: //402,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.PaymentRequired.Reason;
                    return ClientErrorCode.PaymentRequired.Code;
                case HttpStatusCode.Forbidden: //403,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.Forbidden.Reason;
                    return ClientErrorCode.Forbidden.Code;
                case HttpStatusCode.NotFound: //404,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.NotFound.Reason;
                    return ClientErrorCode.NotFound.Code;
                case HttpStatusCode.MethodNotAllowed: //405,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.MethodNotAllowed.Reason;
                    return ClientErrorCode.MethodNotAllowed.Code;
                case HttpStatusCode.NotAcceptable: //406,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.NotAcceptable.Reason;
                    return ClientErrorCode.NotAcceptable.Code;
                case HttpStatusCode.ProxyAuthenticationRequired: //407,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.ProxyAuthenticationRequired.Reason;
                    return ClientErrorCode.ProxyAuthenticationRequired.Code;
                case HttpStatusCode.RequestTimeout: //408,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.RequestTimeout.Reason;
                    return ClientErrorCode.RequestTimeout.Code;
                case HttpStatusCode.Conflict: //409,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.Conflict.Reason;
                    return ClientErrorCode.Conflict.Code;
                case HttpStatusCode.Gone: //410,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.Gone.Reason;
                    return ClientErrorCode.Gone.Code;
                case HttpStatusCode.LengthRequired: //411,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.LengthRequired.Reason;
                    return ClientErrorCode.LengthRequired.Code;
                case HttpStatusCode.PreconditionFailed: //412,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.PreconditionFailed.Reason;
                    return ClientErrorCode.PreconditionFailed.Code;
                case HttpStatusCode.RequestEntityTooLarge: //413,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.RequestEntityTooLarge.Reason;
                    return ClientErrorCode.RequestEntityTooLarge.Code;
                case HttpStatusCode.RequestUriTooLong: //414,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.RequestUriTooLong.Reason;
                    return ClientErrorCode.RequestUriTooLong.Code;
                case HttpStatusCode.UnsupportedMediaType: //415,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.UnsupportedMediaType.Reason;
                    return ClientErrorCode.UnsupportedMediaType.Code;
                case HttpStatusCode.RequestedRangeNotSatisfiable: //416,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.RequestedRangeNotSatisfiable.Reason;
                    return ClientErrorCode.RequestedRangeNotSatisfiable.Code;
                case HttpStatusCode.ExpectationFailed: //417,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.ExpectationFailed.Reason;
                    return ClientErrorCode.ExpectationFailed.Code;
                case HttpStatusCode.UpgradeRequired: //426,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientErrorCode.UpgradeRequired.Reason;
                    return ClientErrorCode.UpgradeRequired.Code;
                case HttpStatusCode.InternalServerError: //500,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ServerErrorCode.InternalServerError.Reason;
                    return ServerErrorCode.InternalServerError.Code;
                case HttpStatusCode.NotImplemented: //501,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ServerErrorCode.ServiceNotImplemented.Reason;
                    return ServerErrorCode.ServiceNotImplemented.Code;
                case HttpStatusCode.BadGateway: //502,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ServerErrorCode.IntermediaryFaulted.Reason;
                    return ServerErrorCode.IntermediaryFaulted.Code;
                case HttpStatusCode.ServiceUnavailable: //503,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ServerErrorCode.ServiceUnavailable.Reason;
                    return ServerErrorCode.ServiceUnavailable.Code;
                case HttpStatusCode.GatewayTimeout: //504,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ServerErrorCode.IntermediaryTimeout.Reason;
                    return ServerErrorCode.IntermediaryTimeout.Code;
                case HttpStatusCode.HttpVersionNotSupported: //505
                    faultReason = new FaultReason(statusReason == null ? "HTTP Version Not Supported" : statusReason);
                    return ServerErrorCode.InternalServerError.Code;
            }

            switch (statusCode)
            {
                case HttpStatusCode.Ambiguous: //300,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.Ambiguous.Reason;
                    return ClientRedirectionCode.Ambiguous.Code;
                case HttpStatusCode.Moved: //301,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.Moved.Reason;
                    return ClientRedirectionCode.Moved.Code;
                case HttpStatusCode.Redirect: //302,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.Redirect.Reason;
                    return ClientRedirectionCode.Redirect.Code;
                case HttpStatusCode.RedirectMethod: //303,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.RedirectMethod.Reason;
                    return ClientRedirectionCode.RedirectMethod.Code;
                case HttpStatusCode.RedirectKeepVerb: //307,
                    faultReason = statusReason != null ? new FaultReason(statusReason) : ClientRedirectionCode.RedirectKeepVerb.Reason;
                    return ClientRedirectionCode.RedirectKeepVerb.Code;
            }

            faultReason = null;

            return null;
        }

        private static string GetReasonPhrase(string reasonPhrase, HttpStatusCode statusCode)
        {
            if (!string.IsNullOrWhiteSpace(reasonPhrase))
            {
                return reasonPhrase;
            }

            return GetReasonPhrase(statusCode);
        }

        private static string GetReasonPhrase(FaultReason faultReason, HttpStatusCode statusCode)
        {
            if (faultReason != null)
            {
                FaultReasonText faultReasonText = faultReason.GetMatchingTranslation();

                if (faultReasonText != null)

                    return faultReasonText.Text;
            }

            return GetReasonPhrase(statusCode);
        }

        private static string GetReasonPhrase(HttpStatusCode statusCode)
        {
            if (statusCode <= HttpStatusCode.PartialContent)
            {
                if (statusCode == HttpStatusCode.Continue)
                {
                    return "Continue";
                }
                if (statusCode == HttpStatusCode.SwitchingProtocols)
                {
                    return "Switching Protocols";
                }
                switch (statusCode)
                {
                    case HttpStatusCode.OK:
                        return "OK";
                    case HttpStatusCode.Created:
                        return "Created";
                    case HttpStatusCode.Accepted:
                        return "Accepted";
                    case HttpStatusCode.NonAuthoritativeInformation:
                        return "Non-Authoritative Information";
                    case HttpStatusCode.NoContent:
                        return "No Content";
                    case HttpStatusCode.ResetContent:
                        return "Reset Content";
                    case HttpStatusCode.PartialContent:
                        return "Partial Content";
                }
            }
            else if (statusCode.GetPredefinedFaultCodeFromHttpStatusCode(null, out FaultReason faultReason) != null)

                return faultReason.GetMatchingTranslation().Text;

            switch ((int)statusCode / (int)HttpStatusCode.Continue)
            {
                case 1:
                    return "Informational";
                case 2:
                    return "Success";
                case 3:
                    return "Redirection";
                case 4:
                    return "Client Error";
                case 5:
                    return "Server Error";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates a standard Sender <see cref="FaultCode"/>.
        /// </summary>
        /// <param name="subCode">The specific <see cref="FaultCode"/> categorizing the Sender <see cref="FaultCode"/>.</param>
        /// <returns>The Sender <see cref="FaultCode"/>.</returns>
        public static FaultCode CreateSenderFaultCode(FaultCode subCode)
        {
            return new FaultCode("Sender", subCode);
        }

        /// <summary>
        /// Creates a standard Sender <see cref="FaultCode"/> for the <paramref name="envelopeVersion"/>.
        /// </summary>
        /// <param name="envelopeVersion">The SoapEnvelope version, or None.</param>
        /// <param name="subCode">The specific <see cref="FaultCode"/> categorizing the Sender <see cref="FaultCode"/>.</param>
        /// <returns>The Sender <see cref="FaultCode"/>.</returns>
        public static FaultCode CreateSenderFaultCode(this EnvelopeVersion envelopeVersion, FaultCode subCode)
        {
            if (envelopeVersion == EnvelopeVersion.Soap12)
            {
                return new FaultCode("Sender", "http://www.w3.org/2003/05/soap-envelope", subCode);
            }
            else if (envelopeVersion == EnvelopeVersion.Soap11)
            {
                return new FaultCode("Client", "http://schemas.xmlsoap.org/soap/envelope/", subCode);
            }
            else if (envelopeVersion == EnvelopeVersion.None)
            {
                return new FaultCode("Sender", "http://schemas.microsoft.com/ws/2005/05/envelope/none", subCode);
            }
            else
            {
                return new FaultCode("Sender", subCode);
            }
        }

        /// <summary>
        /// Creates a standard Receiver <see cref="FaultCode"/>.
        /// </summary>
        /// <param name="subCode">The specific <see cref="FaultCode"/> categorizing the Receiver <see cref="FaultCode"/>.</param>
        /// <returns>The Receiver <see cref="FaultCode"/>.</returns>
        public static FaultCode CreateReceiverFaultCode(FaultCode subCode)
        {
            return new FaultCode("Receiver", subCode);
        }

        /// <summary>
        /// Creates a standard Receiver <see cref="FaultCode"/> for the <paramref name="envelopeVersion"/>.
        /// </summary>
        /// <param name="envelopeVersion">The SoapEnvelope <see cref="EnvelopeVersion"/>.</param>
        /// <param name="subCode">The specific <see cref="FaultCode"/> categorizing the Receiver <see cref="FaultCode"/>.</param>
        /// <returns>The Receiver <see cref="FaultCode"/>.</returns>
        public static FaultCode CreateReceiverFaultCode(this EnvelopeVersion envelopeVersion, FaultCode subCode)
        {
            if (envelopeVersion == EnvelopeVersion.Soap12)
            {
                return new FaultCode("Receiver", "http://www.w3.org/2003/05/soap-envelope", subCode);
            }
            else if (envelopeVersion == EnvelopeVersion.Soap11)
            {
                return new FaultCode("Server", "http://schemas.xmlsoap.org/soap/envelope/", subCode);
            }
            else if (envelopeVersion == EnvelopeVersion.None)
            {
                return new FaultCode("Receiver", "http://schemas.microsoft.com/ws/2005/05/envelope/none", subCode);
            }
            else //if (envelopeVersion == null)
            {
                return new FaultCode("Receiver", subCode);
            }
        }

        /// <summary>
        /// Creates a <see cref="FaultCode"/> and <paramref name="faultReason"/> from the type name and message of <paramref name="exception"/>.  If <paramref name="subCode"/> is provided, it will be appended to the returned <see cref="FaultCode"/>.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="faultReason"></param>
        /// <param name="subCode"></param>
        /// <returns></returns>
        public static FaultCode CreateFaultCode(this Exception exception, out FaultReason faultReason, FaultCode subCode = null)
        {
            if (exception is FaultException)

                throw new ArgumentException($"Cannot be Type {typeof(FaultException).FullName}.", nameof(exception));

            while (exception is AggregateException && (exception as AggregateException).InnerExceptions.Count == 1)

                exception = exception.InnerException;

            if (exception is AggregateException)
            {
                AggregateException aggregateException = exception as AggregateException;

                Dictionary<FaultCode, Dictionary<string, StringBuilder>> reasons =
                    new Dictionary<FaultCode, Dictionary<string, StringBuilder>>();

                FaultCode faultCode = null;

                foreach (Exception e in (exception as AggregateException).InnerExceptions)
                {
                    faultCode = e.CreateFaultCode(out FaultReason reason, faultCode);

                    reasons.Add(faultCode, reason.Translations.Aggregate(new Dictionary<string, StringBuilder>(), (a, t) =>
                    {
                        if (!a.ContainsKey(t.XmlLang))

                            a.Add(t.XmlLang, new StringBuilder());

                        a[t.XmlLang].AppendLine($"{t.Text}");

                        return a;
                    }));
                }

                faultReason = new FaultReason(reasons.Aggregate(new List<FaultReasonText>(), (l, r) =>
                {
                    foreach (string xmlLang in r.Value.Keys)
                    {
                        l.Add(new FaultReasonText($"{r.Key.Name}:${r.Value[xmlLang].ToString()}", xmlLang));
                    }

                    return l;
                }));
            }

            if (exception is TargetInvocationException)

                exception = (exception as TargetInvocationException).InnerException;

            if (exception is EndpointNotFoundException)
            {
                faultReason = new FaultReason(exception.Message);

                return ServerErrorCode.ServiceUnavailable.Code;
            }
            else if (exception is ProtocolException)
            {
                if (exception.InnerException != null)
                {
                    if (exception.InnerException is WebException)
                    {
                        WebException we = (WebException)exception.InnerException;

                        if (we.Status == WebExceptionStatus.ProtocolError)
                        {
                            Regex rx = new Regex(@"The remote server returned an error: \((\d+)\)\s([\w\s]+.)");

                            Match m = rx.Match(we.Message);

                            if (m.Success)
                            {
                                HttpStatusCode statusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), m.Groups[1].Value);

                                string statusReason = m.Groups[2].Value;

                                return statusCode.GetPredefinedFaultCodeFromHttpStatusCode(statusReason, out faultReason);
                            }
                        }
                    }
                    else
                    {
                        exception = exception.InnerException;
                    }
                }
                else if (exception is ActionMismatchAddressingException)
                {
                    WSAddressing10ProblemHeaderQNameFault phf = new WSAddressing10ProblemHeaderQNameFault(exception as ActionMismatchAddressingException);

                    faultReason = phf.Reason;

                    return phf.Code;
                }
                else if (exception is MessageHeaderException)
                {
                    WSAddressing10ProblemHeaderQNameFault phf = new WSAddressing10ProblemHeaderQNameFault(exception as MessageHeaderException);

                    faultReason = phf.Reason;

                    return phf.Code;
                }
            }

            Type exceptionType = exception.GetType();

            if (exception.Message.Contains("\r\n"))

                faultReason = new FaultReason(exception.Message.Replace("\r\n", "-"));

            else

                faultReason = new FaultReason(exception.Message);

            if (subCode == null)

                return new FaultCode(exceptionType.Name, string.Format("urn:{0}", exceptionType.Namespace.Replace('.', '/')));

            else

                return new FaultCode(exceptionType.Name, string.Format("urn:{0}", exceptionType.Namespace.Replace('.', '/')), subCode);
        }

        /// <summary>
        /// Attaches a <paramref name="subCode"/> to a <paramref name="target"/> <see cref="FaultCode"/>.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="subCode"></param>
        /// <returns>Returns a copy of the <see cref="FaultCode"/> <paramref name="target"/> with the <paramref name="subCode"/> attached at the end of the subCode stack.</returns>
        public static FaultCode AttachSubCode(this FaultCode target, FaultCode subCode)
        {
            Stack<FaultCode> faultStack = new Stack<FaultCode>();

            while (target.SubCode != null)
            {
                faultStack.Push(target);

                target = target.SubCode;
            }

            faultStack.Push(target);

            while (faultStack.Count > 0)
            {
                FaultCode source = faultStack.Pop();

                target = new FaultCode(source.Name, source.Namespace, subCode);

                subCode = target;
            }

            return target;
        }

        public static FaultReason CreateFaultReason(string message, params object[] args)
        {
            message = message.FormatString(args);

            return new FaultReason(message);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        //public static FaultDetail CreateFaultDetail(Exception e)
        //{
        //    return new FaultDetail(
        //        new FaultDetail.DetailCode(e),
        //        e.Message, 
        //        new FaultDetail.Tag("Exception", e));
        //}

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        //public static FaultException CreateFaultException(this AggregateException e, Type sourceType)
        //{
        //    FaultException faultException = e.InnerException.CreateFaultException(sourceType, false);

        //    if (e.InnerExceptions.Count == 1)

        //        return faultException;

        //    List<FaultReasonText> reasons = new List<FaultReasonText>();

        //    FaultCode faultCode = faultException.Code;

        //    reasons.Add(faultException.Reason.GetMatchingTranslation());

        //    foreach (Exception additionalException in e.InnerExceptions.Skip(1))
        //    {
        //        FaultCode additionalCode = e.CreateSubCode(out FaultReason additionalReason);

        //        faultCode = faultCode.AttachSubCode(additionalCode);

        //        reasons.Add(additionalReason.GetMatchingTranslation());
        //    }

        //    return new FaultException(new FaultReason(reasons), faultCode, FaultExceptionHelper.DefaultAction);
        //}

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/> with a Sender fault code.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        //public static FaultException CreateSenderFaultException(this AggregateException e, Type sourceType)
        //{
        //    FaultException faultException = e.InnerException.CreateSenderFaultException(sourceType);

        //    if (e.InnerExceptions.Count == 1)

        //        return faultException;

        //    return faultException.Code.CreateDetailedFaultExceptions(faultException.Action, e.InnerExceptions, sourceType);
        //}


        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/> with a Receiver fault code.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        //public static FaultException CreateReceiverFaultException(this AggregateException e, Type sourceType)
        //{
        //    FaultException faultException = e.InnerException.CreateReceiverFaultException(sourceType);

        //    if (e.InnerExceptions.Count == 1)

        //        return faultException;

        //    return faultException.Code.CreateDetailedFaultExceptions(faultException.Action, e.InnerExceptions, sourceType);
        //}

        private static FaultException CreateDetailedFaultExceptions(this FaultCode faultCode, string action, IEnumerable<Exception> exceptions, Type sourceType)
        {
            List<FaultDetail> reasons = new List<FaultDetail>();

            foreach (Exception additionalException in exceptions)
            {
                FaultCode additionalCode = additionalException.CreateFaultCode(out FaultReason additionalReason);

                reasons.Add(new FaultDetail(new FaultDetail.DetailCode(additionalCode), additionalReason.GetMatchingTranslation().Text));
            }

            FaultDetail faultDetail = new FaultDetail(FaultDetail.AGGREGATED_INNER_DETAILS_DETAIL_CODE, FaultDetail.AGGREGATED_INNER_DETAILS_DETAIL_CODE_MESSAGE, reasons.ToArray());

            return faultCode.CreateDetailFaultException(AGGREGATE_FAULT_EXCEPTION_REASON, faultDetail, action, sourceType);

            //return new FaultException(new FaultReason(reasons.GroupBy(r => r.XmlLang, (xmlLang, e) => new FaultReasonText(String.Join("  ", e.Select(t => t.Text).ToArray()), xmlLang))), faultCode, FaultExceptionHelper.DefaultAction);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sourceType"></param>
        /// <param name="isIntermediary"></param>
        /// <returns></returns>
        public static FaultException CreateFaultException(this Exception e, Type sourceType, bool isIntermediary = false)
        {
            FaultReason faultReason;

            if (isIntermediary)
            {
                if (e is FaultException)
                {
                    FaultException faultException = e as FaultException;

                    return ServerErrorCode.IntermediaryFaulted.Code.AttachSubCode(faultException.Code).CreateFaultException(faultException.Reason, sourceType);
                }
                else
                {
                    return ServerErrorCode.IntermediaryFaulted.Code.AttachSubCode(e.CreateFaultCode(out faultReason)).CreateFaultException(faultReason, sourceType);
                }
            }

            if (e is FaultException)

                return e as FaultException;

            FaultCode subCode = e.CreateFaultCode(out faultReason);

            if (subCode == ClientErrorCode.SubCode)

                return CreateSenderFaultCode(subCode).CreateFaultException(faultReason, sourceType);

            else

                return CreateReceiverFaultCode(subCode).CreateFaultException(faultReason, sourceType);
        }

        public static FaultException CreateSenderFaultException(this Exception e, Type sourceType)
        {
            FaultReason reason;

            FaultCode subCode = e.CreateFaultCode(out reason);

            return CreateSenderFaultCode(subCode).CreateFaultException(reason, sourceType);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static FaultException CreateReceiverFaultException(this Exception e, Type sourceType)
        {
            FaultReason faultReason;

            FaultCode subCode = e.CreateFaultCode(out faultReason);

            return CreateReceiverFaultCode(subCode).CreateFaultException(faultReason, sourceType);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="subCode"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        /// Creates an instance of a <see cref="FaultException"/> with a Sender <see cref="FaultCode"/> and attached <paramref name="subCode"/>.  
        /// A <see cref="FaultCode"/> derived from <paramref name="e"/> will be attached to the latter.
        public static FaultException CreateSenderFaultException(this Exception e, FaultCode subCode, Type sourceType)
        {
            FaultReason faultReason;

            subCode.AttachSubCode(e.CreateFaultCode(out faultReason));

            return CreateSenderFaultCode(subCode).CreateFaultException(faultReason, sourceType);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="subCode"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        /// <remarks>
        /// Creates an instance of a <see cref="FaultException"/> with a Receiver <see cref="FaultCode"/> and attached <paramref name="subCode"/>.  
        /// A <see cref="FaultCode"/> derived from <paramref name="e"/> will be attached to the latter.
        /// </remarks>
        public static FaultException CreateReceiverFaultException(this Exception e, FaultCode subCode, Type sourceType)
        {
            FaultReason faultReason;

            subCode.AttachSubCode(e.CreateFaultCode(out faultReason));

            return CreateReceiverFaultCode(subCode).CreateFaultException(faultReason, sourceType);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="code"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        /// <remarks>
        /// Creates an instance of a <see cref="FaultException"/> with a Receiver <paramref name="e"/> of <see cref="Exception"/> and <paramref name="faultCode"/>.  
        /// A <see cref="FaultCode"/> derived from <paramref name="e"/> will be attached to <paramref name="faultCode"/>.
        /// </remarks>
        public static FaultException CreateFaultException(this Exception e, FaultCode faultCode, Type sourceType)
        {
            faultCode = faultCode.AttachSubCode(e.CreateFaultCode(out FaultReason faultReason));

            return faultCode.CreateFaultException(faultReason, sourceType);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="subCode"></param>
        /// <param name="faultReason"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>

        public static FaultException CreateSenderFaultException(this FaultCode subCode, FaultReason faultReason, Type sourceType)
        {
            return CreateSenderFaultCode(subCode).CreateFaultException(faultReason, sourceType);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="subCode"></param>
        /// <param name="faultReason"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static FaultException CreateSenderFaultException<TDetail>(this FaultCode subCode, FaultReason faultReason, TDetail detail, Type sourceType)
        {
            return CreateSenderFaultCode(subCode).CreateFaultException(faultReason, sourceType);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="subCode"></param>
        /// <param name="faultReason"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static FaultException CreateReceiverFaultException(this FaultCode subCode, FaultReason faultReason, Type sourceType)
        {
            return CreateReceiverFaultCode(subCode).CreateFaultException(faultReason, sourceType);
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="faultCode"></param>
        /// <param name="faultReason"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static FaultException CreateFaultException(this FaultCode faultCode, FaultReason faultReason, Type sourceType)
        {
            FaultException faultException;

            if (faultReason == null)

                faultException = faultCode.CreateFaultException();

            else

                faultException = faultReason.CreateFaultException(faultCode);

            if (sourceType != null)

                faultException.Source = sourceType.FullName;

            return faultException;
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <typeparam name="DetailType"></typeparam>
        /// <param name="subCode"></param>
        /// <param name="faultReason"></param>
        /// <param name="faultDetail"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static FaultException<DetailType> CreateSenderDetailFaultException<DetailType>(this FaultCode subCode, FaultReason faultReason, DetailType faultDetail, Type sourceType)
        {
            FaultException<DetailType> faultException =
                faultDetail.CreateFaultException(CreateSenderFaultCode(subCode), faultReason);

            faultException.Source = string.Format("{0}.Exceptions", sourceType.FullName);

            return faultException;
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <typeparam name="DetailType"></typeparam>
        /// <param name="subCode"></param>
        /// <param name="faultReason"></param>
        /// <param name="faultDetail"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static FaultException<DetailType> CreateReceiverDetailFaultException<DetailType>(this FaultCode subCode, FaultReason faultReason, DetailType faultDetail, Type sourceType)
        {
            FaultException<DetailType> faultException =
                faultDetail.CreateFaultException(CreateReceiverFaultCode(subCode), faultReason);

            faultException.Source = string.Format("{0}.Exceptions", sourceType.FullName);

            return faultException;
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <typeparam name="DetailType"></typeparam>
        /// <param name="faultCode"></param>
        /// <param name="faultReason"></param>
        /// <param name="faultDetail"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static FaultException<DetailType> CreateDetailFaultException<DetailType>(this FaultCode faultCode, FaultReason faultReason, DetailType faultDetail, Type sourceType)
        {
            FaultException<DetailType> faultException =
                faultDetail.CreateFaultException(faultCode, faultReason);

            faultException.Source = string.Format("{0}.Exceptions", sourceType.FullName);

            return faultException;
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <typeparam name="DetailType"></typeparam>
        /// <param name="faultCode"></param>
        /// <param name="faultReason"></param>
        /// <param name="faultDetail"></param>
        /// <param name="action"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static FaultException<DetailType> CreateDetailFaultException<DetailType>(this FaultCode faultCode, FaultReason faultReason, DetailType faultDetail, string action, Type sourceType)
        {
            FaultException<DetailType> faultException =
                faultDetail.CreateFaultException(faultCode, faultReason, action);

            faultException.Source = string.Format("{0}.Exceptions", sourceType.FullName);

            return faultException;
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultException"/>.
        /// </summary>
        /// <param name="faults"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static FaultException CreateReceiverFaultException(this IEnumerable<MessageFault> faults, Type sourceType)
        {
            FaultException faultException;

            if (faults.Count() == 1)

                faultException = faults.FirstOrDefault().CreateFaultException();

            else
            {
                List<FaultDetail> faultDetails = new List<FaultDetail>();

                foreach (MessageFault messageFault in faults)
                {

                    faultDetails.Add(messageFault.ToFaultDetail());
                }

                faultException =
                    new FaultDetail(
                        CreateReceiverFaultCode(ServerErrorCode.ServiceFaulted.Code),
                        "MessageFault(s) caught by receiver.",
                        faultDetails.ToArray()).CreateFaultException();
            }

            faultException.Source = string.Format("{0}.Exceptions", sourceType.FullName);

            return faultException;
        }

        /// <summary>
        /// Creates an instance of a <see cref="FaultDetail"/> from an <see cref="Exception.Data"/> dictionary.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static FaultDetail ToFaultDetail(this IDictionary data)
        {
            List<FaultDetail> faultDetails = new List<FaultDetail>();

            IEnumerator dataEnumerator = data.GetEnumerator();

            while (dataEnumerator.MoveNext())
            {
                DictionaryEntry entry = (DictionaryEntry)dataEnumerator.Current;

                faultDetails.Add(
                    new FaultDetail(new FaultCode(entry.Key.ToString()), entry.Value.ToString()));
            }

            FaultDetail faultDetail =
                new FaultDetail(
                    FaultDetail.AGGREGATED_INNER_DETAILS_DETAIL_CODE,
                    FaultDetail.AGGREGATED_INNER_DETAILS_DETAIL_CODE_MESSAGE,
                    faultDetails.ToArray());

            return faultDetail;
        }

        public static FaultDetail ToFaultDetail(this MessageFault messageFault)
        {
            FaultDetail faultDetail =
                new FaultDetail(
                    new FaultDetail.DetailCode(messageFault.Code),
                    messageFault.Reason.ToString());

            return faultDetail;
        }

        public static FaultDetail ToFaultDetail(this FaultException faultException)
        {
            if (faultException is FaultException<FaultDetail>)

                return
                    new FaultDetail(
                        new FaultDetail.DetailCode(faultException.Code),
                        faultException.Reason.ToString(),
                        new FaultDetail[] { (faultException as FaultException<FaultDetail>).Detail });

            else if (faultException is FaultException<FaultDetail.Tag>)

                return
                    new FaultDetail(
                        new FaultDetail.DetailCode(faultException.Code),
                        faultException.Reason.ToString(),
                        (faultException as FaultException<FaultDetail.Tag>).Detail);
            else

                return
                    new FaultDetail(
                        new FaultDetail.DetailCode(faultException.Code),
                        faultException.Reason.ToString());
        }

        public static FaultException ToMergedFaultException(this IEnumerable<FaultException> faultExceptions, Type sourceType)
        {
            if (faultExceptions.All(f => f.Code.IsSenderFault))
            {
                return ClientErrorCode.SubCode.CreateSenderDetailFaultException(
                    new FaultReason("Aggregate sender errors - see Fault Detail."),
                    faultExceptions.ToFaultDetail(),
                    sourceType);
            }
            if (faultExceptions.All(f => f.Code.IsSenderFault))
            {
                return ServerErrorCode.ServiceFaulted.Code.CreateReceiverDetailFaultException(
                    new FaultReason("Aggregate service errors - see Fault Detail."),
                    faultExceptions.ToFaultDetail(),
                    sourceType);
            }
            return ServerErrorCode.ServiceFaulted.Code.CreateReceiverDetailFaultException(
                new FaultReason("Aggregate client and service errors - see Fault Detail."),
                faultExceptions.ToFaultDetail(),
                sourceType);
        }

        public static FaultDetail ToFaultDetail(this IEnumerable<FaultException> faultExceptions)
        {
            List<FaultDetail> faultDetails = new List<FaultDetail>();

            foreach (FaultException faultException in faultExceptions)
            {
                faultDetails.Add(faultException.ToFaultDetail());
            }

            return
                new FaultDetail(
                    FaultDetail.AGGREGATED_INNER_DETAILS_DETAIL_CODE,
                    FaultDetail.AGGREGATED_INNER_DETAILS_DETAIL_CODE_MESSAGE,
                    faultDetails.ToArray());
        }

        public static string GetCodeString(this FaultCode faultcode)
        {
            StringBuilder sb = new StringBuilder();

            BuildCodeString(faultcode, sb);

            return sb.ToString();
        }

        private static void BuildCodeString(FaultCode code, StringBuilder sb)
        {
            sb.AppendFormat("{{{0}}}{1}", code.Namespace, code.Name);

            if (code.SubCode != null)
            {
                sb.Append(" => ");

                BuildCodeString(code.SubCode, sb);
            }
        }
    }
}
