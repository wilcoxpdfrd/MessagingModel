using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Description.Adapters
{
    using AllVerge.SystemPrimitives.Net;

    public class AdapterUtils
    {
        public static string GetConnectionName(string resourceId, string resourcePath)
        {
            if (!String.IsNullOrWhiteSpace(resourceId))

                return resourceId;

            Uri resourcePathUri = new Uri(resourcePath, UriKind.Relative);

            Uri resourceParentPathUri;

            while (resourcePathUri.TryGetParentUri(out resourceParentPathUri))

                resourcePathUri = resourceParentPathUri;

            return resourcePathUri.ToSafeUnescapedHostAndPathString();
        }

        public static String GetActionName(String operationId, String operationVerb, String operationPath)
        {
            if (!String.IsNullOrWhiteSpace(operationId))

                return operationId;

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

            String lastSegment = null;

            IEnumerable<String> segments = operationPath.Split('/').Select(s =>
            {
                String segment = null;

                if (s != null)
                {
                    if (s.StartsWith("{"))

                        segment = "By" + textInfo.ToTitleCase(s.Trim('{', '}'));

                    else if (s.Length > 0)
                    {
                        if (lastSegment == String.Empty)

                            segment = textInfo.ToTitleCase(s);

                        else

                            segment = "At" + textInfo.ToTitleCase(s);
                    }
                }

                if (segment == null)

                    segment = String.Empty;

                lastSegment = segment;

                return segment;
            });

           return operationVerb.ToLower() + String.Concat(segments);
        }

        public static String TryGetHttpStatusCodeName(String responseCode)
        {
            String statusCode;
            String statusCodeName;
            String statusCodeDescription;

            if (TryGetHttpStatusCodeDetails(responseCode, out statusCode, out statusCodeName, out statusCodeDescription))

                return statusCodeName;

            return null;
        }

        public static bool TryGetHttpStatusCodeDetails(String responseCode, out String statusCode, out String statusCodeName, out String statusCodeDescription)
        {
            switch (responseCode)
            {
                case "1XX":
                    statusCode = "1XX";
                    statusCodeName = "Informational";
                    statusCodeDescription = "Informational";
                    break;
                case "100":
                    statusCode = "100";
                    statusCodeName = "Continue";
                    statusCodeDescription = "Continue";
                    break;
                case "101":
                    statusCode = "101";
                    statusCodeName = "SwitchingProtocols";
                    statusCodeDescription = "Switching Protocols";
                    break;
                case "default":
                case "2XX":
                    statusCode = "2XX";
                    statusCodeName = "Successful";
                    statusCodeDescription = "Successful";
                    break;
                case "200":
                    statusCode = "200";
                    statusCodeName = "OK";
                    statusCodeDescription = "OK";
                    break;
                case "201":
                    statusCode = "201";
                    statusCodeName = "Created";
                    statusCodeDescription = "Created";
                    break;
                case "202":
                    statusCode = "202";
                    statusCodeName = "Accepted";
                    statusCodeDescription = "Accepted";
                    break;
                case "203":
                    statusCode = "203";
                    statusCodeName = "NonAuthoritativeInformation";
                    statusCodeDescription = "Non Authoritative Information (Cached Result)";
                    break;
                case "204":
                    statusCode = "204";
                    statusCodeName = "NoContent";
                    statusCodeDescription = "No Content";
                    break;
                case "205":
                    statusCode = "205";
                    statusCodeName = "ResetContent";
                    statusCodeDescription = "Reset Content";
                    break;
                case "206":
                    statusCode = "206";
                    statusCodeName = "PartialContent";
                    statusCodeDescription = "Partial Content";
                    break;
                case "3XX":
                    statusCode = "3XX";
                    statusCodeName = "Redirection";
                    statusCodeDescription = "Redirection";
                    break;
                case "300":
                    statusCode = "300";
                    statusCodeName = "MultipleChoices";
                    statusCodeDescription = "Multiple Choices";
                    break;
                case "301":
                    statusCode = "301";
                    statusCodeName = "MovedPermanently";
                    statusCodeDescription = "Moved Permanently";
                    break;
                case "302":
                    statusCode = "302";
                    statusCodeName = "Found";
                    statusCodeDescription = "Found";
                    break;
                case "303":
                    statusCode = "303";
                    statusCodeName = "SeeOther";
                    statusCodeDescription = "See Other";
                    break;
                case "304":
                    statusCode = "304";
                    statusCodeName = "NotModified";
                    statusCodeDescription = "Not Modified";
                    break;
                case "305":
                    statusCode = "305";
                    statusCodeName = "UseProxy";
                    statusCodeDescription = "Use Proxy";
                    break;
                case "306":
                    statusCode = "306";
                    statusCodeName = "Unused";
                    statusCodeDescription = "Unused (Deprecated Switch Proxy)";
                    break;
                case "307":
                    statusCode = "307";
                    statusCodeName = "TemporaryRedirect";
                    statusCodeDescription = "Temporary Redirect";
                    break;
                case "4XX":
                    statusCode = "4XX";
                    statusCodeName = "ClientError";
                    statusCodeDescription = "Client Error";
                    break;
                case "400":
                    statusCode = "400";
                    statusCodeName = "BadRequest";
                    statusCodeDescription = "Bad Request";
                    break;
                case "401":
                    statusCode = "401";
                    statusCodeName = "Unauthorized";
                    statusCodeDescription = "Unauthorized";
                    break;
                case "402":
                    statusCode = "402";
                    statusCodeName = "PaymentRequired";
                    statusCodeDescription = "Payment Required";
                    break;
                case "403":
                    statusCode = "403";
                    statusCodeName = "Forbidden";
                    statusCodeDescription = "Forbidden";
                    break;
                case "404":
                    statusCode = "404";
                    statusCodeName = "NotFound";
                    statusCodeDescription = "NotFound";
                    break;
                case "405":
                    statusCode = "405";
                    statusCodeName = "MethodNotAllowed";
                    statusCodeDescription = "Method Not Allowed";
                    break;
                case "406":
                    statusCode = "406";
                    statusCodeName = "NotAcceptable";
                    statusCodeDescription = "Not Acceptable";
                    break;
                case "407":
                    statusCode = "407";
                    statusCodeName = "ProxyAuthenticationRequired";
                    statusCodeDescription = "Proxy Authentication Required";
                    break;
                case "408":
                    statusCode = "408";
                    statusCodeName = "RequestTimeout";
                    statusCodeDescription = "Request Timeout";
                    break;
                case "409":
                    statusCode = "409";
                    statusCodeName = "Conflict";
                    statusCodeDescription = "Conflict";
                    break;
                case "410":
                    statusCode = "410";
                    statusCodeName = "Gone";
                    statusCodeDescription = "Gone";
                    break;
                case "411":
                    statusCode = "411";
                    statusCodeName = "LengthRequired";
                    statusCodeDescription = "Length Required";
                    break;
                case "412":
                    statusCode = "412";
                    statusCodeName = "PreconditionFailed";
                    statusCodeDescription = "Precondition Failed";
                    break;
                case "413":
                    statusCode = "413";
                    statusCodeName = "RequestEntityTooLarge";
                    statusCodeDescription = "Request Entity TooLarge";
                    break;
                case "414":
                    statusCode = "414";
                    statusCodeName = "RequestUriTooLong";
                    statusCodeDescription = "Request Uri Too Long";
                    break;
                case "415":
                    statusCode = "415";
                    statusCodeName = "UnsupportedMediaType";
                    statusCodeDescription = "Unsupported Media Type";
                    break;
                case "416":
                    statusCode = "416";
                    statusCodeName = "RequestedRangeNotSatisfiable";
                    statusCodeDescription = "Requested Range Not Satisfiable";
                    break;
                case "417":
                    statusCode = "417";
                    statusCodeName = "ExpectationFailed";
                    statusCodeDescription = "Expectation Failed";
                    break;
                case "428":
                    statusCode = "428";
                    statusCodeName = " PreconditionRequired";
                    statusCodeDescription = " Precondition Required";
                    break;
                case "429":
                    statusCode = "429";
                    statusCodeName = "TooManyRequests";
                    statusCodeDescription = "Too Many Requests ";
                    break;
                case "431":
                    statusCode = "431";
                    statusCodeName = "RequestHeaderFieldsTooLarge";
                    statusCodeDescription = "Request Header Fields Too Large";
                    break;
                case "5XX":
                    statusCode = "5XX";
                    statusCodeDescription = "Server Error";
                    statusCodeName = "ServerError";
                    break;
                case "500":
                    statusCode = "500";
                    statusCodeName = "InternalServerError";
                    statusCodeDescription = "Internal Server Error";
                    break;
                case "501":
                    statusCode = "501";
                    statusCodeName = "NotImplemented";
                    statusCodeDescription = "Not Implemented";
                    break;
                case "502":
                    statusCode = "502";
                    statusCodeName = "BadGateway";
                    statusCodeDescription = "Bad Gateway";
                    break;
                case "503":
                    statusCode = "503";
                    statusCodeName = "ServiceUnavailable";
                    statusCodeDescription = "Service Unavailable";
                    break;
                case "504":
                    statusCode = "504";
                    statusCodeName = "GatewayTimeout";
                    statusCodeDescription = "Gateway Timeout";
                    break;
                case "505":
                    statusCode = "505";
                    statusCodeName = "HttpVersionNotSupported";
                    statusCodeDescription = "Http Version Not Supported";
                    break;
                case "511":
                    statusCode = "511";
                    statusCodeName = "NetworkAuthenticationRequired";
                    statusCodeDescription = "Network Authenticatio nRequired";
                    break;
                default:
                    statusCode = null;
                    statusCodeName = null;
                    statusCodeDescription = null;
                    return false;
            }

            return true;
        }
    }
}
