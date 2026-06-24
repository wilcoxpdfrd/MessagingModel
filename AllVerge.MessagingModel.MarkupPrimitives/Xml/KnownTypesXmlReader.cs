using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MarkupPrimitives.Xml
{
    public class KnownTypesXmlReader : XmlReader
    {
        private readonly XmlReader innerReader;
        private readonly XmlAttributeOverrides overrides;
        private readonly Type[] knownTypes;

        public KnownTypesXmlReader(XmlReader innerReader, params Type[] knownTypes) : base()
        {
            if (innerReader == null)

                throw new ArgumentNullException(nameof(innerReader));

            this.innerReader = innerReader;
            this.overrides = null;
            this.knownTypes = knownTypes;
        }

        public KnownTypesXmlReader(XmlReader innerReader, XmlAttributeOverrides overrides, params Type[] knownTypes) : base()
        {
            if (innerReader == null)

                throw new ArgumentNullException(nameof(innerReader));

            this.overrides = overrides;
            this.innerReader = innerReader;
            this.knownTypes = knownTypes;
        }

        public override int AttributeCount => this.innerReader.AttributeCount;

        public override string BaseURI => this.innerReader.BaseURI;

        public override int Depth => this.innerReader.Depth;

        public override bool EOF => this.innerReader.EOF;

        public override bool IsEmptyElement => this.innerReader.IsEmptyElement;

        public override string LocalName => this.innerReader.LocalName;

        public override string NamespaceURI => this.innerReader.NamespaceURI;

        public override XmlNameTable NameTable => this.innerReader.NameTable;

        public override XmlNodeType NodeType => this.innerReader.NodeType;

        public override string Prefix => this.innerReader.Prefix;

        public override ReadState ReadState => this.innerReader.ReadState;

        public override string Value => this.innerReader.Value;

        public bool TryGetKnownType(String readerLocalName, string readerNamespaceURI, out Type knownType)
        {
            // ToDo: add check for root attribute on known types, also lookup for same using overrides ...

            if (readerLocalName.StartsWith("ArrayOf"))
            {
                String typeName = readerLocalName.Substring(7);

                knownType = knownTypes.FirstOrDefault(t => t.Name == typeName);

                if (knownType != null)
                {
                    knownType = knownType.MakeArrayType();
                }
            }
            else

                knownType = knownTypes.FirstOrDefault(t => t.Name == readerLocalName && t.Namespace == readerNamespaceURI);

            return knownType != null;
        }

        public override string GetAttribute(int i)
        {
            return this.innerReader.GetAttribute(i);
        }

        public override string GetAttribute(string name)
        {
            return this.innerReader.GetAttribute(name);
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            return this.innerReader.GetAttribute(name, namespaceURI);
        }

        public override string LookupNamespace(string prefix)
        {
            return this.innerReader.LookupNamespace(prefix);
        }

        public override bool MoveToAttribute(string name)
        {
            return this.innerReader.MoveToAttribute(name);
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            return this.innerReader.MoveToAttribute(name, ns);
        }

        public override bool MoveToElement()
        {
            return this.innerReader.MoveToElement();
        }

        public override bool MoveToFirstAttribute()
        {
            return this.innerReader.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute()
        {
            return this.innerReader.MoveToNextAttribute();
        }

        public override bool Read()
        {
            return this.innerReader.Read();
        }

        public override bool ReadAttributeValue()
        {
            return this.innerReader.ReadAttributeValue();
        }

        public override void ResolveEntity()
        {
            this.innerReader.ResolveEntity();
        }
    }
}
