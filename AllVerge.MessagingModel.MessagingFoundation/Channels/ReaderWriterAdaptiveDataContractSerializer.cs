using System;
using System.Xml;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    class ReaderWriterAdaptiveDataContractSerializer : XmlObjectSerializer
    {
        DataContractSerializer dataContractSerializer;
        DataContractJsonSerializer dataContractJsonSerializer;

        public ReaderWriterAdaptiveDataContractSerializer(Type type, int maxItems)
        {
            dataContractSerializer = new DataContractSerializer(type);
            dataContractJsonSerializer = new DataContractJsonSerializer(type);
        }

        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            if (reader.GetType().FullName == "System.Runtime.Serialization.Json.XmlJsonReader")
            {
                return dataContractJsonSerializer.IsStartObject(reader);
            }
            else
            {
                return dataContractSerializer.IsStartObject(reader);
            }
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            if (reader.GetType().FullName == "System.Runtime.Serialization.Json.XmlJsonReader")
            {
                return dataContractJsonSerializer.ReadObject(reader);
            }
            else
            {
                return dataContractSerializer.ReadObject(reader);
            }
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            if (writer.GetType().FullName == "System.Runtime.Serialization.Json.XmlJsonWriter")
            {
                dataContractJsonSerializer.WriteStartObject(writer, graph);
            }
            else
            {
                dataContractSerializer.WriteStartObject(writer, graph);
            }
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            if (writer.GetType().FullName == "System.Runtime.Serialization.Json.XmlJsonWriter")
            {
                dataContractJsonSerializer.WriteObjectContent(writer, graph);
            }
            else
            {
                dataContractSerializer.WriteObjectContent(writer, graph);
            }
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            if (writer.GetType().FullName == "System.Runtime.Serialization.Json.XmlJsonWriter")
            {
                dataContractJsonSerializer.WriteEndObject(writer);
            }
            else
            {
                dataContractSerializer.WriteEndObject(writer);
            }
        }
    }
}
