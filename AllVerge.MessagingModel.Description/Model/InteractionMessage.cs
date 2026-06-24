using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.Description.Model
{
    using AllVerge.DataModel.Primitives;
    using AllVerge.DataModel.Primitives.Actuals;
    using AllVerge.DataModel.Primitives.LexicalTypes.Structures;

    using AllVerge.MessagingModel.MarkupPrimitives.Xml;
    
    using AllVerge.SystemPrimitives;
    using AllVerge.SystemPrimitives.Collections;
    using Newtonsoft.Json;

    /// <summary>
    /// An <see cref="InteractionMessage"/> is typically named either <see cref="INPUT"/>, <see cref="OUTPUT"/> or <see cref="FAULT"/>,
    /// and exposes a <see cref="AllVerge.Core.Model.LexicalTypes.Structures.Domain"/> to carry message content, whilst specifying bindings (<see cref="BindingProperties"/>) to a protocol.
    /// </summary>
    public class InteractionMessage : QualifiedAgent, ISpecifiesBindings, ISpecifiesDomain
    {
        public const String INPUT = "Input";
        public const String OUTPUT = "Output";
        public const String FAULT = "Fault";

        public static readonly InteractionMessage[] EmptyMessages = new InteractionMessage[0];

        private const String JSON_PROPERTY_BINDINGS = "bindings";
        private const String PROPERTY_BINDINGS = "Bindings";
        private const String PROPERTY_BINDINGS_ITEMS = BindingProperty.ELEMENT_NAME;
        private string typeName;
        private BindingProperties bindingProperties;
        private Domain domain;

        public InteractionMessage() :
            this(Fixable.FixOnRead)
        {
        }

        [JsonConstructor]
        private InteractionMessage(Fixable fixable) :
            base(fixable)
        {
            SetLocalFields(null, null);
        }

        public InteractionMessage(string name, Domain domain) :
            base(name)
        {
            SetLocalFields(null, domain);
        }

        public InteractionMessage(string name, BindingProperties bindingProperties, Domain domain) :
            base(name)
        {
            SetLocalFields(bindingProperties, domain);
        }

        public InteractionMessage(string name, Annotations annotations, BindingProperties bindingProperties, Domain domain) :
            base(name, annotations)
        {
            SetLocalFields(bindingProperties, domain);
        }

        public InteractionMessage(string name, QualifiedName messageName, Annotations annotations, BindingProperties bindingProperties, Domain domain) :
            base(name, messageName, annotations)
        {
            SetLocalFields(bindingProperties, domain);
        }

        private void SetLocalFields(BindingProperties bindingProperties, Domain domain)
        {
            this.typeName = this.GetType().Name;
            this.bindingProperties = bindingProperties ?? BindingProperties.Create(this.Fixed.ConstructorParameter);
            this.SetDomain(domain ?? Domain.Create(this.Fixed.ConstructorParameter));

            this.SetHandledAttributeNames(Domain.GetHandledAttributeNames());
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
                this.Fixed.ThrowIfNotWriteable(this.typeName, nameof(Bindings));

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

        [JsonProperty(Domain.JSON_PROPERTY_NAME)]
        [XmlIgnore]
        public Domain Domain
        {
            get
            {
                this.Fixed.OnRead();

                return this.domain;
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.typeName, nameof(Domain));

                this.domain = value;
            }
        }

        public Boolean DomainSpecified
        {
            get
            {
                return Domain.Specified(this.domain);
            }
        }

        public bool ShouldSerializeDomain()
        {
            return this.DomainSpecified;
        }

        private void SetDomain(Domain domain)
        {
            this.domain = domain;
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            this.Fixed.OnDeserialized(streamingContext);
        }

        protected override void OnWriteAttributes(XmlWriter writer)
        {
            base.OnWriteAttributes(writer);

            Domain.WriteDomainAttributes(this.domain, writer);
        }

        protected override void OnReadAttributes(XmlReader reader)
        {
            base.OnReadAttributes(reader);

            Domain.ReadDomainAttributes(this.domain, reader);
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

            Domain.WriteDomainProperties(this.domain, writer);
        }

        protected override void OnReadProperties(XmlReader reader, string elementName)
        {
            base.OnReadProperties(reader, elementName);

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_BINDINGS)
            {
                while (reader.Read() && reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_BINDINGS_ITEMS)
                {
                    using (XmlReader r = reader.ReadSubtree())
                    {
                        this.Bindings.Add(r.Deserialize<BindingProperty>());
                    }
                }

                reader.ReadEmptyOrEndElement(PROPERTY_BINDINGS);
            }

            Domain.ReadDomainProperties(this.domain, reader, elementName);
        }

        internal InteractionMessage Clone(Func<String, String> interpolationFunction = null)
        {
            return new InteractionMessage(
                this.Name,
                this.QualifiedName.Clone(),
                this.Annotations.Clone(),
                this.Bindings.Clone(),
                this.Domain.Clone(interpolationFunction));
        }

        internal void Patch(InteractionMessage patchInteractionMessage)
        {
            if (!this.NameSpecified)

                throw new InvalidOperationException($"Cannot target unidentified resouce member ({nameof(patchInteractionMessage)}) for patching.");

            if (!patchInteractionMessage.NameSpecified)

                throw new ArgumentException($"An unidentified resouce member ({nameof(patchInteractionMessage)}) cannot be a patching source.", nameof(patchInteractionMessage.Name));

            if (this.Name != patchInteractionMessage.Name)

                throw new ArgumentException($"Patch resouce member ({nameof(patchInteractionMessage)}) does not map to target.", nameof(patchInteractionMessage.Name));

            if (patchInteractionMessage.AttributesSpecified)

                this.Attributes.Patch(patchInteractionMessage.Attributes);

            if (patchInteractionMessage.AnnotationsSpecified)

                this.Annotations.Patch(patchInteractionMessage.Annotations);

            if (patchInteractionMessage.BindingsSpecified)

                this.bindingProperties.Patch(patchInteractionMessage.bindingProperties, BindingTargets.Message);

            if (patchInteractionMessage.DomainSpecified)

                this.domain.Patch(patchInteractionMessage.Domain);
        }
    }

    public static class ActionMessageExtensions
    {
        public static InteractionMessage[] Clone(this InteractionMessage[] messages, Func<String, String> tokenExpansionFunction)
        {
            List<InteractionMessage> clone = new List<InteractionMessage>();

            foreach (InteractionMessage message in messages)
            {
                clone.Add(
                    message.Clone(
                        tokenExpansionFunction));
            }

            return clone.ToArray();
        }

        public static InteractionMessage[] ShallowClone(this InteractionMessage[] messages)
        {
            List<InteractionMessage> shallowMessages = new List<InteractionMessage>();

            foreach (InteractionMessage message in messages)
            {
                shallowMessages.Add(
                    message.ShallowClone(true));
            }

            return shallowMessages.ToArray();
        }

        public static InteractionMessage ShallowClone(this InteractionMessage message, bool suppressPartitionDetails = false)
        {
            if (suppressPartitionDetails)
                return new InteractionMessage(
                    message.Name,
                    message.QualifiedName,
                    message.Annotations,
                    message.Bindings,
                    Domain.Empty);
            else
                return new InteractionMessage(
                    message.Name,
                    message.QualifiedName,
                    message.Annotations,
                    message.Bindings,
                    message.Domain.ShallowClone(true));
        }

        internal static bool TryGetMessage(this IList<InteractionMessage> messages, String messageName, out InteractionMessage interactionMessage)
        {
            int index = messages.FindIndex(m => m.Name == messageName);

            if (index >= 0)

                interactionMessage = messages[index];

            else

                interactionMessage = null;

            return interactionMessage != null;
        }

        internal static void Patch(this IList<InteractionMessage> messages, IEnumerable<InteractionMessage> patchMessages)
        {
            foreach (InteractionMessage patchMessage in patchMessages)
            {
                if (!patchMessage.NameSpecified)

                    throw new ArgumentException($"An unidentified {nameof(patchMessage)} was encountered!");

                if (messages.TryGetMessage(patchMessage.Name, out InteractionMessage interactionMessage))

                    interactionMessage.Patch(patchMessage);

                else

                    messages.Add(patchMessage.Clone());
            }
        }
    }
}