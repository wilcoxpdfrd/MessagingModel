using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public struct MessagingBindingConstants
    {
        public const string SOAP_ENV_PREFIX = "soap-env";
        public const string SOAP12_ENV_PREFIX = "soap12-env";
        public const string NONE_ENV_PREFIX = "none-env";

        public const string SOAP_ENV_NAMESPACE = "http://schemas.xmlsoap.org/soap/envelope/";
        public const string SOAP12_ENV_NAMESPACE = "http://www.w3.org/2003/05/soap-envelope";
        public const string NONE_ENV_NAMESPACE = "http://schemas.microsoft.com/ws/2005/05/envelope/none";

        public const string HTTP_BINDING_PREFIX = "http";
        public const string MIME_BINDING_PREFIX = "mime";
        public const string SOAP_BINDING_PREFIX = "soap";
        public const string SOAP12_BINDING_PREFIX = "soap12";

        public const string HTTP_BINDING_NAMESPACE = "http://schemas.xmlsoap.org/wsdl/http/";
        public const string MIME_CONTENT_BINDING_NAMESPACE = "http://schemas.xmlsoap.org/wsdl/mime/";
        public const string SOAP_BINDING_NAMESPACE = "http://schemas.xmlsoap.org/wsdl/soap/";
        public const string SOAP12_BINDING_NAMESPACE = "http://schemas.xmlsoap.org/wsdl/soap12/";
    }
}
