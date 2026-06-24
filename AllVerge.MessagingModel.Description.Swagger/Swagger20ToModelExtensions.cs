using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using AllVerge.Core.Collections;
using AllVerge.Core.Resource;

namespace AllVerge.Core.ServiceModel.Description.Swagger
{
    using AllVerge.Core.Model;
    using AllVerge.Core.Model.Caches;
    using AllVerge.Core.Model.DataTypes.Abstractions;
    using AllVerge.Core.Model.IETFTypes;
    using AllVerge.Core.Model.Actuals;
    using AllVerge.Core.Model.JsonSchema;
    using AllVerge.Core.Model.JsonSchema.Adapters;
    using AllVerge.Core.Model.LexicalTypes;
    using AllVerge.Core.Model.LexicalTypes.Facets;
    using AllVerge.Core.Model.LexicalTypes.Structures;
    using AllVerge.Core.Model.SwaggerTypes;

    using AllVerge.Core.Markup.Document;

    using AllVerge.Core.ServiceModel.Description.Adapters;
    using AllVerge.Core.ServiceModel.Description.Model;
    using AllVerge.Core.ServiceModel.Messaging;
    using AllVerge.Core.ServiceModel.Http;

    using AllVerge.Core.Markup.Formatters;
    using AllVerge.Core.ServiceModel.Methods;

    internal static class Swagger20ToModelExtensions
    {
        static Swagger20ToModelExtensions()
        {
            AbstractDataTypes.TryInitializeBuiltInTypes();
            SwaggerDataTypes.InitializeBuiltInTypes();
            JsonSchemaTypes.TryInitializeBuiltInTypes();
            IETFDataTypes.TryInitializeBuiltInTypes();
        }

        private static readonly String[] allowedFileMimeTypes = new String[] { "multipart/form-data", " application/x-www-form-urlencoded" };

        public static ProtocolDescription ReadFullDescription(this MarkupNode rootNode, string descriptionLocator, Uri descriptionCachePathUri, DocumentType documentType)
        {
            Uri descriptionUri = new Uri(descriptionLocator);

            Uri descriptionPath;
            String descriptionToken;

            UriUtils.TryGetResourceName(descriptionUri, out descriptionPath, out descriptionToken);

            Uri descriptionNamespaceUri = descriptionUri.ToNamespaceUri();

            String descriptionNamespace = descriptionNamespaceUri.AbsoluteUri;

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

            List<QualifiedName> fullyQualifiedResourceNames = new List<QualifiedName>();

            SetStandardResourceNames(fullyQualifiedResourceNames);

            MarkupNode schemesNode, hostNode, basePathNode;

            List<String> requestSchemes = new List<string>();

            if (rootNode.TryGetChildNodeByName(SwaggerTokens.SCHEMES, out schemesNode))
            {
                foreach (String scheme in schemesNode.Children.Select(s => s.Text))

                    requestSchemes.Add(scheme);
            }
            else
            {
                requestSchemes.Add(MessagingBindingConstants.HTTP_BINDING_PREFIX);
            }

            String host;

            if (rootNode.TryGetChildNodeByName(SwaggerTokens.HOST, out hostNode))
            {
                host = hostNode.Text;
            }
            else
            {
                host = descriptionUri.Host;
            }

            String basePath = null;

            if (rootNode.TryGetChildNodeByName(SwaggerTokens.BASE_PATH, out basePathNode))
            {
                basePath = basePathNode.Text;
            }

            String path;
            string connectionName;

            if (basePath == null)
            {
                path = host;

                connectionName = "default";
            }
            else
            {
                path = host + basePath;

                connectionName = basePath.Substring(1);
            }

            Attributes descriptionAttributes = new Attributes();

            Annotations descriptionAnnotations = Annotations.CreateImmutableOnRead();

            rootNode.ReadDescriptionAnnotations(descriptionAnnotations);

            rootNode.ReadCompileAndCacheTags(descriptionNamespace, descriptionToken);

            MarkupNode definitionsNode;

            if (rootNode.TryGetChildNodeByName(SwaggerTokens.DEFINITIONS, out definitionsNode))
            {
                MarkupNode schemaRootNode = new MarkupNode(null, JsonSchemaTokens.ROOT, "");

                MarkupNode schemaNode = new MarkupNode(schemaRootNode, JsonSchemaTokens.SCHEMA, JsonSchemaTokens.SCHEMA);

                schemaNode.AddText(JsonSchemaTypes.JSON_SCHEMA_DRAFT_04_URI); // latest version?
                
                MarkupNode schemaExtensionNode = new MarkupNode(schemaRootNode, JsonSchemaTokens.SCHEMA_EXTENTION, JsonSchemaTokens.SCHEMA_EXTENTION);

                schemaExtensionNode.AddText(SwaggerDataTypes.VERSION_2_0_URI);

                schemaRootNode.EnsureIdNode(descriptionNamespaceUri);

                definitionsNode.SetParentNode(schemaRootNode);

                schemaRootNode.CompileAndCacheModel(descriptionCachePathUri, descriptionNamespace, out RepresentationTypeKind representationTypeKind);
            }

            MarkupNode parametersDefinisionsNode;

            if (rootNode.TryGetChildNodeByName(SwaggerTokens.PARAMETERS, out parametersDefinisionsNode))
            {
                throw new NotImplementedException(SwaggerTokens.PARAMETERS);
            }

            MarkupNode responsesDefinitionsNode;

            if (rootNode.TryGetChildNodeByName(SwaggerTokens.RESPONSES, out responsesDefinitionsNode))
            {
                throw new NotImplementedException(SwaggerTokens.RESPONSES);
            }

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
                    QualifiedName.CreatePrefixedName(descriptionToken, host),
                    Annotations.Empty);

            description.AddConnector(connector);

            Connection connection = new Connection();

            connection.Name = connectionName;

            connection.Bindings.Put(
                BindingConstants.HTTP_BINDING_PROPERTY_NAME,
                BindingConstants.BINDING_TRANSPORT_ATTRIBUTE_NAME,
                BindingConstants.BINDING_TRANSPORT_ATTIBUTE_VALUE_HTTP);

            foreach (String requestScheme in requestSchemes)
            {
                String url = String.Format("{0}://{1}", requestScheme, path);

                // ToDo: check requestScheme is http ... otherwise bind appropriately ...

                connection.Bindings.Put(
                    BindingConstants.HTTP_BINDING_ADDRESS_PROPERTY_NAME,
                    BindingAttribute.CreateMutable(
                        BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME, 
                        url));
            }

            connector.AddConnection(connection);

            MarkupNode pathsNode;

            if (rootNode.TryGetChildNodeByName(SwaggerTokens.PATHS, out pathsNode))
            {
                foreach (MarkupNode pathNode in pathsNode.Children)
                {
                    foreach (MarkupNode verbNode in pathNode.Children)
                    {
                        BindingProperties serviceActionBindings = new BindingProperties();

                        serviceActionBindings.Put(
                            BindingConstants.HTTP_BINDING_OPERATION_PROPERTY_NAME,
                            BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME,
                            pathNode.Name);

                        String verbName = verbNode.Name;

                        if (verbName == JsonSchemaTokens.REF)

                            throw new NotImplementedException(JsonSchemaTokens.REF);

                        bool verbConsumes;
                        bool verbProduces;

                        ResourceMethods.GetBodySupport(verbName, out verbConsumes, out verbProduces);

                        serviceActionBindings.Put(
                            BindingConstants.HTTP_BINDING_PROPERTY_NAME,
                            BindingConstants.BINDING_VERB_ATTRIBUTE_NAME,
                            verbName.ToUpper());

                        Annotations verbNodeAnnotations = Annotations.CreateImmutableOnRead();

                        foreach (Annotation annotation in verbNode.GetAnnotationsFromCache(descriptionNamespace, descriptionToken))

                            verbNodeAnnotations.AddAnnotation(annotation.Clone());

                        verbNode.ReadVerbNodeAnnotations(verbNodeAnnotations);

                        (verbNodeAnnotations as IFixable).SetFixed();

                        MarkupNode operationIdNode;

                        String operationId;

                        if (verbNode.TryGetChildNodeByName(SwaggerTokens.OPERATION_ID, out operationIdNode))

                            operationId = operationIdNode.Text;

                        else

                            operationId = null;

                        String serviceActionName = GetOperationNameFromBinding(operationId, serviceActionBindings);

                        InteractionMessage[] serviceActionInputs =
                            verbNode.ReadVerbNodeParameters(
                                descriptionCachePathUri, 
                                descriptionNamespaceUri, 
                                descriptionNamespace, 
                                serviceActionName, 
                                verbConsumes);

                        MarkupNode producesNode = verbNode.ReadProducesNode(verbProduces, serviceActionName);

                        InteractionMessage[] serviceActionOutputs;
                        InteractionMessage[] serviceActionFaults;

                        MarkupNode responsesNode;

                        if (verbNode.TryGetChildNodeByName(SwaggerTokens.RESPONSES, out responsesNode)) // contains responses array
                        {
                            MarkupNode[] successNodes = responsesNode.GetVerbNodeSuccessResponseNodes(producesNode != null);

                            serviceActionOutputs =
                                successNodes.ReadVerbNodeSuccessResponses(
                                    descriptionCachePathUri, 
                                    descriptionNamespaceUri, 
                                    descriptionNamespace, 
                                    producesNode, 
                                    serviceActionName);

                            IEnumerable<MarkupNode> faultNodes = responsesNode.Children.Where(c => !successNodes.Any(n => n == c));

                            serviceActionFaults =
                                faultNodes.ReadVerbNodeFaultResponses(
                                    descriptionCachePathUri, 
                                    descriptionNamespaceUri, 
                                    descriptionNamespace, 
                                    serviceActionName, 
                                    producesNode);
                        }
                        else
                        {
                            serviceActionOutputs = new InteractionMessage[0];
                            serviceActionFaults = new InteractionMessage[0];
                        }

                        Interaction interaction =
                            new Interaction(
                                serviceActionName,
                                Attributes.Empty,
                                verbNodeAnnotations,
                                serviceActionBindings,
                                InteractionStyles.RequestResponse,
                                serviceActionInputs,
                                serviceActionOutputs,
                                serviceActionFaults);

                        connection.AddInteraction(interaction);
                    }
                }
            }

            return description;
        }

        public static void ReadVerbNodeAnnotations(this MarkupNode methodNode, Annotations annotations)
        {
            Annotation annotation;

            if (methodNode.TryReadDocumentationNode(out annotation, SwaggerTokens.SUMMARY))

                annotations.AddAnnotation(annotation);

            if (methodNode.TryReadDocumentationNode(out annotation, SwaggerTokens.DESCRIPTION))

                annotations.AddAnnotation(annotation);

            if (methodNode.TryReadDocumentationNode(out annotation, SwaggerTokens.EXTERNAL_DOCS, SwaggerTokens.DESCRIPTION))

                annotations.AddAnnotation(annotation);

            if (methodNode.TryReadDocumentationNode(out annotation, SwaggerTokens.EXTERNAL_DOCS, SwaggerTokens.URL))

                annotations.AddAnnotation(annotation);
        }

        private static InteractionMessage[] ReadVerbNodeParameters(this MarkupNode verbNode, Uri descriptionCachePathUri, Uri descriptionNamespaceUri, string descriptionNamespace, String serviceActionName, bool verbConsumes)
        {
            MarkupNode consumesNode;

            if (verbNode.TryGetChildNodeByName(SwaggerTokens.CONSUMES, out consumesNode))
            {
                if (!verbConsumes)

                    throw new InvalidOperationException(String.Format("Verb for {0} does not support consuming any entity.", serviceActionName));
            }
            else
            {
                if (verbConsumes)
                {
                    consumesNode = new MarkupNode(verbNode, SwaggerTokens.CONSUMES, SwaggerTokens.CONSUMES);

                    MarkupNode consumesItemNode = new MarkupNode(consumesNode, SwaggerTokens.ITEM, SwaggerTokens.CONSUMES);

                    consumesItemNode.AddText("*/*");
                }
                else

                    consumesNode = null;
            }

            MarkupNode parametersNode;

            if (verbNode.TryGetChildNodeByName(SwaggerTokens.PARAMETERS, out parametersNode) &&
                parametersNode.Children.Count() > 0)
            {
                Domain actionMessagePartition = Domain.CreateImmutableOnRead();

                BindingProperties bindingProperties = new BindingProperties();

                foreach (MarkupNode parameterNode in parametersNode.Children)
                {
                    String parameterName = null;

                    MarkupNode parameterNameNode;

                    if (parameterNode.TryGetChildNodeByName(SwaggerTokens.NAME, out parameterNameNode))

                        parameterName = parameterNameNode.Text;

                    else

                        throw new InvalidOperationException(String.Format("Parameter missing required '{0}' field.", SwaggerTokens.NAME));

                    MarkupNode parameterInNode;

                    if (parameterNode.TryGetChildNodeByName(SwaggerTokens.IN, out parameterInNode))
                    {
                        string parameterIn = parameterInNode.Text;

                        if (parameterIn == SwaggerTokens.IN_BODY)  // ToDo:  Need to handle formData properly - see petstore-swagger-v2.json /pet/{petId} POST
                        {
                            Annotations parameterAnnotations = new Annotations();

                            Annotation parameterAnnotation;

                            if (parameterNode.TryReadDocumentationNode(out parameterAnnotation, SwaggerTokens.DESCRIPTION))

                                parameterAnnotations.Add(parameterAnnotation);

                            MarkupNode requiredNode;

                            if (!parameterNode.HasChildNodeByName(SwaggerTokens.REQUIRED))
                            {
                                requiredNode = new MarkupNode(parameterNode, SwaggerTokens.REQUIRED, "boolean");

                                requiredNode.AddText("false");
                            }

                            parameterNode.GetParameterStructureAttribute(out Structure parameterStructure);

                            parameterNode.GetParameterRequiredValueAttribute(out bool parameterValueIsRequired);

                            if (parameterNode.TryGetChildNodeByName(SwaggerTokens.SCHEMA, out MarkupNode schemaNode))
                            {
                                Potential schemaPotential;

                                schemaPotential =
                                    schemaNode.ReadSchemaNode(
                                        descriptionCachePathUri,
                                        descriptionNamespaceUri,
                                        descriptionNamespace,
                                        parameterName,
                                        parameterAnnotations,
                                        parameterStructure,
                                        parameterValueIsRequired);

                                actionMessagePartition.AddPotential(
                                    Refinement.TotalOrdering,
                                    schemaPotential);

                                bindingProperties.Put(
                                    BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                                    new BindingAttribute(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, schemaPotential.Name)
                                        .Itemize(consumesNode.Children.Select(c => new BindingAttribute(BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME, c.Text))).ToArray());
                            }
                            else

                                throw new InvalidOperationException(String.Format("Parameter {0} missing required 'schema' field.", parameterName));
                        }
                        else
                        {
                            MarkupNode parameterNodeProxy = parameterNode.Clone(null, parameterName);

                            if (!parameterNodeProxy.HasChildNodeByName(SwaggerTokens.TYPE))

                                throw new InvalidOperationException(String.Format("Parameter {0} missing required 'type' field.", parameterName));

                            EnsureParameterInRequiredNode(parameterName, parameterIn, parameterNodeProxy);

                            parameterNodeProxy.GetParameterStructureAttribute(out Structure parameterStructure);

                            // prepare to compile per json schema standards ...

                            parameterNodeProxy.TryRemoveChildNodeByName(SwaggerTokens.REQUIRED, out MarkupNode requiredNode);

                            parameterNodeProxy.EnsureIdNode(descriptionNamespaceUri);

                            RepresentationTypeKind representationTypeKind;

                            IQualifiable qualifiable =
                                parameterNodeProxy.CompileTypedNode(
                                    descriptionCachePathUri,
                                    descriptionNamespace,
                                    JsonSchemaTypes.JSON_SCHEMA_DRAFT_04_URI,
                                    SwaggerDataTypes.VERSION_2_0_URI,
                                    null,
                                    out representationTypeKind);

                            switch (representationTypeKind)
                            {
                                case RepresentationTypeKind.LexicalType:

                                    actionMessagePartition.AddPotential(
                                        Refinement.TotalCovering,
                                        ((ILexicalType)qualifiable).ConstructPotential(
                                            parameterName,
                                            structure: parameterStructure));

                                    break;

                                case RepresentationTypeKind.Potential:

                                    actionMessagePartition.AddPotential(
                                        Refinement.TotalOrdering,
                                        ((Potential)qualifiable).ConstructPotential(
                                            parameterName,
                                            structure: parameterStructure));

                                    break;

                                default:

                                    throw new NotImplementedException(String.Format("Parameter node kind '{0}' is not implemented.", representationTypeKind));
                            }

                            switch (parameterIn)
                            {
                                case SwaggerTokens.QUERY:

                                    bindingProperties.Put(
                                        BindingConstants.HTTP_URL_BINDING_ENCODED_PROPERTY_NAME,
                                        BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, parameterName);

                                    break;

                                case SwaggerTokens.HEADER:

                                    bindingProperties.Put(
                                        BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME,
                                        BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, parameterName);

                                    break;

                                case SwaggerTokens.IN_PATH:

                                    bindingProperties.Put(
                                        BindingConstants.HTTP_BINDING_URLREPLACEMENT_PROPERTY_NAME,
                                        BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, parameterName);

                                    break;

                                case SwaggerTokens.FORM_DATA:

                                    String[] consumeMimeTypes = consumesNode.Children.Select(c => c.Text).ToArray();

                                    if (consumeMimeTypes.Any(c => c != "application/x-www-form-urlencoded" && c != "multipart/form-data"))

                                        throw new InvalidOperationException(String.Format("Parameter type 'formData' is incompatible with specified consumes Mime Type(s) {0}.", String.Join(",", consumeMimeTypes)));

                                    bindingProperties.Put(
                                        BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                                        new BindingAttribute(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, parameterName)
                                        .Itemize(consumeMimeTypes.Select(m => new BindingAttribute(BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME, m))).ToArray());

                                    break;
                            }
                        }
                    }
                    else

                        throw new InvalidOperationException(String.Format("Required parameter argument '{0}' not found.", SwaggerTokens.IN));
                }

                return new InteractionMessage[]
                {
                    new InteractionMessage(
                        String.Format("{0}Parameters", serviceActionName),
                        bindingProperties,
                        actionMessagePartition)
                };
            }

            return new InteractionMessage[0];
        }

        private static void EnsureParameterInRequiredNode(string parameterName, string parameterIn, MarkupNode parameterNode)
        {
            if (!parameterNode.HasChildNodeByName(SwaggerTokens.REQUIRED))
            {
                if (parameterIn == SwaggerTokens.IN_PATH)

                    throw new InvalidOperationException(String.Format("Parameter {0} missing required 'required' field.", parameterName));

                MarkupNode requiredNode = new MarkupNode(parameterNode, SwaggerTokens.REQUIRED, "boolean");

                requiredNode.AddText("false");
            }
        }

        private static void GetParameterStructureAttribute(this MarkupNode parameterNode, out Structure parameterStructure)
        {
            String type;

            if (!parameterNode.TryGetChildNodeTextByName(SwaggerTokens.TYPE, out type))

                type = SwaggerTokens.TYPE_OBJECT;

            MarkupNode requiredNode;

            if (parameterNode.TryGetChildNodeByName(SwaggerTokens.REQUIRED, out requiredNode))
            {
                bool isRequired;

                if (bool.TryParse(requiredNode.Text, out isRequired))
                {
                    if (type == SwaggerTokens.TYPE_ARRAY)
                    {
                        if (isRequired)
                            parameterStructure = Structure.OneOrMoreTotallyOrderedElements;
                        else
                            parameterStructure = Structure.ZeroOrMoreTotallyOrderedElements;
                    }
                    else
                    {
                        if (isRequired)
                            parameterStructure = Structure.ExactlyOneElement;
                        else
                            parameterStructure = Structure.ZeroOrOneElement;
                    }
                }
                else

                    throw new InvalidOperationException(String.Format("Attribute '{0}' is not a boolean.", SwaggerTokens.REQUIRED));
            }
            else
            {
                if (type == SwaggerTokens.TYPE_ARRAY)
                    parameterStructure = Structure.ZeroOrMoreTotallyOrderedElements;
                else
                    parameterStructure = Structure.ExactlyOneElement;
            }
        }

        private static void GetParameterRequiredValueAttribute(this MarkupNode parameterNode, out bool parameterValueIsRequired)
        {
            if (parameterNode.TryGetChildNodeTextByName(SwaggerTokens.ALLOW_EMPTY_VALUE, out String allowEmptyText))
            {
                bool allowEmpty;

                if (!bool.TryParse(allowEmptyText, out allowEmpty))

                    throw new InvalidOperationException(String.Format("Attribute '{0}' is not a boolean.", SwaggerTokens.ALLOW_EMPTY_VALUE));

                if (allowEmpty)
                    parameterValueIsRequired = false;
                else
                    parameterValueIsRequired = true;
            }
            else

                parameterValueIsRequired = true;
        }

        private static Potential ReadSchemaNode(this MarkupNode schemaNode, Uri descriptionCachePathUri, Uri descriptionNamespaceUri, string descriptionNamespace, string potentialName)
        {
            return schemaNode.ReadSchemaNode(descriptionCachePathUri, descriptionNamespaceUri, descriptionNamespace, potentialName, null, null, true);
        }

        private static Potential ReadSchemaNode(this MarkupNode schemaNode, Uri descriptionCachePathUri, Uri descriptionNamespaceUri, string descriptionNamespace, string potentialName, Annotations parameterAnnotations, Structure parameterStructure, bool valueIsRequired)
        {
            IQualifiable qualifiable;
            RepresentationTypeKind representationTypeKind;

            MarkupNode schemaProxyNode = new MarkupNode(schemaNode.ParentNode, schemaNode.ParentNode.ParentNode.Name +"-"+ schemaNode.ParentNode.Name, JsonSchemaTokens.OBJECT_TYPE);

            schemaProxyNode.EnsureIdNode(descriptionNamespaceUri);

            while (schemaNode.Children.Count() > 0)
            {
                schemaNode.Children.First().TryMoveNode(schemaProxyNode);
            }

            qualifiable =
                schemaProxyNode.CompileTypedNode(
                    descriptionCachePathUri,
                    descriptionNamespace,
                    JsonSchemaTypes.JSON_SCHEMA_DRAFT_04_URI,
                    SwaggerDataTypes.VERSION_2_0_URI,
                    valueIsRequired,
                    out representationTypeKind);

            Potential schemaPotential;

            switch (representationTypeKind)
            {
                case RepresentationTypeKind.LexicalType:

                    schemaPotential =
                        ((ILexicalType)qualifiable).ConstructPotential(
                            potentialName,
                            annotations: parameterAnnotations,
                            structure: parameterStructure);

                    break;

                case RepresentationTypeKind.Potential:

                    schemaPotential =
                        ((Potential)qualifiable).ConstructPotential(
                            potentialName,
                            annotations: parameterAnnotations,
                            structure: parameterStructure);

                    break;

                default:

                    throw new NotImplementedException(String.Format("Paramater node kind '{0}' is not implemented.", representationTypeKind));
            }

            return schemaPotential;
        }

        private static MarkupNode ReadProducesNode(this MarkupNode verbNode, bool verbProduces, string serviceActionName)
        {
            MarkupNode producesNode;

            if (verbNode.TryGetChildNodeByName(SwaggerTokens.PRODUCES, out producesNode))
            {
                if (!verbProduces)

                    throw new InvalidOperationException(String.Format("Verb for {0} does not support producing any entity.", serviceActionName));
            }
            else
            {
                if (verbProduces)
                {
                    producesNode = new MarkupNode(verbNode, SwaggerTokens.PRODUCES, SwaggerTokens.PRODUCES);

                    MarkupNode producesItemNode = new MarkupNode(producesNode, SwaggerTokens.ITEM, SwaggerTokens.PRODUCES);

                    producesItemNode.AddText("*/*");
                }
            }

            return producesNode;
        }

        private static MarkupNode[] GetVerbNodeSuccessResponseNodes(this MarkupNode responsesNode, bool hasProducesNode)
        {
            MarkupNode[] successNodes;

            if (!responsesNode.TryGetChildNodesByNamePattern("^(2\\d{2}|2XX)$", out successNodes))
            {
                // From the spec: The  Responses Object  MUST contain at least one response code object, 
                // and it SHOULD be the response object for a successful operation call.
                // Hence, when no success response code object is found, we treat the first default 
                // response object (if any) as success.  If there is no default response objects we create one.
                // Any other response objects will be treated as fault response objects.

                MarkupNode successNode;

                if (!responsesNode.TryGetChildNodeByName(SwaggerTokens.DEFAULT, out successNode))
                {
                    successNode = new MarkupNode(responsesNode, SwaggerTokens.DEFAULT, SwaggerTokens.TYPE_OBJECT);

                    MarkupNode descriptionNode = new MarkupNode(successNode, SwaggerTokens.DESCRIPTION, "string");

                    descriptionNode.AddText("Default operation");
                }

                successNodes = new MarkupNode[] { successNode };
            }

            if (hasProducesNode)
            {
                foreach (MarkupNode successNode in successNodes)
                {
                    MarkupNode schemaNode;

                    if (!successNode.TryGetChildNodeByName(SwaggerTokens.SCHEMA, out schemaNode))
                    {
                        // set "blank" schema object to produce an any type ...

                        schemaNode = new MarkupNode(successNode, SwaggerTokens.SCHEMA, SwaggerTokens.TYPE_OBJECT);

                        MarkupNode typeNode;

                        if (!schemaNode.TryGetChildNodeByNamePattern(
                            BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME,
                            out typeNode))
                        {
                            typeNode =
                                new MarkupNode(
                                    schemaNode,
                                    BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME,
                                    "string");

                            typeNode.AddText(SwaggerTokens.TYPE_OBJECT);
                        }
                    }
                }
            }

            return successNodes;
        }

        private static InteractionMessage[] ReadVerbNodeSuccessResponses(this IEnumerable<MarkupNode> successNodes, Uri descriptionCachePathUri, Uri descriptionNamespaceUri, string descriptionNamespace, MarkupNode producesNode, string serviceActionName)
        {
            BindingProperties responseBindings = new BindingProperties();

            Domain actionMessageDomain = 
                successNodes.ReadResponsePartition(
                    descriptionCachePathUri, 
                    descriptionNamespaceUri,
                    descriptionNamespace,
                    producesNode,
                    responseBindings,
                    Represents.Success);

            InteractionMessage responseMessage =
                new InteractionMessage(
                    String.Format("{0}Response", serviceActionName),
                    responseBindings,
                    actionMessageDomain);

            return new InteractionMessage[] { responseMessage };
        }

        private static InteractionMessage[] ReadVerbNodeFaultResponses(this IEnumerable<MarkupNode> faultResponseNodes, Uri descriptionCachePathUri, Uri descriptionNamespaceUri, string descriptionNamespace, string serviceActionName, MarkupNode producesNode)
        {
            BindingProperties responseBindings = new BindingProperties();

            Domain actionMessageDomain = 
                faultResponseNodes.ReadResponsePartition(
                    descriptionCachePathUri,
                    descriptionNamespaceUri,
                    descriptionNamespace,
                    producesNode,
                    responseBindings,
                    Represents.Fault);

            InteractionMessage responseMessage =
                new InteractionMessage(
                    String.Format("{0}Fault", serviceActionName),
                    responseBindings,
                    actionMessageDomain);

            return new InteractionMessage[] { responseMessage };
        }

        private static Domain ReadResponsePartition(this IEnumerable<MarkupNode> responseNodes, Uri descriptionCachePathUri, Uri descriptionNamespaceUri, string descriptionNamespace, MarkupNode producesNode, BindingProperties bindings, Represents represents)
        {
            Domain responseDomain = Domain.CreateImmutableOnRead();

            foreach (MarkupNode responseNode in responseNodes)
            {
                List<Potential> responseParts = new List<Potential>();

                String responseCode;
                String responseName;
                String responseDescription;

                AdapterUtils.TryGetHttpStatusCodeDetails(responseNode.Name, out responseCode, out responseName, out responseDescription);

                Annotation annotation;

                if (!responseNode.TryReadDocumentationNode(out annotation, SwaggerTokens.DESCRIPTION))

                    annotation = new Annotation(SwaggerTokens.DESCRIPTION, Term.CreateImmutableOnRead(responseDescription));

                Annotations responseAnnotations = Annotations.CreateImmutableOnRead(annotation);

                bindings.Put(
                    BindingConstants.HTTP_BINDING_STATUS_CODE_PROPERTY_NAME,
                    new BindingAttribute(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, responseName));

                // ToDo: responseCode = "2XX"?

                QualifiedName builtInDataTypeQName =
                    SwaggerDataTypes.GetBuiltInScalarValueDataTypeName(StIntegerDataType.NAME);

                ScalarType responsePartType =
                    new ScalarType(
                        builtInDataTypeQName,
                        new ScalarTypeFacets(
                            new LexicalConstantValueFacet(Term.CreateImmutableOnRead(responseCode))));

                Attributes responseAttributes = new Attributes();

                MarkupNode headersNode;

                if (responseNode.TryGetChildNodeByName(SwaggerTokens.HEADERS, out headersNode))
                {
                    foreach (MarkupNode headerNode in headersNode.Children)
                    {
                        MarkupNode headerNodeProxy = headerNode.Clone(null);

                        String headerName = headerNodeProxy.Name;

                        Annotations headerAnnotations = new Annotations();

                        Annotation headerAnnotation;

                        if (headerNodeProxy.TryReadDocumentationNode(out headerAnnotation, SwaggerTokens.DESCRIPTION))

                            headerAnnotations.Add(headerAnnotation);

                        // ToDo: header structure might be 0 or 1?

                        headerNodeProxy.GetParameterStructureAttribute(out Structure headerStructure);

                        // prepare to compile per json schema standards ...

                        headerNodeProxy.TryRemoveChildNodeByName(SwaggerTokens.REQUIRED, out MarkupNode requiredNode);
                        
                        headerNodeProxy.EnsureIdNode(descriptionNamespaceUri);

                        RepresentationTypeKind representationTypeKind;

                        IQualifiable qualifiable =
                            headerNodeProxy.CompileTypedNode(
                                descriptionCachePathUri,
                                descriptionNamespace,
                                JsonSchemaTypes.JSON_SCHEMA_DRAFT_04_URI,
                                SwaggerDataTypes.VERSION_2_0_URI,
                                null,
                                out representationTypeKind);

                        switch (representationTypeKind)
                        {
                            case RepresentationTypeKind.LexicalType:

                                if (qualifiable is ScalarType)

                                    responseAttributes.Add(
                                        new Attribute(headerName, headerAnnotations, new Term(qualifiable as ScalarType))
                                    );

                                else

                                    throw new NotImplementedException(String.Format("Header node '{0}' must be a scalar value type.", headerName));

                                break;

                            default:

                                throw new NotImplementedException(String.Format("Header node '{0}' of kind '{1}' is not implemented.", headerName, representationTypeKind));
                        }

                        bindings.Put(
                            BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME,
                            new BindingAttribute(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, headerName));
                    }
                }

                MarkupNode examplesNode;

                if (responseNode.TryGetChildNodeByName(SwaggerTokens.EXAMPLES, out examplesNode))
                {
                    foreach (MarkupNode exampleNode in examplesNode.Children)

                        responseAnnotations.AddAnnotation(
                            new Annotation("Examples " + examplesNode.Name, MarkupFormatter<MarkupNode>.ToFormattedString(examplesNode, MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE, out Exception e) ?? throw e));
                }

                responseParts.Add(
                    responsePartType.ConstructPotential(
                        responseName,
                        attributes: responseAttributes,
                        annotations: responseAnnotations,
                        structure: Structure.ExactlyOneElement,
                        represents: represents));

                MarkupNode schemaNode;

                if (responseNode.TryGetChildNodeByName(SwaggerTokens.SCHEMA, out schemaNode))
                {
                    String responseContentName = responseName + SwaggerTokens.CONTENT_NAME_SUFFIX;

                    responseParts.Add(
                        schemaNode.ReadSchemaNode(
                            descriptionCachePathUri,
                            descriptionNamespaceUri, 
                            descriptionNamespace, 
                            responseContentName));

                    bindings.Put(
                        BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                        new BindingAttribute(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, responseContentName).Itemize(
                            producesNode.Children.Select(p => new BindingAttribute(BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME, p.Text))).ToArray());

                    responseDomain.AddPotentials(
                        Refinement.TotalCovering,
                        responseParts);
                }
                else

                    responseDomain.AddPotentials(Refinement.Singleton, responseParts);
            }

            if (responseDomain.Count > 1)

                responseDomain.SetRefinement(Refinement.Singleton);

            (responseDomain as IFixable).SetFixed(true);

            return responseDomain;
        }

        private static String GetOperationNameFromBinding(String operationId, BindingProperties serviceOperationBindings)
        {
            BindingAttribute attribute;

            String verb;

            if (serviceOperationBindings.TryGetAttribute(BindingConstants.HTTP_BINDING_PROPERTY_NAME, BindingConstants.BINDING_VERB_ATTRIBUTE_NAME, out attribute))
            {
                verb = attribute.Value;

                String path;

                if (serviceOperationBindings.TryGetAttribute(BindingConstants.HTTP_BINDING_OPERATION_PROPERTY_NAME, BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME, out attribute))
                {
                    path = attribute.Value;

                    return AdapterUtils.GetActionName(operationId, verb, path);
                }
            }

            return null;
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
        }

        public static void ReadDescriptionAnnotations(this MarkupNode rootNode, Annotations annotations)
        {
            MarkupNode infoNode;

            if (rootNode.TryGetChildNodeByName(SwaggerTokens.INFO, out infoNode))
            {
                Annotation infoNodeAnnotation;

                if (infoNode.TryReadDocumentationNode(out infoNodeAnnotation, SwaggerTokens.TITLE))

                    annotations.Add(infoNodeAnnotation);

                if (infoNode.TryReadDocumentationNode(out infoNodeAnnotation, SwaggerTokens.DESCRIPTION))

                    annotations.Add(infoNodeAnnotation);

                if (infoNode.TryReadDocumentationNode(out infoNodeAnnotation, SwaggerTokens.TERMS_OF_SERVICE))

                    annotations.Add(infoNodeAnnotation);

                if (infoNode.TryReadDocumentationNode(out infoNodeAnnotation, SwaggerTokens.CONTACT, SwaggerTokens.NAME))

                    annotations.Add(infoNodeAnnotation);

                if (infoNode.TryReadDocumentationNode(out infoNodeAnnotation, SwaggerTokens.CONTACT, SwaggerTokens.URL))

                    annotations.Add(infoNodeAnnotation);

                if (infoNode.TryReadDocumentationNode(out infoNodeAnnotation, SwaggerTokens.CONTACT, SwaggerTokens.EMAIL))

                    annotations.Add(infoNodeAnnotation);

                if (infoNode.TryReadDocumentationNode(out infoNodeAnnotation, SwaggerTokens.LICENSE, SwaggerTokens.NAME))

                    annotations.Add(infoNodeAnnotation);

                if (infoNode.TryReadDocumentationNode(out infoNodeAnnotation, SwaggerTokens.LICENSE, SwaggerTokens.URL))

                    annotations.Add(infoNodeAnnotation);

                if (infoNode.TryReadDocumentationNode(out infoNodeAnnotation, SwaggerTokens.VERSION))

                    annotations.Add(infoNodeAnnotation);
            }

            Annotation externalDocumentationAnnotation;

            if (rootNode.TryReadDocumentationNode(out externalDocumentationAnnotation, SwaggerTokens.EXTERNAL_DOCS, SwaggerTokens.DESCRIPTION))

                annotations.Add(externalDocumentationAnnotation);

            if (rootNode.TryReadDocumentationNode(out externalDocumentationAnnotation, SwaggerTokens.EXTERNAL_DOCS, SwaggerTokens.URL))

                annotations.Add(externalDocumentationAnnotation);
        }

        public static void ReadCompileAndCacheTags(this MarkupNode tagsRootNode, String descriptionNamespace, String descriptionToken)
        {
            MarkupNode tagsNode;

            if (tagsRootNode.TryGetChildNodeByName(SwaggerTokens.TAGS, out tagsNode))
            {
                foreach (MarkupNode tagNode in tagsNode.Children)
                {
                    MarkupNode tagNameNode;

                    if (tagNode.TryGetChildNodeByName(SwaggerTokens.NAME, out tagNameNode))
                    {
                        Annotations annotations = Annotations.CreateImmutableOnRead();

                        Attributes tagAttributes = Attributes.CreateImmutableOnRead().AddAttribute(new Attribute(SwaggerTokens.TAG, Represents.MetaData, new Term(tagNameNode.Text)));

                        if (tagNode.TryReadDocumentationNode(out Annotation description, tagAttributes.Clone(), SwaggerTokens.DESCRIPTION))

                            annotations.Add(description);

                        if (tagNode.TryReadDocumentationNode(out Annotation externalDocsDescription, tagAttributes.Clone(), SwaggerTokens.EXTERNAL_DOCS, SwaggerTokens.DESCRIPTION))

                            annotations.Add(externalDocsDescription);

                        if (tagNode.TryReadDocumentationNode(out Annotation externalDocsUrl, tagAttributes.Clone(), SwaggerTokens.EXTERNAL_DOCS, SwaggerTokens.URL))

                            annotations.Add(externalDocsUrl);

                        QualifiedName tagQualifiedName = QualifiedName.CreateImmutableFromPrefixAndLocalName(descriptionToken, tagNameNode.Text);

                        ModelCaches.AddToAnnotationGroupCache(
                            descriptionNamespace,
                            new AnnotationGroup(
                                tagQualifiedName,
                                annotations));
                    }
                }
            }
        }

        public static IEnumerable<Annotation> GetAnnotationsFromCache(this MarkupNode methodNode, String descriptionNamespace, String descriptionToken)
        {
            List<Annotation> annotations = new List<Annotation>();

            String[] tags;

            if (methodNode.TryGetChildNodeTextByNameAsArray(SwaggerTokens.TAGS, out tags))
            {
                foreach (String tag in tags)
                {
                    QualifiedName tagQualifiedName = QualifiedName.CreateImmutableFromPrefixAndLocalName(descriptionToken, tag);

                    annotations.AddRange(
                        ModelCaches.GetAnnotationsFromAnnotationGroupCache(descriptionNamespace, tagQualifiedName));
                }
            }

            return annotations;
        }

        //COMMON WITH RAML READER?
        public static bool TryReadDocumentationNode(this MarkupNode node, out Annotation annotation, params string[] entryNames)
        {
            return TryReadDocumentationNode(node, out annotation, null, entryNames);
        }

        public static bool TryReadDocumentationNode(this MarkupNode node, out Annotation annotation, Attributes attributes, params string[] entryNames)
        {
            for (int i = 0; i < entryNames.Length; i++)
            {
                if (node != null)

                    node = node.Children.FirstOrDefault(c => c.Name == entryNames[i]);
            }

            if (node != null)
            {
                if (attributes == null)

                    annotation =
                        new Annotation(
                            String.Join(" ", entryNames),
                            Term.CreateImmutableOnRead(node.Text));
                else

                    annotation =
                        new Annotation(
                            attributes,
                            String.Join(" ", entryNames),
                            Term.CreateImmutableOnRead(node.Text));

                return true;
            }

            annotation = null;

            return false;
        }
    }
}
