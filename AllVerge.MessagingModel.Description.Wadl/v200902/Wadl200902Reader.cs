using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using AllVerge.Core.Resource;

using AllVerge.Core.Model;
using AllVerge.Core.Model.Caches;
using AllVerge.Core.Model.Actuals;
using AllVerge.Core.Model.LexicalTypes;
using AllVerge.Core.Model.LexicalTypes.Structures;

using AllVerge.Core.Markup.Formatters;
using AllVerge.Core.Markup.Xml;
using AllVerge.Core.Markup.Xml.Schema;

using AllVerge.Core.Model.XML;
using AllVerge.Core.Model.Xml.Adapters;
using AllVerge.Core.Model.XMLSchema;
using AllVerge.Core.Model.XMLSchema.Adapters;

using AllVerge.Core.ServiceModel.Description.Model;
using AllVerge.Core.ServiceModel.Description.Adapters;

using wadl.dev.java.net._2009._02;
using AllVerge.Core.ServiceModel.Methods;

namespace AllVerge.Core.ServiceModel.Description.Wadl.v200902
{
    public class Wadl200902Reader : IDescriptionReader
    {
        private Uri descriptionImportsCachePathUri;

        static Wadl200902Reader()
        {
            MarkupFormatter<IRepresentation>.TryRegister(new XmlRepresentationFormatter());
        }

        public Wadl200902Reader(String descriptionImportsCachePath)
        {
            this.descriptionImportsCachePathUri = new Uri(descriptionImportsCachePath);
        }

        private string descriptionNamespace;

        public ProtocolDescription ReadDescription(string descriptionLocator, bool canReadFromCache = true)
        {
            Uri descriptionUri = new Uri(descriptionLocator);

            Uri descriptionBaseUri;
            String descriptionToken;

            UriUtils.TryGetResourceName(descriptionUri, out descriptionBaseUri, out descriptionToken);

            this.descriptionNamespace = descriptionUri.ToNamespaceUri().AbsoluteUri;

            XsDataType.TryInitializeBuiltInTypes();

            QualifiedName.ClearCurrentNamespaceManager();

            List<XmlQualifiedName> applicationNameSpaces;

            application app = ReadApplication(descriptionLocator, canReadFromCache, out applicationNameSpaces);

            applicationNameSpaces.Add(
                new XmlQualifiedName(descriptionToken, descriptionNamespace));

            if (!QualifiedName.TrySetCurrentNamespaceManager(this.descriptionNamespace))
            {
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());

                foreach (XmlQualifiedName name in applicationNameSpaces)
                {
                    namespaceManager.AddNamespace(name.Name, name.Namespace);
                }

                QualifiedName.SetCurrentNamespaceManager(this.descriptionNamespace, namespaceManager);
            }

            QualifiedName fullyQualifiedContractName =
                QualifiedName.CreateImmutable(descriptionNamespace, descriptionToken);

            QualifiedName.TryGetCurrentNamespaceManager(out XmlNamespaceManager currentNamespaceManager, true);

            new XmlSchemas().SetSchemasCache(
                currentNamespaceManager,
                descriptionLocator,
                this.descriptionNamespace);

            app.grammars.doc.ReadSchemaDocs().PutDocumentationCache(this.descriptionNamespace);

            app.grammars.include.ReadSchemaIncludes(descriptionLocator).PutSchemasCache(currentNamespaceManager, this.descriptionNamespace);

            app.grammars.Any.PutSchemasCache(currentNamespaceManager, descriptionLocator, this.descriptionNamespace);

            XmlSchemaToModelExtensions.CompileAndCacheModel(this.descriptionNamespace);

            XmlQualifiedName targetQualifiedName;

            if (applicationNameSpaces.Count() > 1)
            {
                IEnumerable<ModelProperties> modelProperties = ModelCaches.GetModelPropertiesFromCache(this.descriptionNamespace);

                IEnumerable<XmlQualifiedName> intersectionApplicationAndSchemaTargetNamespaces = 
                    applicationNameSpaces.Where(q => modelProperties.Any(p => p.Namespace == q.Namespace));

                targetQualifiedName = intersectionApplicationAndSchemaTargetNamespaces.FirstOrDefault();

                if (targetQualifiedName == null)

                    targetQualifiedName = applicationNameSpaces.First();
            }
            else if (applicationNameSpaces.Count() > 0)
            {
                targetQualifiedName = applicationNameSpaces.First();
            }
            else
            {
                targetQualifiedName = new XmlQualifiedName(descriptionToken, descriptionNamespace);
            }

            ProtocolDescription description =
                new ProtocolDescription(
                    fullyQualifiedContractName,
                    Attributes.Empty,
                    app.doc.ReadSchemaDocs().ReadAnnotations(), // documentation cache?
                    DocumentType.WADL200902,
                    descriptionLocator,
                    applicationNameSpaces.ReadNamespaces(this.descriptionNamespace, BindingConstants.XML_SCHEMA_NAMESPACE, BindingConstants.XML_SCHEMA_INSTANCE_NAMESPACE));

            foreach (resources resources in app.resources)
            {
                Uri baseUri = new Uri(resources.@base);

                String serviceName;

                if (!baseUri.TryGetResourcePath(out serviceName))

                    serviceName = baseUri.Host;

                List<Connection> connections = new List<Connection>();

                //resourceSet.Any;
                //resourceSet.AnyAttr;

                foreach (resource resource in resources.resource)
                {
                    Interaction[] interactions = this.GetServiceActions(resource, null);

                    BindingProperties connectionBindingProperties = new BindingProperties();

                    connectionBindingProperties.Add(
                        BindingProperty.CreateImmutable(
                            BindingConstants.HTTP_BINDING_PROPERTY_NAME,
                            BindingProperties.Empty,
                            BindingAttributes.CreateImmutableOnRead(
                                BindingConstants.BINDING_TRANSPORT_ATTRIBUTE_NAME,
                                BindingConstants.BINDING_TRANSPORT_ATTIBUTE_VALUE_HTTP)));

                    connectionBindingProperties.Add(
                        BindingProperty.CreateImmutable(
                            BindingConstants.HTTP_BINDING_ADDRESS_PROPERTY_NAME,
                            BindingProperties.Empty,
                            BindingAttributes.CreateImmutableOnRead(
                                BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME, 
                                baseUri.AbsoluteUri)));

                    connections.Add(
                        new Connection(
                            AdapterUtils.GetConnectionName(resource.id, resource.path), 
                            Attributes.Empty,
                            resource.doc.ReadSchemaDocs().ReadAnnotations(), 
                            connectionBindingProperties, 
                            interactions));
                }

                description.AddConnector(
                    new Connector(
                        QualifiedName.CreateImmutableFromPrefixAndLocalName(targetQualifiedName.Name, serviceName), 
                        resources.doc.ReadSchemaDocs().ReadAnnotations(), 
                        connections.ToArray()));
            }

            return description;
        }

        private Interaction[] GetServiceActions(resource resource, string ancestorPath)
        {
            List<Interaction> interactions = new List<Interaction>();

            foreach (resource childResource in resource.Items.OfType<resource>())
            {
                interactions.AddRange(
                    GetServiceActions(
                        childResource,
                        ancestorPath == null ? resource.path : ancestorPath + resource.path));
            }

            BindingProperties parameterBindingProperties = new BindingProperties();

            Potential[] resourceParameterPotentials = 
                resource.param.ReadParameters(this.descriptionNamespace, parameterBindingProperties);

            foreach (method verb in resource.Items.OfType<method>())
            {
                String path = ancestorPath == null ? resource.path : ancestorPath + resource.path;

                String verbName = verb.name;

                bool verbConsumesBody;
                bool verbProducesBody;

                ResourceMethods.GetBodySupport(verbName, out verbConsumesBody, out verbProducesBody);

                BindingProperties operationBindingProperties = new BindingProperties();
                    
                operationBindingProperties.Put(
                    BindingConstants.HTTP_BINDING_PROPERTY_NAME,
                    BindingConstants.BINDING_VERB_ATTRIBUTE_NAME,
                    verbName.ToUpper());

                operationBindingProperties.Put(
                    BindingConstants.HTTP_BINDING_OPERATION_PROPERTY_NAME,
                    BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME,
                    path);

                String actionName = AdapterUtils.GetActionName(verb.id, verb.name, path);

                InteractionMessage[] inputs = GetInputs(actionName, parameterBindingProperties.Clone(), resourceParameterPotentials, verbConsumesBody, verb.request);
                InteractionMessage[] outputs = GetOutputs(actionName, verbProducesBody, Represents.Information, verb.response.Where(r => (r.status == null ? true : r.status.Any(s => s < 400))));
                InteractionMessage[] faults = GetOutputs(actionName, verbProducesBody, Represents.Fault, verb.response.Where(r => (r.status == null ? false : r.status.Any(s => s >= 400))));

                Interaction interaction = 
                    new Interaction(
                        actionName,
                        Attributes.Empty,
                        resource.doc.ReadSchemaDocs().ReadAnnotations(), 
                        operationBindingProperties,
                        InteractionStyles.RequestResponse,
                        inputs, 
                        outputs, 
                        faults);

                interactions.Add(interaction);
            }

            return interactions.ToArray();
        }

        private InteractionMessage[] GetInputs(String operationName, BindingProperties parameterBindingProperties, Potential[] resourceParameterPotentials, bool verbAllowsRepresentation, request request)
        {
            List<InteractionMessage> inputs = new List<InteractionMessage>();

            if (request != null)
            {
                int index = 0;

                if (request.representation != null && request.representation.Length > 0)
                {
                    if (request.representation.Length > 1)
                    {
                        if (!verbAllowsRepresentation)

                            throw new InvalidOperationException("Verb does not allow a representation.");

                        foreach (representation representation in request.representation)
                        {
                            inputs.Add(
                                GetInput(
                                    operationName,
                                    parameterBindingProperties,
                                    resourceParameterPotentials,
                                    request,
                                    representation,
                                    ++index));
                        }
                    }
                    else

                        inputs.Add(
                            GetInput(
                                operationName,
                                parameterBindingProperties,
                                resourceParameterPotentials,
                                request,
                                request.representation[0],
                                index));
                }
                else

                    inputs.Add(
                        GetInput(
                            operationName,
                            parameterBindingProperties,
                            resourceParameterPotentials,
                            request,
                            null,
                            index));
            }

            return inputs.ToArray();
        }

        private InteractionMessage GetInput(string operationName, BindingProperties parameterBindingProperties, Potential[] resourceParameterPotentials, request request, representation representation, int index)
        {
            Domain operationMessageDomain = Domain.CreateImmutableOnRead();

            operationMessageDomain.AddPotentials(
                Refinement.TotalCovering, 
                resourceParameterPotentials);

            Potential[] requestParameterPotentials = 
                request.param.ReadParameters(this.descriptionNamespace, parameterBindingProperties);

            operationMessageDomain.AddPotentials(
                Refinement.TotalCovering, 
                requestParameterPotentials);

            if (representation != null)
            {
                operationMessageDomain.AddPotential(
                    Refinement.TotalOrdering,
                    WalkRepresentation(
                        parameterBindingProperties,
                        representation,
                        Represents.Information));
            }

            return 
                new InteractionMessage(
                    String.Format("{0}Parameters{1}", operationName, index > 0 ? index.ToString() : String.Empty),
                    request.doc.ReadSchemaDocs().ReadAnnotations(),
                    parameterBindingProperties,
                    operationMessageDomain);
        }

        private InteractionMessage[] GetOutputs(String operationName, bool verbAllowsRepresentation, Represents representationUsage, IEnumerable<response> responses)
        {
            List<InteractionMessage> outputs = new List<InteractionMessage>();

            if (responses != null)
            {
                foreach (response response in responses)
                {
                    int index = 0;

                    if (response.representation != null && response.representation.Length > 0)
                    {
                        if (response.representation.Length > 1)
                        {
                            foreach (representation representation in response.representation)
                            {
                                outputs.Add(
                                    GetOutput(
                                        operationName, 
                                        response, 
                                        verbAllowsRepresentation, 
                                        representationUsage, 
                                        representation, 
                                        ++index));
                            }
                        }
                        else

                            outputs.Add(
                                GetOutput(
                                    operationName, 
                                    response, 
                                    verbAllowsRepresentation, 
                                    representationUsage, 
                                    response.representation[0], 
                                    index));
                    }
                    else

                        outputs.Add(
                            GetOutput(
                                operationName, 
                                response, 
                                verbAllowsRepresentation, 
                                representationUsage, 
                                null, 
                                index));
                }
            }

            return outputs.ToArray();
        }

        private InteractionMessage GetOutput(string operationName, response response, bool verbAllowsRepresentation, Represents representationSpecifies, representation representation, int index)
        {
            BindingProperties bindingProperties = new BindingProperties();

            Domain domain =
                GetOutputDomain(
                    bindingProperties,
                    verbAllowsRepresentation,
                    response.status,
                    response.param,
                    representation,
                    representationSpecifies);

            //response.Any;
            //response.AnyAttr;

            String messageName;

            if (representationSpecifies == Represents.Fault)

                messageName =
                    String.Format("{0}Fault", operationName);

            else

                messageName =
                    String.Format("{0}Response", operationName);

            return
                new InteractionMessage(
                    messageName,
                    response.doc.ReadSchemaDocs().ReadAnnotations(),
                    bindingProperties,
                    domain);
        }

        private Domain GetOutputDomain(BindingProperties bindingProperties, bool verbAllowsRepresentation, uint[] statusCodes, param[] parameters, representation representation, Represents representationUsage)
        {
            String responseCode;
            String responseName;
            String responseDescription;

            Domain responseDomain = Domain.CreateImmutableOnRead();

            if (statusCodes == null || statusCodes.Length == 0)

                statusCodes = new uint[] { 200 };

            foreach (uint statusCode in statusCodes)
            {
                if (AdapterUtils.TryGetHttpStatusCodeDetails(statusCode.ToString(), out responseCode, out responseName, out responseDescription))
                {
                    bindingProperties.Put(
                        BindingConstants.HTTP_BINDING_STATUS_CODE_PROPERTY_NAME,
                        BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, responseName));

                    responseDomain.AddPotential(
                        Refinement.Singleton,
                        new Actual(
                            responseName,
                            Attributes.Empty,
                            Annotations.CreateImmutableOnRead(responseDescription),
                            Structure.ExactlyOneElement,
                            XsDataType.CreateBuiltInScalarValueType(XsdStringDataType.NAME),
                            representationUsage,
                            Term.CreateImmutableOnRead(responseCode)));
                }
            }

            if (parameters != null)

                foreach (param param in parameters)
                {
                    bindingProperties.Put(
                        BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME,
                        BindingAttribute.CreateMutable(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, param.name));

                    responseDomain.AddPotential(
                        Refinement.TotalOrdering,
                        param.GetOrReadScalarPotential(this.descriptionNamespace));
                }

            if (representation != null)
            {
                responseDomain.AddPotential(
                    Refinement.TotalOrdering,
                    WalkRepresentation(
                        bindingProperties,
                        representation,
                        representationUsage));
            }

            return responseDomain;
        }

        private Potential WalkRepresentation(BindingProperties bindingProperties, representation representation, Represents representationUsage)
        {
            if (representation.id != null)

                throw new NotImplementedException(representation.href);

            //representation.AnyAttr;
            //representation.doc;
            //representation.param;
            //representation.element;
            //representation.Any;
            //representation.profile;

            if (representation.element != null)
            {
                switch (representation.mediaType)
                {
                    case "text/xml":
                    case "application/xml":
                        break;
                    default:
                        throw new InvalidOperationException("When representation element is present, mediaType must specify an XML-based representation.");
                }

                String agentName = representation.element.Name;

                bindingProperties.Put(
                    BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                    BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME,
                    representation.mediaType);

                bindingProperties.Put(
                    BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                    BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME,
                    agentName);
                
                return ModelCaches.GetPotentialFromCache(this.descriptionNamespace, representation.element.ToQualifiedName(), Represents.Information);

                //XmlSchemaType schemaType;

                //schemaType = representation.element.GetElementTypeDefinition(this.targetNamespace);

                //if (schemaType == null)

                //    throw new InvalidOperationException(String.Format("Could not resolve element {0} in namespace {1}", representation.element, this.targetNamespace));

                //if (schemaType is XmlSchemaSimpleType)
                //{
                //    XmlSchemaSimpleType schemaSimpleType = (XmlSchemaSimpleType)schemaType;

                //    return
                //        new CompactMessagePart(
                //            messagePartName,
                //            representation.doc.ToSchemaDocs().ReadAnnotation(),
                //            new CompactMessagePart[0],
                //            MessagePartCardinality.ExactlyOne,
                //            schemaSimpleType.ReadCompactPartType(this.targetNamespace),
                //            CompactPartLexicalValue.Create(),
                //            representationUsage);
                //}
                //else if (schemaType is XmlSchemaComplexType)
                //{
                //    XmlSchemaComplexType schemaComplexType = (XmlSchemaComplexType)schemaType;

                //    // ToDo:  handle schemaComplexType.IsMixed

                //    return
                //        new CompoundMessagePart(
                //            messagePartName,
                //            representation.doc.ToSchemaDocs().ReadAnnotation(),
                //            schemaComplexType.Attributes.ReadSchemaAttributes(targetNamespace, false),
                //            MessagePartCardinality.ExactlyOne,
                //            schemaComplexType.ReadCompoundPartType(targetNamespace, representationUsage, false));
                //}
                //else

                //    throw new InvalidOperationException("Unknown element part type: " + schemaType.Name);
            }

            switch (representation.mediaType)
            {
                case "application/x-www-form-urlencoded":
                case "multipart/form-data":
                    break;
                default:
                    throw new NotImplementedException(representation.mediaType);
            }

            bindingProperties.Put(
                BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME,
                representation.mediaType);

            List<Potential> scalarPotentials = new List<Potential>();

            foreach (param param in representation.param)
            {
                param.doc.ReadSchemaDocs();

                QualifiedName parameterName = param.name;

                switch (param.style)
                {
                    case ParamStyle.header:
                    case ParamStyle.matrix:
                    case ParamStyle.template:
                        throw new InvalidOperationException(String.Format("Parameter style {0} is not allowed for a representation parameter.", param.style));
                    case ParamStyle.plain:
                    case ParamStyle.query:
                        break;
                }

                Potential scalarPotential = param.GetOrReadScalarPotential(this.descriptionNamespace);

                bindingProperties.Put(
                    BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME,
                    BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME,
                    scalarPotential.Name);

                scalarPotentials.Add(
                    scalarPotential);
            }

            //ToDo: representation.Any -> add to childParts

            return
                new Potential(
                    representation.id,
                    representation.AnyAttr.ReadAttributes(Represents.MetaData),
                    representation.doc.ReadSchemaDocs().ReadAnnotations(),
                    Structure.ExactlyOneElement,
                    new DomainType(
                        Domain.CreateImmutableOnRead().AddPotentials
                        (
                            Refinement.TotalOrdering,
                            scalarPotentials.ToArray())
                        )
                    );
        }

        private application ReadApplication(string descriptionLocator, bool canReadFromCache, out List<XmlQualifiedName> applicationNameSpaces)
        {
            Uri descriptionLocatorUri = new Uri(descriptionLocator);

            Stream descriptionStream;
            String descriptionMediaType;
            String descriptionMediaTypeVariant;
            Encoding descriptionEncoding;

            descriptionLocatorUri.DownloadResourceAndGetResourceMediaType(
                canReadFromCache ? descriptionImportsCachePathUri : null,
                out descriptionStream,
                out descriptionMediaType,
                out descriptionMediaTypeVariant,
                out descriptionEncoding);

            using (descriptionStream)
            {
                XmlElement applicationElement;
                Exception e;

                if (XmlFormatterExtensions.ReadXml(descriptionStream, out applicationElement, out e))
                {
                    applicationNameSpaces = applicationElement.Attributes.Cast<XmlAttribute>().Where(a => 
                        a.Prefix == BindingConstants.XML_NS_PREFIX && a.Value != BindingConstants.XML_SCHEMA_NAMESPACE && a.Value != BindingConstants.XML_SCHEMA_INSTANCE_NAMESPACE).Aggregate(new List<XmlQualifiedName>(), (l, a) => { l.Add(new XmlQualifiedName(a.LocalName, a.Value)); return l; });

                    return applicationElement.Deserialize<application>();
                }

                throw e;
            }
        }
    }
}
