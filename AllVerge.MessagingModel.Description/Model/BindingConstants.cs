using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.Description.Model
{
    using AllVerge.DataModel.Primitives;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;

    public class BindingConstants
    {
        public const string TNS_PREFIX = "tns";
        public const string XML_NS_PREFIX = "xmlns";
        public const string XML_SCHEMA_PREFIX = "xsd";
        public const string XML_SCHEMA_INSTANCE_PREFIX = "xsi";

        public const string XML_NS_NAMESPACE = "http://www.w3.org/2000/xmlns/";
        public const string XML_SCHEMA_NAMESPACE = "http://www.w3.org/2001/XMLSchema";
        public const string XML_SCHEMA_INSTANCE_NAMESPACE = "http://www.w3.org/2001/XMLSchema-instance";

        public const string SOAP_OPERATION_BINDING_ACTION_ELEMENT_NAME = "SoapAction"; // Move to wsdl reader
        public const string SOAP_OPERATION_BINDING_ELEMENT_ACTION_REQUIRED_PROPERTY_NAME = "SoapActionRequired"; // Move to wsdl reader
        public const string SOAP_OPERATION_BINDING_ELEMENT_STYLE_PROPERTY_NAME = "Style"; // Move to wsdl reader
        public const string SOAP_BINDING_ELEMENT_TRANSPORT_PROPERTY_NAME = "Transport"; // Move to wsdl reader
        public const string SOAP_FAULT_DETAIL_ELEMENT_NAME = "Detail"; // Move to wsdl reader
        public const string HTTP_BINDING_ELEMENT_LOCATION_PROPERTY_NAME = "Location"; // Move to wsdl reader
        public const string HTTP_BINDING_ELEMENT_VERB_PROPERTY_NAME = "Verb"; // Move to wsdl reader

        public const string BINDING_PROPERTY_LOCAL_NAME = "binding";
        public const string ADDRESS_PROPERTY_LOCAL_NAME = "address";
        public const string OPERATION_PROPERTY_LOCAL_NAME = "operation";

        public const string SOAP_ACTION_BINDING_ATTRIBUTE_NAME = "soapAction";
        public const string SOAP_ACTION_REQUIRED_BINDING_ATTRIBUTE_NAME = "soapActionRequired";
        public const string SOAP_BINDING_STYLE_ATTRIBUTE_NAME = "style";
        public const string SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_DOCUMENT = "document";
        public const string SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_RPC = "rpc";

        public const string BINDING_TRANSPORT_ATTRIBUTE_NAME = "transport";
        public const string BINDING_TRANSPORT_ATTIBUTE_VALUE_HTTP = "http://schemas.xmlsoap.org/soap/http";

        public const string BINDING_LOCATION_ATTRIBUTE_NAME = "location";
        public const string BINDING_VERB_ATTRIBUTE_NAME = "verb";

        public const string MESSAGE_BINDING_MESSAGE_ATTRIBUTE_NAME = "message";
        public const string MESSAGE_BINDING_PARTS_ATTRIBUTE_NAME = "parts"; 
        public const string MESSAGE_BINDING_PART_ATTRIBUTE_NAME = "part";
        public const string MESSAGE_BINDING_USE_ATTRIBUTE_NAME = "use";
        public const string MESSAGE_BINDING_ENCODINGSTYLE_ATTRIBUTE_NAME = "encodingStyle";
        public const string MESSAGE_BINDING_NAME_ATTRIBUTE_NAME = "name";
        public const string MESSAGE_BINDING_NAMESPACE_ATTRIBUTE_NAME = "namespace";
        public const string MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME = "type";

        public static readonly QualifiedName HTTP_BINDING_PROPERTY_NAME;
        public static readonly QualifiedName HTTP_BINDING_ADDRESS_PROPERTY_NAME;
        public static readonly QualifiedName HTTP_BINDING_OPERATION_PROPERTY_NAME;
        public static readonly QualifiedName HTTP_BINDING_STATUS_CODE_PROPERTY_NAME;
        public static readonly QualifiedName HTTP_BINDING_HEADER_PROPERTY_NAME;
        public static readonly QualifiedName HTTP_BINDING_URLREPLACEMENT_PROPERTY_NAME;
        public static readonly QualifiedName HTTP_URL_BINDING_ENCODED_PROPERTY_NAME;
        public static readonly QualifiedName HTTP_BINDING_MATRIX_PROPERTY_NAME;
        public static readonly QualifiedName HTTP_BINDING_PLAIN_PROPERTY_NAME;
        public static readonly QualifiedName MIME_MULTIPART_RELATED_BINDING_PROPERTY_NAME;
        public static readonly QualifiedName MIME_XML_BINDING_PROPERTY_NAME;
        public static readonly QualifiedName MIME_CONTENT_BINDING_PROPERTY_NAME;

        public static readonly QualifiedName SOAP_BINDING_PROPERTY_NAME;
        public static readonly QualifiedName SOAP12_BINDING_PROPERTY_NAME;
        public static readonly QualifiedName SOAP_BINDING_OPERATION_PROPERTY_NAME;
        public static readonly QualifiedName SOAP_12_BINDING_OPERATION_PROPERTY_NAME;
        public static readonly QualifiedName SOAP_BINDING_STATUS_CODE_PROPERTY_NAME;
        public static readonly QualifiedName SOAP12_BINDING_STATUS_CODE_PROPERTY_NAME;
        public static readonly QualifiedName SOAP_BINDING_BODY_PROPERTY_NAME;
        public static readonly QualifiedName SOAP_BINDING_FAULT_PROPERTY_NAME;
        public static readonly QualifiedName SOAP12_BINDING_BODY_PROPERTY_NAME;
        public static readonly QualifiedName SOAP12_BINDING_FAULT_PROPERTY_NAME;
        public static readonly QualifiedName SOAP_BINDING_HEADER_PROPERTY_NAME;
        public static readonly QualifiedName SOAP12_BINDING_HEADER_PROPERTY_NAME;
        public static readonly QualifiedName SOAP_BINDING_HEADER_FAULT_PROPERTY_NAME;

        public const string URLREPLACEMENT_PROPERTY_LOCAL_NAME = "urlReplacement";
        public const string URLENCODED_PROPERTY_LOCAL_NAME = "urlEncoded";
        public const string MATRIXENCODED_PROPERTY_LOCAL_NAME = "x-matrixEncoded";
        public const string PLAINENCODED_PROPERTY_LOCAL_NAME = "x-plainEncoded";
        public const string HEADER_PROPERTY_LOCAL_NAME = "x-header";
        public const string STATUSCODE_PROPERTY_LOCAL_NAME = "x-statusCode";

        public const string MIMEXML_PROPERTY_LOCAL_NAME = "mimeXml";
        public const string MULTIPARTRELATED_PROPERTY_LOCAL_NAME = "multipartRelated";
        public const string CONTENT_PROPERTY_LOCAL_NAME = "content";

        public const string MESSAGE_BODY_PROPERTY_LOCAL_NAME = "body";
        public const string MESSAGE_FAULT_PROPERTY_LOCAL_NAME = "fault";
        public const string MESSAGE_HEADER_PROPERTY_LOCAL_NAME = "header";
        public const string MESSAGE_HEADERFAULT_PROPERTY_LOCAL_NAME = "headerfault";

        static BindingConstants()
        {
            InitializeBindingConstants();

            HTTP_BINDING_PROPERTY_NAME = $"{MessagingBindingConstants.HTTP_BINDING_PREFIX}~{BINDING_PROPERTY_LOCAL_NAME}";
            HTTP_BINDING_ADDRESS_PROPERTY_NAME = $"{MessagingBindingConstants.HTTP_BINDING_PREFIX}~{ADDRESS_PROPERTY_LOCAL_NAME}";
            HTTP_BINDING_OPERATION_PROPERTY_NAME = $"{MessagingBindingConstants.HTTP_BINDING_PREFIX}~{OPERATION_PROPERTY_LOCAL_NAME}";
            HTTP_BINDING_URLREPLACEMENT_PROPERTY_NAME = $"{MessagingBindingConstants.HTTP_BINDING_PREFIX}~{URLREPLACEMENT_PROPERTY_LOCAL_NAME}";
            HTTP_URL_BINDING_ENCODED_PROPERTY_NAME = $"{MessagingBindingConstants.HTTP_BINDING_PREFIX}~{URLENCODED_PROPERTY_LOCAL_NAME}";
            HTTP_BINDING_MATRIX_PROPERTY_NAME = $"{MessagingBindingConstants.HTTP_BINDING_PREFIX}~{MATRIXENCODED_PROPERTY_LOCAL_NAME}";
            HTTP_BINDING_PLAIN_PROPERTY_NAME = $"{MessagingBindingConstants.HTTP_BINDING_PREFIX}~{PLAINENCODED_PROPERTY_LOCAL_NAME}";
            HTTP_BINDING_HEADER_PROPERTY_NAME = $"{MessagingBindingConstants.HTTP_BINDING_PREFIX}~{HEADER_PROPERTY_LOCAL_NAME}";
            HTTP_BINDING_STATUS_CODE_PROPERTY_NAME = $"{MessagingBindingConstants.HTTP_BINDING_PREFIX}~{STATUSCODE_PROPERTY_LOCAL_NAME}";

            MIME_XML_BINDING_PROPERTY_NAME = $"{MessagingBindingConstants.MIME_BINDING_PREFIX}~{MIMEXML_PROPERTY_LOCAL_NAME}";
            MIME_MULTIPART_RELATED_BINDING_PROPERTY_NAME = $"{MessagingBindingConstants.MIME_BINDING_PREFIX}~{MULTIPARTRELATED_PROPERTY_LOCAL_NAME}";
            MIME_CONTENT_BINDING_PROPERTY_NAME = $"{MessagingBindingConstants.MIME_BINDING_PREFIX}~{CONTENT_PROPERTY_LOCAL_NAME}";

            SOAP_BINDING_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP_BINDING_PREFIX}~{BINDING_PROPERTY_LOCAL_NAME}";
            SOAP12_BINDING_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP12_BINDING_PREFIX}~{BINDING_PROPERTY_LOCAL_NAME}";
            SOAP_BINDING_OPERATION_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP_BINDING_PREFIX}~{OPERATION_PROPERTY_LOCAL_NAME}";
            SOAP_12_BINDING_OPERATION_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP12_BINDING_PREFIX}~{OPERATION_PROPERTY_LOCAL_NAME}";
            SOAP_BINDING_STATUS_CODE_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP_BINDING_PREFIX}~{STATUSCODE_PROPERTY_LOCAL_NAME}";
            SOAP12_BINDING_STATUS_CODE_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP12_BINDING_PREFIX}~{STATUSCODE_PROPERTY_LOCAL_NAME}";
            SOAP_BINDING_BODY_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP_BINDING_PREFIX}~{MESSAGE_BODY_PROPERTY_LOCAL_NAME}";
            SOAP_BINDING_FAULT_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP_BINDING_PREFIX}~{MESSAGE_FAULT_PROPERTY_LOCAL_NAME}";
            SOAP12_BINDING_BODY_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP12_BINDING_PREFIX}~{MESSAGE_BODY_PROPERTY_LOCAL_NAME}";
            SOAP12_BINDING_FAULT_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP12_BINDING_PREFIX}~{MESSAGE_FAULT_PROPERTY_LOCAL_NAME}";
            SOAP_BINDING_HEADER_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP_BINDING_PREFIX}~{MESSAGE_HEADER_PROPERTY_LOCAL_NAME}";
            SOAP12_BINDING_HEADER_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP12_BINDING_PREFIX}~{MESSAGE_HEADER_PROPERTY_LOCAL_NAME}";
            SOAP_BINDING_HEADER_FAULT_PROPERTY_NAME = $"{MessagingBindingConstants.SOAP_BINDING_PREFIX}~{MESSAGE_HEADERFAULT_PROPERTY_LOCAL_NAME}";
        }

        internal static void InitializeBindingConstants()
        {
            QualifiedName.TryAddGlobalNamespace(BindingConstants.XML_SCHEMA_PREFIX, BindingConstants.XML_SCHEMA_NAMESPACE);
            QualifiedName.TryAddGlobalNamespace(MessagingBindingConstants.HTTP_BINDING_PREFIX, MessagingBindingConstants.HTTP_BINDING_NAMESPACE);
            QualifiedName.TryAddGlobalNamespace(MessagingBindingConstants.MIME_BINDING_PREFIX, MessagingBindingConstants.MIME_CONTENT_BINDING_NAMESPACE);
            QualifiedName.TryAddGlobalNamespace(MessagingBindingConstants.SOAP_BINDING_PREFIX, MessagingBindingConstants.SOAP_BINDING_NAMESPACE);
            QualifiedName.TryAddGlobalNamespace(MessagingBindingConstants.SOAP12_BINDING_PREFIX, MessagingBindingConstants.SOAP12_BINDING_NAMESPACE);
            QualifiedName.TryAddGlobalNamespace(MessagingBindingConstants.SOAP_ENV_PREFIX, MessagingBindingConstants.SOAP_ENV_NAMESPACE);
            QualifiedName.TryAddGlobalNamespace(MessagingBindingConstants.SOAP12_ENV_PREFIX, MessagingBindingConstants.SOAP12_ENV_NAMESPACE);
            QualifiedName.TryAddGlobalNamespace(MessagingBindingConstants.NONE_ENV_PREFIX, MessagingBindingConstants.NONE_ENV_NAMESPACE);
        }
    }
}
