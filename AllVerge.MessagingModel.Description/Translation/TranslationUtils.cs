using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.Description.Translation
{
    using AllVerge.DataModel.Primitives;
    using AllVerge.DataModel.Primitives.Actuals;
    using AllVerge.DataModel.Primitives.LexicalTypes;
    using AllVerge.DataModel.Primitives.LexicalTypes.Facets;
    
    using AllVerge.DataModel.XMLSchema;
    
    using AllVerge.MessagingModel.Description.Adapters;
    using AllVerge.MessagingModel.Description.Model;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    
    using AllVerge.MessagingModel.ServicePrimitives;
    
    using AllVerge.SystemPrimitives.Net.Mime;
    using System.IdentityModel.Tokens;
    using System.Net.Mime;

    public static class TranslationUtils
    {
        public static Message ValidateAndFormatInputMessage(this Message message, string dispatchActionName, string messageStyleName, Connection dispatchConnection, Interaction dispatchInteraction, string dispatchTargetNamespace, out String accept)
        {
            String mediaType = null;

            if (message.Properties.TryGetProperty(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))
            {
                if (httpRequestMessageProperty.Headers.TryGetHeaderValue(HttpRequestHeader.ContentType, out string contentType))
                    
                    mediaType = new MediaContentType(contentType).MediaType;

                accept = httpRequestMessageProperty.Headers[HttpRequestHeader.Accept];
            }
            else
            {
                if (!message.IsEmpty)
    
                    mediaType = message.Version.CreateMessageContentType().MediaType;

                accept = mediaType;
            }

            InteractionMessage requestMessage = dispatchInteraction.Inputs.FirstOrDefault();

            // Todo: handle null requestMessage ...

            IEnumerable<Potential> headerAgents;
            IEnumerable<Potential> pathAgents;
            IEnumerable<Potential> queryAgents;
            IEnumerable<Potential> formAgents;
            IEnumerable<Potential> bodyAgents;

            requestMessage.GetRequestMessagePotentials(out headerAgents, out pathAgents, out queryAgents, out formAgents, out bodyAgents);

            InteractionMessageStyle messageStyle = dispatchInteraction.GetInteractionMessageStyle();

            String messageContentType;

            string rootElementName;
            string rootElementNamespace;

            switch (messageStyle.Kind)
            {
                case InteractionMessageStyle.BINDING_KIND_HTTP:

                    String[] mimeContentTypes;

                    if (requestMessage.TryGetMimeContentBindings(out mimeContentTypes))
                    {
                        if (mimeContentTypes.Any(m =>
                        {
                            if (MediaTypes.TryGetNormalizedResourceMediaType(m, out String n))

                                return n == mediaType;

                            return m == mediaType;
                        }))

                            messageContentType = mediaType;

                        else

                            messageContentType = mimeContentTypes[0];
                    }
                    else

                        throw new InvalidOperationException("Mime content binding not found.");

                    rootElementName = requestMessage.QualifiedName.LocalName;
                    rootElementNamespace = requestMessage.QualifiedName.Namespace;

                    break;

                case InteractionMessageStyle.BINDING_KIND_SOAP:
                case InteractionMessageStyle.BINDING_KIND_SOAP12:

                    if (messageStyle.BindingPrefix == MessagingBindingConstants.SOAP_BINDING_PREFIX)
                        messageContentType = MediaTypeConstants.TEXT_XML_MEDIA_TYPE;
                    else if(messageStyle.BindingPrefix == MessagingBindingConstants.SOAP12_BINDING_PREFIX)
                        messageContentType = MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE;
                    else
                        throw new InvalidOperationException(String.Format("Unrecognized message style binding prefix {0}.", messageStyle.BindingPrefix));

                    if (messageStyle.BindingStyle == InteractionMessageStyle.BINDING_STYLE_RPC)
                    {
                        rootElementName = dispatchInteraction.Name;
                        rootElementNamespace = dispatchTargetNamespace;
                    }
                    else
                    {
                        rootElementName = requestMessage.QualifiedName.LocalName;
                        rootElementNamespace = requestMessage.QualifiedName.Namespace;
                    }

                    break;

                default:

                    throw new InvalidOperationException(String.Format("Unrecognized message style {0}.", messageStyle));
            }

            MemoryStream stream;

            using (XmlReader reader = message.GetReaderAtBodyContents().ReadSubtree())
            {
                using (stream = ValidateAndFormatMessageBody(reader, messageStyle, rootElementName, rootElementNamespace, bodyAgents, mediaType, messageContentType))
                {
                    if (messageStyle.BindingPrefix == MessagingBindingConstants.SOAP_BINDING_PREFIX)

                        return Message.CreateMessage(MessageVersion.Soap11WSAddressing10, dispatchActionName, stream.CreateBufferedStreamBodyWriter(Encoding.UTF8, false, false));

                    else if (messageStyle.BindingPrefix == MessagingBindingConstants.SOAP12_BINDING_PREFIX)

                        return Message.CreateMessage(MessageVersion.Soap12WSAddressing10, dispatchActionName, stream.CreateBufferedStreamBodyWriter(Encoding.UTF8, false, false));

                    else if (messageContentType == MediaTypeConstants.APPLICATION_XML_MEDIA_TYPE)

                        return Message.CreateMessage(MessageVersion.None, dispatchActionName, stream.CreateBufferedStreamBodyWriter(Encoding.UTF8, false, false));

                    else if (messageContentType == MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE)

                        return Message.CreateMessage(MessageVersion.None, dispatchActionName, stream.CreateBufferedStreamBodyWriter(Encoding.UTF8, false, true));

                    else

                        return null;
                }
            }
        }

        public static Message ValidateAndFormatOuputMessage(this Message message, string dispatchActionName, string messageStyleName, Connection dispatchConnection, Interaction dispatchInteraction, String dispatchTargetNamespace, String accepts)
        {
            InteractionMessageStyle interactionMessageStyle = dispatchInteraction.GetInteractionMessageStyle();

            String contentType;

            if (interactionMessageStyle.BindingPrefix == MessagingBindingConstants.HTTP_BINDING_PREFIX)
            {
                if (message.Properties.TryGetProperty(HttpResponseMessageProperty.Name, out HttpResponseMessageProperty httpResponseMessageProperty) && httpResponseMessageProperty.StatusCode > HttpStatusCode.OK)
                {
                    throw new NotImplementedException("Http fault.");
                }
                else
                {
                    contentType = httpResponseMessageProperty.Headers[HttpResponseHeader.ContentType];

                    XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents();

                    String rootElementName = null;
                    String rootElementNamespace = null;

                    InteractionMessage responseMessage = dispatchInteraction.Outputs.FirstOrDefault();

                    // Todo: handle responseMessage null ...

                    IEnumerable<Potential> headerAgents;
                    IEnumerable<Potential> pathAgents;
                    IEnumerable<Potential> queryAgents;
                    IEnumerable<Potential> statusCodeAgents;
                    IEnumerable<Potential> formAgents;
                    IEnumerable<Potential> bodyAgents;

                    responseMessage.GetResponseMessagePotentials(out headerAgents, out pathAgents, out queryAgents, out formAgents, out bodyAgents, out statusCodeAgents);

                    //rootElementName = responseMessage.MessageQualifiedName.LocalName;
                    //rootElementNamespace = responseMessage.MessageQualifiedName.Namespace;

                    Potential bodyAgent = bodyAgents.FirstOrDefault();

                    rootElementName = bodyAgent.QualifiedName.LocalName;
                    rootElementNamespace = bodyAgent.QualifiedName.Namespace;

                    using (MemoryStream replyStream = ValidateAndFormatMessageBody(bodyReader, interactionMessageStyle, rootElementName, rootElementNamespace, bodyAgents, contentType, accepts))
                    {
                        if (accepts == MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE)

                            return Message.CreateMessage(MessageVersion.None, dispatchActionName, replyStream.CreateBufferedStreamBodyWriter(Encoding.UTF8, false, true));

                        else

                            return Message.CreateMessage(MessageVersion.Default, dispatchActionName, replyStream.CreateBufferedStreamBodyWriter(Encoding.UTF8, false, false));
                    }
                }
            }
            else if (interactionMessageStyle.Kind == InteractionMessageStyle.BINDING_KIND_SOAP)
            {
                String soapBindingStatusCodePropertyName;

                if (interactionMessageStyle.BindingPrefix == MessagingBindingConstants.SOAP_BINDING_PREFIX)
                {
                    contentType = MediaTypeConstants.TEXT_XML_MEDIA_TYPE;

                    soapBindingStatusCodePropertyName = BindingConstants.SOAP_BINDING_STATUS_CODE_PROPERTY_NAME;
                }
                else
                {
                    contentType = MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE;

                    soapBindingStatusCodePropertyName = BindingConstants.SOAP12_BINDING_STATUS_CODE_PROPERTY_NAME;
                }

                if (message.IsFault)
                {
                    InteractionMessage faultMessage = dispatchInteraction.Faults.FirstOrDefault(f => f.Name == ((int)HttpStatusCode.InternalServerError).ToString());

                    if (faultMessage == null)

                        throw new InvalidOperationException(
                            String.Format("Output message is fault, but no corresponding fault message is described for status-code {0} and action {1}.", HttpStatusCode.InternalServerError, dispatchActionName));

                    BindingProperty soapStatusCodeBindingProperty;

                    if (faultMessage.Bindings.TryGetProperty(out soapStatusCodeBindingProperty, soapBindingStatusCodePropertyName))
                    {
                        BindingAttribute bindingPropertyAttribute;

                        if (soapStatusCodeBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, out bindingPropertyAttribute))
                        {
                            bool foundStatudCodeAgent;
                            Actual statusCodeAgent;

                            if (faultMessage.Domain.TryGetPotentialOfKind(LexicalTypeKind.Scalar, bindingPropertyAttribute.Value, out statusCodeAgent))

                                foundStatudCodeAgent = true;

                            else if (faultMessage.Domain.TryGetPotentialOfKind(
                                LexicalTypeKind.Domain,
                                (p) =>
                                {
                                    if (p.Name == bindingPropertyAttribute.Value)
                                    {
                                        DomainType domainType = p.LexicalType as DomainType;

                                        if (domainType.IsUnion && domainType.Domain.IsScalarDomain)

                                            return true;
                                    }

                                    return false;
                                }, 
                                out statusCodeAgent))

                                foundStatudCodeAgent = true;

                            else

                                foundStatudCodeAgent = false;

                            if (foundStatudCodeAgent)
                            {
                                HttpStatusCode statusCode;

                                if (!Enum.TryParse<HttpStatusCode>(statusCodeAgent.Representation.ToFormattedString(), out statusCode))

                                    throw new InvalidOperationException("Not an Http status code.");

                                if (HttpStatusCode.InternalServerError != statusCode)
                                {
                                    throw new InvalidOperationException(
                                        String.Format("Unexpected Http status-code {0}.", statusCode));
                                }
                            }
                        }
                        else

                            throw new InvalidOperationException(
                                String.Format("No x-statusCode agent found for {0}.", faultMessage.Name));
                    }

                    BindingProperty soapFaultCodeBindingProperty;

                    if (faultMessage.Bindings.TryGetProperty(out soapFaultCodeBindingProperty, BindingConstants.MESSAGE_FAULT_PROPERTY_LOCAL_NAME))
                    {
                        BindingAttribute bindingPropertyAttribute;

                        if (soapFaultCodeBindingProperty.Attributes.TryGetItem(BindingConstants.MESSAGE_BINDING_NAME_ATTRIBUTE_NAME, out bindingPropertyAttribute))
                        {
                            Actual soapFaultCodeAgent;

                            if (faultMessage.Domain.TryGetPotentialOfKind(LexicalTypeKind.Domain, bindingPropertyAttribute.Value, out soapFaultCodeAgent))
                            {
                                String rootElementName = "Fault";
                                String rootElementNamespace;

                                if (contentType == MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE)
                                    rootElementNamespace = "http://www.w3.org/2003/05/soap-envelope";
                                else
                                    rootElementNamespace = "http://schemas.xmlsoap.org/soap/envelope/";

                                XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents();

                                XmlDictionaryReader xmlDictionaryReader;

                                using (MemoryStream replyStream = ValidateAndFormatMessageBody(bodyReader, interactionMessageStyle, rootElementName, rootElementNamespace, new Actual[] { soapFaultCodeAgent }, contentType, accepts))
                                {
                                    if (accepts == MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE)

                                        xmlDictionaryReader = replyStream.CreateXmlDictionaryReader(Encoding.UTF8, true);

                                    else

                                        xmlDictionaryReader = replyStream.CreateXmlDictionaryReader(Encoding.UTF8, false);
                                }

                                Message responseMessage = Message.CreateMessage(MessageVersion.Default, MessageFaultHelper.CreateFault(xmlDictionaryReader, 64 * 1024), dispatchActionName);

                                return responseMessage;
                            }
                        }
                    }

                    throw new InvalidOperationException("Fault binding or agent description not found.");
                }
                else
                {
                    String rootElementName = null;
                    String rootElementNamespace = null;

                    InteractionMessage responseMessage = dispatchInteraction.Outputs.FirstOrDefault();

                    // Todo: throw if responseMessage null

                    IEnumerable<Potential> headerAgents;
                    IEnumerable<Potential> pathAgents;
                    IEnumerable<Potential> queryAgents;
                    IEnumerable<Potential> statusCodeAgents;
                    IEnumerable<Potential> formAgents;
                    IEnumerable<Potential> bodyAgents;

                    responseMessage.GetResponseMessagePotentials(out headerAgents, out pathAgents, out queryAgents, out formAgents, out bodyAgents, out statusCodeAgents);

                    switch (interactionMessageStyle.GetBindingString(false))
                    {
                        case InteractionMessageStyle.SOAP_RPC_BINDING:

                            rootElementName = dispatchInteraction.Name +"Response";
                            rootElementNamespace = dispatchTargetNamespace;

                            break;

                        case InteractionMessageStyle.SOAP_DOCUMENT_BINDING:

                            Potential bodyAgent = bodyAgents.FirstOrDefault();

                            if (bodyAgent.LexicalType.Kind == LexicalTypeKind.Domain)
                            {
                                DomainType aggregateValueType = (DomainType)bodyAgent.LexicalType;

                                if (aggregateValueType.QualifiedName != QualifiedName.Empty)
                                {
                                    rootElementName = aggregateValueType.QualifiedName.LocalName;
                                    rootElementNamespace = aggregateValueType.QualifiedName.Namespace;
                                }
                                else if (bodyAgent.Name != QualifiedName.Empty)
                                {
                                    rootElementName = bodyAgent.QualifiedName.LocalName;
                                    rootElementNamespace = bodyAgent.QualifiedName.Namespace;
                                }
                                else

                                    throw new InvalidOperationException("Root of Document style message not found.");
                            }
                            else
                            {
                                rootElementName = bodyAgent.QualifiedName.LocalName;
                                rootElementNamespace = bodyAgent.QualifiedName.Namespace;
                            }

                            break;

                        default:

                            throw new InvalidOperationException("Unknown message style.");
                    }

                    XmlDictionaryReader bodyReader = message.GetReaderAtBodyContents();

                    using (MemoryStream replyStream = ValidateAndFormatMessageBody(bodyReader, interactionMessageStyle, rootElementName, rootElementNamespace, bodyAgents, contentType, accepts))
                    {
                        if (accepts == MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE)

                            return Message.CreateMessage(MessageVersion.Default, dispatchActionName, replyStream.CreateBufferedStreamBodyWriter(Encoding.UTF8, false, true));

                        else

                            return Message.CreateMessage(MessageVersion.Default, dispatchActionName, replyStream.CreateBufferedStreamBodyWriter(Encoding.UTF8, false, false));
                    }
                }
            }
            else

                throw new NotImplementedException(interactionMessageStyle.ToString());
        }

        private static MemoryStream ValidateAndFormatMessageBody(XmlReader reader, InteractionMessageStyle messageStyle, string rootElementName, string rootElementNamespace, IEnumerable<Potential> agents, string contentType, string accepts)
        {
            MemoryStream stream = new MemoryStream();

            while (!reader.EOF && !reader.IsStartElement())
            {
                reader.Read();
            }

            if (reader.EOF)
            {
                if (agents != null && agents.Count() > 0)

                    throw new InvalidOperationException("Body agent definitions found but message has no body.");

                return stream;
            }
            else if (agents == null || agents.Count() == 0)

                throw new InvalidOperationException("Message has body but no body agent definitions found.");

            bool isJsonInput = contentType == MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE;
            bool isJsonOutput = accepts == MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE;

            Func<String, String> emptyIfIsJsonInput = (s) => { if (isJsonInput) return String.Empty; return s; };

            XmlDictionaryWriter writer = null;

            if (isJsonOutput)
                writer =
                    JsonReaderWriterFactory.CreateJsonWriter(stream);
            else
                writer =
                     XmlDictionaryWriter.CreateDictionaryWriter(
                         XmlWriter.Create(stream, new XmlWriterSettings() { OmitXmlDeclaration = true }));

            switch (messageStyle.GetBindingString(false))
            {
                case InteractionMessageStyle.SOAP_RPC_BINDING:

                    if (isJsonInput)
                    {
                        if (reader.LocalName == "root" && reader.NamespaceURI == "")
                        {
                            if (isJsonOutput)
                            {
                                writer.WriteStartElement("root");
                                writer.WriteAttributeString("type", "object");
                            }
                            else
                            {
                                writer.WriteStartElement(rootElementName, rootElementNamespace);
                            }
                        }
                        else

                            throw new InvalidOperationException("Root of RPC style message not found.");
                    }
                    else if (reader.LocalName == rootElementName && messageStyle.IsRootOrEnvelopeNamespace(reader.NamespaceURI, rootElementNamespace))
                    {
                        if (isJsonOutput)
                        {
                            writer.WriteStartElement("root");
                            writer.WriteAttributeString("type", "object");
                        }
                        else
                        {
                            writer.WriteStartElement(rootElementName, rootElementNamespace);
                        }
                    }
                    else

                        throw new InvalidOperationException("Root of RPC style message not found.");

                    break;

                case InteractionMessageStyle.SOAP_DOCUMENT_BINDING:

                    if (isJsonInput)
                    {
                        if (reader.LocalName == "root" && reader.NamespaceURI == "")
                        {
                            if (isJsonOutput)
                            {
                                writer.WriteStartElement("root");
                                writer.WriteAttributeString("type", "object");
                            }
                            else
                            {
                                writer.WriteStartElement(rootElementName, rootElementNamespace);
                            }
                        }
                        else

                            throw new InvalidOperationException("Root of RPC style message not found.");
                    }
                    else if (reader.LocalName == rootElementName && reader.NamespaceURI == rootElementNamespace)
                    {
                        if (isJsonOutput)
                        {
                            writer.WriteStartElement("root");
                            writer.WriteAttributeString("type", "object");
                        }
                        else
                        {
                            writer.WriteStartElement(rootElementName, rootElementNamespace);
                        }
                    }
                    else

                        throw new InvalidOperationException("Root of Document style message not found.");

                    break;

                case InteractionMessageStyle.HTTP_POST_BINDING:

                    if (isJsonInput)
                    {
                        if (reader.LocalName == "root" && reader.NamespaceURI == String.Empty)
                        {
                            if (isJsonOutput)
                            {
                                writer.WriteStartElement("root");
                                writer.WriteAttributeString("type", "object");
                            }
                            else
                            {
                                writer.WriteStartElement(rootElementName, rootElementNamespace);
                            }
                        }
                    }
                    else if (reader.LocalName == rootElementName && reader.NamespaceURI == rootElementNamespace)
                    {
                        if (isJsonOutput)
                        {
                            writer.WriteStartElement("root");
                            writer.WriteAttributeString("type", "object");
                        }
                        else
                        {
                            writer.WriteStartElement(rootElementName, rootElementNamespace);
                        }
                    }

                    break;
            }

            ValidationErrors actualValidationErrors = new ValidationErrors();

            Potential currentAgent = null;

            Potential currentScalarAgent = null;
            Potential currentUnionAgent = null;
            Potential currentAnyAgent = null;
            Potential currentCompositeAgent = null;

            Stack<Potential> AggregateAgentsStack = new Stack<Potential>();

            Carrier carrier = null;

            Stack<Carrier> carrierArrayStack = new Stack<Carrier>();

            reader.Read();

            while (!reader.EOF)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:

                        if (carrier != null)
                        {
                            if (isJsonInput)
                            {
                                if (reader.LocalName == "item" && reader.NamespaceURI == String.Empty)
                                {
                                    if (carrier.RepresentationMultiplicity.HasValue)

                                        carrier.RepresentationMultiplicity++;

                                    else if (carrier.ArrayIndex.HasValue)

                                        carrier.ArrayIndex++;

                                    else if (carrier.Validate)

                                        throw new InvalidOperationException("Unexpected array item");
                                }
                                else
                                {
                                    if (carrier.ArrayIndex.HasValue)

                                        carrierArrayStack.Push(carrier);

                                    carrier = null;
                                }
                            }
                            else if (reader.LocalName == carrier.Name && reader.NamespaceURI == emptyIfIsJsonInput(carrier.NamespaceUri))
                            {
                                carrier.RepresentationMultiplicity++;
                            }
                            else if (reader.NamespaceURI == "http://schemas.microsoft.com/2003/10/Serialization/")
                            {
                                if (reader.Name == "string")
                                {
                                    carrier.Realization = Term.CreateImmutable(reader.ReadElementContentAsString());

                                    continue;
                                }
                                else

                                    throw new NotImplementedException(reader.Name);
                            }
                            else

                                carrier = null;
                        }

                        if (currentAgent == null)
                        {
                            if (isJsonInput)

                                currentAgent = agents.TryFindPotential(reader.LocalName);

                            else

                                currentAgent = agents.TryFindPotential(reader.LocalName, reader.NamespaceURI);

                            if (currentAgent == null)
                            {
                                if (!agents.TryGetPotentialOfKind(LexicalTypeKind.Any, rootElementName, rootElementNamespace, out currentAgent))

                                    throw new InvalidOperationException(String.Format("Unexpected start element '{0}' encountered.", reader.Name));
                            }

                            if (currentAgent.LexicalType.Kind == LexicalTypeKind.Domain)
                            {
                                DomainType curentAgentDomainType = currentAgent.LexicalType as DomainType;

                                switch (curentAgentDomainType.Derivation)
                                {
                                    case DomainTypeDerivation.Union:

                                        currentUnionAgent = currentAgent;

                                        if (curentAgentDomainType.Domain.IsScalarDomain)

                                            currentScalarAgent = currentAgent;

                                        break;

                                    default:

                                        currentCompositeAgent = currentAgent;

                                        break;
                                }
                            }
                            else if (currentAgent.LexicalType.Kind == LexicalTypeKind.Scalar)

                                currentScalarAgent = currentAgent;

                            else if (currentAgent.LexicalType.Kind == LexicalTypeKind.Any)

                                currentAnyAgent = currentAgent;
                        }
                        else if (currentCompositeAgent != null)
                        {
                            DomainType currentCompositeAgentType = (DomainType)currentCompositeAgent.LexicalType;

                            Potential currentChildAgent;

                            if (isJsonInput)
                            {
                                if (carrier != null && carrier.ArrayIndex.HasValue && reader.LocalName == "item" && reader.NamespaceURI == String.Empty)

                                    currentChildAgent = null;

                                else
                                {
                                    if (new Object() == null) //??

                                        currentChildAgent = currentCompositeAgentType.Domain.GetPotential(carrier.Name, false);

                                    else

                                        currentChildAgent = currentCompositeAgentType.Domain.GetPotential(reader.LocalName, false);

                                    if (currentChildAgent == null)

                                        throw new InvalidOperationException(String.Format("Unexpected element name '{0}'.", reader.Name));
                                }
                            }
                            else
                            {
                                currentChildAgent = currentCompositeAgentType.Domain.GetPotential(reader.LocalName, false);

                                if (currentChildAgent == null)

                                    throw new InvalidOperationException(String.Format("Unexpected element name '{0}'.", reader.Name));
                            }

                            if (currentChildAgent.LexicalType.Kind == LexicalTypeKind.Domain)
                            {
                                DomainType currentChildAgentDomainType = currentChildAgent.LexicalType as DomainType;

                                switch (currentChildAgentDomainType.Derivation)
                                {
                                    case DomainTypeDerivation.Union:

                                        currentUnionAgent = currentChildAgent;

                                        if (currentChildAgentDomainType.Domain.IsScalarDomain)

                                            currentScalarAgent = currentChildAgent;

                                        break;

                                    default:

                                        AggregateAgentsStack.Push(currentCompositeAgent);

                                        currentCompositeAgent = currentChildAgent;

                                        break;
                                }
                            }
                            else if (currentChildAgent.LexicalType.Kind == LexicalTypeKind.Scalar)

                                currentScalarAgent = currentChildAgent;

                            else if (currentChildAgent.LexicalType.Kind == LexicalTypeKind.Any)

                                currentAnyAgent = currentChildAgent;
                        }

                        if (currentCompositeAgent != null)
                        {
                            if (currentScalarAgent != null)
                            {
                                if (carrier == null)
                                {
                                    ScalarType scalarValueType = (ScalarType)currentScalarAgent.LexicalType;

                                    carrier = new Carrier();

                                    carrier.Name = reader.LocalName;
                                    carrier.NamespaceUri = reader.NamespaceURI;
                                    carrier.SimpleTypeName = scalarValueType.GetSimpleType(out carrier.SimpleTypeFormat, out carrier.RepresentationMultiplicity);

                                    if (currentScalarAgent.Structure.HasStructure)

                                        carrier.ArrayIndex = -1;

                                    // ToDo:  currentScalarAgent.Structure.Domain.Count > 1?
                                }
                            }
                            else if (currentUnionAgent != null)
                            {
                                if (carrier == null)
                                {
                                    DomainType currentUnionValueType = (DomainType)currentScalarAgent.LexicalType;

                                    carrier = new Carrier();

                                    carrier.Name = reader.LocalName;
                                    carrier.NamespaceUri = reader.NamespaceURI;
                                    carrier.SimpleTypeName = currentUnionValueType.GetSimpleType(out carrier.SimpleTypeFormat, out carrier.RepresentationMultiplicity);

                                    if (currentScalarAgent.Structure.HasStructure)

                                        carrier.ArrayIndex = -1;

                                    // ToDo:  currentScalarAgent.Structure.Domain.Count > 1?
                                }
                            }
                            else if (currentAnyAgent != null)
                            {
                                carrier = new Carrier();

                                carrier.Name = reader.LocalName;
                                carrier.NamespaceUri = reader.NamespaceURI;

                                if (isJsonInput && reader.MoveToAttribute("type", ""))
                                {
                                    carrier.SimpleTypeName = reader.Value;

                                    reader.MoveToElement();
                                }
                                else

                                    carrier.SimpleTypeName = "string";

                                carrier.Validate = false;
                            }
                            else
                            {
                                if (isJsonInput && reader.LocalName == "item" && reader.NamespaceURI == String.Empty)
                                {
                                    if (carrier == null || !carrier.ArrayIndex.HasValue)
                                    {
                                        carrier = new Carrier();

                                        carrier.Name = reader.LocalName;
                                        carrier.NamespaceUri = reader.NamespaceURI;
                                        carrier.SimpleTypeName = "object";
                                    }
                                }
                                else
                                {
                                    carrier = new Carrier();

                                    carrier.Name = reader.LocalName;
                                    carrier.NamespaceUri = reader.NamespaceURI;
                                    carrier.SimpleTypeName = "object";

                                    if (currentCompositeAgent.Structure.HasStructure)

                                        carrier.ArrayIndex = -1;

                                    // ToDo:  currentCompositeAgent.Structure.Domain.Count > 1?
                                }
                            }
                        }
                        else if (currentScalarAgent != null)
                        {
                            if (carrier == null)
                            {
                                ScalarType currentScalarValueType = (ScalarType)currentScalarAgent.LexicalType;

                                carrier = new Carrier();

                                carrier.Name = reader.LocalName;
                                carrier.NamespaceUri = reader.NamespaceURI;
                                carrier.SimpleTypeName = currentScalarValueType.GetSimpleType(out carrier.SimpleTypeFormat, out carrier.RepresentationMultiplicity);

                                if (reader.HasAttributes)
                                {
                                    reader.MoveToFirstAttribute();

                                    do
                                    {
                                        if (reader.Prefix == "xmlns" && reader.NamespaceURI == "http://www.w3.org/2000/xmlns/")

                                            carrier.AddNameSpaceAttr(reader.LocalName, reader.Value);
                                    }
                                    while (reader.MoveToNextAttribute());
                                }
                            }
                        }
                        else if (currentUnionAgent != null)
                        {
                            if (carrier == null)
                            {
                                DomainType currentUnionValueType = (DomainType)currentScalarAgent.LexicalType;

                                carrier = new Carrier();

                                carrier.Name = reader.LocalName;
                                carrier.NamespaceUri = reader.NamespaceURI;
                                carrier.SimpleTypeName = currentUnionValueType.GetSimpleType(out carrier.SimpleTypeFormat, out carrier.RepresentationMultiplicity);

                                if (reader.HasAttributes)
                                {
                                    reader.MoveToFirstAttribute();

                                    do
                                    {
                                        if (reader.Prefix == "xmlns" && reader.NamespaceURI == "http://www.w3.org/2000/xmlns/")

                                            carrier.AddNameSpaceAttr(reader.LocalName, reader.Value);
                                    }
                                    while (reader.MoveToNextAttribute());
                                }
                            }
                        }
                        else if (currentAnyAgent != null)
                        {
                            carrier = new Carrier();

                            carrier.Name = reader.LocalName;
                            carrier.NamespaceUri = reader.NamespaceURI;

                            if (isJsonInput && reader.MoveToAttribute("type", ""))
                            {
                                carrier.SimpleTypeName = reader.Value;

                                reader.MoveToElement();
                            }
                            else

                                carrier.SimpleTypeName = "string";

                            carrier.Validate = false;
                        }

                        if (isJsonOutput)
                        {
                            if (carrier.RepresentationMultiplicity.HasValue)
                            {
                                if (carrier.RepresentationMultiplicity < 0)
                                {
                                    writer.WriteStartElement(carrier.Name);

                                    writer.WriteAttributeString("type", "array");
                                }
                                else
                                {
                                    writer.WriteStartElement("item");

                                    writer.WriteAttributeString("type", carrier.SimpleTypeName);
                                }
                            }
                            else if (carrier.ArrayIndex.HasValue)
                            {
                                if (carrier.ArrayIndex < 0)
                                {
                                    writer.WriteStartElement(carrier.Name);

                                    writer.WriteAttributeString("type", "array");
                                }
                                else
                                {
                                    writer.WriteStartElement("item");

                                    writer.WriteAttributeString("type", carrier.SimpleTypeName);
                                }
                            }
                            else
                            {
                                writer.WriteStartElement(carrier.Name);

                                writer.WriteAttributeString("type", carrier.SimpleTypeName);
                            }
                        }
                        else

                            writer.WriteStartElement(carrier.Name);

                        break;

                    case XmlNodeType.Attribute:

                        carrier.Attributes.Add(ReadAttribute(reader));

                        break;

                    case XmlNodeType.Text:

                        if (carrier != null)

                            carrier.Realization = Term.CreateImmutable(reader.Value);

                        break;

                    case XmlNodeType.EndElement:

                        if (currentCompositeAgent != null)
                        {
                            if (reader.LocalName == rootElementName && reader.NamespaceURI == rootElementNamespace)
                            {
                                if (AggregateAgentsStack.Count == 0)

                                    currentCompositeAgent = null;

                                else

                                    currentCompositeAgent = AggregateAgentsStack.Pop();

                                writer.WriteEndElement();
                            }
                            else if (
                                reader.LocalName == currentCompositeAgent.QualifiedName.LocalName && 
                                reader.NamespaceURI == emptyIfIsJsonInput(currentCompositeAgent.QualifiedName.Namespace))
                            {
                                if (AggregateAgentsStack.Count == 0)

                                    currentCompositeAgent = null;

                                else

                                    currentCompositeAgent = AggregateAgentsStack.Pop();

                                writer.WriteEndElement();
                            }
                            else
                            {
                                if (carrier != null)
                                {
                                    foreach (Tuple<String, String> attibute in carrier.Attributes)

                                        writer.WriteAttributeString(attibute.Item1, attibute.Item2);

                                    if (currentScalarAgent != null)

                                        currentScalarAgent.TryValidateAndWriteCurrentScalarAgentValue(carrier, reader, writer, isJsonInput, isJsonOutput, actualValidationErrors);

                                    else if (currentUnionAgent != null)

                                        currentUnionAgent.TryValidateAndWriteCurrentUnionAgentValue(carrier, reader, writer, isJsonInput, isJsonOutput, actualValidationErrors);

                                    else if (currentAnyAgent != null)

                                        writer.WriteString(carrier.Realization.ToFormattedString()); // ToDo:  supply xml write function?

                                    carrier = null;
                                }
                                else if (carrierArrayStack.Count > 0 && reader.LocalName == "item" && reader.NamespaceURI == string.Empty)

                                    carrierArrayStack.Pop();

                                else

                                    throw new InvalidOperationException("Mising signal.");

                                if (currentScalarAgent != null && reader.LocalName == currentScalarAgent.QualifiedName.LocalName && reader.NamespaceURI == currentScalarAgent.QualifiedName.Namespace)
                                {
                                    currentScalarAgent = null;
                                }
                                else if (currentUnionAgent != null && reader.LocalName == currentUnionAgent.QualifiedName.LocalName && reader.NamespaceURI == currentUnionAgent.QualifiedName.Namespace)
                                {
                                    currentUnionAgent = null;
                                }
                                else if (currentAnyAgent != null && reader.LocalName == currentAnyAgent.QualifiedName.LocalName && reader.NamespaceURI == currentAnyAgent.QualifiedName.Namespace)
                                {
                                    currentAnyAgent = null;
                                }

                                writer.WriteEndElement();
                            }
                        }
                        else if (currentAnyAgent != null)
                        {
                            if (carrier != null)
                            {
                                if (reader.LocalName != carrier.Name || reader.NamespaceURI != carrier.NamespaceUri)

                                    throw new InvalidOperationException(String.Format("Unexpected element name '{0}'.", reader.Name));

                                foreach (Tuple<String, String> attibute in carrier.Attributes)

                                    writer.WriteAttributeString(attibute.Item1, attibute.Item2);

                                writer.WriteString(carrier.Realization.ToFormattedString()); // ToDo:  supply xml write function?

                                carrier = null;
                            }

                            if (reader.LocalName == currentAnyAgent.QualifiedName.LocalName && reader.NamespaceURI == currentAnyAgent.QualifiedName.Namespace)
                            {
                                currentAnyAgent = null;
                            }

                            writer.WriteEndElement();
                        }
                        else if (currentUnionAgent != null)
                        {
                            if (carrier != null)
                            {
                                foreach (Tuple<String, String> attibute in carrier.Attributes)

                                    writer.WriteAttributeString(attibute.Item1, attibute.Item2);

                                currentUnionAgent.TryValidateAndWriteCurrentUnionAgentValue(carrier, reader, writer, isJsonInput, isJsonOutput, actualValidationErrors);

                                carrier = null;
                            }

                            if (reader.LocalName == currentUnionAgent.QualifiedName.LocalName && reader.NamespaceURI == currentUnionAgent.QualifiedName.Namespace)
                            {
                                currentUnionAgent = null;
                            }

                            writer.WriteEndElement();
                        }
                        else if (currentScalarAgent != null)
                        {
                            if (carrier != null)
                            {
                                foreach (Tuple<String, String> attibute in carrier.Attributes)

                                    writer.WriteAttributeString(attibute.Item1, attibute.Item2);

                                currentScalarAgent.TryValidateAndWriteCurrentScalarAgentValue(carrier, reader, writer, isJsonInput, isJsonOutput, actualValidationErrors);

                                carrier = null;
                            }

                            if (reader.LocalName == currentScalarAgent.QualifiedName.LocalName && reader.NamespaceURI == currentScalarAgent.QualifiedName.Namespace)
                            {
                                currentScalarAgent = null;
                            }

                            writer.WriteEndElement();
                        }
                        else
                        {
                            if (isJsonInput && reader.LocalName == "root" && reader.NamespaceURI == "")
                            {
                                writer.WriteEndElement();
                            }
                            else if (reader.LocalName == rootElementName && reader.NamespaceURI == rootElementNamespace)
                            {
                                writer.WriteEndElement();
                            }
                            else if (reader.LocalName == rootElementName && messageStyle.Kind == "Soap" && messageStyle.BindingStyle == "rpc" && reader.NamespaceURI == messageStyle.EnvelopeNamespace)
                            {
                                writer.WriteEndElement();
                            }
                            else if (!messageStyle.IsRootOrEnvelopeNamespace(reader.NamespaceURI, rootElementNamespace))

                                throw new InvalidOperationException(String.Format("Unexpected end element '{0}' encountered.", reader.Name));
                        }

                        if (currentAgent != null && 
                            reader.LocalName == currentAgent.QualifiedName.LocalName && 
                            reader.NamespaceURI == emptyIfIsJsonInput(currentAgent.QualifiedName.Namespace))
                        {
                            currentAgent = null;
                        }

                        break;

                    default:

                        break;
                }

                reader.Read();
            }

            writer.Flush();

            // Todo:  close writer ...

            stream.Position = 0;

            if (actualValidationErrors.Count > 0)

                throw actualValidationErrors.ToInvalidDataException();

            return stream;
        }

        private static void TryValidateAndWriteCurrentUnionAgentValue(this Potential currentUnionAgent, Carrier signal, XmlReader reader, XmlDictionaryWriter writer, bool isJsonInput, bool isJsonOutput, ValidationErrors errors)
        {
            DomainType currentUnionValueType;

            if (currentUnionAgent.LexicalType.Kind == LexicalTypeKind.Domain && (currentUnionAgent.LexicalType as DomainType).Derivation == DomainTypeDerivation.Union)

                currentUnionValueType = (DomainType)currentUnionAgent.LexicalType;

            else

                throw new ArgumentException("Parameter value type is not a union type.", nameof(currentUnionAgent));

            Func<String, String> emptyIfIsJsonInput = (s) => { if (isJsonInput) return String.Empty; return s; };

            Func<ScalarType, String, String> formatValue = (vallueType, value) =>
            {
                if (isJsonOutput)
                {
                    if (vallueType.QualifiedName.Namespace == XsDataType.NAMESPACE)
                    {
                        switch (vallueType.QualifiedName.LocalName)
                        {
                            case "QName":

                                return QualifiedName.FromXmlName(value).LocalName;
                        }
                    }
                }

                return value;
            };

            try
            {
                if (signal.NameSpaceAttrs.Count > 0)
                {
                    QualifiedName.PushCurrentNamespaceManagerScope();

                    foreach (KeyValuePair<String, String> nsAttr in signal.NameSpaceAttrs)
                    {
                        QualifiedName.TryAddNamespace(nsAttr.Key, nsAttr.Value);
                    }
                }

                String simplifiedValue;

                if (currentUnionValueType.IsValid(currentUnionAgent.QualifiedName, signal.Realization, out simplifiedValue, errors))
                {
                    if (isJsonInput && reader.LocalName == "item" && reader.NamespaceURI == String.Empty)
                    {
                        if (signal.RepresentationMultiplicity.HasValue)
                        {
                            if (signal.RepresentationMultiplicity.Value > 0)
                            {
                                writer.WriteString(signal.GetPrependedDelimiterAndValue(currentUnionValueType.Facets.ValuesSeparatorFacet));
                            }
                            else
                            {
                                if (isJsonOutput && simplifiedValue != null)

                                    writer.WriteString(simplifiedValue);

                                else

                                    writer.WriteString(signal.Realization.ToFormattedString()); // ToDo:  supply xml write function?
                            }
                        }
                        else

                            throw new InvalidOperationException("Agent does not have values.");
                    }
                    else if (reader.LocalName == signal.Name && reader.NamespaceURI == emptyIfIsJsonInput(signal.NamespaceUri))
                    {
                        if (isJsonOutput && simplifiedValue != null)

                            writer.WriteString(simplifiedValue);

                        else

                            writer.WriteString(signal.Realization.ToFormattedString()); // ToDo:  supply xml write function?
                    }
                    else

                        throw new InvalidOperationException(String.Format("Unexpected element name '{0}'.", reader.Name));
                }
            }
            finally
            {
                if (signal.NameSpaceAttrs.Count > 0)

                    QualifiedName.PopCurrentNamespaceManagerScope();
            }
        }

        private static void TryValidateAndWriteCurrentScalarAgentValue(this Potential scalarAgent, Carrier signal, XmlReader reader, XmlDictionaryWriter writer, bool isJsonInput, bool isJsonOutput, ValidationErrors errors)
        {
            Func<String, String> emptyIfIsJsonInput = (s) => { if (isJsonInput) return String.Empty; return s; };

            Func<ScalarType, String, String> formatValue = (valueType, value) =>
            {
                if (isJsonOutput)
                {
                    if (valueType.QualifiedName.Namespace == XsDataType.NAMESPACE)
                    {
                        switch (valueType.QualifiedName.LocalName)
                        {
                            case "QName":

                                return QualifiedName.FromXmlName(value).LocalName;
                        }
                    }
                }

                return value;
            };

            try
            {
                if (signal.NameSpaceAttrs.Count > 0)
                {
                    QualifiedName.PushCurrentNamespaceManagerScope();

                    foreach (KeyValuePair<String, String> nsAttr in signal.NameSpaceAttrs)
                    {
                        QualifiedName.TryAddNamespace(nsAttr.Key, nsAttr.Value);
                    }
                }

                String simplifiedValue;

                if (scalarAgent.LexicalType.IsValid(scalarAgent.QualifiedName, signal.Realization, out simplifiedValue, errors))
                {
                    if (isJsonInput && reader.LocalName == "item" && reader.NamespaceURI == String.Empty)
                    {
                        if (signal.RepresentationMultiplicity.HasValue)
                        {
                            if (signal.RepresentationMultiplicity.Value > 0)
                            {
                                writer.WriteString(signal.GetPrependedDelimiterAndValue(scalarAgent.LexicalType.Facets.ValuesSeparatorFacet));
                            }
                            else
                            {
                                if (isJsonOutput && simplifiedValue != null)

                                    writer.WriteString(simplifiedValue);

                                else

                                    writer.WriteString(signal.Realization.ToFormattedString()); // ToDo:  supply xml write function?
                            }
                        }
                        else

                            throw new InvalidOperationException("Agent does not have values.");
                    }
                    else if (reader.LocalName == signal.Name && reader.NamespaceURI == emptyIfIsJsonInput(signal.NamespaceUri))
                    {
                        if (isJsonOutput && simplifiedValue != null)

                            writer.WriteString(simplifiedValue);

                        else

                            writer.WriteString(signal.Realization.ToFormattedString());  // ToDo:  supply xml write function?
                    }
                    else

                        throw new InvalidOperationException(String.Format("Unexpected element name '{0}'.", reader.Name));
                }
            }
            finally
            {
                if (signal.NameSpaceAttrs.Count > 0)

                    QualifiedName.PopCurrentNamespaceManagerScope();
            }
        }

        private static bool TrySetInstanceValue(XmlReader reader, Carrier signal)
        {
            String instanceType = null;

            if (reader.HasAttributes)
            {
                if (reader.MoveToFirstAttribute())
                {
                    do
                    {
                        if (reader.LocalName == "type" && reader.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance")
                        {
                            instanceType = reader.Value;

                            break;
                        }
                    }
                    while (reader.MoveToNextAttribute());

                    reader.MoveToElement();
                }
            }

            if (instanceType != null)
            {
                if (instanceType.EndsWith("string")) // ToDo: use type namespace for prefix

                    signal.Realization = Term.CreateImmutable(reader.ReadString());

                else

                    throw new NotImplementedException("instanceType");
            }

            return signal.Realization != null;
        }

        private static Tuple<String, String> ReadAttribute(XmlReader reader)
        {
            if (reader.IsDefault)
            {
                throw new NotImplementedException("LoadDefaultAttribute");
            }

            string attributeName = reader.Name;

            StringBuilder sb = new StringBuilder();

            while (reader.ReadAttributeValue())
            {
                XmlNodeType nodeType = reader.NodeType;

                if (nodeType != XmlNodeType.Text)
                {
                    if (nodeType != XmlNodeType.EntityReference)

                        throw new ArgumentException("UnexpectedNodeType", reader.NodeType.ToString());

                    throw new NotImplementedException("ResolveEntityReference");
                }
                else

                    sb.Append(reader.Value);

            }

            return new Tuple<String, String>(attributeName, sb.ToString());
        }
    }

    internal class Carrier
    {
        private Dictionary<String, String> namespaceAttrs = new Dictionary<string, string>();

        public String Name;
        public String NamespaceUri;
        public String SimpleTypeName;
        public String SimpleTypeFormat = null;
        public int? ArrayIndex;
        public int? RepresentationMultiplicity;
        public IRealization Realization;
        public List<Tuple<String, String>> Attributes = new List<Tuple<string, string>>();
        public IDictionary<String, String> NameSpaceAttrs { get { return this.namespaceAttrs; } }
        public bool Validate = true;

        public void AddNameSpaceAttr(string prefix, string ns)
        {
            this.namespaceAttrs.Add(prefix, ns);
        }

        public String GetPrependedDelimiterAndValue(LexicalValuesSeparatorFacet separatorFacet)
        {
            if (this.Realization == null)

                return null;

            switch (separatorFacet)
            {
                case LexicalValuesSeparatorFacet.Comma:

                    return ", "+ this.Realization;

                case LexicalValuesSeparatorFacet.Ampersand:

                    return "$" + this.Realization;

                case LexicalValuesSeparatorFacet.Pipe:

                    return "|" + this.Realization;

                case LexicalValuesSeparatorFacet.Space:

                    return " " + this.Realization;

                case LexicalValuesSeparatorFacet.Tab:

                    return "\t" + this.Realization;

                case LexicalValuesSeparatorFacet.None:
                default:

                    return this.Realization.ToFormattedString();
            }
        }
    }
}
