using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.Core.ServiceModel.Description.Raml
{
    internal struct RamlTokens
    {
        public const String VERSION_LINE_RAML_PREAMBLE = "%RAML ";
        public const String VERSION_10 = "1.0";
        public static readonly String VERSION_LINE_RAML_1_0_PREAMBLE = VERSION_LINE_RAML_PREAMBLE + VERSION_10;
        public static readonly int VERSION_LINE_RAML_1_0_PREAMBLE_LENGTH = VERSION_LINE_RAML_1_0_PREAMBLE.Length;

        public const String FRAGMENT_IDENTIFIER_DOCUMENTATION_ITEM = "DocumentationItem";
        public const String FRAGMENT_IDENTIFIER_DATA_TYPE = "DataType";
        public const String FRAGMENT_IDENTIFIER_NAMED_EXAMPLE = "NamedExample";
        public const String FRAGMENT_IDENTIFIER_RESOURCE_TYPE = "ResourceType";
        public const String FRAGMENT_IDENTIFIER_TRAIT = "Trait";
        public const String FRAGMENT_IDENTIFIER_LIBRARY = "Library";
        public const String FRAGMENT_IDENTIFIER_OVERLAY = "Overlay";
        public const String FRAGMENT_IDENTIFIER_EXTENSION = "Extension";
        public const String FRAGMENT_IDENTIFIER_SECURITY_SCHEME = "SecurityScheme";
        public const String ANNOTATION_TYPES = "annotationTypes";
        public const String USES = "uses";
        public const String TYPES = "types";
        public const String TYPE = "type";
        public const String DEFAULT = "default";
        public const String RESOURCE_TYPES = "resourceTypes";
        public const String RESOURCE_PATH_PARAMETER = "<<resourcePath>>";
        public const String RESOURCE_PATH_NAME_PARAMETER = "<<resourcePathName>>";
        public const String DISPLAY_NAME = "displayName";
        public const String DESCRIPTION = "description";
        public const String DOCUMENTATION = "documentation";
        public const String TITLE = "title";
        public const String COMMENT = "comment";
        public const String VERSION = "version";
        public const String CONTENT = "content";
        public const String USAGE = "usage";
        public const String TRAITS = "traits";
        public const String SECURITY_SCHEMES = "securitySchemes";
        public const String SECURED_BY = "securedBy";
        public const String BASE_URI = "baseUri";
        public const String BASE_URI_PARAMETERS = "baseUriParameters";
        public const String BASE_URI_PARAMETER_NAME_VERSION = "version";
        public const String URI_PARAMETERS = "uriParameters";
        public const String HEADERS = "headers";
        public const String QUERY_STRING = "queryString";
        public const String QUERY_PARAMETERS = "queryParameters";
        public const String PROTOCOLS = "protocols";
        public const String MEDIA_TYPE = "mediaType";
        public const String ALLOWED_TARGETS = "allowedTargets";
        public const String INCLUDE_PREFIX = "!include ";
        public const String BODY = "body";
        public const String RESPONSES = "responses";

        public static readonly int INCLUDE_PREFIX_LENGTH = INCLUDE_PREFIX.Length;
    }
}
