using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.Core.ServiceModel.Description.Swagger
{
    internal struct SwaggerTokens
    {
        public const String SWAGGER = "swagger";
        public const String VERSION_20 = "2.0";
        public const String TAGS = "tags";
        public const String TAG = "tag";
        public const String SCHEMES = "schemes";
        public const String INFO = "info";
        public const String TITLE = "title";
        public const String SUMMARY = "summary";
        public const String DESCRIPTION = "description";
        public const String TERMS_OF_SERVICE = "termsOfService";
        public const String CONTACT = "contact";
        public const String NAME = "name";
        public const String URL = "url";
        public const String EMAIL = "email";
        public const String LICENSE = "license";
        public const String VERSION = "version";
        public const String EXTERNAL_DOCS = "externalDocs";
        public const String DEFINITIONS = "definitions";
        public const String HOST = "host";
        public const String BASE_PATH = "basePath";
        public const String PATHS = "paths";
        public const String OPERATION_ID = "operationId";
        public const String CONSUMES = "consumes";
        public const String PRODUCES = "produces";
        public const String ITEM = "item";
        public const String PARAMETERS = "parameters";
        public const String IN = "in";
        public const String IN_QUERY = "query";
        public const String IN_HEADER = "header";
        public const String IN_PATH = "path";
        public const String IN_FORM_DATA = "formData";
        public const String IN_BODY = "body";
        public const String ALLOW_EMPTY_VALUE = "allowEmptyValue";
        public const String REQUIRED = "required";
        public const String TYPE = "type";
        public const String TYPE_OBJECT = "object";
        public const String TYPE_ARRAY = "array";
        public const String SCHEMA = "schema";
        public const String QUERY = "query";
        public const String HEADER = "header";
        public const String FORM_DATA = "formData";
        public const String RESPONSES = "responses";
        public const String CONTENT_NAME_SUFFIX = "Content";
        public const String DEFAULT = "default";
        public const String HEADERS = "headers";
        public const String EXAMPLES = "examples";
        public const String ALL_OF = "allOf";
    }
}
