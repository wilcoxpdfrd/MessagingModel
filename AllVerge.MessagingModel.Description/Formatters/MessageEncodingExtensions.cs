using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.Description.Formatters
{
    using AllVerge.DataModel.Primitives.LexicalTypes;
    using AllVerge.DataModel.Primitives.LexicalTypes.Structures;

    using AllVerge.MessagingModel.Description.Adapters;
    using AllVerge.MessagingModel.Description.Model;

    public static class MessageEncodingExtensions
    {
        public static Message Encode(this Interaction interaction, InteractionMessage interactionMessage, NameValueCollection values)
        {
            if (interaction.Bindings.TryGetProperty(out BindingProperty httpBindingProperty, BindingConstants.HTTP_BINDING_PROPERTY_NAME))
            {
                interactionMessage.GetRequestMessagePotentials(out IEnumerable<Potential> headerPotentials, out IEnumerable<Potential> pathPotentials, out IEnumerable<Potential> queryPotentials, out IEnumerable<Potential> formPotentials, out IEnumerable<Potential> bodyPotentials);

                throw new NotImplementedException(BindingConstants.HTTP_BINDING_PROPERTY_NAME);
            }
            else if (interaction.Bindings.TryGetProperty(out BindingProperty soapBindingProperty, BindingConstants.SOAP_BINDING_PROPERTY_NAME))
            {
                String soapAction = null;

                if (interaction.Bindings.TryGetProperty(out BindingProperty soapOperationBindingProperty, BindingConstants.SOAP_BINDING_OPERATION_PROPERTY_NAME))
                {
                    if (soapOperationBindingProperty.Attributes.TryGetItem(BindingConstants.SOAP_ACTION_BINDING_ATTRIBUTE_NAME, out BindingAttribute soapActionBindingAttribute))
                    {
                        soapAction = soapActionBindingAttribute.Value;
                    }
                }

                if (soapBindingProperty.Attributes.TryGetItem(BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_NAME, out BindingAttribute soapBindingStyleAttribute))
                {
                    Domain bodyDomain;

                    if (interactionMessage.TryGetSoapBodyBinding(out BindingProperty soapBodyBindingProperty))
                    {
                        bodyDomain = interactionMessage.Domain;
                    }
                    else
                    {
                        bodyDomain = Domain.Empty;
                    }

                    //XmlTextBuffer xmlBuffer = new XmlTextBuffer(Int16.MaxValue);

                    XmlBuffer xmlBuffer = new XmlBuffer(Int16.MaxValue);

                    using (XmlDictionaryWriter writer = xmlBuffer.OpenSection(XmlDictionaryReaderQuotas.Max))
                    {
                        switch (soapBindingStyleAttribute.Value)
                        {
                            case BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_RPC:
                                writer.WriteStartElement(interaction.Name);
                                foreach (Potential bodyPotential in bodyDomain.GetPotentials(false))
                                {
                                    writer.WriteElementString(bodyPotential.Name, values[bodyPotential.Name]);
                                }
                                writer.WriteEndElement();
                                break;
                            case BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_DOCUMENT:
                                writer.WritePotentialsValues(interactionMessage.Name, bodyDomain.GetPotentials(false), values);
                                break;
                        }
                    }

                    xmlBuffer.CloseSection();

                    xmlBuffer.Close();

                    return Message.CreateMessage(MessageVersion.Soap11WSAddressing10, soapAction, xmlBuffer.GetReader(0));
                }
                else

                    throw new InvalidOperationException("Interaction model Soap binding style attribute not found.");
            }
            else if (interaction.Bindings.TryGetProperty(out BindingProperty soap12BindingProperty, BindingConstants.SOAP12_BINDING_PROPERTY_NAME))
            {
                String soapAction = null;

                if (interaction.Bindings.TryGetProperty(out BindingProperty soap12OperationBindingProperty, BindingConstants.SOAP_12_BINDING_OPERATION_PROPERTY_NAME))
                {
                    if (soap12OperationBindingProperty.Attributes.TryGetItem(BindingConstants.SOAP_ACTION_BINDING_ATTRIBUTE_NAME, out BindingAttribute soapActionBindingAttribute))
                    {
                        soapAction = soapActionBindingAttribute.Value;
                    }
                }

                if (soap12BindingProperty.Attributes.TryGetItem(BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_NAME, out BindingAttribute soapBindingStyleAttribute))
                {
                    Domain bodyDomain;

                    if (interactionMessage.TryGetSoapBodyBinding(out BindingProperty soapBodyBindingProperty))
                    {
                        //Todo:  refer to "parts" attribute
                        bodyDomain = interactionMessage.Domain;
                    }
                    else
                    {
                        bodyDomain = Domain.Empty;
                    }

                    MemoryStream stream = new MemoryStream();

                    using (XmlWriter writer = XmlDictionaryWriter.Create(stream))
                    {
                        switch (soapBindingStyleAttribute.Value)
                        {
                            case BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_RPC:
                                writer.WriteStartElement(interaction.Name);
                                foreach (Potential bodyPotential in bodyDomain.GetPotentials(false))
                                {
                                    writer.WriteElementString(bodyPotential.Name, values[bodyPotential.Name]);
                                }
                                writer.WriteEndElement();
                                break;
                            case BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_VALUE_DOCUMENT:
                                writer.WritePotentialsValues(null, bodyDomain.GetPotentials(false), values);
                                break;
                        }
                    }

                    stream.Seek(0, SeekOrigin.Begin);

                    using (XmlReader reader = XmlDictionaryReader.Create(stream))
                    {
                        return Message.CreateMessage(MessageVersion.Soap12WSAddressing10, soapAction, reader);
                    }
                }
                else

                    throw new InvalidOperationException("Interaction model Soap binding style attribute not found.");
            }
            else

                throw new InvalidOperationException("Interaction model Bindings are not recognized.");
        }

        private static void WritePotentialsValues(this XmlWriter writer, String parentDomainName, IEnumerable<Potential> bodyPotentials, NameValueCollection values)
        {
            int index = 0;

            foreach (Potential bodyPotential in bodyPotentials)
            {
                String bodyName = $"{parentDomainName}-{index}-{bodyPotential.Name}";

                if (bodyPotential.LexicalType.IsLexicalTypeKind(LexicalTypeKind.Scalar))

                    writer.WriteElementString(bodyPotential.Name, values[bodyName]);

                else
                {
                    writer.WriteStartElement(bodyPotential.Name);

                    writer.WritePotentialsValues(bodyName, (bodyPotential.LexicalType as DomainType).Domain.GetPotentials(false), values);

                    writer.WriteEndElement();
                }

                index++;
            }
        }
    }
}
