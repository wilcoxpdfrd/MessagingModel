using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xml;
using Microsoft.Xml.Schema;
using Microsoft.Xml.Serialization;

#region WebServiceDescriptionTypes

using System.Web.Services.Configuration;

using WebService = System.Web.Services.Description.Service;
using WSDescription = System.Web.Services.Description.ServiceDescription;
using WSTypes = System.Web.Services.Description.Types;
using WSExtensionElement = System.Web.Services.Description.ServiceDescriptionFormatExtension;
using WSImport = System.Web.Services.Description.Import;
using WSExtensionElementCollection = System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection;
using WSPort = System.Web.Services.Description.Port;
using WSPortBinding = System.Web.Services.Description.Binding;
using HttpBinding = System.Web.Services.Description.HttpBinding;
using SoapBinding = System.Web.Services.Description.SoapBinding;
using SoapBindingStyle = System.Web.Services.Description.SoapBindingStyle;
using Soap12Binding = System.Web.Services.Description.Soap12Binding;
using HttpAddressBinding = System.Web.Services.Description.HttpAddressBinding;
using SoapAddressBinding = System.Web.Services.Description.SoapAddressBinding;
using Soap12AddressBinding = System.Web.Services.Description.Soap12AddressBinding;
using HttpOperationBinding = System.Web.Services.Description.HttpOperationBinding;
using SoapOperationBinding = System.Web.Services.Description.SoapOperationBinding;
using Soap12OperationBinding = System.Web.Services.Description.Soap12OperationBinding;
using SoapBodyBinding = System.Web.Services.Description.SoapBodyBinding;
using Soap12BodyBinding = System.Web.Services.Description.Soap12BodyBinding;
using SoapBindingUse = System.Web.Services.Description.SoapBindingUse;
using SoapFaultBinding = System.Web.Services.Description.SoapFaultBinding;
using Soap12FaultBinding = System.Web.Services.Description.Soap12FaultBinding;
using SoapHeaderBinding = System.Web.Services.Description.SoapHeaderBinding;
using Soap12HeaderBinding = System.Web.Services.Description.Soap12HeaderBinding;
using SoapHeaderFaultBinding = System.Web.Services.Description.SoapHeaderFaultBinding;
using WSPortType = System.Web.Services.Description.PortType;
using WSOperationBinding = System.Web.Services.Description.OperationBinding;
using WSOperation = System.Web.Services.Description.Operation;
using WSOperationMessage = System.Web.Services.Description.OperationMessage;
using WSOperationInput = System.Web.Services.Description.OperationInput;
using WSOperationOutput = System.Web.Services.Description.OperationOutput;
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

using AllVerge.Core.Reflection;
using AllVerge.Core.Resource;

using AllVerge.Core.Model;
using AllVerge.Core.Model.DataTypes.Abstractions;
using AllVerge.Core.Model.LexicalTypes;
using AllVerge.Core.Model.LexicalTypes.Structures;

using AllVerge.Core.Model.XMLSchema;
using AllVerge.Core.Model.XMLSchema.Adapters;

using AllVerge.Core.ServiceModel.Description.Model;
using AllVerge.Core.ServiceModel.Description.Adapters;

namespace AllVerge.Core.ServiceModel.Description.Wsdl
{
    internal class Wsdl11Writer : IDescriptionWriter
    {
        private string descriptionExportsPath;

        static Wsdl11Writer()
        {
            AbstractDataTypes.TryInitializeBuiltInTypes();
            XsDataType.TryInitializeBuiltInTypes();
        }

        public Wsdl11Writer(String descriptionExportsPath)
        {
            this.descriptionExportsPath = descriptionExportsPath;
        }

        public void WriteDescription(ProtocolDescription description, String connectorNameOrIndex, String connectionNameOrIndex, String interactionNameOrIndex, string hostName)
        {
            QualifiedName.ClearCurrentNamespaceManager();

            String descriptionNamespace = description.QualifiedName.Namespace;

            if (!QualifiedName.TrySetCurrentNamespaceManager(descriptionNamespace))
            {
                global::System.Xml.XmlNamespaceManager namespaceManager = 
                    new global::System.Xml.XmlNamespaceManager(new global::System.Xml.NameTable());

                foreach (String @namespace in description.Resources)
                {
                    QualifiedName qualifiedName = QualifiedName.CreateImmutableFromFQN(@namespace);

                    if (qualifiedName.Prefix.Length == 0)

                        namespaceManager.AddNamespace(qualifiedName.Name, qualifiedName.Namespace);
                }

                QualifiedName.SetCurrentNamespaceManager(descriptionNamespace, namespaceManager);
            }

            String descriptionNamespacePrefix;

            if (!QualifiedName.TryLookupNamespacePrefix(descriptionNamespace, out descriptionNamespacePrefix))

                throw new InvalidOperationException(String.Format("Resource targetNamespace '{0}' is not a registered namespace.", descriptionNamespace));

            WSDescription serviceDescription = new WSDescription();

            foreach (String @namespace in description.Resources)
            {
                QualifiedName qualifiedName = QualifiedName.CreateImmutableFromFQN(@namespace);

                serviceDescription.Namespaces.Add(qualifiedName.Prefix, qualifiedName.Namespace);
            }

            serviceDescription.TargetNamespace = descriptionNamespace;

            global::System.Xml.Serialization.XmlSchemas schemas = new global::System.Xml.Serialization.XmlSchemas();

            XmlElement contractDocumentationElement;

            if (description.AnnotationsSpecified)
            {
                if (TryGetDocumentationElement(description.Annotations, out contractDocumentationElement))
                {
                    if (contractDocumentationElement.ChildNodes.OfType<XmlElement>().Count() > 0)

                        serviceDescription.DocumentationElement = contractDocumentationElement;

                    else

                        serviceDescription.Documentation = contractDocumentationElement.InnerText;
                }
            }

            Connector connector;

            if (!description.TryGetConnector(connectorNameOrIndex, out connector))

                throw new ArgumentOutOfRangeException(nameof(connectorNameOrIndex), "Not found.");

            WebService service = new WebService();

            service.Name = connector.QualifiedName.LocalName;

            XmlElement serviceDocumentationElement;

            if (TryGetDocumentationElement(connector.Annotations, out serviceDocumentationElement))
            {
                if (serviceDocumentationElement.ChildNodes.OfType<XmlElement>().Count() > 0)

                    service.DocumentationElement = serviceDocumentationElement;

                else

                    service.Documentation = serviceDocumentationElement.InnerText;
            }

            Connection connection;

            if (!connector.TryGetConnection(connectionNameOrIndex, out connection))

                throw new ArgumentOutOfRangeException(nameof(connectionNameOrIndex), "Not found.");

            BindingProperty protocolBindingProperty;

            connection.Bindings.TryGetProperty(out protocolBindingProperty, BindingConstants.BINDING_PROPERTY_LOCAL_NAME);

            WSExtensionElement protocolBinding;

            switch (protocolBindingProperty.QualifiedName.Namespace)
            {
                case SoapBinding.Namespace:

                    BindingAttribute transportAttribute;

                    if (protocolBindingProperty.Attributes.TryGetItem(BindingConstants.BINDING_TRANSPORT_ATTRIBUTE_NAME, out transportAttribute))

                        protocolBinding = new SoapBinding() { Transport = transportAttribute.Value };

                    else

                        protocolBinding = new SoapBinding();

                    break;

                case Soap12Binding.Namespace:

                    BindingAttribute transport12Attribute;

                    if (protocolBindingProperty.Attributes.TryGetItem(BindingConstants.BINDING_TRANSPORT_ATTRIBUTE_NAME, out transport12Attribute))

                        protocolBinding = new Soap12Binding() { Transport = transport12Attribute.Value };

                    else

                        protocolBinding = new Soap12Binding();

                    break;

                case HttpBinding.Namespace:

                    protocolBinding = new HttpBinding();

                    break;

                default:

                    throw new InvalidOperationException("Protocol binding not found.");
            }

            WSPort port = null;

            WSPortBinding portBinding = null;

            port = new WSPort();

            service.Ports.Add(port);

            port.Name = connection.Name;

            XmlElement behaviorDocumentationElement;

            if (TryGetDocumentationElement(connection.Annotations, out behaviorDocumentationElement))
            {
                if (behaviorDocumentationElement.ChildNodes.OfType<XmlElement>().Count() > 0)

                    port.DocumentationElement = behaviorDocumentationElement;

                else

                    port.Documentation = behaviorDocumentationElement.InnerText;
            }

            String addressBindingNamespace;

            Uri addressBindingLocation = connection.GetLocation(out addressBindingNamespace);

            WSExtensionElement addressBinding;

            switch (addressBindingNamespace)
            {
                case SoapBinding.Namespace:
                    addressBinding = new SoapAddressBinding() { Location = addressBindingLocation.AbsoluteUri };
                    break;
                case Soap12Binding.Namespace:
                    addressBinding = new Soap12AddressBinding() { Location = addressBindingLocation.AbsoluteUri };
                    break;
                case HttpBinding.Namespace:
                    addressBinding = new HttpAddressBinding() { Location = addressBindingLocation.AbsoluteUri };
                    break;
                default:
                    addressBinding = null;
                    break;
            }

            if (addressBinding != null)

                port.Extensions.Add(addressBinding);

            QualifiedName portBindingName = String.Format("{0}~{1}Binding", descriptionNamespacePrefix, port.Name);

            QualifiedName portTypeName = String.Format("{0}~{1}Type", descriptionNamespacePrefix, port.Name);

            port.Binding = portBindingName.ToXmlQualifiedName().ToMicrosoft();

            portBinding = new WSPortBinding() { Name = portBindingName.LocalName, Type = portTypeName.ToXmlQualifiedName().ToMicrosoft() };

            serviceDescription.Bindings.Add(portBinding);

            portBinding.Extensions.Add(protocolBinding);

            WSPortType portType = new WSPortType() { Name = portTypeName.LocalName };

            serviceDescription.PortTypes.Add(portType);

            foreach (Interaction interaction in connection.Interactions.Where(o => interactionNameOrIndex == "*" ? true : o.Name == interactionNameOrIndex))
            {
                WSPortType currentPortType = serviceDescription.PortTypes.Cast<WSPortType>().Last();

                WSOperation operation = new WSOperation() { Name = interaction.Name };

                //operation.SetParent(currentPortType);

                XmlElement operationDocumentationElement;

                if (this.TryGetDocumentationElement(interaction.Annotations, out operationDocumentationElement))
                {
                    if (operationDocumentationElement.ChildNodes.OfType<XmlElement>().Count() > 0)

                        operation.DocumentationElement = operationDocumentationElement;

                    else

                        operation.Documentation = operationDocumentationElement.InnerText;
                }

                currentPortType.Operations.Add(operation);

                InteractionMessage inputOperationMessage = interaction.Inputs.FirstOrDefault();

                if (inputOperationMessage != null)
                {
                    WSOperationInput operationInput =
                        new WSOperationInput()
                        {
                            Name = inputOperationMessage.Name,
                            Message = inputOperationMessage.QualifiedName.ToXmlQualifiedName().ToMicrosoft()
                        };

                    XmlElement inputDocumentationElement;

                    if (this.TryGetDocumentationElement(inputOperationMessage.Annotations, out inputDocumentationElement))
                    {
                        if (inputDocumentationElement.ChildNodes.OfType<XmlElement>().Count() > 0)

                            operationInput.DocumentationElement = inputDocumentationElement;

                        else

                            operationInput.Documentation = inputDocumentationElement.InnerText;
                    }

                    operation.Messages.Add(operationInput);

                    WSMessage inputMessage = new WSMessage() { Name = operationInput.Message.Name };

                    this.MapOperationMessageParts(serviceDescription, schemas, inputOperationMessage, inputMessage);

                    serviceDescription.Messages.Add(inputMessage);
                }

                InteractionMessage outputOperationMessage = interaction.Outputs.FirstOrDefault();

                if (outputOperationMessage != null)
                {
                    WSOperationOutput operationOutput =
                        new WSOperationOutput()
                        {
                            Name = outputOperationMessage.Name,
                            Message = outputOperationMessage.QualifiedName.ToXmlQualifiedName().ToMicrosoft()
                        };

                    XmlElement outputDocumentationElement;

                    if (this.TryGetDocumentationElement(outputOperationMessage.Annotations, out outputDocumentationElement))
                    {
                        if (outputDocumentationElement.ChildNodes.OfType<XmlElement>().Count() > 0)

                            operationOutput.DocumentationElement = outputDocumentationElement;

                        else

                            operationOutput.Documentation = outputDocumentationElement.InnerText;
                    }

                    operation.Messages.Add(operationOutput);

                    WSMessage outputMessage = new WSMessage() { Name = operationOutput.Message.Name };

                    this.MapOperationMessageParts(serviceDescription, schemas, outputOperationMessage, outputMessage);

                    serviceDescription.Messages.Add(outputMessage);
                }

                // ToDo: Faults ...

                //foreach (OperationMessage faultMessage in serviceOperation.Faults)

                //    operation.Faults.Add(faultMessage);

                WSOperationBinding portOperationBinding = new WSOperationBinding() { Name = interaction.Name };

                BindingProperty operationBindingProperty;

                if (interaction.Bindings.TryGetProperty(out operationBindingProperty, BindingConstants.OPERATION_PROPERTY_LOCAL_NAME))
                {
                    WSExtensionElement operationBinding;

                    switch (operationBindingProperty.QualifiedName.Namespace)
                    {
                        case SoapBinding.Namespace:

                            SoapBindingStyle soapBindingStyle;

                            BindingProperty soapBindingProperty;

                            if (interaction.Bindings.TryGetProperty(out soapBindingProperty, BindingConstants.BINDING_PROPERTY_LOCAL_NAME))
                            {
                                BindingAttribute styleAttribute;

                                if (soapBindingProperty.Attributes.TryGetItem(BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_NAME, out styleAttribute))
                                {
                                    switch (styleAttribute.Value)
                                    {
                                        case BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_DOCUMENT:
                                            soapBindingStyle = SoapBindingStyle.Document;
                                            break;
                                        case BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_RPC:
                                            soapBindingStyle = SoapBindingStyle.Rpc;
                                            break;
                                        default:
                                            soapBindingStyle = SoapBindingStyle.Default;
                                            break;
                                    }
                                }
                                else

                                    soapBindingStyle = SoapBindingStyle.Default;
                            }
                            else

                                soapBindingStyle = SoapBindingStyle.Default;

                            String soapAtion;

                            BindingAttribute soapActionAttribute;

                            if (operationBindingProperty.Attributes.TryGetItem(BindingConstants.SOAP_ACTION_BINDING_ATTRIBUTE_NAME, out soapActionAttribute))

                                soapAtion = soapActionAttribute.Value;

                            else

                                soapAtion = null;

                            operationBinding =
                                new SoapOperationBinding()
                                {
                                    SoapAction = soapAtion,
                                    Style = soapBindingStyle
                                };

                            break;

                        case Soap12Binding.Namespace:

                            SoapBindingStyle soap12BindingStyle;

                            BindingProperty soap12BindingProperty;

                            if (interaction.Bindings.TryGetProperty(out soap12BindingProperty, BindingConstants.BINDING_PROPERTY_LOCAL_NAME))
                            {
                                BindingAttribute styleAttribute;

                                if (soap12BindingProperty.Attributes.TryGetItem(BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_NAME, out styleAttribute))
                                {
                                    switch (styleAttribute.Value)
                                    {
                                        case BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_DOCUMENT:
                                            soap12BindingStyle = SoapBindingStyle.Document;
                                            break;
                                        case BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_RPC:
                                            soap12BindingStyle = SoapBindingStyle.Rpc;
                                            break;
                                        default:
                                            soap12BindingStyle = SoapBindingStyle.Default;
                                            break;
                                    }
                                }
                                else

                                    soap12BindingStyle = SoapBindingStyle.Default;
                            }
                            else

                                soap12BindingStyle = SoapBindingStyle.Default;

                            String soap12Action;

                            BindingAttribute soap12ActionAttribute;

                            if (operationBindingProperty.Attributes.TryGetItem(BindingConstants.SOAP_ACTION_BINDING_ATTRIBUTE_NAME, out soap12ActionAttribute))

                                soap12Action = soap12ActionAttribute.Value;

                            else

                                soap12Action = null;

                            bool soap12ActionRequired;

                            BindingAttribute soap12ActionRequiredAttribute;

                            if (operationBindingProperty.Attributes.TryGetItem(BindingConstants.SOAP_ACTION_REQUIRED_BINDING_ATTRIBUTE_NAME, out soap12ActionRequiredAttribute))
                            {
                                if (!bool.TryParse(soap12ActionRequiredAttribute.Value, out soap12ActionRequired))

                                    soap12ActionRequired = true;
                            }
                            else

                                soap12ActionRequired = true;

                            operationBinding =
                                new Soap12OperationBinding()
                                {
                                    SoapAction = soap12Action,
                                    SoapActionRequired = soap12ActionRequired,
                                    Style = soap12BindingStyle
                                };

                            break;

                        case HttpBinding.Namespace:

                            BindingProperty httpBindingProperty;

                            if (interaction.Bindings.TryGetProperty(out httpBindingProperty, BindingConstants.BINDING_PROPERTY_LOCAL_NAME))
                            {
                                BindingAttribute verbAttribute;

                                if (httpBindingProperty.Attributes.TryGetItem(BindingConstants.BINDING_VERB_ATTRIBUTE_NAME, out verbAttribute))
                                {
                                    if (protocolBinding is HttpBinding)
                                    {
                                        HttpBinding httpProtocolBinding = (HttpBinding)protocolBinding;

                                        if (string.IsNullOrEmpty(httpProtocolBinding.Verb))

                                            httpProtocolBinding.Verb = verbAttribute.Value;

                                        else if (httpProtocolBinding.Verb != verbAttribute.Value)

                                            throw new InvalidOperationException("Selected operations are bound to different verbs.");
                                    }
                                }
                            }

                            BindingAttribute locationAttribute;

                            String location;

                            if (operationBindingProperty.Attributes.TryGetItem(
                                BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME, out locationAttribute))

                                location = locationAttribute.Value;

                            else

                                location = null;

                            operationBinding =
                                new HttpOperationBinding()
                                {
                                    Location = location
                                };

                            break;

                        default:

                            operationBinding = null;

                            break;
                    }

                    if (operationBinding != null)

                        portOperationBinding.Extensions.Add(operationBinding);

                    portBinding.Operations.Add(portOperationBinding);

                    if (inputOperationMessage != null)
                    {
                        WSInputBinding inputBinding = new WSInputBinding();

                        foreach (BindingProperty inputBindingProperty in inputOperationMessage.Bindings)
                        {
                            WSExtensionElement bindingElement = this.TryMapOperationMessageBinding(inputBindingProperty);

                            if (bindingElement != null)

                                inputBinding.Extensions.Add(bindingElement);
                        }

                        portOperationBinding.Input = inputBinding;
                    }

                    // ToDo: Complete ...

                    //portOperationBinding.Output = serviceOperation.Outputs;

                    //foreach (OperationMessage faultMessage in serviceOperation.Faults)

                    //    operationBinding.Faults.Add(faultMessage);
                }
            }

            serviceDescription.Services.Add(service);

            List<global::System.Xml.Schema.ValidationEventArgs> validationEventArgs = new List<global::System.Xml.Schema.ValidationEventArgs>();

            global::System.Xml.Schema.ValidationEventHandler validationEventHandler = (o, e) => { validationEventArgs.Add(e); };

            schemas.Compile(validationEventHandler, true);

            if (validationEventArgs.Count > 0)
            {
                throw new AggregateException("", validationEventArgs.Where(a => a.Severity == global::System.Xml.Schema.XmlSeverityType.Error).Select(a => a.Exception).ToArray());
            }

            foreach (global::System.Xml.Schema.XmlSchema schema in schemas)
            {
                serviceDescription.Types.Schemas.AddFromSystem(schema, out IEnumerable<Microsoft.Xml.Schema.ValidationEventArgs> msValidationEventArgs);

                AggregateException aggregateException = new AggregateException("", msValidationEventArgs.Where(a => a.Severity == Microsoft.Xml.Schema.XmlSeverityType.Error).Select(a => a.Exception).ToArray());

                if (aggregateException.InnerExceptions.Count > 0)

                    throw aggregateException;
            }

            serviceDescription.Types.Schemas.Compile(null, false);

            Uri documentUri = new Uri(description.DocumentUrl);

            Uri documentCacheUri = documentUri.GetCachePathUri(new Uri(this.descriptionExportsPath));

            switch (description.DocumentType)
            {
                case DocumentType.WSDL11:
                case DocumentType.WSDL20:

                    break;

                default:

                    if (!documentCacheUri.TryAppendSuffixToResourceName("wsdl", out documentCacheUri))

                        throw new InvalidOperationException($"{documentUri.AbsoluteUri} is not a full resource identifier.");

                    break;
            }

            if (documentCacheUri.IsFile)

                Directory.CreateDirectory(Directory.GetParent(documentCacheUri.LocalPath).FullName);

            serviceDescription.Write(documentCacheUri.LocalPath);
        }

        private void MapOperationMessageParts(WSDescription description, global::System.Xml.Serialization.XmlSchemas schemas, InteractionMessage operationMessage, WSMessage message)
        {
            if (operationMessage.Domain.Count > 0)
            {
                if (operationMessage.Domain.Count > 1)

                    throw new InvalidOperationException("Too may fields encountered while writing operation message.");

                if (operationMessage.Domain.Any(f => f.Refinement != Refinement.TotalOrdering))

                    throw new InvalidOperationException("Unexpected mixture of fields encountered while writing operation message.");

                foreach (Potential potential in operationMessage.Domain.FirstOrDefault(f => f.Refinement == Refinement.TotalOrdering))
                {
                    message.Parts.Add(
                        MapPart(potential, description.TargetNamespace, schemas));
                }
            }
        }

        private WSMessagePart MapPart(Potential potential, String targetNamespace, global::System.Xml.Serialization.XmlSchemas schemas)
        {
            global::System.Xml.XmlQualifiedName partElement;
            global::System.Xml.XmlQualifiedName partType;

            potential.MapSchemaElement(targetNamespace, schemas, out partElement, out partType);

            WSMessagePart part = new WSMessagePart()
            {
                Name = potential.Name,
                Element = partElement?.ToMicrosoft(),
                Type = partType?.ToMicrosoft()
            };

            XmlElement partDocumentationElement;

            if (TryGetDocumentationElement(potential.Annotations, out partDocumentationElement))
            {
                if (partDocumentationElement.ChildNodes.OfType<XmlElement>().Count() > 0)

                    part.DocumentationElement = partDocumentationElement;

                else

                    part.Documentation = partDocumentationElement.InnerText;
            }

            return part;
        }

        private WSExtensionElement TryMapOperationMessageBinding(BindingProperty inputBindingProperty)
        {
            if (inputBindingProperty.QualifiedName == BindingConstants.SOAP_BINDING_BODY_PROPERTY_NAME || inputBindingProperty.QualifiedName == BindingConstants.SOAP12_BINDING_BODY_PROPERTY_NAME)
            {
                SoapBodyBinding soapBodyBinding;

                if (inputBindingProperty.QualifiedName == BindingConstants.SOAP_BINDING_BODY_PROPERTY_NAME)

                    soapBodyBinding = new SoapBodyBinding();

                else

                    soapBodyBinding = new Soap12BodyBinding();

                BindingAttribute attribute;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_ENCODINGSTYLE_ATTRIBUTE_NAME, out attribute))

                    soapBodyBinding.Encoding = attribute.Value;

                else

                    soapBodyBinding.Encoding = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_NAMESPACE_ATTRIBUTE_NAME, out attribute))

                    soapBodyBinding.Namespace = attribute.Value;

                else

                    soapBodyBinding.Namespace = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_PARTS_ATTRIBUTE_NAME, out attribute))

                    soapBodyBinding.PartsString = attribute.Value;

                else

                    soapBodyBinding.PartsString = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_USE_ATTRIBUTE_NAME, out attribute))

                    soapBodyBinding.Use = attribute.Value.ParseString<SoapBindingUse>(false);

                else

                    soapBodyBinding.Use = SoapBindingUse.Default;

                return soapBodyBinding;
            }

            if (inputBindingProperty.QualifiedName == BindingConstants.SOAP_BINDING_HEADER_PROPERTY_NAME || inputBindingProperty.QualifiedName == BindingConstants.SOAP12_BINDING_HEADER_PROPERTY_NAME)
            {
                SoapHeaderBinding soapHeaderBinding;

                if (inputBindingProperty.QualifiedName == BindingConstants.SOAP_BINDING_HEADER_PROPERTY_NAME)

                    soapHeaderBinding = new SoapHeaderBinding();

                else

                    soapHeaderBinding = new Soap12HeaderBinding();

                BindingAttribute attribute;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_MESSAGE_ATTRIBUTE_NAME, out attribute))
                {
                    QualifiedName messageQName = attribute.Value;

                    soapHeaderBinding.Message = messageQName.ToXmlQualifiedName().ToMicrosoft();
                }
                else

                    soapHeaderBinding.Message = XmlQualifiedName.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_ENCODINGSTYLE_ATTRIBUTE_NAME, out attribute))

                    soapHeaderBinding.Encoding = attribute.Value;

                else

                    soapHeaderBinding.Encoding = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_NAMESPACE_ATTRIBUTE_NAME, out attribute))

                    soapHeaderBinding.Namespace = attribute.Value;

                else

                    soapHeaderBinding.Namespace = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, out attribute))

                    soapHeaderBinding.Part = attribute.Value;

                else

                    soapHeaderBinding.Part = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_USE_ATTRIBUTE_NAME, out attribute))
                {
                    XmlSerializer s = new XmlSerializer(typeof(SoapBindingUse));

                    soapHeaderBinding.Use = (SoapBindingUse)s.Deserialize(new StringReader("<SoapBindingUse>" + attribute.Value + "</SoapBindingUse>"));
                }
                else

                    soapHeaderBinding.Use = SoapBindingUse.Default;

                return soapHeaderBinding;
            }

            if (inputBindingProperty.QualifiedName == BindingConstants.SOAP_BINDING_FAULT_PROPERTY_NAME || inputBindingProperty.QualifiedName == BindingConstants.SOAP12_BINDING_FAULT_PROPERTY_NAME)
            {
                SoapFaultBinding soapFaultBinding;

                if (inputBindingProperty.QualifiedName == BindingConstants.SOAP_BINDING_FAULT_PROPERTY_NAME)

                    soapFaultBinding = new SoapFaultBinding();

                else

                    soapFaultBinding = new Soap12FaultBinding();

                BindingAttribute attribute;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_ENCODINGSTYLE_ATTRIBUTE_NAME, out attribute))

                    soapFaultBinding.Encoding = attribute.Value;

                else

                    soapFaultBinding.Encoding = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_NAMESPACE_ATTRIBUTE_NAME, out attribute))

                    soapFaultBinding.Namespace = attribute.Value;

                else

                    soapFaultBinding.Namespace = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, out attribute))

                    soapFaultBinding.Name = attribute.Value;

                else

                    soapFaultBinding.Name = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_USE_ATTRIBUTE_NAME, out attribute))
                {
                    XmlSerializer s = new XmlSerializer(typeof(SoapBindingUse));

                    soapFaultBinding.Use = (SoapBindingUse)s.Deserialize(new StringReader("<SoapBindingUse>" + attribute.Value + "</SoapBindingUse>"));
                }
                else

                    soapFaultBinding.Use = SoapBindingUse.Default;

                return soapFaultBinding;
            }

            if (inputBindingProperty.QualifiedName == BindingConstants.SOAP_BINDING_HEADER_FAULT_PROPERTY_NAME)
            {
                SoapHeaderFaultBinding soapHeaderFaultBinding;

                soapHeaderFaultBinding = new SoapHeaderFaultBinding();

                BindingAttribute attribute;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_MESSAGE_ATTRIBUTE_NAME, out attribute))
                {
                    QualifiedName messageQName = attribute.Value;

                    soapHeaderFaultBinding.Message = messageQName.ToXmlQualifiedName().ToMicrosoft();
                }
                else

                    soapHeaderFaultBinding.Message = XmlQualifiedName.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_ENCODINGSTYLE_ATTRIBUTE_NAME, out attribute))

                    soapHeaderFaultBinding.Encoding = attribute.Value;

                else

                    soapHeaderFaultBinding.Encoding = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_NAMESPACE_ATTRIBUTE_NAME, out attribute))

                    soapHeaderFaultBinding.Namespace = attribute.Value;

                else

                    soapHeaderFaultBinding.Namespace = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, out attribute))

                    soapHeaderFaultBinding.Part = attribute.Value;

                else

                    soapHeaderFaultBinding.Part = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_USE_ATTRIBUTE_NAME, out attribute))
                {
                    XmlSerializer s = new XmlSerializer(typeof(SoapBindingUse));

                    soapHeaderFaultBinding.Use = (SoapBindingUse)s.Deserialize(new StringReader("<SoapBindingUse>" + attribute.Value + "</SoapBindingUse>"));
                }
                else

                    soapHeaderFaultBinding.Use = SoapBindingUse.Default;

                return soapHeaderFaultBinding;
            }

            if (inputBindingProperty.QualifiedName == BindingConstants.HTTP_URL_BINDING_ENCODED_PROPERTY_NAME)
            {
                return new HttpUrlEncodedBinding();
            }

            if (inputBindingProperty.QualifiedName == BindingConstants.HTTP_BINDING_URLREPLACEMENT_PROPERTY_NAME)
            {
                return new HttpUrlReplacementBinding();
            }

            if (inputBindingProperty.QualifiedName == BindingConstants.MIME_XML_BINDING_PROPERTY_NAME)
            {
                MimeXmlBinding mimeXmlBinding = new MimeXmlBinding();

                BindingAttribute attribute;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, out attribute))

                    mimeXmlBinding.Part = attribute.Value;

                else

                    mimeXmlBinding.Part = String.Empty;

                return mimeXmlBinding;
            }

            if (inputBindingProperty.QualifiedName == BindingConstants.MIME_MULTIPART_RELATED_BINDING_PROPERTY_NAME)
            {
                MimeMultipartRelatedBinding mimeMultipartRelatedBinding = new MimeMultipartRelatedBinding();

                foreach (BindingProperty partAttribute in inputBindingProperty.Properties)
                {
                    mimeMultipartRelatedBinding.Parts.Add(new MimePart() { });
                }

                return mimeMultipartRelatedBinding;
            }
            //if (bindingElement is MimeMultipartRelatedBinding)
            //{
            //    TryMapBindingExtensionElement(bindingElement, out bindingProperty);

            //    bindingProperties.Add(bindingProperty);

            //    foreach (MimePart mimePart in ((MimeMultipartRelatedBinding)bindingElement).Parts)
            //    {
            //        BindingProperties mimePartBindingProperties = new BindingProperties();

            //        BindingProperty mimePartBindingProperty;

            //        if (TryMapBindingExtensionElement(new MimePartExtension(mimePart), out mimePartBindingProperty))
            //        {
            //            bindingProperty.Properties.Add(mimePartBindingProperty);

            //            mimePartBindingProperty.Properties = mimePartBindingProperties;
            //        }

            //        foreach (WSExtensionElement mimePartExtension in mimePart.Extensions)
            //        {
            //            BindingProperty mimePartExtensionBindingProperty;

            //            TryMapOperationMessageBinding(mimePartExtension, mimePartBindingProperties, out mimePartExtensionBindingProperty);

            //            bindingProperties.Finish(mimePartExtensionBindingProperty.Name);
            //        }
            //    }
            //    //TryMapBindingElement(bindingProperties, bindingElement, "Parts", true);

            //    return true;
            //}

            if (inputBindingProperty.QualifiedName == BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME)
            {
                MimeContentBinding mimeContentBinding = new MimeContentBinding();

                BindingAttribute attribute;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, out attribute))

                    mimeContentBinding.Part = attribute.Value;

                else

                    mimeContentBinding.Part = String.Empty;

                if (inputBindingProperty.Attributes.TryGetItem(BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME, out attribute))

                    mimeContentBinding.Type = attribute.Value;

                else

                    mimeContentBinding.Type = String.Empty;

                return mimeContentBinding;
            }

            return null;
        }

        private bool TryGetDocumentationElement(Annotations annotations, out XmlElement documentationElement)
        {
            documentationElement = null;

            if (annotations.HasAnnotations)
            {
                Dictionary<String, object> descriptionItems = new Dictionary<string, object>();

                foreach (Annotation annotation in annotations)
                {
                    if (annotation.NameSpecified)
                    {
                        if (annotation.Name.Contains(" "))
                        {
                            string[] entryNames = annotation.Name.Split(' ');

                            if (!descriptionItems.ContainsKey(entryNames[0]))

                                descriptionItems.Add(entryNames[0], new Dictionary<String, String>());

                            (descriptionItems[entryNames[0]] as Dictionary<String, String>).Add(entryNames[1], annotation.Representation.ToFormattedString());
                        }
                        else

                            descriptionItems.Add(annotation.Name, annotation.Representation);
                    }
                    else if (descriptionItems.ContainsKey("description"))

                        descriptionItems["description"] += "\n" + annotation.Representation;

                    else

                        descriptionItems.Add("description", annotation.Representation);
                }

                if (descriptionItems.Count > 0)
                {
                    XmlDocument doc = new XmlDocument();

                    if (descriptionItems.Count == 1 && descriptionItems.ContainsKey("description"))
                    {
                        XmlNode el = doc.AppendChild(doc.CreateElement("description", "http://schemas.xmlsoap.org/wsdl/"));

                        el.AppendChild(doc.CreateTextNode(descriptionItems["description"].ToString()));
                    }
                    else
                    {
                        doc.AppendChild(doc.CreateElement("documentation", "http://schemas.xmlsoap.org/wsdl/"));

                        foreach (KeyValuePair<String, Object> descriptionItem in descriptionItems)
                        {
                            if (descriptionItem.Value is Dictionary<String, String>)
                            {
                                XmlNode el1 = doc.DocumentElement.AppendChild(doc.CreateElement(descriptionItem.Key, "http://schemas.xmlsoap.org/wsdl/"));

                                foreach (KeyValuePair<String, String> valueItem in (Dictionary<String, String>)descriptionItem.Value)
                                {
                                    XmlNode el2 = el1.AppendChild(doc.CreateElement(valueItem.Key, "http://schemas.xmlsoap.org/wsdl/"));

                                    el2.AppendChild(doc.CreateTextNode(valueItem.Value));
                                }
                            }
                            else
                            {
                                XmlNode el = doc.DocumentElement.AppendChild(doc.CreateElement(descriptionItem.Key, "http://schemas.xmlsoap.org/wsdl/"));

                                el.AppendChild(doc.CreateTextNode((String)descriptionItem.Value));
                            }
                        }
                    }

                    documentationElement = doc.DocumentElement;
                }
            }

            return documentationElement != null;
        }
    }
}