using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.Description.Model
{
    using AllVerge.DataModel.Primitives;
    using AllVerge.DataModel.Primitives.Actuals;

    using AllVerge.MessagingModel.MarkupPrimitives.Xml;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions;

    using AllVerge.SystemPrimitives;

    using Newtonsoft.Json;

    public class Interaction : Agent, ISpecifiesBindings
    {
        public const String JSON_PROPERTY_BINDINGS = "bindings";
        public const String PROPERTY_BINDINGS = "Bindings";
        public const String PROPERTY_BINDINGS_ITEMS = BindingProperty.ELEMENT_NAME;
        public const String JSON_ATTRIBUTE_INTERACTION_STYLE = "@style";
        public const String ATTRIBUTE_INTERACTION_STYLE = "style";
        public const String JSON_PROPERTY_INPUTS = "inputs";
        public const String PROPERTY_INPUTS = "Inputs";
        public const String PROPERTY_INPUTS_ITEMS = "Input";
        public const String JSON_PROPERTY_OUTPUTS = "outputs";
        public const String PROPERTY_OUTPUTS = "Outputs";
        public const String PROPERTY_OUTPUTS_ITEMS = "Output";
        public const String JSON_PROPERTY_FAULTS = "faults";
        public const String PROPERTY_FAULTS = "Faults";
        public const String PROPERTY_FAULTS_ITEMS = "Fault";

        private static readonly Type INTERACTION_STYLES_TYPE = typeof(InteractionStyles);
        private static readonly XmlAttributeOverrides INPUT_ITEMS_XML_ATTRIBUTE_OVERRIDES =
            new XmlAttributeOverrides().AddXmlRootAttribute(typeof(InteractionMessage), PROPERTY_INPUTS_ITEMS);
        private static readonly XmlAttributeOverrides OUPUT_ITEMS_XML_ATTRIBUTE_OVERRIDES =
            new XmlAttributeOverrides().AddXmlRootAttribute(typeof(InteractionMessage), PROPERTY_OUTPUTS_ITEMS);
        private static readonly XmlAttributeOverrides FAULT_ITEMS_XML_ATTRIBUTE_OVERRIDES =
            new XmlAttributeOverrides().AddXmlRootAttribute(typeof(InteractionMessage), PROPERTY_FAULTS_ITEMS);

        private BindingProperties bindingProperties;
        private InteractionStyles interactionStyle;

        private List<InteractionMessage> inputs;
        private List<InteractionMessage> outputs;
        private List<InteractionMessage> faults;

        public Interaction() :
            this(Fixable.FixOnRead)
        {
        }

        [JsonConstructor]
        private Interaction(Fixable fixable)
            : base(fixable)
        {
            this.SetLocalFields(null, null, null, null, null);
        }

        public Interaction(
            String name,
            Attributes attributes,
            Annotations annotations,
            BindingProperties bindingProperties,
            InteractionStyles interactionStyle,
            InteractionMessage[] inputs,
            InteractionMessage[] outputs,
            InteractionMessage[] faults) :
            base(name, attributes, annotations)
        {
            this.SetLocalFields(bindingProperties, interactionStyle, inputs, outputs, faults);
        }

        private void SetLocalFields(BindingProperties bindingProperties, InteractionStyles? interactionStyle, InteractionMessage[] inputs, InteractionMessage[] outputs, InteractionMessage[] faults)
        {
            this.bindingProperties = bindingProperties ?? BindingProperties.Create(this.Fixed.ConstructorParameter);
            this.interactionStyle = interactionStyle ?? InteractionStyles.None;
            this.inputs = inputs == null ? new List<InteractionMessage>() : new List<InteractionMessage>(inputs);
            this.outputs = outputs == null ? new List<InteractionMessage>() : new List<InteractionMessage>(outputs);
            this.faults = faults == null ? new List<InteractionMessage>() : new List<InteractionMessage>(faults);

            base.SetHandledAttributeNames(ATTRIBUTE_INTERACTION_STYLE);
        }

        [JsonProperty(JSON_PROPERTY_BINDINGS)]
        [XmlArray(PROPERTY_BINDINGS)]
        [XmlArrayItem(PROPERTY_BINDINGS_ITEMS)]
        public BindingProperties Bindings
        {
            get
            {
                this.Fixed.OnRead();

                return this.bindingProperties;
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(Bindings));

                this.bindingProperties = value;
            }
        }

        public Boolean BindingsSpecified
        {
            get
            {
                return this.bindingProperties.Count > 0;
            }
        }

        public bool ShouldSerializeBindings()
        {
            return this.BindingsSpecified;
        }

        [JsonProperty(JSON_ATTRIBUTE_INTERACTION_STYLE)]
        [XmlAttribute(ATTRIBUTE_INTERACTION_STYLE)]
        public InteractionStyles InteractionStyle
        {
            get
            {
                this.Fixed.OnRead();

                return this.interactionStyle;
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(InteractionStyle));

                this.interactionStyle = value;
            }
        }

        [JsonProperty(JSON_PROPERTY_INPUTS)]
        [XmlArray(PROPERTY_INPUTS)]
        [XmlArrayItem(PROPERTY_INPUTS_ITEMS)]
        public InteractionMessage[] Inputs
        {
            get
            {
                this.Fixed.OnRead();

                return this.inputs.ToArray();
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(Inputs));

                this.inputs.Clear();

                if (value != null)

                    this.inputs.AddRange(value);
            }
        }

        public bool InputsSpecified => this.inputs.Count > 0;

        public bool ShouldSerializeInputs()
        {
            return this.InputsSpecified;
        }

        [JsonProperty(JSON_PROPERTY_OUTPUTS)]
        [XmlArray(PROPERTY_OUTPUTS)]
        [XmlArrayItem(PROPERTY_OUTPUTS_ITEMS)]
        public InteractionMessage[] Outputs
        {
            get
            {
                this.Fixed.OnRead();

                return this.outputs.ToArray();
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(Outputs));

                this.outputs.Clear();

                if (value != null)

                    this.outputs.AddRange(value);
            }
        }

        public bool OutputsSpecified => this.outputs.Count > 0;

        public bool ShouldSerializeOutputs()
        {
            return this.OutputsSpecified;
        }

        [JsonProperty(JSON_PROPERTY_FAULTS)]
        [XmlArray(PROPERTY_FAULTS)]
        [XmlArrayItem(PROPERTY_FAULTS_ITEMS)]
        public InteractionMessage[] Faults
        {
            get
            {
                this.Fixed.OnRead();

                return this.faults.ToArray();
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(Faults));

                this.faults.Clear();

                if (value != null)

                    this.faults.AddRange(value);
            }
        }

        public bool FaultsSpecified => this.faults.Count > 0;

        public bool ShouldSerializeFaults()
        {
            return this.FaultsSpecified;
        }

        protected override void EnsureNameSpecified()
        {
            throw new NotImplementedException();
        }

        public void AddInput(InteractionMessage inputMessage)
        {
            this.inputs.Add(inputMessage);
        }

        public void AddOutput(InteractionMessage outputMessage)
        {
            this.outputs.Add(outputMessage);
        }

        public void AddFault(InteractionMessage faultMessage)
        {
            this.faults.Add(faultMessage);
        }

        /// <summary>
        /// Returns a URI "action" for the operation.
        /// </summary>
        /// <remarks>
        /// For Soap bindings, SOAPAction is defined as a "URI-reference"
        /// (see http://www.w3.org/TR/2000/NOTE-SOAP-20000508/ para. 6.1.1).
        /// URI-reference can include a fragment (see http://www.ietf.org/rfc/rfc2396.txt para. 4).
        /// But, in REST a path is considered a URI (not a URI-reference).
        /// To support interoperability between REST clients and SOAP back-ends 
        /// we must URI "normalize" this fragment.
        /// </remarks>
        /// <param name="normalizeURI"></param>
        /// <returns></returns>
        public Uri GetLocationOrAction(bool normalizeURI)
        {
            BindingProperty operationBinding;
            
            if (this.Bindings.TryGetProperty(out operationBinding, BindingConstants.OPERATION_PROPERTY_LOCAL_NAME))
            {
                if (operationBinding.QualifiedName.Namespace == MessagingBindingConstants.HTTP_BINDING_NAMESPACE)
                {
                    BindingAttribute locationAttribute;

                    if (!operationBinding.Attributes.TryGetItem(BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME, out locationAttribute))

                        return null;

                    return new Uri(locationAttribute.Value, UriKind.Relative);
                }
                else
                {
                    BindingAttribute actionAttribute;
                    
                    if (!operationBinding.Attributes.TryGetItem(BindingConstants.SOAP_ACTION_BINDING_ATTRIBUTE_NAME, out actionAttribute))

                        return null;

                    if (normalizeURI)

                        return new Uri(actionAttribute.Value.Replace('#', '/'), UriKind.Absolute);

                    return new Uri(actionAttribute.Value, UriKind.Absolute);
                }
            }

            return null;
        }

        internal void Patch(Interaction patchInteraction)
        {
            if (!this.NameSpecified)

                throw new InvalidOperationException($"Cannot target unidentified resouce member ({nameof(patchInteraction)}) for patching.");

            if (!patchInteraction.NameSpecified)

                throw new ArgumentException($"An unidentified resouce member ({nameof(patchInteraction)}) cannot be a patching source.", nameof(patchInteraction.Name));

            if (this.Name != patchInteraction.Name)

                throw new ArgumentException($"Patch resouce member ({nameof(patchInteraction)}) does not map to target.", nameof(patchInteraction.Name));

            if (patchInteraction.AttributesSpecified)

                this.Attributes.Patch(patchInteraction.Attributes);

            if (patchInteraction.AnnotationsSpecified)

                this.Annotations.Patch(patchInteraction.Annotations);

            if (patchInteraction.BindingsSpecified)

                this.bindingProperties.Patch(patchInteraction.bindingProperties, BindingTargets.Interaction);

            if (patchInteraction.InputsSpecified)

                this.inputs.Patch(patchInteraction.inputs);

            if (patchInteraction.OutputsSpecified)

                this.outputs.Patch(patchInteraction.outputs);

            if (patchInteraction.FaultsSpecified)

                this.faults.Patch(patchInteraction.faults);
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            this.Fixed.OnDeserialized(streamingContext);
        }

        protected override void OnWriteAttributes(XmlWriter writer)
        {
            base.OnWriteAttributes(writer);

            if (!XmlFormatterExtensions.TryGetXmlEnumAttributeNameFromEnum(this.InteractionStyle, out String messaging))

                messaging = this.InteractionStyle.ToString();

            writer.WriteAttributeString(ATTRIBUTE_INTERACTION_STYLE, messaging);
        }

        protected override void OnReadAttributes(XmlReader reader)
        {
            base.OnReadAttributes(reader);

            if (reader.MoveToAttribute(ATTRIBUTE_INTERACTION_STYLE))
            {
                if (!reader.Value.TryGetEnumFromXmlEnumAttributeName(out InteractionStyles interactionStyle))

                    interactionStyle = (InteractionStyles)Enum.Parse(INTERACTION_STYLES_TYPE, reader.Value);

                this.interactionStyle = interactionStyle;

                reader.MoveToElement();
            }
        }

        protected override void OnWriteProperties(XmlWriter writer)
        {
            base.OnWriteProperties(writer);

            if (this.ShouldSerializeBindings())
            {
                writer.WriteStartElement(PROPERTY_BINDINGS);

                foreach (BindingProperty binding in this.Bindings)

                    writer.WriteRaw(binding.Serialize(XmlSerialization.EmptyNSMap).OuterXml);

                writer.WriteEndElement();
            }

            if (this.ShouldSerializeInputs())
            {
                writer.WriteStartElement(PROPERTY_INPUTS);

                XmlAttributeOverrides xmlAttributeOverrides =
                    new XmlAttributeOverrides().AddXmlRootAttribute(typeof(InteractionMessage), PROPERTY_INPUTS_ITEMS);

                foreach (InteractionMessage message in this.Inputs)

                    writer.WriteRaw(message.Serialize(xmlAttributeOverrides, XmlSerialization.EmptyNSMap).OuterXml);

                writer.WriteEndElement();
            }

            if (this.ShouldSerializeOutputs())
            {
                writer.WriteStartElement(PROPERTY_OUTPUTS);

                XmlAttributeOverrides xmlAttributeOverrides = 
                    new XmlAttributeOverrides().AddXmlRootAttribute(typeof(InteractionMessage), PROPERTY_OUTPUTS_ITEMS);

                foreach (InteractionMessage message in this.Outputs)

                    writer.WriteRaw(message.Serialize(xmlAttributeOverrides, XmlSerialization.EmptyNSMap).OuterXml);

                writer.WriteEndElement();
            }

            if (this.ShouldSerializeFaults())
            {
                writer.WriteStartElement(PROPERTY_FAULTS);

                XmlAttributeOverrides xmlAttributeOverrides =
                    new XmlAttributeOverrides().AddXmlRootAttribute(typeof(InteractionMessage), PROPERTY_FAULTS_ITEMS);

                foreach (InteractionMessage message in this.Faults)

                    writer.WriteRaw(message.Serialize(xmlAttributeOverrides, XmlSerialization.EmptyNSMap).OuterXml);

                writer.WriteEndElement();
            }
        }

        protected override void OnReadProperties(XmlReader reader, string elementName)
        {
            base.OnReadProperties(reader, elementName);

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_BINDINGS)
            {
                while (reader.Read() && reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_BINDINGS_ITEMS)
                {
                    using (var r = reader.ReadSubtree())
                    {
                        this.Bindings.Add(r.Deserialize<BindingProperty>());
                    }
                }

                reader.ReadEmptyOrEndElement(PROPERTY_BINDINGS);
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_INPUTS)
            {
                List<InteractionMessage> messages = new List<InteractionMessage>();

                while (reader.Read() && reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_INPUTS_ITEMS)
                {
                    using (var r = reader.ReadSubtree())
                    {
                        messages.Add(r.Deserialize<InteractionMessage>(INPUT_ITEMS_XML_ATTRIBUTE_OVERRIDES));
                    }
                }

                reader.ReadEmptyOrEndElement(PROPERTY_INPUTS);

                this.inputs.AddRange(messages);
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_OUTPUTS)
            {
                List<InteractionMessage> messages = new List<InteractionMessage>();

                while (reader.Read() && reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_OUTPUTS_ITEMS)
                {
                    using (var r = reader.ReadSubtree())
                    {
                        messages.Add(r.Deserialize<InteractionMessage>(OUPUT_ITEMS_XML_ATTRIBUTE_OVERRIDES));
                    }
                }

                reader.ReadEmptyOrEndElement(PROPERTY_OUTPUTS);

                this.outputs.AddRange(messages);
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_FAULTS)
            {
                List<InteractionMessage> messages = new List<InteractionMessage>();

                while (reader.Read() && reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_FAULTS_ITEMS)
                {
                    using (var r = reader.ReadSubtree())
                    {
                        messages.Add(r.Deserialize<InteractionMessage>(FAULT_ITEMS_XML_ATTRIBUTE_OVERRIDES));
                    }
                }

                reader.ReadEmptyOrEndElement(PROPERTY_FAULTS);

                this.faults.AddRange(messages);
            }
        }
    }

    public static class interactionExtensions
    {
        public static Interaction[] ShallowClone(this Interaction[] interactions)
        {
            List<Interaction> clonedInteractions = new List<Interaction>();

            foreach (Interaction interaction in interactions)
            {
                clonedInteractions.Add(
                    interaction.ShallowClone(true));
            }

            return clonedInteractions.ToArray();
        }

        public static Interaction ShallowClone(this Interaction interaction, bool suppressMessageDetails = false)
        {
            if (suppressMessageDetails)
                return new Interaction(
                    interaction.Name,
                    interaction.Attributes.Clone(),
                    interaction.Annotations.Clone(),
                    interaction.Bindings.Clone(),
                    interaction.InteractionStyle,
                    InteractionMessage.EmptyMessages,
                    InteractionMessage.EmptyMessages,
                    InteractionMessage.EmptyMessages);
            else
                return new Interaction(
                    interaction.Name, 
                    interaction.Attributes.Clone(), 
                    interaction.Annotations.Clone(), 
                    interaction.Bindings.Clone(), 
                    interaction.InteractionStyle,
                    interaction.Inputs.ShallowClone(),
                    interaction.Outputs.ShallowClone(),
                    interaction.Faults.ShallowClone());
        }

        public static String GetDispatchAction(this Interaction interaction, Uri baseUri)
        {
            BindingProperty property;

            if (interaction.Bindings.TryGetProperty(out property, BindingConstants.HTTP_BINDING_OPERATION_PROPERTY_NAME))
            {
                BindingAttribute locationBindingAttribute;

                if (property.Attributes.TryGetItem(BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME, out locationBindingAttribute))
                    
                    return new Uri(baseUri, baseUri.AbsolutePath + locationBindingAttribute.Value).ToString();
            }
            else if (interaction.Bindings.TryGetProperty(out property, BindingConstants.SOAP_BINDING_OPERATION_PROPERTY_NAME))
            {
                BindingAttribute soapActionBindingAttribute;

                if (property.Attributes.TryGetItem(BindingConstants.SOAP_ACTION_BINDING_ATTRIBUTE_NAME, out soapActionBindingAttribute))

                    return soapActionBindingAttribute.Value;
            }
            else if (interaction.Bindings.TryGetProperty(out property, BindingConstants.SOAP_12_BINDING_OPERATION_PROPERTY_NAME))
            {
                BindingAttribute soapActionBindingAttribute;

                if (property.Attributes.TryGetItem(BindingConstants.SOAP_ACTION_BINDING_ATTRIBUTE_NAME, out soapActionBindingAttribute))

                    return soapActionBindingAttribute.Value;
            }

            return null;
        }

        public static InteractionMessageStyle GetInteractionMessageStyle(this Interaction interaction)
        {
            BindingProperty property;

            if (interaction.Bindings.TryGetProperty(out property, BindingConstants.HTTP_BINDING_PROPERTY_NAME))
            {
                BindingAttribute verbBindingAttribute;

                if (property.Attributes.TryGetItem(BindingConstants.BINDING_VERB_ATTRIBUTE_NAME, out verbBindingAttribute))

                    return new InteractionMessageStyle(MessagingBindingConstants.HTTP_BINDING_PREFIX, BindingConstants.HTTP_BINDING_PROPERTY_NAME.Namespace, verbBindingAttribute.Value, interaction.Name, interaction.InteractionStyle);
            }
            else if (interaction.Bindings.TryGetProperty(out property, BindingConstants.SOAP_BINDING_PROPERTY_NAME))
            {
                BindingAttribute soapStyleBindingAttribute;

                if (property.Attributes.TryGetItem(BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_NAME, out soapStyleBindingAttribute))
                    
                    return new InteractionMessageStyle(MessagingBindingConstants.SOAP_BINDING_PREFIX, BindingConstants.SOAP_BINDING_PROPERTY_NAME.Namespace, soapStyleBindingAttribute.Value, interaction.Name, interaction.InteractionStyle);
            }
            else if (interaction.Bindings.TryGetProperty(out property, BindingConstants.SOAP12_BINDING_PROPERTY_NAME))
            {
                BindingAttribute soapStyleBindingAttribute;

                if (property.Attributes.TryGetItem(BindingConstants.SOAP_BINDING_STYLE_ATTRIBUTE_NAME, out soapStyleBindingAttribute))

                    return new InteractionMessageStyle(MessagingBindingConstants.SOAP12_BINDING_PREFIX, BindingConstants.SOAP12_BINDING_PROPERTY_NAME.Namespace, soapStyleBindingAttribute.Value, interaction.Name, interaction.InteractionStyle);
            }

            return null;
        }
    }
}