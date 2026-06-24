using System;
using System.Collections.Generic;
using System.Linq;
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

    using Newtonsoft.Json;

    public class Connection : Agent, ISpecifiesDomain, ISpecifiesBindings
    {
        private const String JSON_PROPERTY_BINDINGS = "bindings";
        private const String PROPERTY_BINDINGS = "Bindings";
        private const String PROPERTY_BINDINGS_ITEMS = BindingProperty.ELEMENT_NAME;
        private const String PROPERTY_DOMAIN_PARAMETERS = "Parameters";
        private const String JSON_PROPERTY_INTERACTIONS = "interactions";
        private const String PROPERTY_INTERACTIONS = "Interactions";
        private const String PROPERTY_INTERACTIONS_ITEMS = nameof(Interaction);

        private BindingProperties bindingProperties;
        private Domain parameters;
        private List<Interaction> interactions;

        private static readonly XmlAttributeOverrides domainParameterRootOverride = new XmlAttributeOverrides().AddXmlRootAttribute(typeof(Domain), PROPERTY_DOMAIN_PARAMETERS);
        private static readonly XmlSerializer domainParametersSerializer = new XmlSerializer(typeof(Domain), domainParameterRootOverride);

        public Connection()
            : this(Fixable.FixOnRead)
        {
        }

        [JsonConstructor]
        private Connection(Fixable fixable)
            : base(fixable)
        {
            SetLocalFields(null, null, null);
        }

        public Connection(String name) :
            base(name)
        {
            SetLocalFields(null, null, null);
        }

        public Connection(String name, BindingProperties bindingProperties, params Interaction[] interactions) :
            base(name)
        {
            SetLocalFields(bindingProperties, null, interactions);
        }

        public Connection(String name, Attributes attributes, Annotations annotation, BindingProperties bindingProperties, params Interaction[] interactions) :
            base(name, attributes, annotation)
        {
            SetLocalFields(bindingProperties, null, interactions);
        }

        // ToDo: pass uriParameters here instead of exposing SetUriParameters method?
        public Connection(String name, Attributes attributes, Annotations annotation, BindingProperties bindingProperties, Domain domain, params Interaction[] interactions) :
            base(name, attributes, annotation)
        {
            SetLocalFields(bindingProperties, domain, interactions);
        }

        private void SetLocalFields(BindingProperties bindingProperties, Domain domain, Interaction[] interactions)
        {
            this.bindingProperties = bindingProperties ?? BindingProperties.CreateImmutableOnRead();
            this.SetParameters(domain ?? Domain.Create(this.Fixed.ConstructorParameter));
            this.interactions = interactions == null ? new List<Interaction>() : new List<Interaction>(interactions);

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

        public Domain Parameters
        {
            get
            {
                return this.Domain;
            }
        }

        [JsonProperty(Domain.JSON_PROPERTY_NAME)]
        public Domain Domain
        {
            get
            {
                this.Fixed.OnRead();

                return this.parameters;
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(Domain));

                this.parameters = value;
            }
        }

        public Boolean DomainSpecified
        {
            get
            {
                return Domain.Specified(this.parameters);
            }
        }

        public bool ShouldSerializeDomain()
        {
            return this.DomainSpecified;
        }

        [JsonProperty(JSON_PROPERTY_INTERACTIONS)]
        [XmlArray(PROPERTY_INTERACTIONS)]
        [XmlArrayItem(PROPERTY_INTERACTIONS_ITEMS)]
        public Interaction[] Interactions
        {
            get
            {
                this.Fixed.OnRead();

                return this.interactions.ToArray();
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(Interactions));

                this.interactions.Clear();

                if (value != null)

                    this.interactions.AddRange(value);
            }
        }

        public Boolean InteractionsSpecified
        {
            get
            {
                return this.interactions.Count > 0;
            }
        }

        public bool ShouldSerializeInteractions()
        {
            return this.InteractionsSpecified;
        }

        protected override void EnsureNameSpecified()
        {
            //throw new NotImplementedException();
        }

        private void SetParameters(Domain parameters)
        {
            this.parameters = parameters;
        }

        public void SetUriParameters(Domain baseUriParameters)
        {
            this.SetParameters(baseUriParameters);
        }

        public void AddInteraction(Interaction interaction)
        {
            this.interactions.Add(interaction);
        }

        public Uri GetLocation()
        {
            String bindingNamespace;

            return GetLocation(out bindingNamespace);
        }

        public Uri GetLocation(out String bindingNamespace)
        {
            BindingProperty behaviorBinding;

            if (this.Bindings.TryGetProperty(out behaviorBinding, "address"))
            {
                bindingNamespace = behaviorBinding.QualifiedName.Namespace;

                if (behaviorBinding != null)
                {
                    BindingAttribute locationAttribute = behaviorBinding.Attributes.FirstOrDefault(a => a.Name == BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME);

                    return new Uri(locationAttribute.Value);
                }
            }

            bindingNamespace = null;

            return null;
        }

        internal bool TryGetInteraction(string interactionNameOrIndex, out Interaction interaction)
        {
            int interactionIndex;

            if (int.TryParse(interactionNameOrIndex, out interactionIndex))
            {
                if (interactionIndex >= 0 && interactionIndex < this.Interactions.Length)

                    interaction = this.Interactions[interactionIndex];

                else

                    interaction = null;
            }
            else

                interaction = this.interactions.FirstOrDefault(b => b.Name == interactionNameOrIndex);

            return interaction != null;
        }


        internal void Patch(Connection patchConnection)
        {
            if (!this.NameSpecified)

                throw new InvalidOperationException($"Cannot target unidentified resouce member ({nameof(patchConnection)}) for patching.");

            if (!patchConnection.NameSpecified)

                throw new ArgumentException($"An unidentified resouce member ({nameof(patchConnection)}) cannot be a patching source.", nameof(patchConnection.Name));

            if (this.Name != patchConnection.Name)

                throw new ArgumentException($"Patch resouce member ({nameof(patchConnection)}) does not map to target.", nameof(patchConnection.Name));

            if (patchConnection.AttributesSpecified)

                this.Attributes.Patch(patchConnection.Attributes);

            if (patchConnection.AnnotationsSpecified)

                this.Annotations.Patch(patchConnection.Annotations);

            if (patchConnection.BindingsSpecified)

                this.bindingProperties.Patch(patchConnection.bindingProperties, BindingTargets.Connection);

            if (patchConnection.DomainSpecified)

                this.Domain.Patch(patchConnection.Domain);

            if (patchConnection.InteractionsSpecified)
            {
                foreach (Interaction patchInteraction in patchConnection.interactions)
                {
                    if (!patchInteraction.NameSpecified)

                        throw new ArgumentException($"An unidentified {patchInteraction} was encountered!");

                    if (this.TryGetInteraction(patchInteraction.Name, out Interaction interaction))

                        interaction.Patch(patchInteraction);

                    else

                        this.AddInteraction(patchInteraction);
                }
            }
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            this.Fixed.OnDeserialized(streamingContext);
        }

        protected override void OnWriteAttributes(XmlWriter writer)
        {
            base.OnWriteAttributes(writer);
        }

        protected override void OnReadAttributes(XmlReader reader)
        {
            base.OnReadAttributes(reader);

            Domain.ReadDomainAttributes(this.parameters, reader);
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

            if (this.ShouldSerializeDomain())

                domainParametersSerializer.Serialize(writer, this.parameters);

            if (this.ShouldSerializeInteractions())
            {
                writer.WriteStartElement(PROPERTY_INTERACTIONS);

                foreach (Interaction interaction in this.Interactions)

                    writer.WriteRaw(interaction.Serialize(XmlSerialization.EmptyNSMap).OuterXml);

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

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_DOMAIN_PARAMETERS)
            {
                using (XmlReader r = reader.ReadSubtree())
                {
                    this.parameters = (Domain)domainParametersSerializer.Deserialize(r);
                }

                reader.ReadEmptyOrEndElement(PROPERTY_DOMAIN_PARAMETERS);
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_INTERACTIONS)
            {
                while (reader.Read() && reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_INTERACTIONS_ITEMS)
                {
                    using (var r = reader.ReadSubtree())
                    {
                        this.interactions.Add(r.Deserialize<Interaction>());
                    }
                }

                reader.ReadEmptyOrEndElement(PROPERTY_INTERACTIONS);
            }

            reader.ReadEmptyOrEndElement(elementName);
        }
    }

    public static class ConnectionExtensions
    {
        public static Connection[] ShallowClone(this Connection[] connections)
        {
            List<Connection> clone = new List<Connection>();

            foreach (Connection connection in connections)
            {
                clone.Add(connection.ShallowClone(true));
            }

            return clone.ToArray();
        }

        public static Connection ShallowClone(this Connection connection, bool suppressInteractionDetails = false)
        {
            if (suppressInteractionDetails)
                return new Connection(
                    connection.Name,
                    connection.Attributes.Clone(),
                    connection.Annotations.Clone(),
                    connection.Bindings.Clone(),
                    connection.Parameters.Clone(),
                    Array.Empty<Interaction>());
            else
                return new Connection(
                    connection.Name,
                    connection.Attributes.Clone(),
                    connection.Annotations.Clone(),
                    connection.Bindings.Clone(),
                    connection.Parameters.ShallowClone(),
                    connection.Interactions.ShallowClone());
        }
    }
}