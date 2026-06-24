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

    using AllVerge.MessagingModel.MarkupPrimitives.Xml;

    using AllVerge.SystemPrimitives;

    using Newtonsoft.Json;

    /// <summary>
    /// A Connector actor.
    /// Unless otherwise noted, the connector is that on the origon server.
    /// </summary>
    public class Connector : QualifiedAgent
    {
        public const String JSON_PROPERTY_CONNECTIONS = "connections";
        public const String PROPERTY_CONNECTIONS = "Connections";
        public const String PROPERTY_CONNECTIONS_ITEMS = nameof(Connection);

        private List<Connection> connections;

        public Connector() : 
            this(Fixable.FixOnRead)
        {
        }

        [JsonConstructor]
        private Connector(Fixable fixable)
            : base(fixable)
        {
            SetLocalFields(null);
        }

        public Connector(QualifiedName qualifiedName, Attributes attributes, params Connection[] connections) :
            base(qualifiedName, attributes)
        {
            SetLocalFields(connections);
        }

        public Connector(QualifiedName qualifiedName, Annotations annotations, params Connection[] connections) :
            base(qualifiedName, annotations)
        {
            SetLocalFields(connections);
        }

        public Connector(QualifiedName qualifiedName, Attributes attributes, Annotations annotations, params Connection[] connections) :
            base(qualifiedName, attributes, annotations)
        {
            SetLocalFields(connections);
        }

        private void SetLocalFields(IEnumerable<Connection> connections)
        {
            this.connections = connections == null ? new List<Connection>() : new List<Connection>(connections);
        }

        [JsonProperty(JSON_PROPERTY_CONNECTIONS)]
        [XmlArray(PROPERTY_CONNECTIONS)]
        [XmlArrayItem(PROPERTY_CONNECTIONS_ITEMS)]
        public Connection[] Connections
        {
            get
            {
                this.Fixed.OnRead();

                return this.connections.ToArray();
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(Connections));

                this.connections.Clear();

                this.connections.AddRange(value);
            }
        }

        public void AddConnection(Connection connection)
        {
            this.connections.Add(connection);
        }

        public bool TryGetConnection(string connectionNameOrIndex, out Connection connection)
        {
            int connectionIndex;

            if (int.TryParse(connectionNameOrIndex, out connectionIndex))
            {
                if (connectionIndex >= 0 && connectionIndex < this.Connections.Length)

                    connection = this.Connections[connectionIndex];

                else

                    connection = null;
            }
            else

                connection = this.connections.FirstOrDefault(b => b.Name == connectionNameOrIndex);

            return connection != null;
        }

        internal void Patch(Connector patchConnector)
        {
            if (!this.IsQualified)

                throw new InvalidOperationException("Cannot target unqualified resource member for patching.");

            if (!patchConnector.IsQualified)

                throw new ArgumentException("An unqualified resource member cannot be a patching source.", nameof(patchConnector.QualifiedName));

            if (this.QualifiedName != patchConnector.QualifiedName)

                throw new ArgumentException("Patch resource member does not map to target.", nameof(patchConnector.QualifiedName));

            foreach (Connection patchConnection in patchConnector.connections)
            {
                if (!patchConnection.NameSpecified)

                    throw new ArgumentException("An unidentified resource member cannot be a patching source.", nameof(patchConnector.QualifiedName));

                if (this.TryGetConnection(patchConnection.Name, out Connection connection))

                    connection.Patch(patchConnection);

                else

                    this.AddConnection(patchConnection);
            }
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            this.Fixed.OnDeserialized(streamingContext);
        }

        protected override void OnWriteProperties(XmlWriter writer)
        {
            base.OnWriteProperties(writer);

            writer.WriteStartElement(PROPERTY_CONNECTIONS);

            foreach (Connection connection in this.Connections)

                writer.WriteRaw(connection.Serialize(XmlSerialization.EmptyNSMap).OuterXml);

            writer.WriteEndElement();
        }

        protected override void OnReadProperties(XmlReader reader, string elementName)
        {
            base.OnReadProperties(reader, elementName);

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_CONNECTIONS)
            {
                while (reader.Read() && reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_CONNECTIONS_ITEMS)
                {
                    using (XmlReader r = reader.ReadSubtree())
                    {
                        this.connections.Add(r.Deserialize<Connection>());
                    }
                }

                reader.ReadEmptyOrEndElement(PROPERTY_CONNECTIONS);
            }
        }
    }

    public static class ConnectorExtensions
    {
        public static Connector[] ShallowClone(this Connector[] connectors)
        {
            List<Connector> clone = new List<Connector>();

            foreach (Connector connector in connectors)
            {
                clone.Add(connector.ShallowClone(true));
            }

            return clone.ToArray();
        }

        public static Connector ShallowClone(this Connector connector, bool suppressConnectionDetails = false)
        {
            if (suppressConnectionDetails)
                return new Connector(
                    connector.QualifiedName,
                    connector.Attributes.Clone(),
                    connector.Annotations.Clone(),
                    Array.Empty<Connection>());
            else
                return new Connector(
                    connector.QualifiedName,
                    connector.Attributes.Clone(),
                    connector.Annotations.Clone(),
                    connector.Connections.ShallowClone());

        }
    }
}