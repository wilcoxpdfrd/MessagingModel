using AllVerge.MessagingModel.MessagingFoundation.Channels;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Description;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation.Description
{
    public class ResourceTransferFormatSerializerOperationBehavior : DataContractSerializerOperationBehavior
    {
        public ResourceTransferFormatSerializerOperationBehavior(OperationDescription operation) : 
            this(operation, new ResourceTransferFormatSerializerAttribute())
        {
        }

        public ResourceTransferFormatSerializerOperationBehavior(OperationDescription operation, ResourceTransferFormatSerializerAttribute transferMessageSerializerFormatAttribute) : 
            base(operation)
        {
            this.TransferMessageSerializerFormatAttribute = transferMessageSerializerFormatAttribute;
        }

        public ResourceTransferFormatSerializerAttribute TransferMessageSerializerFormatAttribute { get; }

        public override XmlObjectSerializer CreateSerializer(Type type, string name, string ns, IList<Type> knownTypes)
        {
            switch (this.TransferMessageSerializerFormatAttribute.Format)
            {
                case MessageEncodingFormat.Binary:
                case MessageEncodingFormat.Default:
                case MessageEncodingFormat.BinaryPlusGzip:
                case MessageEncodingFormat.BinaryPlusDeflate:
                case MessageEncodingFormat.Raw:
                case MessageEncodingFormat.Soap11WSAddressing10:
                case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                case MessageEncodingFormat.Soap11:
                case MessageEncodingFormat.Soap12WSAddressing10:
                case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                case MessageEncodingFormat.Soap12:
                case MessageEncodingFormat.Xml:
                case MessageEncodingFormat.Html:
                case MessageEncodingFormat.Text:
                    return new DataContractSerializer(type, name, ns, knownTypes);//, this.MaxItemsInObjectGraph, this.IgnoreExtensionDataObject, this.DataContractSurrogate, alwaysEmitTypeInformation);
                case MessageEncodingFormat.Json:
                    return new DataContractJsonSerializer(type, name, knownTypes);//, this.MaxItemsInObjectGraph, this.IgnoreExtensionDataObject, this.DataContractSurrogate, alwaysEmitTypeInformation);
                default:
                    throw new NotImplementedException(nameof(CreateSerializer));
            }
        }

        public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
        {
            switch (this.TransferMessageSerializerFormatAttribute.Format)
            {
                case MessageEncodingFormat.Binary:
                case MessageEncodingFormat.Default:
                case MessageEncodingFormat.BinaryPlusGzip:
                case MessageEncodingFormat.BinaryPlusDeflate:
                case MessageEncodingFormat.Raw:
                case MessageEncodingFormat.Soap11WSAddressing10:
                case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                case MessageEncodingFormat.Soap11:
                case MessageEncodingFormat.Soap12WSAddressing10:
                case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                case MessageEncodingFormat.Soap12:
                case MessageEncodingFormat.Xml:
                case MessageEncodingFormat.Html:
                case MessageEncodingFormat.Text:
                    return new DataContractSerializer(type, name, ns, knownTypes);//, this.MaxItemsInObjectGraph, this.IgnoreExtensionDataObject, this.DataContractSurrogate, alwaysEmitTypeInformation);
                case MessageEncodingFormat.Json:
                    return new DataContractJsonSerializer(type, name, knownTypes);//, this.MaxItemsInObjectGraph, this.IgnoreExtensionDataObject, this.DataContractSurrogate, alwaysEmitTypeInformation);
                default:
                    throw new NotImplementedException(nameof(CreateSerializer));
            }
        }
    }
}
