using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using static AllVerge.MessagingModel.MessagingFoundation.Http.HttpStatusExtensions;

namespace AllVerge.MessagingModel.MessagingFoundation.Http
{
    internal static class HttpStatusExtensions
    {
        private static readonly string[][] HTTP_STATUS_DESCRIPTIONS = new string[][]
        {
            null,
            new string[]
            {
                "Continue",
                "Switching Protocols",
                "Processing"
            },
            new string[]
            {
                "OK",
                "Created",
                "Accepted",
                "Non-Authoritative Information",
                "No Content",
                "Reset Content",
                "Partial Content",
                "Multi-Status"
            },
            new string[]
            {
                "Multiple Choices",
                "Moved Permanently",
                "Found",
                "See Other",
                "Not Modified",
                "Use Proxy",
                null,
                "Temporary Redirect"
            },
            new string[]
            {
                "Bad Request",
                "Unauthorized",
                "Payment Required",
                "Forbidden",
                "Not Found",
                "Method Not Allowed",
                "Not Acceptable",
                "Proxy Authentication Required",
                "Request Timeout",
                "Conflict",
                "Gone",
                "Length Required",
                "Precondition Failed",
                "Request Entity Too Large",
                "Request-Uri Too Long",
                "Unsupported Media Type",
                "Requested Range Not Satisfiable",
                "Expectation Failed",
                null,
                null,
                null,
                null,
                "Unprocessable Entity",
                "Locked",
                "Failed Dependency",
                null,
                "Upgrade Required"
            },
            new string[]
            {
                "Internal Server Error",
                "Not Implemented",
                "Bad Gateway",
                "Service Unavailable",
                "Gateway Timeout",
                "Http Version Not Supported",
                null,
                "Insufficient Storage"
            }
        };

        private static readonly HttpStatusCode[] httpStatusCodes = new HttpStatusCode[] 
        {
            HttpStatusCode.Continue,
            HttpStatusCode.SwitchingProtocols,
            HttpStatusCode.OK,
            HttpStatusCode.Created,
            HttpStatusCode.Accepted,
            HttpStatusCode.NonAuthoritativeInformation,
            HttpStatusCode.NoContent,
            HttpStatusCode.ResetContent,
            HttpStatusCode.PartialContent,
            HttpStatusCode.Ambiguous,
            HttpStatusCode.MultipleChoices,
            HttpStatusCode.Moved,
            HttpStatusCode.MovedPermanently,
            HttpStatusCode.Found,
            HttpStatusCode.Redirect,
            HttpStatusCode.RedirectMethod,
            HttpStatusCode.SeeOther,
            HttpStatusCode.NotModified,
            HttpStatusCode.UseProxy,
            HttpStatusCode.Unused,
            HttpStatusCode.RedirectKeepVerb,
            HttpStatusCode.TemporaryRedirect,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.PaymentRequired,
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.MethodNotAllowed,
            HttpStatusCode.NotAcceptable,
            HttpStatusCode.ProxyAuthenticationRequired,
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.Conflict,
            HttpStatusCode.Gone,
            HttpStatusCode.LengthRequired,
            HttpStatusCode.PreconditionFailed,
            HttpStatusCode.RequestEntityTooLarge,
            HttpStatusCode.RequestUriTooLong,
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.RequestedRangeNotSatisfiable,
            HttpStatusCode.ExpectationFailed,
            HttpStatusCode.UpgradeRequired,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.NotImplemented,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.HttpVersionNotSupported
        };

        public static bool isHttpStatusCode(HttpStatusCode statusCode)
        {
            return httpStatusCodes.Contains(statusCode);
        }

        public struct ExtendedHttpStatusCode
        {
            private HttpStatusCode statusCode;

            public override bool Equals(object obj)
            {
                return obj is ExtendedHttpStatusCode code &&
                       statusCode == code.statusCode;
            }

            public override int GetHashCode()
            {
                return -1996992690 + statusCode.GetHashCode();
            }

            public static ExtendedHttpStatusCode Processing = new ExtendedHttpStatusCode() { statusCode = (HttpStatusCode)102 };
            public static ExtendedHttpStatusCode MultiStatus = new ExtendedHttpStatusCode() { statusCode = (HttpStatusCode)207 };
            public static ExtendedHttpStatusCode UnprocessableEntity = new ExtendedHttpStatusCode() { statusCode = (HttpStatusCode)422 };
            public static ExtendedHttpStatusCode Locked = new ExtendedHttpStatusCode() { statusCode = (HttpStatusCode)423 };
            public static ExtendedHttpStatusCode FailedDependency = new ExtendedHttpStatusCode() { statusCode = (HttpStatusCode)424 };
            public static ExtendedHttpStatusCode InsufficientStorage = new ExtendedHttpStatusCode() { statusCode = (HttpStatusCode)507 };

            public static bool operator ==(ExtendedHttpStatusCode left, ExtendedHttpStatusCode right) => left.statusCode == right.statusCode;
            public static bool operator !=(ExtendedHttpStatusCode left, ExtendedHttpStatusCode right) => left.statusCode != right.statusCode;

            public static implicit operator ExtendedHttpStatusCode(HttpStatusCode value)
            {
                return new ExtendedHttpStatusCode() { statusCode = value };
            }

            public static implicit operator HttpStatusCode (ExtendedHttpStatusCode value)
            {
                return value.statusCode;
            }
        }

        public static string SupplyStatusCodeDescription(this HttpStatusCode httpStatusCode, String httpStatusCodeDescription = null)
        {
            if (!String.IsNullOrWhiteSpace(httpStatusCodeDescription))

                return httpStatusCodeDescription;

            return GetDefaultHttpStatusCodeDescription((int)httpStatusCode);
        }

        public static string GetDefaultHttpStatusCodeDescription(int code)
        {
            if (code >= 100 && code < 600)
            {
                int num = code / 100;
                int num2 = code % 100;
                if (num2 < HTTP_STATUS_DESCRIPTIONS[num].Length)
                {
                    return HTTP_STATUS_DESCRIPTIONS[num][num2];
                }
            }
            return null;
        }
    }
}
