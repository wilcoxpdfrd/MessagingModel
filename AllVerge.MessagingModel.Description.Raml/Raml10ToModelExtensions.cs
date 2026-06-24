using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

using AllVerge.Core.Collections;
using AllVerge.Core.Resource;
using AllVerge.Core.Text;

using AllVerge.Core.ServiceModel.Description.Model;
using AllVerge.Core.ServiceModel.Description.Adapters;

namespace AllVerge.Core.ServiceModel.Description.Raml
{
    using AllVerge.Core.Markup;
    using AllVerge.Core.Markup.Document;
    using AllVerge.Core.Markup.Formatters;
    using AllVerge.Core.Markup.Yaml;
    using AllVerge.Core.Markup.Xml.Schema;

    using AllVerge.Core.Model;
    using AllVerge.Core.Model.Caches;
    using AllVerge.Core.Model.Actuals;
    using AllVerge.Core.Model.LexicalTypes;
    using AllVerge.Core.Model.LexicalTypes.Structures;

    using AllVerge.Core.Model.DataTypes.Abstractions;
    using AllVerge.Core.Model.IETFTypes;
    using AllVerge.Core.Model.JsonSchema;
    using AllVerge.Core.Model.RamlTypes;
    using AllVerge.Core.Model.RamlTypes.Adapters;
    using AllVerge.Core.Model.XMLSchema;
    using AllVerge.Core.Model.YamlTypes;

    internal static class Raml10ToModelExtensions
    {
        static Raml10ToModelExtensions()
        {
            AbstractDataTypes.TryInitializeBuiltInTypes();
            RamlDataTypes.TryInitializeBuiltInTypes();
            YamlTypes.TryInitializeBuiltInTypes();
            IETFDataTypes.TryInitializeBuiltInTypes();
            XsDataType.TryInitializeBuiltInTypes();
            JsonSchemaTypes.TryInitializeBuiltInTypes();
        }

        private static readonly string[] INTERACTION_METHODS = new string[] { "get", "patch", "put", "post", "delete", "head", "options" };

        public static ProtocolDescription ReadFullDescription(this MarkupNode rootNode, string descriptionLocator, Uri descriptionImportsCachePathUri, DocumentType documentType)
        {
            Uri descriptionUri = new Uri(descriptionLocator);

            Uri descriptionBaseUri;
            String descriptionToken;

            UriUtils.TryGetResourceName(descriptionUri, out descriptionBaseUri, out descriptionToken);

            String descriptionNamespace = descriptionUri.ToNamespaceUri().AbsoluteUri;

            QualifiedName.ClearCurrentNamespaceManager();

            if (!QualifiedName.TrySetCurrentNamespaceManager(descriptionNamespace))
            {
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());

                namespaceManager.AddNamespace(descriptionToken, descriptionNamespace);

                QualifiedName.SetCurrentNamespaceManager(descriptionNamespace, namespaceManager);
            }

            QualifiedName fullyQualifiedContractName =
                QualifiedName.CreateImmutableFromFQN(
                    QualifiedName.CreateFullyQualifiedName(descriptionNamespace, descriptionToken));

            QualifiedName.TryGetCurrentNamespaceManager(out XmlNamespaceManager currentNamespaceManager, true);

            new XmlSchemas().SetSchemasCache(
                currentNamespaceManager,
                descriptionLocator,
                descriptionNamespace);

            List<QualifiedName> fullyQualifiedResourceNames = new List<QualifiedName>();

            MarkupNode annotationTypesNode;

            if (rootNode.TryGetChildNodeByName(RamlTokens.ANNOTATION_TYPES, out annotationTypesNode))
            {
                foreach (MarkupNode annotationTypeNode in annotationTypesNode.Children)
                {
                    String annotationName;
                    RepresentationTypeKind annotationRepresentationTypeKind;

                    annotationTypeNode.ReadCompileAndCacheRepresentationType(
                        RamlMarkupNodeType.AnnotationType,
                        descriptionLocator,
                        descriptionToken,
                        descriptionImportsCachePathUri,
                        descriptionNamespace,
                        Represents.Deferred,
                        out annotationName,
                        out annotationRepresentationTypeKind);
                }
            }

            MarkupNode usesNode;

            if (rootNode.TryGetChildNodeByName(RamlTokens.USES, out usesNode))
            {
                foreach (MarkupNode libraryNode in usesNode.Children)
                {
                    if (libraryNode.TryGetText(out String libraryPath) && UriUtils.TryCreateAbsoluteUri(libraryPath, descriptionLocator, out Uri libraryUri))

                        libraryUri.TryReadCompileAndCacheLibraryFragment(libraryNode.Name, descriptionImportsCachePathUri, descriptionNamespace, out QualifiedName fullyQualifiedLibraryName);

                    else

                        throw new InvalidOperationException(String.Format("Could not resolve library '{0}' relative to '{1}.", libraryPath, descriptionLocator));
                }
            }

            MarkupNode typesNode;

            if (rootNode.TryGetChildNodeByName(RamlTokens.TYPES, out typesNode))
            {
                foreach (MarkupNode typeNode in typesNode.Children)
                {
                    String typeNodeName;
                    RepresentationTypeKind representationTypeKind;

                    typeNode.ReadCompileAndCacheRepresentationType(
                        RamlMarkupNodeType.Type, 
                        descriptionLocator, 
                        descriptionToken,
                        descriptionImportsCachePathUri,
                        descriptionNamespace, 
                        Represents.Deferred, 
                        out typeNodeName, 
                        out representationTypeKind);
                }
            }

            MarkupNode resourceTypesNode;

            if (rootNode.TryGetChildNodeByName(RamlTokens.RESOURCE_TYPES, out resourceTypesNode))
            {
                foreach (MarkupNode resourceTypeNode in resourceTypesNode.Children)
                {
                    ModelCaches.AddToAgentCache(
                        descriptionNamespace,
                        resourceTypeNode.ReadResourceTypeConnection(
                            descriptionImportsCachePathUri,
                            descriptionLocator,
                            descriptionToken,
                            descriptionNamespace));
                }
            }

            Attributes descriptionAttributes
                = Attributes.Empty;

            Annotations descriptionAnnotations = new Annotations();

            rootNode.ReadDocumentationAnnotation(descriptionAnnotations);

            rootNode.TryReadMetaDataAnnotationNodes(RamlAnnotationTargets.API, descriptionNamespace, descriptionToken, descriptionLocator, ref descriptionAnnotations);

            UriTemplate baseUriTemplate;
            Uri baseUri;
            Annotations baseUriAnnotations = new Annotations();
            string connectionName;

            String baseUriString;
            bool hasValueNode;

            if (rootNode.TryGetChildNodeScalarValuedTextByName(RamlTokens.BASE_URI, out baseUriString, out hasValueNode))
            {
                baseUriTemplate = new UriTemplate(baseUriString);
                baseUri = new Uri(Uri.EscapeUriString(baseUriString.Replace("{", "").Replace("}", "")));
                connectionName = baseUri.LocalPath.Substring(1);

                MarkupNode baseUriNode;

                if (hasValueNode && rootNode.TryGetChildNodeByName(RamlTokens.BASE_URI, out baseUriNode))

                    baseUriNode.TryReadMetaDataAnnotationNodes(RamlAnnotationTargets.Resource, descriptionNamespace, descriptionToken, descriptionLocator, ref baseUriAnnotations);
            }
            else
            {
                baseUriTemplate = new UriTemplate(DescriptionConstants.TEMP_URI);
                baseUri = new Uri(DescriptionConstants.TEMP_URI);
                connectionName = "default";
            }

            Domain baseUriParameters = Domain.CreateImmutableOnRead();

            Potential hostUriParameter = null;

            MarkupNode baseUriParametersNode;

            if (rootNode.TryGetChildNodeByName(RamlTokens.BASE_URI_PARAMETERS, out baseUriParametersNode))
            {
                foreach (MarkupNode baseUriParameterNode in baseUriParametersNode.Children)
                {
                    Potential baseUriParameter =
                        baseUriParameterNode.ReadValueTypedNodePotential(
                            RamlMarkupNodeType.Parameter,
                            descriptionLocator,
                            descriptionToken,
                            descriptionImportsCachePathUri,
                            descriptionNamespace,
                            Represents.Identity);

                    baseUriParameters.AddPotential(
                        Refinement.TotalCovering,
                        baseUriParameter);

                    if (baseUriTemplate.PathSegmentVariableNames.Any(v => v == baseUriParameterNode.Name))
                    {
                        if (baseUri.Host == baseUriParameterNode.Name)

                            hostUriParameter = baseUriParameter;
                    }
                }
            }

            if (baseUriTemplate.PathSegmentVariableNames.Any(v => v.Equals(RamlTokens.BASE_URI_PARAMETER_NAME_VERSION, StringComparison.InvariantCultureIgnoreCase)))
            {
                baseUriParameters.AddPotential(
                    Refinement.TotalCovering,
                    new Potential(
                        RamlTokens.BASE_URI_PARAMETER_NAME_VERSION,
                        Structure.ExactlyOneElement,
                        YamlTypes.CreateBuiltInScalarValueType(YtStrDataType.NAME),
                        Represents.Identity));
            }

            String[] schemes;

            MarkupNode protocolsNode;

            if (rootNode.TryGetChildNodeByName(RamlTokens.PROTOCOLS, out protocolsNode))
            {
                MarkupNode[] protocolNodes;

                protocolsNode.TryGetChildNodesByName(MarkupTokens.ARRAY_ITEM_ELEMENT_NAME, out protocolNodes);

                schemes = protocolNodes.Select(n => n.Text).ToArray();
            }
            else

                schemes = new String[] { baseUri.Scheme };

            String[] defaultMediaTypes;

            MarkupNode mediaTypesNode;

            if (rootNode.TryGetChildNodeByName(RamlTokens.MEDIA_TYPE, out mediaTypesNode))
            {
                if (!mediaTypesNode.TryGetTextArray(out defaultMediaTypes))

                    throw new InvalidOperationException("Failed to read default mediaType node content.");
            }
            else

                defaultMediaTypes = new String[0];

            // ToDo:  security

            SetStandardResourceNames(fullyQualifiedResourceNames);

            ProtocolDescription description =
                new ProtocolDescription(
                    fullyQualifiedContractName,
                    descriptionAttributes,
                    descriptionAnnotations,
                    documentType,
                    descriptionLocator,
                    fullyQualifiedResourceNames.ToArray()
                );

            Connector connector =
                new Connector(
                    QualifiedName.CreatePrefixedName(descriptionToken, baseUri.Host),
                    Annotations.Empty);

            description.AddConnector(connector);

            Connection connection = new Connection(connectionName);

            connection.Annotations.Put(baseUriAnnotations.ToArray());

            connection.SetUriParameters(baseUriParameters);

            connector.AddConnection(connection);

            connection.Bindings.Put(
                BindingConstants.HTTP_BINDING_PROPERTY_NAME,
                BindingConstants.BINDING_TRANSPORT_ATTRIBUTE_NAME,
                BindingConstants.BINDING_TRANSPORT_ATTIBUTE_VALUE_HTTP);

            foreach (String scheme in schemes)
            {
                String locationUrl;

                if (baseUri.LocalPath != "/")

                    locationUrl = String.Format("{0}://{1}{2}", scheme, baseUri.Host, baseUri.LocalPath);

                else

                    locationUrl = String.Format("{0}://{1}", scheme, baseUri.Host);

                connection.Bindings.Put(
                    BindingConstants.HTTP_BINDING_ADDRESS_PROPERTY_NAME,
                    BindingAttribute.CreateMutable(BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME, locationUrl));
            }

            foreach (String pathVariableName in baseUriTemplate.PathSegmentVariableNames)
            {
                connection.Bindings.Put(
                    BindingConstants.HTTP_BINDING_URLREPLACEMENT_PROPERTY_NAME,
                    BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, pathVariableName.ToLower()));
            }

            foreach (String queryVariableName in baseUriTemplate.QueryValueVariableNames)
            {
                connection.Bindings.Put(
                    BindingConstants.HTTP_URL_BINDING_ENCODED_PROPERTY_NAME,
                    BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, queryVariableName));
            }

            rootNode.ReadResourceInteractions(
                descriptionLocator, 
                descriptionToken,
                descriptionImportsCachePathUri,
                descriptionNamespace, 
                connection);

            return description;
        }

        private static void SetStandardResourceNames(List<QualifiedName> fullyQualifiedResourceNames)
        {
            IDictionary<String, String> nsKVPairs = QualifiedName.GetNamespacesInScope(XmlNamespaceScope.ExcludeXml);

            foreach (String nsKey in nsKVPairs.Keys)
            {
                fullyQualifiedResourceNames.Add(
                    QualifiedName.CreateImmutableFromFQN(
                        QualifiedName.CreateFullyQualifiedName(nsKVPairs[nsKey], nsKey)));
            }

            //String @namespace;

            //if (QName.TryLookupNamespace(BindingConstants.XML_SCHEMA_PREFIX, out @namespace))

            //    fullyQualifiedResourceNames.Add(
            //        QName.CreateImmutableFromFQN(
            //            QName.CreateFullyQualifiedName(@namespace, BindingConstants.XML_SCHEMA_PREFIX)));

            //if (QName.TryLookupNamespace(YamlTypes.NAMESPACE_PREFIX, out @namespace))

            //    fullyQualifiedResourceNames.Add(
            //        QName.CreateImmutableFromFQN(
            //            QName.CreateFullyQualifiedName(@namespace, YamlTypes.NAMESPACE_PREFIX)));

            //if (QName.TryLookupNamespace(JsonSchemaTypes.NAMESPACE_PREFIX, out @namespace))

            //    fullyQualifiedResourceNames.Add(
            //        QName.CreateImmutableFromFQN(
            //            QName.CreateFullyQualifiedName(@namespace, JsonSchemaTypes.NAMESPACE_PREFIX)));

            //if (QName.TryLookupNamespace(IETFTypes.RFC2616_NAMESPACE_PREFIX, out @namespace))

            //    fullyQualifiedResourceNames.Add(
            //        QName.CreateImmutableFromFQN(
            //            QName.CreateFullyQualifiedName(@namespace, IETFTypes.RFC2616_NAMESPACE_PREFIX)));

            //if (QName.TryLookupNamespace(IETFTypes.RFC3339_NAMESPACE_PREFIX, out @namespace))

            //    fullyQualifiedResourceNames.Add(
            //        QName.CreateImmutableFromFQN(
            //            QName.CreateFullyQualifiedName(@namespace, IETFTypes.RFC3339_NAMESPACE_PREFIX)));

            //if (QName.TryLookupNamespace(IETFTypes.RFC5322_NAMESPACE_PREFIX, out @namespace))

            //    fullyQualifiedResourceNames.Add(
            //        QName.CreateImmutableFromFQN(
            //            QName.CreateFullyQualifiedName(@namespace, IETFTypes.RFC5322_NAMESPACE_PREFIX)));

            //if (QName.TryLookupNamespace(BindingConstants.HTTP_PREFIX, out @namespace))

            //    fullyQualifiedResourceNames.Add(
            //        QName.CreateImmutableFromFQN(
            //            QName.CreateFullyQualifiedName(@namespace, BindingConstants.HTTP_PREFIX)));
        }

        private static void ReadResourceInteractions(this MarkupNode parentResourceNode, string descriptionLocator, string descriptionToken, Uri descriptionImportsCachePathUri, string descriptionNamespace, Connection connection)
        {
            MarkupNode[] resourceNodes;

            if (parentResourceNode.TryFindChildNodes((n) => n.Name.StartsWith("/"), out resourceNodes))
            {
                foreach (MarkupNode resourceNode in resourceNodes)
                {
                    MarkupNode uriParametersNode;
                    Potential[] uriInputParameters;

                    if (resourceNode.TryGetChildNodeByName(RamlTokens.URI_PARAMETERS, out uriParametersNode))

                        uriInputParameters =
                            uriParametersNode.ReadUriParameters(
                                descriptionLocator,
                                descriptionToken,
                                descriptionImportsCachePathUri,
                                descriptionNamespace);

                    else

                        uriInputParameters = Potential.AsArray();

                    MarkupNode typeNode;

                    if (resourceNode.TryGetChildNodeByName(RamlTokens.TYPE, out typeNode))
                    {
                        foreach (Interaction interactions in ModelCaches.GetAgentFromCache<Connection>(descriptionNamespace, typeNode.Text).Interactions)

                            connection.AddInteraction(
                                (Interaction)resourceNode.ApplyResourceTypeOrTraitParameters(interactions));
                    }

                    foreach (string interactionMethod in INTERACTION_METHODS)
                    {
                        if (resourceNode.TryGetChildNodeByName(interactionMethod, out MarkupNode resourceInteractionNode))
                        {
                            connection.AddInteraction(
                                resourceInteractionNode.ReadResourceInteraction(
                                    RamlAnnotationTargets.Resource,
                                    uriInputParameters,
                                    resourceNode.Name,
                                    descriptionLocator,
                                    descriptionToken,
                                    descriptionImportsCachePathUri,
                                    descriptionNamespace));
                        }
                    }

                    resourceNode.ReadResourceInteractions(
                        descriptionLocator, 
                        descriptionToken,
                        descriptionImportsCachePathUri,
                        descriptionNamespace, 
                        connection);
                }
            }
        }

        public static bool TryGetChildNodeScalarValuedTextByName(this MarkupNode node, string childNodeName, out String nodeText, out bool hasValueNode)
        {
            nodeText = null;
            hasValueNode = false;

            MarkupNode childNode;

            if (node.TryGetChildNodeByName(childNodeName, out childNode))
            {
                if (!childNode.TryGetText(out nodeText))
                {
                    MarkupNode valueNode;

                    if (childNode.TryGetChildNodeByName("value", out valueNode))
                    {
                        hasValueNode = true;

                        if (!valueNode.TryGetText(out nodeText))

                            throw new FormatException("value");
                    }
                }
            }

            return nodeText != null;
        }

        public static bool TryReadMetaDataAnnotationNodes(this MarkupNode node, String ramlAnnotationTarget, String descriptionNamespace, String descriptionToken, String descriptionLocator, ref Annotations annotations)
        {
            MarkupNode[] annotationNodes;

            if (node.TryFindChildNodes((n) => n.Name.StartsWith("(") && n.Name.EndsWith(")"), out annotationNodes))
            {
                if (annotationNodes.Count() > 0)
                {
                    annotations.AddRange(annotationNodes.Select(n => n.GetMetaDataAnnotation(ramlAnnotationTarget, descriptionNamespace, descriptionToken, descriptionLocator)));

                    return true;
                }
            }

            return false;
        }

        private static Annotation GetMetaDataAnnotation(this MarkupNode annotationNode, String ramlAnnotationTarget, String descriptionNamespace, String descriptionToken, String descriptionLocator)
        {
            if (!RamlAnnotationTargets.IsTargetLocation(ramlAnnotationTarget))

                throw new ArgumentOutOfRangeException(nameof(ramlAnnotationTarget));

            String name = annotationNode.Name.Trim('(', ')');

            QualifiedName typeQualifiedName = new Uri(descriptionLocator).GetQualifiedResourceName(name, descriptionToken);

            ILexicalType lexicalType = ModelCaches.GetLexicalTypeFromCache(descriptionNamespace, typeQualifiedName, Represents.MetaData);

            Annotation annotation = new Annotation(name, annotationNode.ToLexicalRepresentation().Representation);

            ValidationErrors errors = new ValidationErrors();

            if (!lexicalType.IsValid(annotation.Name, annotation.Representation, out String normalizedRepresentation, errors))

                throw errors.ToInvalidDataException();

            return annotation;
        }

        private static Entity GetAnnotationRepresentation(MarkupNode annotationNode, string ramlLocation, string name, DomainType partitionType)
        {
            Expression propertyElements = new Expression();

            foreach (MarkupNode propertyNode in annotationNode.Children)
            {
                Potential propertyPotential;

                if (partitionType.TryGetPotential(propertyNode.Name, out propertyPotential))
                {
                    if (propertyPotential.LexicalType.Kind == LexicalTypeKind.Domain)
                    {
                        propertyElements.Add(
                            GetAnnotationRepresentation(annotationNode, ramlLocation, name, (DomainType)propertyPotential.LexicalType));
                    }
                    else
                    {
                        Attribute allowedTargetAttribute;

                        if (propertyPotential.LexicalType.Attributes.TryGetAttribute(RamlTokens.ALLOWED_TARGETS, out allowedTargetAttribute))
                        {
                            ValidationErrors e = new ValidationErrors();

                            if (!allowedTargetAttribute.GetLexicalType().IsValid(propertyNode.Name, Term.CreateImmutableOnRead(ramlLocation), out String s, e))

                                throw new ArgumentOutOfRangeException(nameof(ramlLocation));
                        }

                        ValidationErrors errors = new ValidationErrors();

                        propertyNode.TryGetText(out String propertyValue);

                        if (propertyPotential.LexicalType.IsValid(propertyNode.Name, Term.CreateImmutableOnRead(propertyValue), out String normalizedPropertyValue, errors))

                            propertyElements.Add(
                                Entity.CreateImmutableOnRead(propertyNode.Name, Term.CreateImmutableOnRead(propertyValue)));
                    }
                }
            }

            return Entity.CreateImmutableOnRead(name, propertyElements);
        }

        private static Connection ReadResourceTypeConnection(this MarkupNode resourceTypeNode, Uri descriptionImportsCachePathUri, string descriptionLocator, string descriptionToken, string descriptionNamespace)
        {
            MarkupNode uriParametersNode;
            Potential[] uriInputParameters;

            if (resourceTypeNode.TryGetChildNodeByName(RamlTokens.URI_PARAMETERS, out uriParametersNode))

                uriInputParameters = uriParametersNode.ReadUriParameters(
                    descriptionLocator, 
                    descriptionToken,
                    descriptionImportsCachePathUri,
                    descriptionNamespace);

            else

                uriInputParameters = Potential.AsArray();

            String nodeContent;
            Uri includeUri;

            if (resourceTypeNode.TryGetText(out nodeContent) && 
                Raml10TypeToModelExtensions.IsNodeContentIncludeUri(nodeContent, descriptionLocator, out includeUri))

                return includeUri.ReadIncludedResourceServiceConnection(
                    descriptionLocator,
                    descriptionToken,
                    descriptionImportsCachePathUri,
                    descriptionNamespace);

            else
            {
                List<Interaction> interactions = new List<Interaction>();

                foreach (string interActionMethod in INTERACTION_METHODS)
                {
                    MarkupNode actionNode;

                    if (resourceTypeNode.TryGetChildNodeByName(interActionMethod, out actionNode))
                    {
                        interactions.Add(
                            actionNode.ReadResourceInteraction(
                                RamlAnnotationTargets.ResourceType,
                                uriInputParameters,
                                RamlTokens.RESOURCE_PATH_PARAMETER,
                                descriptionLocator,
                                descriptionToken,
                                descriptionImportsCachePathUri,
                                descriptionNamespace));
                    }
                }

                return
                    new Connection(
                        resourceTypeNode.Name,
                        Attributes.Empty,
                        resourceTypeNode.ReadResourceTypeOrTraitAnnotations(),
                        new BindingProperties(),
                        interactions.ToArray());
            }
        }

        private static Connection ReadIncludedResourceServiceConnection(this Uri includedResourceUri, string descriptionLocator, string descriptionToken, Uri descriptionImportsCachePathUri, string descriptionNamespace)
        {
            Stream resourceStream;
            String resourceMediaType;
            String resourceMediaTypeVariant;
            Encoding resourceEncoding;

            if (!includedResourceUri.TryStreamCachedResource(descriptionImportsCachePathUri, out resourceStream, out resourceMediaType))

                resourceMediaType = includedResourceUri.DownloadResourceAndGetNormalizedResourceMediaType(
                    descriptionImportsCachePathUri,
                    out resourceStream,
                    out resourceMediaTypeVariant,
                    out resourceEncoding);

            using (resourceStream)
            {
                switch (resourceMediaType)
                {
                    case MediaTypeConstants.APPLICATION_RAML_PLUS_YAML_MEDIA_TYPE:

                        String fragmentIdentifier;

                        if (!resourceStream.TryReadFragmentIdentifier(out fragmentIdentifier))

                            throw new InvalidOperationException(
                                "Could not read included resource fragment identifier.");

                        switch (fragmentIdentifier)
                        {
                            case RamlTypesTokens.FRAGMENT_IDENTIFIER_RESOURCE_TYPE:

                                MarkupNode rootNode = MarkupFormatter<MarkupNode>.FromFormattedStream(resourceStream, Formats.YAML, null, out Exception e) ?? throw e;

                                if (rootNode == null)

                                    return null;

                                return rootNode.ReadResourceTypeConnection(
                                    descriptionImportsCachePathUri,
                                    descriptionLocator,
                                    descriptionToken,
                                    descriptionNamespace);

                            default:

                                throw new InvalidOperationException(
                                    String.Format(
                                        "The resource fragment identifier '{0}' is not recognized.",
                                        fragmentIdentifier));
                        }

                    default:

                        throw new NotImplementedException(resourceMediaType.ToString());
                }
            }
        }

        //private static T ReadIncludedResourceAndCompileIfCodeAndCacheIfReusable<T>(this Uri resourceIncludeUri, String descriptionLocator, String descriptionToken, String descriptionNamespace)
        //{
        //    Uri resourceNamespaceUri;
        //    Object resourceData;

        //    Type targetType = typeof(T);

        //    if (!resourceIncludeUri.IsResourceRegistered(out resourceNamespaceUri, out resourceData))
        //    {
        //        Stream resourceStream;
        //        String resourceEncoding;

        //        String resourceContentType = resourceIncludeUri.GetResourceType(out resourceStream, out resourceEncoding);

        //        using (resourceStream)
        //        {
        //            switch (resourceContentType)
        //            {
        //                case ResourceTypeConstants.APPLICATION_RAML_YAML_CONTENT_TYPE:

        //                    String fragmentIdentifier;

        //                    if (!resourceStream.TryReadFragmentIdentifier(out fragmentIdentifier))

        //                        throw new InvalidOperationException("Could not read included resource fragment identifier.");

        //                    switch (fragmentIdentifier)
        //                    {
        //                        case RamlResourceTokens.FRAGMENT_IDENTIFIER_DOCUMENTATION_ITEM:

        //                            throw new NotSupportedException("Use " + nameof(RamlType10ToModelExtensions.ReadDocumentationAnnotation));

        //                        case RamlResourceTokens.FRAGMENT_IDENTIFIER_DATA_TYPE:

        //                            throw new NotSupportedException("Use " + nameof(RamlType10ToModelExtensions.ReadIncludedResourceCodeFileAndCompileAndCache));

        //                        case RamlResourceTokens.FRAGMENT_IDENTIFIER_LIBRARY:

        //                            throw new NotSupportedException("Use " + nameof(RamlType10ToModelExtensions.TryReadCompileAndCacheLibraryFragment));

        //                        case RamlResourceTokens.FRAGMENT_IDENTIFIER_NAMED_EXAMPLE:

        //                            throw new NotImplementedException(fragmentIdentifier);

        //                        case RamlResourceTokens.FRAGMENT_IDENTIFIER_RESOURCE_TYPE:

        //                            throw new NotImplementedException(fragmentIdentifier);

        //                        case RamlResourceTokens.FRAGMENT_IDENTIFIER_TRAIT:

        //                            throw new NotImplementedException(fragmentIdentifier);

        //                        case RamlResourceTokens.FRAGMENT_IDENTIFIER_OVERLAY:

        //                            throw new NotImplementedException(fragmentIdentifier);

        //                        case RamlResourceTokens.FRAGMENT_IDENTIFIER_EXTENSION:

        //                            throw new NotImplementedException(fragmentIdentifier);

        //                        case RamlResourceTokens.FRAGMENT_IDENTIFIER_SECURITY_SCHEME:

        //                            throw new NotImplementedException(fragmentIdentifier);

        //                        default:

        //                            throw new InvalidOperationException(
        //                                String.Format("The resource fragment identifier '{0}' is not recognized.",
        //                                fragmentIdentifier));
        //                    }

        //                case ResourceTypeConstants.APPLICATION_SCHEMA_JSON_CONTENT_TYPE:

        //                    throw new NotSupportedException("Use " + nameof(RamlType10ToModelExtensions.ReadIncludedResourceCodeFileAndCompileAndCache));

        //                case ResourceTypeConstants.APPLICATION_XML_CONTENT_TYPE:

        //                    fragmentIdentifier = null;

        //                    switch (typeof(T).FullName)
        //                    {
        //                        case "System.Xml.XmlDocument":

        //                            XmlDocument x1 = new XmlDocument();

        //                            x1.Load(XmlReader.Create(resourceStream));

        //                            return (T)(Object)x1;

        //                        case "System.String":

        //                            XmlDocument x2 = new XmlDocument();

        //                            x2.Load(XmlReader.Create(resourceStream));

        //                            return (T)(Object)x2.OuterXml;
        //                    }

        //                    break;

        //                case ResourceTypeConstants.APPLICATION_SCHEMA_XML_CONTENT_TYPE:

        //                    throw new NotSupportedException("Use " + nameof(RamlType10ToModelExtensions.ReadIncludedResourceCodeFileAndCompileAndCache));

        //                default:

        //                    throw new InvalidOperationException(
        //                        String.Format("The resource content type '{0}' is not recognized.",
        //                        resourceContentType.ToString()));
        //            }
        //        }
        //    }

        //    throw new InvalidOperationException(
        //        String.Format("The resource cannot be formatted as type {0}", targetType));
        //}

        public static String ReadResourceInteractionDisplayNameOrLocationName(this MarkupNode resourceInteractionNode, String resourceLocation)
        {
            String displayName;

            if (resourceInteractionNode.TryGetChildNodeTextByName(RamlTokens.DISPLAY_NAME, out displayName))

                return displayName;

            MatchCollection nameMatches = Raml10TypeToModelExtensions.resourceTypeAndTraitParametersRegex.Matches(resourceLocation);

            if (nameMatches.Count > 0)
            {
                Group resourcePathGroup = nameMatches[0].Groups["resourcePath"];

                if (resourcePathGroup.Captures.Count > 0)

                    return resourceInteractionNode.Name + "_" + resourceLocation.Replace("resourcePath", "resourcePathName");
            }

            return resourceInteractionNode.Name +"_"+ ReadResourceInteractionPathName(resourceLocation);
        }

        private static String ReadResourceInteractionPathName(String resourceLocation)
        {
            if (String.IsNullOrWhiteSpace(resourceLocation))

                return resourceLocation;

            int resourceLocationLastSlash = resourceLocation.LastIndexOf('/');

            if (resourceLocationLastSlash < 0)

                return resourceLocation;

            return resourceLocation.Substring(resourceLocationLastSlash + 1);
        }

        public static Annotations ReadResourceTypeOrTraitAnnotations(this MarkupNode resourceTypeOrTraitNode)
        {
            Annotations annotations = new Annotations();

            resourceTypeOrTraitNode.TryReadDocumentationNode(annotations, RamlTokens.USAGE);
            resourceTypeOrTraitNode.TryReadDocumentationNode(annotations, RamlTokens.DESCRIPTION);

            return annotations;
        }

        private static Interaction ApplyResourceTypeOrTraitParameters(this MarkupNode resourceNode, Interaction serviceAction)
        {
            String resourcePath = null;
            String resourcePathName = null;

            resourcePath = resourceNode.Name;
            resourcePathName = ReadResourceInteractionPathName(resourceNode.Name);

            Func<String, String> resourceTypeAndTraitTokenExpansionFunction = 
                GetResourceTypeAndTraitTokenExpansionFunction(resourcePath, resourcePathName);

            String serviceActionName = resourceTypeAndTraitTokenExpansionFunction(serviceAction.Name);

            Annotations serviceActionAnnotations;

            if (serviceAction.AnnotationsSpecified)

                serviceActionAnnotations =
                    serviceAction.Annotations.Clone(
                        resourceTypeAndTraitTokenExpansionFunction);

            else

                serviceActionAnnotations = Annotations.Empty;

            BindingProperties serviceActionBindings = 
                serviceAction.Bindings.Clone(
                    resourceTypeAndTraitTokenExpansionFunction);

            InteractionMessage[] serviceActionInputs = serviceAction.Inputs.Clone(resourceTypeAndTraitTokenExpansionFunction);

            InteractionMessage[] serviceActionOutputs = serviceAction.Outputs.Clone(resourceTypeAndTraitTokenExpansionFunction);

            InteractionMessage[] serviceActionFaults = serviceAction.Faults.Clone(resourceTypeAndTraitTokenExpansionFunction);

            return new Interaction
            (
                serviceActionName,
                Attributes.Empty,
                serviceActionAnnotations,
                serviceActionBindings,
                InteractionStyles.RequestResponse,
                serviceActionInputs,
                serviceActionOutputs,
                serviceActionFaults
            );
        }

        //private static Func<String, String> GetResourceTypeAndTraitTokenExpansionFunction(this BindingProperties serviceActionBindings)
        //{
        //    string resourcePath = "";
        //    string resourcePathName = "";

        //    BindingProperty locationBinding;

        //    if (serviceActionBindings.TryGetProperty(out locationBinding, BindingConstants.HTTP_OPERATION_BINDING_PROPERTY_NAME))
        //    {
        //        BindingAttribute locationAttribute;

        //        if (locationBinding.Attributes.TryGet(BindingConstants.LOCATION_BINDING_ATTRIBUTE_NAME, out locationAttribute))
        //        {
        //            Dictionary<String, String> resourcePathTokenValuesMap = new Dictionary<string, string>();

        //            resourcePath = locationAttribute.Value;
        //            resourcePathName = ReadResourcePathName(resourcePath);

        //            return GetResourceTypeAndTraitTokenExpansionFunction(resourcePath, resourcePathName);
        //        }
        //    }

        //    return null;
        //}

        private static Func<String, String> GetResourceTypeAndTraitTokenExpansionFunction(string resourcePath, string resourcePathName)
        { 
            return (String matchExpression) =>
            {
                MatchCollection matches = Raml10TypeToModelExtensions.resourceTypeAndTraitParametersRegex.Matches(matchExpression);

                if (matches.Count > 0)
                {
                    CaptureCollection parameterCaptures = matches[0].Groups["param"].Captures;

                    matchExpression = matchExpression.Replace("<<", "").Replace(">>", "");

                    Group resourcePathGroup = matches[0].Groups["resourcePath"];
                    Group resourcePathNameGroup = matches[0].Groups["resourcePathName"];
                    Group parameterNameGroup = matches[0].Groups["parameterName"];

                    if (resourcePathGroup.Captures.Count > 0)
                    {
                        foreach (Capture parameterCapture in parameterCaptures)
                        {
                            switch (parameterCapture.Value)
                            {
                                case "!singularize":
                                    matchExpression = matchExpression.Replace("|!singularize", "");
                                    resourcePath = resourcePath.ToSingular();
                                    break;
                                case "!pluralize":
                                    matchExpression = matchExpression.Replace("|!pluralize", "");
                                    resourcePath = resourcePath.ToPlural();
                                    break;
                                case "!uppercase":
                                    matchExpression = matchExpression.Replace("|!uppercase", "");
                                    resourcePath = resourcePath.ToUpperCase();
                                    break;
                                case "!lowercase":
                                    matchExpression = matchExpression.Replace("|!lowercase", "");
                                    resourcePath = resourcePath.ToLowercase();
                                    break;
                                case "!lowercamelcase":
                                    matchExpression = matchExpression.Replace("|!lowercamelcase", "");
                                    resourcePath = resourcePath.ToLowerCamelcase();
                                    break;
                                case "!uppercamelcase":
                                    matchExpression = matchExpression.Replace("|!uppercamelcase", "");
                                    resourcePath = resourcePath.ToUpperCamelcase();
                                    break;
                                case "!lowerunderscorecase":
                                    matchExpression = matchExpression.Replace("|!lowerunderscorecase", "");
                                    resourcePath = resourcePath.ToLowerUnderscoreCase();
                                    break;
                                case "!upperunderscorecase":
                                    matchExpression = matchExpression.Replace("|!upperunderscorecase", "");
                                    resourcePath = resourcePath.ToUpperUnderscoreCase();
                                    break;
                                case "!lowerhyphencase":
                                    matchExpression = matchExpression.Replace("|!lowerhyphencase", "");
                                    resourcePath = resourcePath.ToLowerHyphenCase();
                                    break;
                                case "!upperhyphencase":
                                    matchExpression = matchExpression.Replace("|!upperhyphencase", "");
                                    resourcePath = resourcePath.ToUpperHyphenCase();
                                    break;
                            }
                        }

                        matchExpression = matchExpression.Replace("resourcePath", resourcePath);
                    }

                    if (resourcePathNameGroup.Captures.Count > 0)
                    {
                        foreach (Capture parameterCapture in parameterCaptures)
                        {
                            switch (parameterCapture.Value)
                            {
                                case "!singularize":
                                    matchExpression = matchExpression.Replace("|!singularize", "");
                                    resourcePathName = resourcePathName.ToSingular();
                                    break;
                                case "!pluralize":
                                    matchExpression = matchExpression.Replace("|!pluralize", "");
                                    resourcePathName = resourcePathName.ToPlural();
                                    break;
                                case "!uppercase":
                                    matchExpression = matchExpression.Replace("|!uppercase", "");
                                    resourcePathName = resourcePathName.ToUpperCase();
                                    break;
                                case "!lowercase":
                                    matchExpression = matchExpression.Replace("|!lowercase", "");
                                    resourcePathName = resourcePathName.ToLowercase();
                                    break;
                                case "!lowercamelcase":
                                    matchExpression = matchExpression.Replace("|!lowercamelcase", "");
                                    resourcePathName = resourcePathName.ToLowerCamelcase();
                                    break;
                                case "!uppercamelcase":
                                    matchExpression = matchExpression.Replace("|!uppercamelcase", "");
                                    resourcePathName = resourcePathName.ToUpperCamelcase();
                                    break;
                                case "!lowerunderscorecase":
                                    matchExpression = matchExpression.Replace("|!lowerunderscorecase", "");
                                    resourcePathName = resourcePathName.ToLowerUnderscoreCase();
                                    break;
                                case "!upperunderscorecase":
                                    matchExpression = matchExpression.Replace("|!upperunderscorecase", "");
                                    resourcePathName = resourcePathName.ToUpperUnderscoreCase();
                                    break;
                                case "!lowerhyphencase":
                                    matchExpression = matchExpression.Replace("|!lowerhyphencase", "");
                                    resourcePath = resourcePath.ToLowerHyphenCase();
                                    break;
                                case "!upperhyphencase":
                                    matchExpression = matchExpression.Replace("|!upperhyphencase", "");
                                    resourcePathName = resourcePathName.ToUpperHyphenCase();
                                    break;
                            }
                        }

                        matchExpression = matchExpression.Replace("resourcePathName", resourcePathName);
                    }
                }

                return matchExpression;
            };
        }

        private static Interaction ReadResourceInteraction(this MarkupNode resourceInteractionNode, String annotationTarget, Potential[] uriInputParameters, String resourceLocation, string descriptionLocator, string descriptionToken, Uri descriptionImportsCachePathUri, string descriptionNamespace)
        {
            // (annotations)

            Annotations resourceAnnotations = 
                resourceInteractionNode.ReadResourceInteractionAnnotations();

            resourceInteractionNode.TryReadMetaDataAnnotationNodes(annotationTarget, descriptionNamespace, descriptionToken, descriptionLocator, ref resourceAnnotations);

            // ToDo: protocols 

            BindingProperties inputBindingProperties = new BindingProperties();

            foreach (Potential uriInputParameter in uriInputParameters)
            {
                inputBindingProperties.Put(
                    BindingConstants.HTTP_BINDING_URLREPLACEMENT_PROPERTY_NAME,
                    BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, uriInputParameter.Name));
            }

            Domain inputDomain = Domain.CreateImmutableOnRead();

            if (uriInputParameters.Length > 0)

                inputDomain.AddPotentials(
                    Refinement.TotalOrdering, 
                    uriInputParameters);

            // headers

            MarkupNode headersNode;

            if (resourceInteractionNode.TryGetChildNodeByName(RamlTokens.HEADERS, out headersNode))
            {
                foreach (MarkupNode headerNode in headersNode.Children)
                {
                    Potential inputHeaderPotential =
                        headerNode.ReadValueTypedNodePotential(
                            RamlMarkupNodeType.Property, 
                            descriptionLocator, 
                            descriptionToken,
                            descriptionImportsCachePathUri,
                            descriptionNamespace, 
                            Represents.MetaData);

                    inputBindingProperties.Put(
                        BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME,
                        BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, inputHeaderPotential.Name));

                    inputDomain.AddPotential(
                        Refinement.TotalCovering,
                        inputHeaderPotential);
                }
            }

            // queryParameters or queryString

            MarkupNode queryStringNode;

            if (resourceInteractionNode.TryGetChildNodeByName(RamlTokens.QUERY_STRING, out queryStringNode))
            {
                Potential inputQueryStringPotential =
                    queryStringNode.ReadValueTypedNodePotential(
                        RamlMarkupNodeType.Type, 
                        descriptionLocator, 
                        descriptionToken,
                        descriptionImportsCachePathUri,
                        descriptionNamespace, 
                        Represents.Query);

                if (inputQueryStringPotential.LexicalType is DomainType)
                {
                    foreach (Block inputQueryStringBlock in (inputQueryStringPotential.LexicalType as DomainType).Domain)
                    {
                        foreach (Potential inputQueryPotential in inputQueryStringBlock)
                        {
                            inputBindingProperties.Put(
                                BindingConstants.HTTP_URL_BINDING_ENCODED_PROPERTY_NAME,
                                BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, inputQueryPotential.Name));

                            inputDomain.AddPotential(
                                Refinement.TotalOrdering,
                                inputQueryPotential);
                        }
                    }
                }
                else
                {
                    inputBindingProperties.Put(
                        BindingConstants.HTTP_URL_BINDING_ENCODED_PROPERTY_NAME,
                        BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, inputQueryStringPotential.Name));

                    inputDomain.AddPotential(
                        Refinement.TotalOrdering,
                        inputQueryStringPotential);
                }
            }
            else
            {
                MarkupNode queryParametersNode;

                if (resourceInteractionNode.TryGetChildNodeByName(RamlTokens.QUERY_PARAMETERS, out queryParametersNode))
                {
                    foreach (MarkupNode queryParameterNode in queryParametersNode.Children)
                    {
                        Potential inputQueryParameterPotential =
                            queryParameterNode.ReadValueTypedNodePotential(
                                RamlMarkupNodeType.Property, 
                                descriptionLocator, 
                                descriptionToken,
                                descriptionImportsCachePathUri,
                                descriptionNamespace, 
                                Represents.Query);

                        inputBindingProperties.Put(
                            BindingConstants.HTTP_URL_BINDING_ENCODED_PROPERTY_NAME,
                            BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, inputQueryParameterPotential.Name));

                        inputDomain.AddPotential(
                            Refinement.TotalOrdering,
                            inputQueryParameterPotential);
                    }
                }
            }

            // body

            MarkupNode bodyNode;

            if (resourceInteractionNode.TryGetChildNodeByName(RamlTokens.BODY, out bodyNode))
            {
                foreach (MarkupNode mediaTypeNode in bodyNode.Children)
                {
                    Potential mediaTypePotential =
                        mediaTypeNode.ReadValueTypedNodePotential(
                            RamlMarkupNodeType.MediaType, 
                            descriptionLocator, 
                            descriptionToken,
                            descriptionImportsCachePathUri,
                            descriptionNamespace, 
                            Represents.Information);

                    inputBindingProperties.Put(
                        BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                        BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, mediaTypePotential.Name),
                        BindingAttribute.CreateMutable(BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME, mediaTypeNode.Name));

                    inputDomain.AddPotential(
                        Refinement.Singleton,
                        mediaTypePotential);
                }
            }

            // is
            // securedBy

            // responses

            Domain outputDomain = Domain.CreateImmutableOnRead();
            Domain faultDomain = Domain.CreateImmutableOnRead();

            BindingProperties outputBindingProperties = new BindingProperties();
            BindingProperties faultBindingProperties = new BindingProperties();

            MarkupNode responsesNode;

            if (resourceInteractionNode.TryGetChildNodeByName(RamlTokens.RESPONSES, out responsesNode))
            {
                foreach (MarkupNode responseNode in responsesNode.Children)
                {
                    Annotations responseAnnotations = 
                        resourceInteractionNode.ReadResourceInteractionAnnotations();

                    // (annotations)

                    resourceInteractionNode.TryReadMetaDataAnnotationNodes(annotationTarget, descriptionNamespace, descriptionToken, descriptionLocator, ref responseAnnotations);

                    // description?

                    // ToDo: apply responseAnnotations
                    // ToDo: responseAnnotationAttributes

                    int responseCode;

                    if (int.TryParse(responseNode.Name, out responseCode))
                    {
                        MarkupNode responseHeadersNode;

                        if (resourceInteractionNode.TryGetChildNodeByName(RamlTokens.HEADERS, out responseHeadersNode))
                        {
                            foreach (MarkupNode responseHeaderNode in responseHeadersNode.Children)
                            {
                                Potential outputHeaderPotential =
                                    responseHeaderNode.ReadValueTypedNodePotential(
                                        RamlMarkupNodeType.Property,
                                        descriptionLocator,
                                        descriptionToken,
                                        descriptionImportsCachePathUri,
                                        descriptionNamespace,
                                        Represents.MetaData);

                                if (responseCode < 400)
                                {
                                    outputBindingProperties.Put(
                                        BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME,
                                        BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, outputHeaderPotential.Name));

                                    outputDomain.AddPotential(
                                        Refinement.TotalCovering,
                                        outputHeaderPotential);
                                }
                                else
                                {
                                    faultBindingProperties.Put(
                                        BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME,
                                        BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, outputHeaderPotential.Name));

                                    faultDomain.AddPotential(
                                        Refinement.TotalCovering,
                                        outputHeaderPotential);
                                }
                            }
                        }

                        MarkupNode responseBodyNode;

                        if (responseNode.TryGetChildNodeByName(RamlTokens.BODY, out responseBodyNode))
                        {
                            foreach (MarkupNode responseMediaTypeNode in responseBodyNode.Children)
                            {
                                Potential responseMediaTypePotential =
                                    responseMediaTypeNode.ReadValueTypedNodePotential(
                                        RamlMarkupNodeType.MediaType,
                                        descriptionLocator,
                                        descriptionToken,
                                        descriptionImportsCachePathUri,
                                        descriptionNamespace,
                                        Represents.Information);

                                if (responseCode < 400)
                                {
                                    outputBindingProperties.Put(
                                        BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                                        BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, responseMediaTypePotential.Name),
                                        BindingAttribute.CreateMutable(BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME, responseMediaTypeNode.Name));

                                    outputDomain.AddPotential(
                                        Refinement.Singleton,
                                        responseMediaTypePotential);
                                }
                                else
                                {
                                    faultBindingProperties.Put(
                                        BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                                        BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, responseMediaTypePotential.Name),
                                        BindingAttribute.CreateMutable(BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME, responseMediaTypeNode.Name));

                                    faultDomain.AddPotential(
                                        Refinement.Singleton,
                                        responseMediaTypePotential);
                                }
                            }
                        }
                    }
                    else

                        throw new InvalidOperationException("Cannot format '{0}' as a response status code.".FormatString(responseNode.Name));
                }
            }

            return
                new Interaction(
                    resourceInteractionNode.ReadResourceInteractionDisplayNameOrLocationName(resourceLocation), // displayName
                    Attributes.Empty,
                    resourceAnnotations,
                    BindingProperties.CreateImmutableOnRead(
                        BindingProperty.CreateImmutable(BindingConstants.HTTP_BINDING_OPERATION_PROPERTY_NAME, BindingProperties.Empty, BindingAttributes.CreateImmutableOnRead(BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME, resourceLocation)),
                        BindingProperty.CreateImmutable(BindingConstants.HTTP_BINDING_PROPERTY_NAME, BindingProperties.Empty, BindingAttributes.CreateImmutableOnRead(BindingConstants.BINDING_VERB_ATTRIBUTE_NAME, resourceInteractionNode.Name))),
                    InteractionStyles.RequestResponse,
                    CollectionUtils.ToArray(
                        new InteractionMessage(
                            InteractionMessage.INPUT,
                            //QName.CreatePrefixedName(descriptionToken, "input"),
                            Annotations.Empty,
                            inputBindingProperties,
                            inputDomain
                        )
                    ),
                    CollectionUtils.ToArray(
                        new InteractionMessage(
                            InteractionMessage.OUTPUT,
                            //QName.CreatePrefixedName(descriptionToken, "output"),
                            Annotations.Empty,
                            outputBindingProperties,
                            outputDomain)),
                    CollectionUtils.ToArray(
                        new InteractionMessage(
                            InteractionMessage.FAULT,
                            //QName.CreatePrefixedName(descriptionToken, "fault"),
                            Annotations.Empty,
                            faultBindingProperties,
                            faultDomain)
                        )
                    );
        }

        private static Potential[] ReadUriParameters(this MarkupNode uriParametersNode, string descriptionLocator, string descriptionToken, Uri descriptionImportsCachePathUri, string descriptionNamespace)
        {
            List<Potential> inputs = new List<Potential>();

            foreach (MarkupNode uriParameterNode in uriParametersNode.Children)
            {
                inputs.Add(
                    uriParameterNode.ReadValueTypedNodePotential(
                        RamlMarkupNodeType.Parameter, 
                        descriptionLocator, 
                        descriptionToken,
                        descriptionImportsCachePathUri,
                        descriptionNamespace, 
                        Represents.Identity));
            }

            return inputs.ToArray();
        }

        private static Annotations ReadResourceInteractionAnnotations(this MarkupNode resourceInteractionNode)
        {
            Annotations annotations = new Annotations();

            resourceInteractionNode.TryReadDocumentationNode(annotations, RamlTokens.DESCRIPTION);

            return annotations;
        }

        public static Entity ToLexicalRepresentation(this MarkupNode representationNode)
        {
            String representationContent;

            Expression representations = new Expression();

            foreach (MarkupNode representationChildNode in representationNode.Children)
            {
                representations.Add(representationChildNode.ToLexicalRepresentation());
            }

            IRepresentation content;

            if (representationNode.TryGetText(out representationContent))
            {
                if (representations.Count > 0)
                {
                    representations.Add(Term.CreateImmutableOnRead(representationContent));

                    content = representations;
                }
                else
                {
                    content = (Term.CreateImmutableOnRead(representationContent));
                }
            }
            else

                content = representations;

            Attributes attributes;

            if (representationNode.HasAttributes)
            {
                attributes = new Attributes();

                foreach (MarkupAttribute attribute in representationNode.Attributes)
                {
                    attributes.Add(
                        new Attribute(
                            attribute.Name, 
                            Term.CreateImmutableOnRead(attribute.Value))
                    );
                }
            }
            else

                attributes = Attributes.Empty;

            return new Entity(representationNode.Name, attributes, Annotations.Empty, content);
        }
    }
}
