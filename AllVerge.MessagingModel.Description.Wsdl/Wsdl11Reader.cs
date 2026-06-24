using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

#region WebServiceDescriptionTypes

using System.Web.Services.Configuration;

using WebService = System.Web.Services.Description.Service;
using WSDescription = System.Web.Services.Description.ServiceDescription;
using WSExtensionElement = System.Web.Services.Description.ServiceDescriptionFormatExtension;
using WSImport = System.Web.Services.Description.Import;
using WSExtensionElementCollection = System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection;
using WSPort = System.Web.Services.Description.Port;
using WSPortBinding = System.Web.Services.Description.Binding;
using HttpBinding = System.Web.Services.Description.HttpBinding;
using SoapBinding = System.Web.Services.Description.SoapBinding;
using Soap12Binding = System.Web.Services.Description.Soap12Binding;
using HttpAddressBinding = System.Web.Services.Description.HttpAddressBinding;
using SoapAddressBinding = System.Web.Services.Description.SoapAddressBinding;
using Soap12AddressBinding = System.Web.Services.Description.Soap12AddressBinding;
using HttpOperationBinding = System.Web.Services.Description.HttpOperationBinding;
using SoapOperationBinding = System.Web.Services.Description.SoapOperationBinding;
using Soap12OperationBinding = System.Web.Services.Description.Soap12OperationBinding;
using SoapBodyBinding = System.Web.Services.Description.SoapBodyBinding;
using Soap12BodyBinding = System.Web.Services.Description.Soap12BodyBinding;
using SoapFaultBinding = System.Web.Services.Description.SoapFaultBinding;
using Soap12FaultBinding = System.Web.Services.Description.Soap12FaultBinding;
using SoapHeaderBinding = System.Web.Services.Description.SoapHeaderBinding;
using SoapHeaderFaultBinding = System.Web.Services.Description.SoapHeaderFaultBinding;
using WSPortType = System.Web.Services.Description.PortType;
using WSOperationBinding = System.Web.Services.Description.OperationBinding;
using WSOperation = System.Web.Services.Description.Operation;
using WSOperationFlow = System.Web.Services.Description.OperationFlow;
using WSOperationMessage = System.Web.Services.Description.OperationMessage;
using HttpUrlEncodedBinding = System.Web.Services.Description.HttpUrlEncodedBinding;
using HttpUrlReplacementBinding = System.Web.Services.Description.HttpUrlReplacementBinding;
using MimeContentBinding = System.Web.Services.Description.MimeContentBinding;
using MimeMultipartRelatedBinding = System.Web.Services.Description.MimeMultipartRelatedBinding;
using MimePart = System.Web.Services.Description.MimePart;
using MimePartCollection = System.Web.Services.Description.MimePartCollection;
using MimeTextBinding = System.Web.Services.Description.MimeTextBinding;
using MimeTextMatch = System.Web.Services.Description.MimeTextMatch;
using MimeTextMatchCollection = System.Web.Services.Description.MimeTextMatchCollection;
using MimeXmlBinding = System.Web.Services.Description.MimeXmlBinding;
using WSMessage = System.Web.Services.Description.Message;
using WSMessagePart = System.Web.Services.Description.MessagePart;
using WSMessagePartCollection = System.Web.Services.Description.MessagePartCollection;
using WSInputBinding = System.Web.Services.Description.InputBinding;
using WSOutputBinding = System.Web.Services.Description.OutputBinding;
using WSOperationFault = System.Web.Services.Description.OperationFault;
using WSFaultBinding = System.Web.Services.Description.FaultBinding;

#endregion

using AllVerge.Core.Collections;
using AllVerge.Core.Resource;

using AllVerge.Core.Markup.Markdown;
using AllVerge.Core.Markup.Xml;
using AllVerge.Core.Markup.Xml.Schema;

namespace AllVerge.Core.ServiceModel.Description.Wsdl
{
    using AllVerge.Core.Markup.Formatters;

    using AllVerge.Core.Model;
    using AllVerge.Core.Model.Caches;
    using AllVerge.Core.Model.Actuals;
    using AllVerge.Core.Model.LexicalTypes;
    using AllVerge.Core.Model.LexicalTypes.Facets;
    using AllVerge.Core.Model.LexicalTypes.Structures;

    using AllVerge.Core.Model.Markdown;
    using AllVerge.Core.Model.XML;
    using AllVerge.Core.Model.XMLSchema;
    using AllVerge.Core.Model.XMLSchema.Adapters;

    using AllVerge.Core.ServiceModel.Description.Model;
    using AllVerge.Core.ServiceModel.Description.Adapters;
    using AllVerge.Core.ServiceModel.Messaging;

    using AllVerge.Core.Model.DataTypes.Abstractions;

    using Microsoft.Xml.Serialization;

    internal class Wsdl11Reader : IDescriptionReader
    {
        private Uri descriptionImportsCachePathUri;

        static Wsdl11Reader()
        {
            MarkupFormatter<IRepresentation>.TryRegister(new XmlRepresentationFormatter());
            MarkupFormatter<IRepresentation>.TryRegister(new MarkdownModelFormatter());
            AbstractDataTypes.TryInitializeBuiltInTypes();
            XsDataType.TryInitializeBuiltInTypes();
        }

        public Wsdl11Reader(String descriptionImportsCachePath)
        {
            this.descriptionImportsCachePathUri = new Uri(descriptionImportsCachePath);
        }

        private string descriptionNamespace;

        public ProtocolDescription ReadDescription(string descriptionLocator, bool canReadFromCache = true)
        {
            Uri descriptionLocatorUri = new Uri(descriptionLocator);

            WSDescription serviceDescription = 
                ReadWSDescription(
                    descriptionLocatorUri, 
                    canReadFromCache, 
                    out IEnumerable<XmlQualifiedName> descriptionNameSpaces, 
                    out IEnumerable<XmlSchema> descriptionSchemas);

            this.descriptionNamespace = descriptionLocatorUri.ToNamespaceUri().AbsoluteUri;

            XmlQualifiedName descriptionQualifiedName = 
                descriptionNameSpaces.FirstOrDefault(n => n.Namespace == serviceDescription.TargetNamespace && !String.IsNullOrWhiteSpace(n.Name));

            String descriptionToken;

            if (descriptionQualifiedName != null)

                descriptionToken = descriptionQualifiedName.Name;

            else if (!String.IsNullOrWhiteSpace(serviceDescription.Name))
            {
                descriptionQualifiedName = new XmlQualifiedName(serviceDescription.Name, this.descriptionNamespace);

                descriptionToken = descriptionQualifiedName.Name;

                descriptionNameSpaces =
                    descriptionNameSpaces.Concat(
                        descriptionQualifiedName.ToEnumerable());
            }
            else
            {
                UriUtils.TryGetResourceName(descriptionLocatorUri, out Uri descriptionBaseUri, out String descriptionName);

                descriptionQualifiedName = new XmlQualifiedName(descriptionName, this.descriptionNamespace);

                descriptionToken = descriptionQualifiedName.Name;

                descriptionNameSpaces =
                    descriptionNameSpaces.Concat(
                        descriptionQualifiedName.ToEnumerable());
            }

            QualifiedName.ClearCurrentNamespaceManager();

            if (!QualifiedName.TrySetCurrentNamespaceManager(this.descriptionNamespace)) {

                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());

                foreach (XmlQualifiedName name in descriptionNameSpaces.ToFront(n => String.IsNullOrWhiteSpace(n.Name)))
                {
                    namespaceManager.AddNamespace(name.Name, name.Namespace);
                }

                QualifiedName.SetCurrentNamespaceManager(this.descriptionNamespace, namespaceManager);
            }

            if (serviceDescription.Types.Extensions.Count > 0)
            {
                XmlElement extensionElement = serviceDescription.Types.Extensions.Cast<XmlElement>().First();

                throw new XmlSchemaException(String.Format("Type extension '{0}' in namespace '{1}' not supported at this time.", extensionElement.Name, extensionElement.NamespaceURI));
            }

            QualifiedName.TryGetCurrentNamespaceManager(out XmlNamespaceManager currentNamespaceManager, true);

            descriptionSchemas.SetSchemasCache(
                currentNamespaceManager,
                descriptionLocator,
                this.descriptionNamespace);

            if (canReadFromCache)

                serviceDescription.Imports.Cast<WSImport>().Select(i => new SchemaImport(i.Namespace, i.Location, descriptionLocator, descriptionImportsCachePathUri)).PutSchemasCache(currentNamespaceManager, descriptionNamespace);

            else

                serviceDescription.Imports.Cast<WSImport>().Select(i => new SchemaImport(i.Namespace, i.Location, descriptionLocator)).PutSchemasCache(currentNamespaceManager, descriptionNamespace);

            XmlSchemaToModelExtensions.CompileAndCacheModel(this.descriptionNamespace);

            ProtocolDescription description =
                new ProtocolDescription(
                    descriptionQualifiedName.ToQualifiedName(),
                    Attributes.Empty,
                    ReadAnnotations(serviceDescription.Documentation),
                    DocumentType.WSDL11,
                    descriptionLocator,
                    descriptionNameSpaces.Select(n => { QualifiedName n1 = n.ToQualifiedName(); return n1; }).ToArray());

            description.NamespaceManager = currentNamespaceManager;

            foreach (WebService service in serviceDescription.Services)
            {
                Connector connector =
                    new Connector(
                        QualifiedName.CreateImmutable(serviceDescription.TargetNamespace, service.Name), 
                        ReadAnnotations(service.Documentation)
                    );

                description.AddConnector(connector);

                foreach (WSPort port in service.Ports)
                {
                    BindingProperties connectionBindingProperties = new BindingProperties();

                    foreach (WSExtensionElement bindingElement in port.Extensions)
                    {
                        TryMapBaseAddressBindingProperties(bindingElement, connectionBindingProperties);
                    }

                    WSPortBinding portBinding = serviceDescription.Bindings.Cast<WSPortBinding>().First(b => b.Name == port.Binding.Name);

                    foreach (WSExtensionElement bindingElement in portBinding.Extensions)
                    {
                        TryMapTransportBindingProperties(bindingElement, connectionBindingProperties);
                    }

                    Connection connection =
                        new Connection(
                            port.Name,
                            Attributes.Empty,
                            ReadAnnotations(port.Documentation),
                            connectionBindingProperties);

                    connector.AddConnection(connection);

                    BindingProperties defaultOperationBindings = new BindingProperties();

                    foreach (WSExtensionElement bindingElement in portBinding.Extensions)
                    {
                        TryMapDefaultBindingProperties(bindingElement, defaultOperationBindings);
                    }

                    WSPortType portType = serviceDescription.PortTypes.Cast<WSPortType>().First(p => p.Name == portBinding.Type.Name);

                    foreach (WSOperationBinding portOperationBinding in portBinding.Operations)
                    {
                        WSOperation operation = portType.Operations.Cast<WSOperation>().FirstOrDefault(o => o.Name == portOperationBinding.Name);

                        if (operation == null)

                            throw new InvalidOperationException(String.Format("No port binding found for the operation '{0}'.", portOperationBinding.Name));

                        String operationBindingName = portOperationBinding.Name;

                        String operationDocumentation = operation.Documentation;

                        WSInputBinding inputBinding = portOperationBinding.Input;

                        BindingProperties inputMessageBinding = new BindingProperties();

                        foreach (WSExtensionElement extensionElement in inputBinding.Extensions)
                        {
                            BindingProperty bindingProperty;

                            TryMapOperationMessageBinding(extensionElement, inputMessageBinding, out bindingProperty);
                        }

                        WSOperationMessage inputOperationMessage = operation.Messages.Input;

                        WSMessage inputMessage = 
                            serviceDescription.Messages.Cast<WSMessage>().First(m => m.Name == inputOperationMessage.Message.Name);

                        WSOutputBinding ouputBinding = portOperationBinding.Output;

                        BindingProperties outputMessageBinding = new BindingProperties();

                        WSOperationMessage outputOperationMessage;

                        WSMessage outputMessage;

                        if (ouputBinding == null)
                        {
                            outputOperationMessage = null;

                            outputMessage = null;
                        }
                        else {

                            foreach (WSExtensionElement extensionElement in ouputBinding.Extensions)
                            {
                                BindingProperty bindingProperty;

                                TryMapOperationMessageBinding(extensionElement, outputMessageBinding, out bindingProperty);
                            }

                            outputOperationMessage = operation.Messages.Output;

                            outputMessage = 
                                serviceDescription.Messages.Cast<WSMessage>().First(m => m.Name == outputOperationMessage.Message.Name);
                        }

                        BindingProperties operationBindings = new BindingProperties();

                        if (portOperationBinding.Extensions.Count == 0)

                            operationBindings = defaultOperationBindings; //.Clone()? Todo

                        else
                        {
                            foreach (WSExtensionElement bindingElement in portOperationBinding.Extensions)

                                TryMapOperationBindingProperties(bindingElement, defaultOperationBindings, operationBindings);
                        }

                        BindingProperty protocolBindingProperty;

                        connection.Bindings.TryGetProperty(out protocolBindingProperty, "binding");

                        connection.AddInteraction(
                            new Interaction(
                                portOperationBinding.Name,
                                Attributes.Empty,
                                ReadAnnotations(operationDocumentation),
                                operationBindings,
                                this.GetInteractionStyle(operation.Messages.Flow),
                                this.GetOperationMessages(protocolBindingProperty, inputMessageBinding, inputOperationMessage, inputMessage, false),
                                this.GetOperationMessages(protocolBindingProperty, outputMessageBinding, outputOperationMessage, outputMessage, true),
                                this.GetFaultOperationMessages(protocolBindingProperty, portOperationBinding, serviceDescription, operation)));

                        //operationBinding.Name;
                        //operationBinding.Namespaces;
                        //operationBinding.Binding;
                        //operationBinding.Documentation;
                        //operationBinding.Extensions;
                        //operationBinding.Faults;
                        //operationBinding.Input;
                        //operationBinding.Output;
                    }
                }
            }

            //serviceDescription.Bindings;
            //serviceDescription.DocumentationElement;
            //serviceDescription.ExtensibleAttributes;
            //serviceDescription.Extensions;
            //serviceDescription.Imports;
            //serviceDescription.Messages;
            //serviceDescription.Namespaces;
            //serviceDescription.ServiceDescriptions;
            //serviceDescription.Services;
            //serviceDescription.Types;

            return description;
        }

        private WSDescription ReadWSDescription(Uri descriptionLocatorUri, bool canReadFromCache, out IEnumerable<XmlQualifiedName> descriptionNameSpaces, out IEnumerable<XmlSchema> descriptionSchemas)
        {
            Stream descriptionStream;
            String descriptionMediaType;
            String descriptionMediaTypeVariant;
            Encoding descriptionEncoding;

            Uri descriptionImportsCachePathUri;

            if (canReadFromCache)
            {
                if (!descriptionLocatorUri.TryStreamCachedResource(this.descriptionImportsCachePathUri, out descriptionStream, out descriptionMediaType))

                    descriptionLocatorUri.DownloadResourceAndGetResourceMediaType(
                        this.descriptionImportsCachePathUri,
                        out descriptionStream,
                        out descriptionMediaType,
                        out descriptionMediaTypeVariant,
                        out descriptionEncoding);

                descriptionImportsCachePathUri = this.descriptionImportsCachePathUri;
            }
            else
            {
                descriptionLocatorUri.DownloadResourceAndGetResourceMediaType(
                    null,
                    out descriptionStream,
                    out descriptionMediaType,
                    out descriptionMediaTypeVariant,
                    out descriptionEncoding);

                descriptionImportsCachePathUri = null;
            }

            //Uri descriptionLocatorUri = new Uri(descriptionLocator);

            //Stream descriptionStream;
            //String descriptionMediaType;
            //String descriptionMediaTypeVariant;
            //Encoding descriptionEncoding;

            //descriptionLocatorUri.DownloadResourceAndGetResourceMediaType(
            //    canReadFromCache ? descriptionImportsCachePathUri : null,
            //    out descriptionStream,
            //    out descriptionMediaType,
            //    out descriptionMediaTypeVariant,
            //    out descriptionEncoding);

            List<XmlSchema> serviceDescriptionSchemas = new List<XmlSchema>();

            XmlDocument descriptionDocument = new XmlDocument();

            using (descriptionStream)
            {
                descriptionDocument.Load(descriptionStream);
            }

            XmlNamespaceManager nsManager = new XmlNamespaceManager(descriptionDocument.NameTable);

            nsManager.AddNamespace("wsdl", "http://schemas.xmlsoap.org/wsdl/");
            nsManager.AddNamespace("soap12", "http://schemas.xmlsoap.org/wsdl/soap12/");

            XmlNode typesNode = descriptionDocument.SelectSingleNode("/wsdl:definitions/wsdl:types", nsManager);

            if (typesNode != null)
            {
                foreach (XmlNode schemaNode in typesNode.ChildNodes)
                {
                    XmlReaderSettings settings = new XmlReaderSettings();

                    serviceDescriptionSchemas.Add(XmlSchema.Read(XmlReader.Create(new XmlNodeReader(schemaNode), settings), new ValidationEventHandler((o, s) => { })));
                }
            }

            // Soap12 operation bindings include optional soapActionRequired attribute. 
            // MS thinks default should be false (Soap12OperationBinding::soapActionRequired::DefaultValue custom attribute).
            // Oracle (and I) thinks default should be true (https://docs.oracle.com/cd/E19182-01/820-0595/ggeil2/index.html) 
            // So, transform descriptionDocument replacing any missing soapActionRequired attributes with true ones.
            // Then, stream descriptionDocument to descriptionStream ...

            foreach (XmlNode soap12OperationNode in descriptionDocument.SelectNodes("/wsdl:definitions/wsdl:binding/wsdl:operation/soap12:operation", nsManager))
            {
                if (!soap12OperationNode.Attributes.TryGetAttribute("soapActionRequired", String.Empty, out XmlAttribute soapActionRequiredAttribute))
                {
                    soap12OperationNode.Attributes.Append(descriptionDocument.CreateAttribute("soapActionRequired")).Value = "true";
                }
            }

            WSDescription serviceDescription;

            using (MemoryStream descriptionDocumentStream = new MemoryStream())
            {
                descriptionDocument.Save(descriptionDocumentStream);

                descriptionDocumentStream.Seek(0, SeekOrigin.Begin);

                serviceDescription = WSDescription.Read(descriptionDocumentStream);
            }

            if (serviceDescription == null)

                throw new NullReferenceException("serviceDescription");

            if (serviceDescription.ValidationWarnings.Count > 0)
            {
                // ToDo:  process warnings
            }

            descriptionNameSpaces = serviceDescription.Namespaces.ToSystemQualifiedNames();

            descriptionSchemas = serviceDescriptionSchemas;

            return serviceDescription;
        }

        private InteractionStyles GetInteractionStyle(WSOperationFlow flow)
        {
            switch (flow)
            {
                default:
                case WSOperationFlow.None:
                    return InteractionStyles.None;
                case WSOperationFlow.Notification:
                    return InteractionStyles.Notification;
                case WSOperationFlow.OneWay:
                    return InteractionStyles.Request;
                case WSOperationFlow.RequestResponse:
                    return InteractionStyles.RequestResponse;
                case WSOperationFlow.SolicitResponse:
                    return InteractionStyles.SolicitResponse;
            }
        }

        private InteractionMessage[] GetOperationMessages(BindingProperty protocolBindingProperty, BindingProperties messageBindingProperties, WSOperationMessage operationMessage, WSMessage message, bool isOutput)
        {
            QualifiedName statusCodeBindingName = QualifiedName.Empty;
            String statusCode = null;
            String statusCodeName = null;
            String statusCodeDescription = null;

            if (isOutput)
            {
                String transport = null;

                if (protocolBindingProperty != null)
                {
                    BindingAttribute transportAttribute;

                    if (protocolBindingProperty.Attributes.TryGetItem(BindingConstants.BINDING_TRANSPORT_ATTRIBUTE_NAME, out transportAttribute))

                        transport = transportAttribute.Value;

                    if (transport == SoapBinding.HttpTransport)
                    {
                        switch (protocolBindingProperty.QualifiedName.Namespace)
                        {
                            case SoapBinding.Namespace:
                                statusCodeBindingName = BindingConstants.SOAP_BINDING_STATUS_CODE_PROPERTY_NAME;
                                AdapterUtils.TryGetHttpStatusCodeDetails("200", out statusCode, out statusCodeName, out statusCodeDescription);
                                break;
                            case Soap12Binding.Namespace:
                                statusCodeBindingName = BindingConstants.SOAP12_BINDING_STATUS_CODE_PROPERTY_NAME;
                                AdapterUtils.TryGetHttpStatusCodeDetails("200", out statusCode, out statusCodeName, out statusCodeDescription);
                                break;
                            case HttpBinding.Namespace:
                                statusCodeBindingName = BindingConstants.HTTP_BINDING_STATUS_CODE_PROPERTY_NAME;
                                AdapterUtils.TryGetHttpStatusCodeDetails("2XX", out statusCode, out statusCodeName, out statusCodeDescription);
                                break;
                        }
                    }
                }
            }

            List<InteractionMessage> operationMessages = new List<InteractionMessage>();

            if (operationMessage == null)
            {
                if (statusCode != null)
                {
                    operationMessages.Add(
                        new InteractionMessage(
                            statusCodeName,
                            new Annotations(),
                            new BindingProperties(),
                            CreateAndBindStatusCodeMessageParts(
                                statusCode, 
                                statusCodeName, 
                                statusCodeDescription, 
                                statusCodeBindingName, 
                                messageBindingProperties, 
                                Represents.Success)
                        )
                    );
                }
            }
            else
            {
                Domain domain;

                if (statusCode != null)

                    domain =
                        CreateAndBindStatusCodeMessageParts(
                            statusCode,
                            statusCodeName,
                            statusCodeDescription,
                            statusCodeBindingName,
                            messageBindingProperties,
                            Represents.Success);
                else

                    domain = Domain.CreateImmutableOnRead();

                domain.AddPotentials(
                    Refinement.TotalOrdering,
                    ReadPotentials(message.Parts, Represents.Information, false));

                operationMessages.Add(
                    new InteractionMessage(
                        GetMessageName(operationMessage.Name, operationMessage.Message.ToSystem()),
                        operationMessage.Message.ToSystem().ToQualifiedName(),
                        ReadAnnotations(operationMessage.Documentation),
                        messageBindingProperties,
                        domain));
            }

            return operationMessages.ToArray();
        }

        private InteractionMessage[] GetFaultOperationMessages(BindingProperty protocolBindingProperty, WSOperationBinding portOperationBinding, WSDescription serviceDescription, WSOperation operation)
        {
            List<InteractionMessage> faultOperationMessages = new List<InteractionMessage>();

            IEnumerable<WSOperationFault> faultMessages = operation.Faults.Cast<WSOperationFault>();

            String statusCode = null;
            String statusCodeName = null;
            String statusCodeDescription = null;
            QualifiedName statusCodeBindingName = QualifiedName.Empty;

            String transport = null;

            if (protocolBindingProperty != null)
            {
                BindingAttribute transportAttribute;

                if (protocolBindingProperty.Attributes.TryGetItem(BindingConstants.BINDING_TRANSPORT_ATTRIBUTE_NAME, out transportAttribute))

                    transport = transportAttribute.Value;

                if (transport == SoapBinding.HttpTransport)
                {
                    switch (protocolBindingProperty.QualifiedName.Namespace)
                    {
                        case SoapBinding.Namespace:
                            statusCodeBindingName = BindingConstants.SOAP_BINDING_STATUS_CODE_PROPERTY_NAME;
                            AdapterUtils.TryGetHttpStatusCodeDetails("500", out statusCode, out statusCodeName, out statusCodeDescription);
                            break;
                        case Soap12Binding.Namespace:
                            statusCodeBindingName = BindingConstants.SOAP12_BINDING_STATUS_CODE_PROPERTY_NAME;
                            AdapterUtils.TryGetHttpStatusCodeDetails("500", out statusCode, out statusCodeName, out statusCodeDescription);
                            break;
                        case HttpBinding.Namespace:
                            statusCodeBindingName = BindingConstants.HTTP_BINDING_STATUS_CODE_PROPERTY_NAME;
                            AdapterUtils.TryGetHttpStatusCodeDetails("5XX", out statusCode, out statusCodeName, out statusCodeDescription);
                            break;
                    }
                }
            }

            if (portOperationBinding.Faults.Count == 0)
            {
                if (statusCode != null)
                {
                    WSExtensionElement extensionElement = null;
                    String faultName = "Fault";

                    switch (protocolBindingProperty.QualifiedName.Namespace)
                    {
                        case SoapBinding.Namespace:
                        case Soap12Binding.Namespace:

                            SoapBodyBinding bodyBinding =
                                (SoapBodyBinding)portOperationBinding.Output.Extensions.Find(typeof(SoapBodyBinding));

                            if (bodyBinding == null)

                                bodyBinding =
                                    (SoapBodyBinding)portOperationBinding.Input.Extensions.Find(typeof(SoapBodyBinding));

                            if (bodyBinding != null)
                            {
                                if (bodyBinding is Soap12BodyBinding)

                                    extensionElement = new Soap12FaultBinding() { Name = faultName, Encoding = bodyBinding.Encoding, Use = bodyBinding.Use };

                                else

                                    extensionElement = new SoapFaultBinding() { Name = faultName, Encoding = bodyBinding.Encoding, Use = bodyBinding.Use };
                            }

                            break;
                        case HttpBinding.Namespace:
                            break;
                    }

                    BindingProperties faultMessageBindingProperties = new BindingProperties();

                    BindingProperty bindingProperty;

                    TryMapOperationMessageBinding(extensionElement, faultMessageBindingProperties, out bindingProperty);

                    Domain domain = 
                        CreateAndBindStatusCodeMessageParts(
                            statusCode,
                            statusCodeName,
                            statusCodeDescription,
                            statusCodeBindingName,
                            faultMessageBindingProperties,
                            Represents.Fault);

                    if (statusCodeBindingName == BindingConstants.SOAP_BINDING_STATUS_CODE_PROPERTY_NAME)
                    {
                        domain.AddPotential(
                            Refinement.TotalOrdering,
                            CreateSoapFaultPotential(
                                faultName,
                                SoapBinding.Namespace,
                                new Potential(
                                    QualifiedName.CreateImmutable("", "detail"),
                                    Attributes.Empty,
                                    Annotations.Empty,
                                    Structure.ZeroOrOneElement,
                                    AnyType.CreateImmutable(),
                                    Represents.Fault)
                            )
                        );
                    }
                    else if (statusCodeBindingName == BindingConstants.SOAP12_BINDING_STATUS_CODE_PROPERTY_NAME)
                    {
                        domain.AddPotential(
                            Refinement.TotalOrdering,
                            CreateSoapFaultPotential(
                                faultName,
                                Soap12Binding.Namespace,
                                new Potential(
                                    QualifiedName.CreateImmutable(
                                        MessagingBindingConstants.SOAP12_ENV_NAMESPACE, 
                                        BindingConstants.SOAP_FAULT_DETAIL_ELEMENT_NAME),
                                    Attributes.Empty,
                                    Annotations.Empty,
                                    Structure.ZeroOrOneElement,
                                    AnyType.CreateImmutable(),
                                    Represents.Fault)
                            )
                        );
                    }

                    faultOperationMessages.Add(
                        new InteractionMessage(
                            statusCodeName,
                            new Annotations(),
                            faultMessageBindingProperties, 
                            domain
                        )
                    );
                }
            }
            else
            {
                for (int faultIndex = 0; faultIndex < portOperationBinding.Faults.Count; faultIndex++)
                {
                    WSOperationFault operationFault = faultMessages.ElementAt(faultIndex);

                    WSFaultBinding faultBinding = portOperationBinding.Faults[faultIndex];

                    BindingProperties faultMessageBindingProperties = new BindingProperties();

                    foreach (WSExtensionElement extensionElement in faultBinding.Extensions)
                    {
                        BindingProperty bindingProperty;

                        TryMapOperationMessageBinding(extensionElement, faultMessageBindingProperties, out bindingProperty);
                    }

                    WSMessage faultMessage = serviceDescription.Messages.Cast<WSMessage>().First(m => m.Name == operationFault.Name);

                    Domain domain;

                    if (statusCode != null)

                        domain =
                            CreateAndBindStatusCodeMessageParts(
                                statusCode,
                                statusCodeName,
                                statusCodeDescription,
                                statusCodeBindingName,
                                faultMessageBindingProperties,
                                Represents.Fault);
                    else

                        domain = Domain.CreateImmutableOnRead();

                    if (statusCodeBindingName == BindingConstants.SOAP_BINDING_STATUS_CODE_PROPERTY_NAME)
                    {
                        domain.AddPotential(
                            Refinement.TotalOrdering,
                            CreateSoapFaultPotential(
                                faultMessage.Name,
                                SoapBinding.Namespace,
                                new Potential(
                                    "detail",
                                    Attributes.Empty,
                                    Annotations.Empty,
                                    Structure.ZeroOrOneElement,
                                    new DomainType(
                                        Domain.CreateImmutableOnRead().AddPotentials(
                                            Refinement.TotalOrdering,
                                            this.ReadPotentials(faultMessage.Parts, Represents.Fault, false))
                                    )
                                )
                            )
                        );
                    }
                    else if (statusCodeBindingName == BindingConstants.SOAP12_BINDING_STATUS_CODE_PROPERTY_NAME)
                    {
                        domain.AddPotential(
                            Refinement.TotalOrdering,
                            CreateSoapFaultPotential(
                                faultMessage.Name,
                                Soap12Binding.Namespace,
                                new Potential(
                                    QualifiedName.CreateImmutable(
                                        MessagingBindingConstants.SOAP12_ENV_NAMESPACE, 
                                        BindingConstants.SOAP_FAULT_DETAIL_ELEMENT_NAME),
                                    Attributes.Empty,
                                    Annotations.Empty,
                                    Structure.ZeroOrOneElement,
                                    new DomainType(
                                        Domain.CreateImmutableOnRead().AddPotentials(
                                            Refinement.TotalOrdering,
                                            this.ReadPotentials(faultMessage.Parts, Represents.Fault, false))
                                    )
                                )
                            )
                        );
                    }

                    faultOperationMessages.Add(
                        new InteractionMessage(
                            GetMessageName(operationFault.Name, operationFault.Message.ToSystem()),
                            operationFault.Message.ToSystem().ToQualifiedName(),
                            ReadAnnotations(operationFault.Documentation),
                            faultMessageBindingProperties,
                            domain));
                }
            }

            return faultOperationMessages.ToArray();
        }

        private string GetMessageName(string name, XmlQualifiedName message)
        {
            if (String.IsNullOrEmpty(name))

                return message.Name;

            return name;
        }

        private Domain CreateAndBindStatusCodeMessageParts(string statusCode, string statusCodeName, string statusDescription, QualifiedName bindingName, BindingProperties messageBindings, Represents usage)
        {
            Domain domain = Domain.CreateImmutableOnRead();

            messageBindings.Insert(
                0,
                BindingProperty.CreateImmutable(
                    bindingName,
                    BindingProperties.Empty,
                    BindingAttributes.CreateImmutableOnRead(
                        BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, 
                        statusCodeName))
            );

            domain.AddPotential(
                Refinement.TotalOrdering,
                new Potential(
                    statusCodeName,
                    Attributes.Empty,
                    ReadAnnotations(statusDescription),
                    Structure.ZeroOrOneElement,
                    XsDataType.CreateBuiltInScalarValueType(XsdStringDataType.NAME, ScalarTypeFacets.CreateImmutableOnRead(new LexicalConstantValueFacet(Term.CreateImmutableOnRead(statusCode)))),
                    usage)
                );

            return domain;
        }

        private static Potential CreateSoapFaultPotential(String soapFaultName, String soapBindingNamespace, Potential soapFaultDetailPotential)
        {
            ScalarTypeFacets faultCodeFacets = new ScalarTypeFacets();

            switch (soapBindingNamespace)
            {
                case SoapBinding.Namespace:

                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("Client", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("CLIENT", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("Server", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("SERVER", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("VersionMismatch", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("VERSIONMISMATCH", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("MustUnderstand", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("MUSTUNDERSTAND", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap-env~Client", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap-env~CLIENT", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap-env~Server", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap-env~SERVER", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap-env~VersionMismatch", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap-env~VERSIONMISMATCH", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap-env~MustUnderstand", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap-env~MUSTUNDERSTAND", "ServiceFaulted"));

                    return
                        new Potential(
                            soapFaultName,
                            Attributes.Empty,
                            Annotations.Empty,
                            Structure.ExactlyOneElement,
                            new DomainType(
                                QualifiedName.CreateImmutable(soapBindingNamespace, "Fault"),
                                Domain.CreateImmutableOnRead()
                                    .AddPotential(
                                        Refinement.TotalOrdering,
                                        new Potential(
                                            QualifiedName.CreateImmutable("", "faultcode"),
                                            XsDataType.CreateBuiltInScalarValueType(
                                                XsdQNameDataType.NAME, 
                                                faultCodeFacets),
                                            Represents.Fault))
                                    .AddPotential(
                                        Refinement.TotalOrdering,
                                        new Potential(
                                            QualifiedName.CreateImmutable("", "faultstring"),
                                            Structure.ExactlyOneElement,
                                            XsDataType.CreateBuiltInScalarValueType(
                                                XsdStringDataType.NAME),
                                            Represents.Fault))
                                    .AddPotential(
                                        Refinement.TotalOrdering,
                                        new Potential(
                                            QualifiedName.CreateImmutable("", "faultactor"),
                                            Structure.ZeroOrOneElement,
                                            XsDataType.CreateBuiltInScalarValueType(
                                                XsdAnyUriDataType.NAME),
                                            Represents.Fault))
                                    .AddPotential(
                                        Refinement.TotalOrdering,
                                        soapFaultDetailPotential
                                    )
                                )
                            );

                case Soap12Binding.Namespace:

                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("Sender", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("SENDER", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("Receiver", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("RECEIVER", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("VersionMismatch", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("VERSIONMISMATCH", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("MustUnderstand", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("MUSTUNDERSTAND", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("DataEncodingUnknown", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("DATAENCODINGUNKNOWN", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~Sender", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~SENDER", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~Receiver", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~RECEIVER", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~VersionMismatch", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~VERSIONMISMATCH", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~MustUnderstand", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~MUSTUNDERSTAND", "ServiceFaulted"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~DataEncodingUnknown", "InvalidInput"));
                    faultCodeFacets.AddEnumerationConstraint(
                        GetAnnotatedFaultCodeEnumeralFacet("soap12-env~DATAENCODINGUNKNOWN", "InvalidInput"));

                    return
                        new Potential(
                            soapFaultName,
                            Attributes.Empty,
                            Annotations.Empty,
                            Structure.ExactlyOneElement,
                            new DomainType(
                                QualifiedName.CreateImmutable(soapBindingNamespace, "Fault"),
                                Domain.CreateImmutableOnRead()
                                    .AddPotential(
                                        Refinement.TotalOrdering,
                                        // ToDo:  type of Code is compound see http://www.w3.org/TR/2007/REC-soap12-part1-20070427/#faultcodeelement
                                        new Potential(
                                            QualifiedName.CreateImmutable(soapBindingNamespace, "Code"),
                                            Structure.ExactlyOneElement,
                                            XsDataType.CreateBuiltInScalarValueType(
                                                XsdQNameDataType.NAME,
                                                faultCodeFacets),
                                            Represents.Fault))
                                    .AddPotential(
                                        Refinement.TotalOrdering,
                                        // ToDo:  type of Reason is compound see http://www.w3.org/TR/2007/REC-soap12-part1-20070427/#faultstringelement
                                        new Potential(
                                            QualifiedName.CreateImmutable(soapBindingNamespace, "Reason"),
                                            Structure.ExactlyOneElement,
                                            XsDataType.CreateBuiltInScalarValueType(
                                                XsdStringDataType.NAME),
                                            Represents.Fault))
                                    .AddPotential(
                                        Refinement.TotalOrdering,
                                        new Potential(
                                            QualifiedName.CreateImmutable(soapBindingNamespace, "Node"),
                                            Structure.ZeroOrOneElement,
                                            XsDataType.CreateBuiltInScalarValueType(
                                                XsdAnyUriDataType.NAME),
                                            Represents.Fault))
                                    .AddPotential(
                                        Refinement.TotalOrdering,
                                        new Potential(
                                            QualifiedName.CreateImmutable(soapBindingNamespace, "Role"),
                                            Structure.ZeroOrOneElement,
                                            XsDataType.CreateBuiltInScalarValueType(
                                                XsdAnyUriDataType.NAME),
                                            Represents.Fault))
                                    .AddPotential(
                                        Refinement.TotalOrdering,
                                        soapFaultDetailPotential
                                    )
                                )
                            );

                default:

                    throw new ArgumentOutOfRangeException(nameof(soapBindingNamespace));
            }
        }

        private static LexicalConstantValueFacet GetAnnotatedFaultCodeEnumeralFacet(String faultCode, String faultMetaDataAttribute)
        {
            return new LexicalConstantValueFacet(
                Attributes.CreateImmutableOnRead().AddAttribute(
                    new Attribute(
                        faultMetaDataAttribute,
                        Represents.MetaData)),
                Annotations.Empty, 
                Term.CreateImmutableOnRead(faultCode));
        }

        private Potential[] ReadPotentials(WSMessagePartCollection parts, Represents represents, bool compilingModel)
        {
            List<Potential> potentials = new List<Potential>();

            foreach (WSMessagePart part in parts)
            {
                XmlSchemaType schemaType;

                if (!part.Element.IsEmpty)

                    potentials.Add(
                        ModelCaches.GetPotentialFromCache(this.descriptionNamespace, part.Element.ToSystem().ToQualifiedName(), represents));

                else
                {
                    schemaType = part.Type.ToSystem().GetSchemaType(this.descriptionNamespace);

                    if (schemaType == null)

                        throw new InvalidOperationException(String.Format("Could not resolve type {0} in namespace {1}", part.Type, this.descriptionNamespace));

                    if (schemaType is XmlSchemaSimpleType)
                    {
                        XmlSchemaSimpleType schemaSimpleType = (XmlSchemaSimpleType)schemaType;

                        if (part.Element != null)

                            potentials.Add(
                                schemaSimpleType.ReadSchemaSimpleTypeFromElement(
                                    part.Name,
                                    part.Element == null ? QualifiedName.CreateImmutableFromXmlQualifiedName(part.Element.ToSystem()) : QualifiedName.Empty,
                                    Attributes.Empty,
                                    (Annotations)part.Documentation,
                                    1,
                                    1,
                                    null,
                                    descriptionNamespace,
                                    compilingModel));
                    }
                    else
                    {
                        XmlSchemaComplexType schemaComplexType = (XmlSchemaComplexType)schemaType;

                        potentials.Add(
                            new Potential(
                                part.Name,
                                part.Element == null ? QualifiedName.CreateImmutableFromXmlQualifiedName(part.Element.ToSystem()) : QualifiedName.Empty,
                                Attributes.Empty,
                                ReadAnnotations(part.Documentation),
                                Structure.ExactlyOneElement,
                                compilingModel ?
                                    schemaComplexType.ReadDomainType(this.descriptionNamespace, represents, null, compilingModel) :
                                    schemaComplexType.GetOrReadDomainType(this.descriptionNamespace, represents, null)
                            )
                        );
                    }
                }
            }

            return potentials.ToArray();
        }

        private static Annotations ReadAnnotations(String documentation)
        {
            String markdown;

            if (documentation.TryConvertHtml2Markdown(out markdown))

                return Annotations.CreateImmutableOnRead(
                    new Annotation(
                        Attributes.CreateImmutableOnRead().AddAttribute(
                            new Attribute(
                                MediaTypeConstants.CONTENT_TYPE_ATTRIBUTE_NAME,
                                Represents.MetaData,
                                Term.CreateImmutableOnRead(MediaTypeConstants.TEXT_MARKDOWN_MEDIA_TYPE))),
                        "description",
                        MarkupFormatter<IRepresentation>.FromFormattedString(markdown, MediaTypeConstants.TEXT_MARKDOWN_MEDIA_TYPE, out Exception e)));

            else

                return Annotations.CreateImmutableOnRead(
                    Annotation.CreateImmutableOnRead("description", documentation.Itemize()));

        }

        //private CompactPartNumericFacet[] GetCompactPartNumericFacets(XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction)
        //{
        //    if (xmlSchemaSimpleTypeRestriction == null)

        //        return null;

        //    List<CompactPartNumericFacet> facets = new List<CompactPartNumericFacet>();

        //    foreach (XmlSchemaFacet facet in xmlSchemaSimpleTypeRestriction.Facets)
        //    {
        //        if (facet is XmlSchemaNumericFacet)
        //        {
        //            XmlSchemaNumericFacet numericFacet = (XmlSchemaNumericFacet)facet;

        //            if (facet is XmlSchemaMaxExclusiveFacet)

        //                facets.Add(
        //                    new CompactPartNumericFacet(
        //                        CompactPartNumericFacet.NumericConstraints.MaxExclusive,
        //                        numericFacet.Value,
        //                        numericFacet.IsFixed));

        //            else if (facet is XmlSchemaMinExclusiveFacet)

        //                facets.Add(
        //                    new CompactPartNumericFacet(
        //                        CompactPartNumericFacet.NumericConstraints.MinExclusive,
        //                        numericFacet.Value,
        //                        numericFacet.IsFixed));

        //            else if (facet is XmlSchemaMaxInclusiveFacet)

        //                facets.Add(
        //                    new CompactPartNumericFacet(
        //                        CompactPartNumericFacet.NumericConstraints.MaxInclusive,
        //                        numericFacet.Value,
        //                        numericFacet.IsFixed));

        //            else if (facet is XmlSchemaMinInclusiveFacet)

        //                facets.Add(
        //                    new CompactPartNumericFacet(
        //                        CompactPartNumericFacet.NumericConstraints.MinInclusive,
        //                        numericFacet.Value,
        //                        numericFacet.IsFixed));

        //            else if (facet is XmlSchemaTotalDigitsFacet)

        //                facets.Add(
        //                    new CompactPartNumericFacet(
        //                        CompactPartNumericFacet.NumericConstraints.TotalDigits,
        //                        numericFacet.Value,
        //                        numericFacet.IsFixed));

        //            else if (facet is XmlSchemaFractionDigitsFacet)

        //                facets.Add(
        //                    new CompactPartNumericFacet(
        //                        CompactPartNumericFacet.NumericConstraints.FractionDigits,
        //                        numericFacet.Value,
        //                        numericFacet.IsFixed));
        //        }
        //    }

        //    return facets.ToArray();
        //}

        //private CompactPartLengthFacet[] GetCompactPartLengthFacets(XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction)
        //{
        //    if (xmlSchemaSimpleTypeRestriction == null)

        //        return null;

        //    List<CompactPartLengthFacet> facets = new List<CompactPartLengthFacet>();

        //    foreach (XmlSchemaFacet facet in xmlSchemaSimpleTypeRestriction.Facets)
        //    {
        //        if (facet is XmlSchemaNumericFacet)
        //        {
        //            XmlSchemaNumericFacet numericFacet = (XmlSchemaNumericFacet)facet;

        //            if (facet is XmlSchemaLengthFacet)

        //                facets.Add(
        //                    new CompactPartLengthFacet(
        //                        CompactPartLengthFacet.LengthConstraints.Length,
        //                        numericFacet.Value,
        //                        numericFacet.IsFixed));

        //            else if (facet is XmlSchemaMaxLengthFacet)

        //                facets.Add(
        //                    new CompactPartLengthFacet(
        //                        CompactPartLengthFacet.LengthConstraints.MaxLength,
        //                        numericFacet.Value,
        //                        numericFacet.IsFixed));

        //            else if (facet is XmlSchemaMinLengthFacet)

        //                facets.Add(
        //                    new CompactPartLengthFacet(
        //                        CompactPartLengthFacet.LengthConstraints.MinLength,
        //                        numericFacet.Value,
        //                        numericFacet.IsFixed));
        //        }
        //    }

        //    return facets.ToArray();
        //}

        //private CompactPartConstrainedFacet[] GetCompactPartConstrainingFacets(XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction)
        //{
        //    if (xmlSchemaSimpleTypeRestriction == null)

        //        return null;

        //    List<CompactPartConstrainedFacet> facets = new List<CompactPartConstrainedFacet>();

        //    foreach (XmlSchemaFacet facet in xmlSchemaSimpleTypeRestriction.Facets)
        //    {
        //        if (facet is XmlSchemaNumericFacet) {

        //            XmlSchemaNumericFacet numericFacet = (XmlSchemaNumericFacet)facet;
        //        }
        //    }

        //    return facets.ToArray();
        //}

        //private CompactPartEnumerationFacet[] GetCompactPartEnumerationFacets(XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction, XmlSchemaDerivationMethod derivedBy)
        //{
        //    if (xmlSchemaSimpleTypeRestriction == null)

        //        return null;

        //    List<CompactPartEnumerationFacet> enumerations = new List<CompactPartEnumerationFacet>();

        //    foreach (XmlSchemaFacet facet in xmlSchemaSimpleTypeRestriction.Facets)
        //    {
        //        if (facet is XmlSchemaEnumerationFacet)
        //        {
        //            XmlSchemaEnumerationFacet enumeration = (XmlSchemaEnumerationFacet)facet;

        //            enumerations.Add(new CompactPartEnumerationFacet(enumeration.Value, GetMessagePartDocumentation(enumeration.Annotation)));
        //        }
        //    }

        //    return enumerations.ToArray();
        //}

        //private CompactPartPatternFacet[] GetCompactPartPatternFacets(XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction)
        //{
        //    if (xmlSchemaSimpleTypeRestriction == null)

        //        return null;

        //    List<CompactPartPatternFacet> patternFacets = new List<CompactPartPatternFacet>();

        //    foreach (XmlSchemaFacet facet in xmlSchemaSimpleTypeRestriction.Facets)
        //    {
        //        if (facet is XmlSchemaPatternFacet)
        //        {
        //            XmlSchemaPatternFacet patternFacet = (XmlSchemaPatternFacet)facet;

        //            patternFacets.Add(new CompactPartPatternFacet(patternFacet.Value, GetMessagePartDocumentation(patternFacet.Annotation)));
        //        }
        //    }

        //    return patternFacets.ToArray();
        //}

        //private Annotation GetMessagePartDocumentation(XmlSchemaAnnotation annotation)
        //{
        //    if (annotation == null)

        //        return new Annotation();

        //    IEnumerable<XmlSchemaObject> elements = annotation.Items.Cast<XmlSchemaObject>();

        //    List<Annotation.Entry> documentations = new List<Annotation.Entry>();

        //    foreach (XmlSchemaDocumentation docItem in elements.OfType<XmlSchemaDocumentation>())
        //    {
        //        documentations.Add(
        //            new Annotation.Entry(
        //                docItem.Language.ToCulture(), 
        //                docItem.Markup.Select(m => m.InnerText).ToArray()));
        //    }

        //    List<Annotation.Entry> instructions = new List<Annotation.Entry>();

        //    foreach (XmlSchemaAppInfo appInfo in elements.OfType<XmlSchemaAppInfo>())
        //    {
        //        instructions.AddRange(appInfo.Markup.Select(m => (Annotation.Entry)m.OuterXml).ToArray());
        //    }

        //    return new Annotation(documentations.ToArray(), instructions.ToArray());
        //}

        //private CompactPartType GetCompactPartType(PartType partType, XmlSchemaSimpleTypeContent content)
        //{
        //    List<QName> dataTypes = new List<QName>();

        //    if (content is XmlSchemaSimpleTypeList)
        //    {
        //        XmlQualifiedName builtInTypeName = WsdlUtils.GetBuiltInTypeName(this.targetNamespace, (content as XmlSchemaSimpleTypeList).BaseItemType.QualifiedName);

        //        if (builtInTypeName.Namespace == XmlSchema.Namespace)

        //            return new CompactPartType(CompactPartValues.SpaceDelimited, partType);

        //        else

        //            return new CompactPartType(CompactPartValues.SpaceDelimited, partType, builtInTypeName);
        //    }
        //    else if (content is XmlSchemaSimpleTypeUnion)
        //    {
        //        return 
        //            new CompactPartType(
        //                partType, 
        //                (content as XmlSchemaSimpleTypeUnion).BaseMemberTypes.Select(memberSchemaType => WsdlUtils.GetBuiltInTypeName(this.targetNamespace, memberSchemaType.QualifiedName)).Select(t => (QName)t).ToArray());
        //    }
        //    else if (content is XmlSchemaSimpleTypeRestriction)
        //    {
        //        if (partType.QualifiedName.Namespace == XmlSchema.Namespace)

        //            return new CompactPartType(partType);

        //        else
        //        {
        //            XmlQualifiedName builtInTypeName = WsdlUtils.GetBuiltInTypeName(this.targetNamespace, (content as XmlSchemaSimpleTypeRestriction).BaseTypeName);

        //            return new CompactPartType(partType, builtInTypeName);
        //        }
        //    }

        //    throw new ArgumentOutOfRangeException("content");
        //}

        //private CompoundPartType GetCompoundPartType(PartType partType, XmlSchemaContentModel contentModel)
        //{
        //    CompoundPartType compoundPartType;

        //    if (contentModel != null && contentModel is XmlSchemaComplexContent && contentModel.Content != null)
        //    {
        //        CompoundPartContent partContent;

        //        if (contentModel.Content is XmlSchemaComplexContentRestriction)
        //        {
        //            XmlSchemaComplexContentRestriction contentRestriction =
        //                (XmlSchemaComplexContentRestriction)contentModel.Content;

        //            partContent =
        //                new CompoundPartContent(
        //                    contentRestriction.BaseTypeName,
        //                    TypeModifier.Restricts,
        //                    contentRestriction.Attributes.OfType<XmlSchemaAttribute>().Select(a =>
        //                    {
        //                        return ReadSchemaAttribute(a);

        //                    }).Where(a => a != null).ToArray());
        //        }
        //        else if (contentModel.Content is XmlSchemaComplexContentExtension)
        //        {
        //            XmlSchemaComplexContentExtension contentRestriction =
        //                (XmlSchemaComplexContentExtension)contentModel.Content;

        //            partContent =
        //                new CompoundPartContent(
        //                    contentRestriction.BaseTypeName,
        //                    TypeModifier.Restricts,
        //                    contentRestriction.Attributes.OfType<XmlSchemaAttribute>().Select(a =>
        //                    {
        //                        return ReadSchemaAttribute(a);

        //                    }).Where(a => a != null).ToArray());
        //        }
        //        else

        //            throw new NotImplementedException(contentModel.Content.GetType().ToString());

        //        compoundPartType = new CompoundPartType(partType, partContent);
        //    }
        //    else

        //        compoundPartType = new CompoundPartType(partType);

        //    return compoundPartType;
        //}

        //private CompactMessagePart[] ReadAttributes(XmlSchemaObjectCollection schemaComplexTypeAttributes)
        //{
        //    List<CompactMessagePart> partAttributes = new List<CompactMessagePart>();

        //    foreach (XmlSchemaAttribute schemaAttribute in schemaComplexTypeAttributes.OfType<XmlSchemaAttribute>())
        //    {
        //        CompactMessagePart compactMessagePart = ReadSchemaAttribute(schemaAttribute);

        //        if (compactMessagePart != null)

        //            partAttributes.Add(compactMessagePart);
        //    }

        //    return partAttributes.ToArray();
        //}

        //private CompactMessagePart ReadSchemaAttribute(XmlSchemaAttribute schemaAttribute)
        //{
        //    CompactMessagePart compactMessagePart;

        //    XmlSchemaSimpleType schemaSimpleType;

        //    if (schemaAttribute.AttributeSchemaType == null)

        //        schemaSimpleType = new XmlSchemaSimpleType() { Name = QName.Empty };

        //    else

        //        schemaSimpleType = schemaAttribute.AttributeSchemaType;

        //    MessagePartCardinality attributeCardinality;

        //    switch (schemaAttribute.Use)
        //    {
        //        case XmlSchemaUse.None:
        //        case XmlSchemaUse.Optional:
        //        default:
        //            attributeCardinality = MessagePartCardinality.ZeroOrOne;
        //            break;
        //        case XmlSchemaUse.Required:
        //            attributeCardinality = MessagePartCardinality.ExactlyOne;
        //            break;
        //        case XmlSchemaUse.Prohibited:
        //            attributeCardinality = MessagePartCardinality.Prohibited;
        //            break;
        //    }

        //    if (attributeCardinality == MessagePartCardinality.Prohibited)

        //        compactMessagePart = null;

        //    else
        //    {
        //        PartType attributeType;

        //        if (schemaAttribute.RefName != null)

        //            attributeType = PartType.Create(schemaAttribute.RefName, schemaSimpleType.QualifiedName);

        //        else

        //            attributeType = PartType.Create(schemaSimpleType.QualifiedName);

        //        if (schemaSimpleType.Content == null)

        //            compactMessagePart =
        //                new CompactMessagePart(
        //                    schemaAttribute.QualifiedName,
        //                    GetMessagePartDocumentation(schemaAttribute.Annotation),
        //                    schemaAttribute.UnhandledAttributes.Select(a =>
        //                    {
        //                        return ReadAttribute(a);

        //                    }).Where(a => a != null).ToArray(),
        //                    attributeCardinality,
        //                    ConcretePartUsage.Data,
        //                    new CompactPartType(attributeType),
        //                    new CompactPartFacets(),
        //                    CompactPartLexicalValue.Create(schemaAttribute.DefaultValue));

        //        else

        //            compactMessagePart =
        //                new CompactMessagePart(
        //                    schemaAttribute.QualifiedName,
        //                    GetMessagePartDocumentation(schemaAttribute.Annotation),
        //                    schemaAttribute.UnhandledAttributes.Select(a =>
        //                    {
        //                        return ReadAttribute(a);

        //                    }).Where(a => a != null).ToArray(),
        //                    attributeCardinality,
        //                    ConcretePartUsage.Data,
        //                    GetCompactPartType(attributeType, schemaSimpleType.Content),
        //                    CompactPartFacets.Create(
        //                        GetCompactPartNumericFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction),
        //                        GetCompactPartLengthFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction),
        //                        GetCompactPartConstrainingFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction),
        //                        GetCompactPartEnumerationFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction, schemaSimpleType.DerivedBy),
        //                        GetCompactPartPatternFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction),
        //                        GetMessagePartDocumentation(schemaSimpleType.Annotation)),
        //                    CompactPartLexicalValue.Create(schemaAttribute.DefaultValue));
        //    }

        //    return compactMessagePart;
        //}

        //private CompactMessagePart ReadAttribute(XmlAttribute attribute)
        //{
        //    return new CompactMessagePart(
        //        QName.FromNMToken(attribute.Name), 
        //        (String)null, 
        //        new CompactMessagePart[0], 
        //        MessagePartCardinality.ExactlyOne, 
        //        ConcretePartUsage.Data, 
        //        new CompactPartType(PartType.Create("string")), 
        //        new CompactPartFacets(), 
        //        new CompactPartLexicalValue(QName.FromFQName(attribute.Value)));
        //}

        [Obsolete("?")]
        private bool IsSoapEncodingNamespace(string @namespace, out string soapEncodingNamespace)
        {
            soapEncodingNamespace = null;

            if (@namespace == ProtocolDescription.BindingWsdlUriEncodingSoap11)
                soapEncodingNamespace = @namespace;
            else if (@namespace == ProtocolDescription.BindingWsdlUriEncodingSoap12)
                soapEncodingNamespace = @namespace;

            return soapEncodingNamespace != null;
        }

    //    private MessagePart[] ReadChildParts(XmlSchemaParticle schemaParticle, bool areFaultParts)
    //    {
    //        if (schemaParticle == null)

    //            return null;

    //        List<MessagePart> parts = new List<MessagePart>();

    //        if (schemaParticle is XmlSchemaElement)
    //        {
    //            XmlSchemaElement schemaElement = (XmlSchemaElement)schemaParticle;

    //            //while (schemaElement.isElementDeclarationReference())

    //            //    schemaElement = schemaElement.getResolvedElementDeclaration();

    //            XmlSchemaType schemaElementType =
    //                schemaElement.ElementSchemaType;

    //            if (schemaElementType == null)

    //                schemaElementType = schemaElement.SchemaType;

    //            if (schemaElementType == null)

    //                schemaElementType = 
    //                    WsdlUtils.GetSchemaType(
    //                        this.targetNamespace, 
    //                        schemaElement.SchemaTypeName);

    //            parts.AddRange(
    //                PopulateElementParts(
    //                    schemaParticle,
    //                    schemaElement,
    //                    schemaElementType,
    //                    areFaultParts));

    //        }
    //        else if (schemaParticle is XmlSchemaAny)
    //        {
    //            XmlSchemaAny schemaElement = (XmlSchemaAny)schemaParticle;

    //            parts.Add(
    //                new AnyMessagePart(
    //                    "anyURI",
    //                    GetMessagePartDocumentation(schemaParticle.Annotation),
    //                    new CompactMessagePart[0],
    //                    MessagePartCardinality.ExactlyOne,
    //                    areFaultParts ? ConcretePartUsage.Fault : ConcretePartUsage.Data));
    //        }
    //        else if (schemaParticle is XmlSchemaGroupBase)
    //        {
    //            XmlSchemaGroupBase schemaGroupBase = (XmlSchemaGroupBase)schemaParticle;

    //            if (schemaGroupBase is XmlSchemaAll)

    //                parts.Add(new CompoundGroupHead(CompoundGroupMix.All));

    //            else if (schemaGroupBase is XmlSchemaChoice)

    //                parts.Add(new CompoundGroupHead(CompoundGroupMix.Choice));

    //            else if (schemaGroupBase is XmlSchemaSequence)

    //                parts.Add(new CompoundGroupHead(CompoundGroupMix.Sequence));

    //            foreach (XmlSchemaParticle particle in schemaGroupBase.Items)
    //            {
    //                MessagePart[] childParts =
    //                    ReadChildParts(particle, areFaultParts);

    //                parts.AddRange(childParts);
    //            }
    //        }
    //        else if (schemaParticle is XmlSchemaGroupRef)
    //        {
    //            throw new NotImplementedException();
    //        }

    //        return parts.ToArray();
    //    }

    //    private MessagePart[] PopulateElementParts(XmlSchemaParticle particle, XmlSchemaElement schemaElement, XmlSchemaType schemaElementType, bool areFaultParts)
    //    {
    //        List<MessagePart> parts = new List<MessagePart>();

    //        PartType partType = PartType.Create(schemaElement.QualifiedName, schemaElementType.QualifiedName);

    //        if (schemaElementType is XmlSchemaSimpleType)
    //        {
    //            XmlSchemaSimpleType schemaSimpleType = (XmlSchemaSimpleType)schemaElementType;

    //            parts.Add(
    //                new CompactMessagePart(
    //                    schemaElement.QualifiedName,
    //                    GetMessagePartDocumentation(schemaElement.Annotation),
    //                    new CompactMessagePart[0],
    //                    MessagePartCardinality.Create((int)particle.MinOccurs, (int)particle.MaxOccurs, schemaElement.IsNillable ? MessagePartContent.Optional : MessagePartContent.Required),
    //                    areFaultParts ? ConcretePartUsage.Fault : ConcretePartUsage.Data,
    //                    GetCompactPartType(partType, schemaSimpleType.Content),
    //                    CompactPartFacets.Create(
    //                        GetCompactPartNumericFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction),
    //                        GetCompactPartLengthFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction),
    //                        GetCompactPartConstrainingFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction),
    //                        GetCompactPartEnumerationFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction, schemaSimpleType.DerivedBy),
    //                        GetCompactPartPatternFacets(schemaSimpleType.Content as XmlSchemaSimpleTypeRestriction),
    //                        GetMessagePartDocumentation(schemaSimpleType.Annotation)),
    //                    CompactPartLexicalValue.Create()));
    //        }
    //        else if (schemaElementType is XmlSchemaComplexType)
    //        {
    //            XmlSchemaComplexType schemaComplexType = (XmlSchemaComplexType)schemaElementType;

    //            CompactMessagePart[] attributes = 
    //                ReadAttributes(schemaComplexType.Attributes);

    //            MessagePart[] childParts =
    //                ReadChildParts(schemaComplexType.Particle, areFaultParts);

    //            parts.Add(
    //                new CompoundMessagePart(
    //                    schemaElement.QualifiedName,
    //                    GetMessagePartDocumentation(schemaElement.Annotation),
    //                    attributes,
    //                    MessagePartCardinality.Create((int)particle.MinOccurs, (int)particle.MaxOccurs, schemaElement.IsNillable ? MessagePartContent.Optional : MessagePartContent.Required),
    //                    new CompoundPartType(partType),
    //                    childParts));
    //        }
    //        else
				
				//throw new InvalidOperationException("Unknown element part type: " + schemaElementType.Name);

    //        return parts.ToArray();
    //    }

        private bool TryMapBaseAddressBindingProperties(WSExtensionElement bindingElement, BindingProperties defaultTransportBindingProperties)
        {
            if (bindingElement is HttpAddressBinding || bindingElement is SoapAddressBinding) // Soap12AddressBinding derives from SoapAddressBinding.
            {
                BindingProperty bindingProperty;

                TryMapBindingElement(defaultTransportBindingProperties, bindingElement, BindingConstants.HTTP_BINDING_ELEMENT_LOCATION_PROPERTY_NAME, BindingPropertyFlags.None, out bindingProperty);

                return true;
            }

            return false;
        }

        private bool TryMapTransportBindingProperties(WSExtensionElement bindingElement, BindingProperties transportBindingProperties)
        {
            if (bindingElement is HttpBinding)
            {
                transportBindingProperties.Put(BindingConstants.HTTP_BINDING_PROPERTY_NAME, BindingConstants.BINDING_TRANSPORT_ATTRIBUTE_NAME, BindingConstants.BINDING_TRANSPORT_ATTIBUTE_VALUE_HTTP);

                return true;
            }

            if (bindingElement is SoapBinding) // Soap12Binding derives from SoapBinding.
            {
                BindingProperty bindingProperty;

                TryMapBindingElement(transportBindingProperties, bindingElement, BindingConstants.SOAP_BINDING_ELEMENT_TRANSPORT_PROPERTY_NAME, BindingPropertyFlags.None, out bindingProperty);

                return true;
            }

            return false;
        }

        private bool TryMapDefaultBindingProperties(WSExtensionElement bindingElement, BindingProperties defaultBindingProperties)
        {
            if (bindingElement is HttpBinding)
            {
                BindingProperty bindingProperty;

                TryMapBindingElement(defaultBindingProperties, bindingElement, BindingConstants.HTTP_BINDING_ELEMENT_VERB_PROPERTY_NAME, BindingPropertyFlags.None, out bindingProperty);

                return true;
            }

            if (bindingElement is SoapBinding) // Soap12Binding derives from SoapBinding.
            {
                BindingProperty bindingProperty;

                TryMapBindingElement(defaultBindingProperties, bindingElement, BindingConstants.SOAP_OPERATION_BINDING_ELEMENT_STYLE_PROPERTY_NAME, BindingPropertyFlags.ENum, out bindingProperty);

                return true;
            }

            return false;
        }

        private static bool TryMapOperationBindingProperties(WSExtensionElement bindingElement, BindingProperties defaultBindingProperties, BindingProperties bindingProperties)
        {
            if (bindingElement is HttpOperationBinding)
            {
                BindingProperty bindingProperty;

                TryMapBindingElement(bindingProperties, bindingElement, BindingConstants.HTTP_BINDING_ELEMENT_LOCATION_PROPERTY_NAME, BindingPropertyFlags.None, out bindingProperty);

                BindingProperty httpBindingProperty = defaultBindingProperties.FirstOrDefault(p => p.QualifiedName.Name == "http~binding");

                if (httpBindingProperty != null)

                    bindingProperties.Add(httpBindingProperty.Clone());

                return true;
            }
            else if (bindingElement is SoapOperationBinding)
            {
                BindingProperty defaultSoapBindingProperty;

                if (bindingElement is Soap12OperationBinding)
                {
                    defaultSoapBindingProperty = defaultBindingProperties.FirstOrDefault(p => p.QualifiedName == BindingConstants.SOAP12_BINDING_PROPERTY_NAME);
                }
                else
                {
                    defaultSoapBindingProperty = defaultBindingProperties.FirstOrDefault(p => p.QualifiedName == BindingConstants.SOAP_BINDING_PROPERTY_NAME);
                }

                if (defaultSoapBindingProperty == null)

                    throw new InvalidOperationException("Default soap binding not found.");

                BindingProperty soapOperationBindingProperty;

                TryMapBindingElement(bindingProperties, bindingElement, BindingConstants.SOAP_OPERATION_BINDING_ACTION_ELEMENT_NAME, BindingPropertyFlags.None, out soapOperationBindingProperty);

                if (bindingElement is Soap12OperationBinding)
                {
                    TryMapBindingElement(bindingProperties, bindingElement, BindingConstants.SOAP_OPERATION_BINDING_ELEMENT_ACTION_REQUIRED_PROPERTY_NAME, BindingPropertyFlags.None, out soapOperationBindingProperty);
                }

                if (TryMapBindingElement(bindingProperties, bindingElement, BindingConstants.SOAP_OPERATION_BINDING_ELEMENT_STYLE_PROPERTY_NAME, BindingPropertyFlags.ENum, out soapOperationBindingProperty))
                {
                    defaultSoapBindingProperty = defaultSoapBindingProperty.Clone(BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_NAME);

                    BindingAttribute styleAttbibute;

                    if (soapOperationBindingProperty.GetAttributes().TryGetItem(BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_NAME, out styleAttbibute))
                    {
                        soapOperationBindingProperty.GetAttributes().Remove(styleAttbibute);

                        defaultSoapBindingProperty.GetAttributes().Add(styleAttbibute.Name, styleAttbibute.Value);
                    }

                    if (defaultSoapBindingProperty.AttributesSpecified)

                        bindingProperties.Add(defaultSoapBindingProperty);
                }
                else 

                    bindingProperties.Add(defaultSoapBindingProperty.Clone());

                return true;
            }

            return false;
        }

        [Flags]
        private enum BindingPropertyFlags
        {
            None = 0,
            ENum = 1,
            QName = 2,
            LiteralPartName = 4,
        }

        private static bool TryMapBindingElement(BindingProperties bindingProperties, WSExtensionElement bindingElement, string bindingElementPropertyName, BindingPropertyFlags bindingPropertyFlags, out BindingProperty bindingProperty)
        {
            Type bindingelementType = bindingElement.GetType();

            QualifiedName bindingName = bindingelementType.GetCustomAttributes(false).OfType<XmlFormatExtensionAttribute>().First().ToQName();

            PropertyInfo bindingElementProperty = bindingelementType.GetProperty(bindingElementPropertyName);

            if (bindingElementProperty != null)
            {
                //ToDo:  Check XmlIgnore and throw?
                XmlAttributeAttribute bindingPropertyAttributeAttribute = bindingElementProperty.GetCustomAttribute<XmlAttributeAttribute>();
                XmlElementAttribute bindingPropertyElementAttribute = bindingElementProperty.GetCustomAttribute<XmlElementAttribute>();
                DefaultValueAttribute bindingPropertyDefaultValueAttribute = bindingElementProperty.GetCustomAttribute<DefaultValueAttribute>();
                XmlFormatExtensionAttribute bindingElementExtensionAttribute = bindingelementType.GetCustomAttribute<XmlFormatExtensionAttribute>();

                String bindingPropertyName;

                if (bindingPropertyAttributeAttribute != null)
                    bindingPropertyName = bindingPropertyAttributeAttribute.AttributeName;
                else if (bindingPropertyElementAttribute != null)
                    bindingPropertyName = bindingPropertyElementAttribute.ElementName;
                else if (bindingElementExtensionAttribute != null)
                    bindingPropertyName = bindingElementExtensionAttribute.ElementName;
                else
                    throw new NotImplementedException();

                if (typeof(ICollection).IsAssignableFrom(bindingElementProperty.PropertyType))
                {
                    ICollection c = (ICollection)bindingElementProperty.GetValue(bindingElement);

                    if (c != null)
                    {
                        object[] values = null;

                        values = new Object[c.Count];

                        c.CopyTo(values, 0);

                        bindingProperty = bindingProperties.Put(bindingName, bindingPropertyName, values.Select(o => o.ToString()).ToArray());

                        return true;
                    }
                    else
                    {
                        //throw new InvalidOperationException("Null Binding element attributes collection.");

                        bindingProperty = null;

                        return false;
                    }
                }
                else
                {
                    object bindingElementPropertyValue = bindingElementProperty.GetValue(bindingElement);

                    string bindingElementPropertyTextValue = null;

                    if (bindingElementPropertyValue != null)
                    {
                        if ((bindingPropertyFlags & BindingPropertyFlags.ENum) == BindingPropertyFlags.ENum)
                        {
                            Type enumType = bindingElementPropertyValue.GetType();

                            MemberInfo enumInfo = enumType.GetMember(bindingElementPropertyValue.ToString()).FirstOrDefault();

                            XmlEnumAttribute enumAttribute = null;

                            if (enumInfo != null)

                                enumAttribute = enumInfo.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault();

                            if (enumAttribute != null)

                                bindingElementPropertyTextValue = enumAttribute.Name;

                            else {

                                XmlIgnoreAttribute ignoreAttribute = enumInfo.GetCustomAttributes<XmlIgnoreAttribute>().FirstOrDefault();

                                if (ignoreAttribute != null)

                                    bindingElementPropertyTextValue = String.Empty;

                                else

                                    bindingElementPropertyTextValue = bindingElementPropertyValue.ToString();
                            }
                        }
                        else

                            bindingElementPropertyTextValue = bindingElementPropertyValue.ToString();
                    }

                    if (!String.IsNullOrWhiteSpace(bindingElementPropertyTextValue))
                    {
                        if ((bindingPropertyFlags & BindingPropertyFlags.QName) == BindingPropertyFlags.QName)
                        {
                            QualifiedName bindingElementPropertyTextValueQName = bindingElementPropertyTextValue.ParseXmlQualifiedName().ToQualifiedName();

                            bindingElementPropertyTextValue = bindingElementPropertyTextValueQName;
                        }

                        if ((bindingPropertyFlags & BindingPropertyFlags.LiteralPartName) == BindingPropertyFlags.LiteralPartName)

                            bindingProperty = bindingProperties.Put(bindingName, "part", bindingElementPropertyTextValue);

                        else

                            bindingProperty = bindingProperties.Put(bindingName, bindingPropertyAttributeAttribute.AttributeName, bindingElementPropertyTextValue);
                    }
                    else
                    {
                        //throw new InvalidOperationException("Binding element attribute has no value.");

                        bindingProperty = null;

                        return false;
                    }
                }

                return true;
            }
            else

                throw new InvalidOperationException("Invalid cast to ICollection.");
        }

        private bool TryMapBindingExtensionElement(WSExtensionElement bindingElement, out BindingProperty property)
        {
            Type bindingelementType = bindingElement.GetType();

            XmlFormatExtensionAttribute extensionAttribute = 
                bindingelementType.GetCustomAttributes(false).OfType<XmlFormatExtensionAttribute>().FirstOrDefault();

            if (extensionAttribute != null)
            {
                QualifiedName bindingName = QualifiedName.CreateImmutable(extensionAttribute.Namespace, extensionAttribute.ElementName);

                property = BindingProperty.CreateImutableOnRead(
                    bindingName, 
                    BindingProperties.CreateImmutableOnRead(), 
                    BindingAttributes.CreateImmutableOnRead());

                return true;
            }

            property = null;

            return false;
        }

        private bool TryMapOperationMessageBinding(WSExtensionElement bindingElement, BindingProperties bindingProperties, out BindingProperty bindingProperty)
        {
            if (bindingElement is SoapBodyBinding)
            {
                TryMapBindingElement(bindingProperties, bindingElement, "Encoding", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Namespace", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "PartsString", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Use", BindingPropertyFlags.ENum, out bindingProperty);

                return true;
            }

            if (bindingElement is SoapHeaderBinding)
            {
                TryMapBindingElement(bindingProperties, bindingElement, "Message", BindingPropertyFlags.QName, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Encoding", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Namespace", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Part", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Use", BindingPropertyFlags.ENum, out bindingProperty);

                BindingProperties soapHeaderBindingProperties = new BindingProperties();

                BindingProperty soapHeaderFaultProperty;

                if (TryMapOperationMessageBinding(((SoapHeaderBinding)bindingElement).Fault, soapHeaderBindingProperties, out soapHeaderFaultProperty))

                    bindingProperties.Add(soapHeaderFaultProperty);

                return true;
            }

            if (bindingElement is SoapFaultBinding)
            {
                TryMapBindingElement(bindingProperties, bindingElement, "Encoding", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Namespace", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Name", BindingPropertyFlags.LiteralPartName, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Use", BindingPropertyFlags.ENum, out bindingProperty);

                return true;
            }

            if (bindingElement is SoapHeaderFaultBinding)
            {
                TryMapBindingElement(bindingProperties, bindingElement, "Message", BindingPropertyFlags.QName, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Encoding", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Namespace", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Part", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Use", BindingPropertyFlags.ENum, out bindingProperty);

                return true;
            }

            if (bindingElement is HttpUrlEncodedBinding)
            {
                if (TryMapBindingExtensionElement(bindingElement, out bindingProperty))

                    bindingProperties.Add(bindingProperty);

                return true;
            }

            if (bindingElement is HttpUrlReplacementBinding)
            {
                if (TryMapBindingExtensionElement(bindingElement, out bindingProperty))

                    bindingProperties.Add(bindingProperty);

                return true;
            }

            if (bindingElement is MimeXmlBinding)
            {
                TryMapBindingElement(bindingProperties, bindingElement, "Part", BindingPropertyFlags.None, out bindingProperty);

                return true;
            }

            if (bindingElement is MimeMultipartRelatedBinding)
            {
                TryMapBindingExtensionElement(bindingElement, out bindingProperty);

                bindingProperties.Add(bindingProperty);

                foreach (MimePart mimePart in ((MimeMultipartRelatedBinding)bindingElement).Parts)
                {
                    BindingProperty mimePartBindingProperty;

                    if (TryMapBindingExtensionElement(new MimePartExtension(mimePart), out mimePartBindingProperty))
                    {
                        bindingProperty.GetProperties().Add(mimePartBindingProperty);

                        foreach (WSExtensionElement mimePartExtension in mimePart.Extensions)
                        {
                            BindingProperty mimePartExtensionBindingProperty;

                            TryMapOperationMessageBinding(mimePartExtension, mimePartBindingProperty.GetProperties(), out mimePartExtensionBindingProperty);
                        }
                    }
                }

                return true;
            }

            if (bindingElement is MimeContentBinding)
            {
                TryMapBindingElement(bindingProperties, bindingElement, "Part", BindingPropertyFlags.None, out bindingProperty);
                TryMapBindingElement(bindingProperties, bindingElement, "Type", BindingPropertyFlags.None, out bindingProperty);

                return true;
            }

            bindingProperty = null;

            return false;
        }
    }

    public static class Wsdl11ReaderExtenstions
    {
        public static QualifiedName ToQName(this XmlFormatExtensionAttribute extensionAttribute)
        {
            if (extensionAttribute == null)

                return null;

            return QualifiedName.CreateImmutable(extensionAttribute.Namespace, extensionAttribute.ElementName);
        }
    }
}
