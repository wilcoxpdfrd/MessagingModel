using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.Core.ServiceModel.Description.Raml
{
    internal struct RamlAnnotationTargets
    {
        /// <summary>
        /// The root of a RAML document
        /// </summary>
        public const string API = "API";
        /// <summary>
        /// An item in the collection of items that is the value of the root-level documentation node
        /// </summary>
        public const string DocumentationItem = "DocumentationItem";
        /// <summary>
        /// A resource (relative URI) node, root-level or nested
        /// </summary>
        public const string Resource = "Resource";
        /// <summary>
        /// A method node
        /// </summary>
        public const string Method = "Method";
        /// <summary>
        /// A declaration of the responses node, whose key is an HTTP status code
        /// </summary>
        public const string Response = "Response";
        /// <summary>
        /// The body node of a method
        /// </summary>
        public const string RequestBody = "RequestBody";
        /// <summary>
        /// The body node of a response
        /// </summary>
        public const string ResponseBody = "ResponseBody";
        /// <summary>
        /// A data type declaration(inline or in a global types collection), header declaration, query parameter declaration, URI parameter declaration, or a property within any of these declarations, where the type property can be used
        /// </summary>
        public const string TypeDeclaration = "TypeDeclaration";
        /// <summary>
        /// Either an example or examples node
        /// </summary>
        public const string Example = "Example";
        /// <summary>
        /// A resource type node
        /// </summary>
        public const string ResourceType = "ResourceType";
        /// <summary>
        /// A trait node
        /// </summary>
        public const string Trait = "Trait";
        /// <summary>
        /// A security scheme declaration
        /// </summary>
        public const string SecurityScheme = "SecurityScheme";
        /// <summary>
        /// The settings node of a security scheme declaration
        /// </summary>
        public const string SecuritySchemeSettings = "SecuritySchemeSettings";
        /// <summary>
        /// A declaration of the annotationTypes node, whose key is a name of an annotation type and whose value describes the annotation
        /// </summary>
        public const string AnnotationType = "AnnotationType";
        /// <summary>
        /// The root of a library
        /// </summary>
        public const string Library = "Library";
        /// <summary>
        /// The root of an overlay
        /// </summary>
        public const string Overlay = "Overlay";
        /// <summary>
        /// The root of an extension
        /// </summary>
        public const string Extension = "Extension";

        public static bool IsTargetLocation(String ramlLocation)
        {
            switch (ramlLocation)
            {
                case API:
                case DocumentationItem:
                case Resource:
                case Method:
                case Response:
                case RequestBody:
                case ResponseBody:
                case TypeDeclaration:
                case Example:
                case ResourceType:
                case Trait:
                case SecurityScheme:
                case SecuritySchemeSettings:
                case AnnotationType:
                case Library:
                case Overlay:
                case Extension:
                    return true;
            }

            return false;
        }
    }
}
