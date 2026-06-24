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
    internal static class HttpRequestMessagePropertyExtensions
    {
        private static (string, string, bool)[] GetSoapActionSingletonParameter(this HttpRequestMessageProperty httpRequestMessageProperty)
        {
            int i = Array.IndexOf(httpRequestMessageProperty.Headers.AllKeys, HttpHeaderNames.SOAPAction);

            if (i < 0)

                return new (string, string, bool)[0];

            return new (string, string, bool)[] { (MediaContentType.PARAMETER_KEY_SOAP_ACTION, httpRequestMessageProperty.Headers[i], true) };
        }

        public static bool TryGetMediaContentType(this HttpRequestMessageProperty httpRequestMessageProperty, out MediaContentType mediaContentType)
        {
            if (httpRequestMessageProperty != null && httpRequestMessageProperty.Headers.TryGetHeaderValues(HttpHeaderNames.ContentType, out string[] values))
            {
                mediaContentType = 
                    new MediaContentType(values.First(), httpRequestMessageProperty.GetSoapActionSingletonParameter());

                return true;
            }

            mediaContentType = null;

            return false;
        }

        /// <summary>
        /// Gets the action by inspecting the Content-Type action parameter.  
        /// Returns null if the transfer format does not use WS-Addressing.
        /// </summary>
        /// <param name="httpRequestMessageProperty"></param>
        /// <returns></returns>
        public static string GetAction(this HttpRequestMessageProperty httpRequestMessageProperty)
        {
            if (httpRequestMessageProperty.TryGetMediaContentType(out MediaContentType mediaContentType))
            {
                switch (mediaContentType.TransferFormat)
                {
                    case MessageEncodingFormat.Soap11WSAddressing10:
                    case MessageEncodingFormat.Soap11WSAddressingAugust2004:

                        return mediaContentType.Parameters[MediaContentType.PARAMETER_KEY_SOAP_ACTION];

                    case MessageEncodingFormat.Soap11:

                        return null;

                    case MessageEncodingFormat.Soap12WSAddressing10:
                    case MessageEncodingFormat.Soap12WSAddressingAugust2004:

                        return mediaContentType.Parameters[MediaContentType.PARAMETER_KEY_SOAP_ACTION];

                    case MessageEncodingFormat.Soap12:

                        return null;

                    default:

                        return null;
                }
            }

            return null;
        }
    }
}
