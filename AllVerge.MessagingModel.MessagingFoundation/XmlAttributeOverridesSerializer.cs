using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    using AllVerge.MessagingModel.MarkupPrimitives.Xml;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Based on https://blogs.msdn.microsoft.com/andrewarnottms/2008/01/18/using-the-xmlserializer-as-an-xmlobjectserializer-with-wcf/
    /// </remarks>
    public class XmlAttributeOverridesSerializer : XmlObjectSerializer
    {
        private readonly Type type;
        private readonly XmlRootAttribute typeRootAttribute;
        private readonly XmlDictionaryString rootName;
        private readonly XmlDictionaryString rootNamespace;
        private readonly XmlAttributeOverrides xmlAttributeOverrides;
        private readonly Type[] knownTypes;

        public XmlAttributeOverridesSerializer(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace, XmlAttributeOverrides xmlAttributeOverrides, params Type[] knownTypes)
        {
            this.type = type ?? throw new ArgumentNullException("type");
            if (type.TryGetXmlRootAttribute(out XmlRootAttribute xmlRootAttribute))
                this.typeRootAttribute = xmlRootAttribute;
            else
                this.typeRootAttribute = null;
            this.rootName = rootName;
            this.rootNamespace = rootNamespace;
            this.xmlAttributeOverrides = xmlAttributeOverrides;
            this.knownTypes = knownTypes;
        }

        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            if (reader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("reader"));
            }

            reader.MoveToElement();

            if (this.rootName != null)
            {
                return reader.IsStartElement(this.rootName, this.rootNamespace);
            }

            if (this.xmlAttributeOverrides.TryGetXmlRootAttribute(this.type, out XmlRootAttribute xmlRootAttribute))

                return reader.IsStartElement(xmlRootAttribute.ElementName, xmlRootAttribute.Namespace);

            if (this.typeRootAttribute != null)
            {
                if (!String.IsNullOrEmpty(this.typeRootAttribute.ElementName))
                {
                    if (reader.IsStartElement(this.typeRootAttribute.ElementName, this.typeRootAttribute.Namespace))

                        return true;
                }
                else if (reader.IsStartElement(this.type.Name, this.typeRootAttribute.Namespace))

                    return true;
            }

            return reader.IsStartElement(this.type.Name, this.type.Namespace.Replace('.', '/'));
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            if (verifyObjectName && !IsStartObject(reader))

                throw new InvalidOperationException("Object name Verification failed.");

            using (var r = reader.ReadSubtree())
            {
                bool hasRootElement = this.rootName != null && reader.IsStartElement(this.rootName, this.rootNamespace);

                if (hasRootElement)
                {
                    reader.ReadStartElement();
                }
                Object obj = r.Deserialize(this.type, this.xmlAttributeOverrides, this.knownTypes);
                if (hasRootElement)
                {
                    reader.ReadEndElement();
                }
                return obj;
            }
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            if (this.rootName != null)

                writer.WriteStartElement(this.rootName, this.rootNamespace);

            else if (this.xmlAttributeOverrides.TryGetXmlRootAttribute(this.type, out XmlRootAttribute xmlRootAttribute))

                writer.WriteStartElement(xmlRootAttribute.ElementName, xmlRootAttribute.Namespace);

            else if (this.typeRootAttribute != null && !String.IsNullOrEmpty(this.typeRootAttribute.ElementName))

                writer.WriteStartElement(this.typeRootAttribute.ElementName, this.typeRootAttribute.Namespace);

            else

                writer.WriteStartElement(this.type.Name, this.type.Namespace.Replace('.', '/'));
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            if (writer == null)

                throw new ArgumentNullException("writer");

            if (writer.WriteState != WriteState.Element)

                throw new SerializationException(
                    string.Format(
                        "WriteState '{0}' not valid. Caller must write start element before serializing in contentOnly mode.",
                        writer.WriteState)
                    );

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (XmlWriter bufferWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings() { OmitXmlDeclaration = true }))
                {
                    graph.Serialize(bufferWriter, this.xmlAttributeOverrides, this.knownTypes);

                    bufferWriter.Flush();

                    memoryStream.Position = 0;

                    using (XmlReader reader = XmlReader.Create(memoryStream))
                    {
                        reader.MoveToContent();

                        if (reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                if (reader.Name != "xmlns") // don't write type's namespace, let it inherit ...
                                {
                                    if (!String.IsNullOrEmpty(reader.Prefix))
                                    {
                                        if (reader.Prefix == "xmlns")
                                        {
                                            switch (reader.LocalName)
                                            {
                                                case "xsd":
                                                case "xsi":
                                                    break;
                                                default:
                                                    writer.WriteAttributeString(reader.Prefix, reader.LocalName, reader.NamespaceURI, reader.Value);
                                                    break;
                                            }
                                        }
                                        else

                                            writer.WriteAttributeString(reader.Prefix, reader.LocalName, reader.NamespaceURI, reader.Value);
                                    }
                                    else

                                        writer.WriteAttributeString(reader.LocalName, reader.Value);
                                }
                            }
                            while (reader.MoveToNextAttribute());

                            reader.MoveToElement();
                        }

                        if (reader.Read()) // move off start node (we want to skip it)
                        {
                            while (reader.NodeType != XmlNodeType.EndElement) // also skip end node.

                                writer.WriteNode(reader, false); // this will take us to the start of the next child node, or the end node.

                            reader.ReadEndElement(); // not necessary, but clean
                        }
                    }
                }
            }
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            writer.WriteEndElement();
        }
    }
}
