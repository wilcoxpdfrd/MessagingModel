using System;
using System.Xml;
using System.Xml.Serialization;

using System.Runtime.Serialization;

using System.ServiceModel;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    using AllVerge.MessagingModel.MarkupPrimitives.Xml;

    public class DispatchObjectSerializer : XmlObjectSerializer
    {
        private XmlSerializer serializer;

        private Type rootType;

        private string rootName;

        private string rootNamespace;

        private bool isSerializerSetExplicit;

        internal DispatchObjectSerializer(Type type)
        {
            this.Initialize(type, null, null, null);
        }

        internal DispatchObjectSerializer(Type type, Type[] knownTypes)
        {
            this.Initialize(type, knownTypes, null, null);
        }

        internal DispatchObjectSerializer(Type type, XmlQualifiedName qualifiedName)
        {
            if (qualifiedName == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("qualifiedName");
            }

            this.Initialize(type, null, qualifiedName.Name, qualifiedName.Namespace);
        }

        internal DispatchObjectSerializer(Type type, Type[] knownTypes, XmlQualifiedName qualifiedName)
        {
            if (qualifiedName == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("qualifiedName");
            }

            this.Initialize(type, knownTypes, qualifiedName.Name, qualifiedName.Namespace);
        }

        private void Initialize(Type type, Type[] knownTypes, string rootName, string rootNamespace)
        {
            if (type == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("type");
            }
            this.rootType = type;
            this.rootName = rootName;
            this.rootNamespace = ((rootNamespace == null) ? string.Empty : rootNamespace);
            if (this.rootName == null)
            {
                if (knownTypes == null)

                    this.serializer = new XmlSerializer(type);

                else

                    this.serializer = new XmlSerializer(type, knownTypes);
            }
            else
            {
                if (knownTypes == null)

                    this.serializer = 
                        new XmlSerializer(
                        type, 
                        new XmlRootAttribute
                        {
                            ElementName = this.rootName,
                            Namespace = this.rootNamespace
                        });

                else

                    this.serializer = 
                        new XmlSerializer(
                            type, 
                            new XmlAttributeOverrides(), 
                            knownTypes, 
                            new XmlRootAttribute
                            {
                                ElementName = this.rootName,
                                Namespace = this.rootNamespace
                            },
                            null);
            }
            this.isSerializerSetExplicit = false;
            if (this.rootName == null)
            {
                XmlTypeMapping xmlTypeMapping = new XmlReflectionImporter().ImportTypeMapping(this.rootType);
                this.rootName = xmlTypeMapping.ElementName;
                this.rootNamespace = xmlTypeMapping.Namespace;
            }
        }

        public override void WriteObject(XmlDictionaryWriter writer, object graph)
        {
            if (this.isSerializerSetExplicit)
            {
                this.serializer.Serialize(writer, new object[]
                {
                    graph
                });
                return;
            }

            this.serializer.Serialize(writer, graph, XmlSerialization.EmptyNSMap);
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotImplementedException());
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotImplementedException());
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotImplementedException());
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            if (!this.isSerializerSetExplicit)
            {
                return this.serializer.Deserialize(reader);
            }
            object[] array = (object[])this.serializer.Deserialize(reader);
            if (array != null && array.Length != 0)
            {
                return array[0];
            }
            return null;
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
            return reader.IsStartElement();
        }

        public static DispatchObjectSerializer CreateSerializer(Type type)
        {
            return new DispatchObjectSerializer(type);
        }

        public static DispatchObjectSerializer CreateSerializer(Type type, Type[] knownTypes)
        {
            return new DispatchObjectSerializer(type, knownTypes);
        }

        public static DispatchObjectSerializer CreateSerializer(Type type, string name, string @namespace)
        {
            return new DispatchObjectSerializer(type, new XmlQualifiedName(name, @namespace));
        }

        public static DispatchObjectSerializer CreateSerializer(Type type, Type[] knownTypes, string name, string @namespace)
        {
            return new DispatchObjectSerializer(type, knownTypes, new XmlQualifiedName(name, @namespace));
        }
    }
}
