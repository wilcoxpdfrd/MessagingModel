//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatcher
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using DiagnosticUtility = System.ServiceModel.DiagnosticUtility;
    using System.Runtime.Serialization.Json;
    using System.Collections;
    using System.Linq;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    class SingleBodyParameterDataContractMessageFormatter : SingleBodyParameterMessageFormatter
    {
        static readonly Type TypeOfNullable = typeof(Nullable<>);
        static readonly Type[] CollectionDataContractInterfaces = new Type[] { typeof(IEnumerable), typeof(IList), typeof(ICollection), typeof(IDictionary) };
        static readonly Type[] GenericCollectionDataContractInterfaces = new Type[] { typeof(IEnumerable<>), typeof(IList<>), typeof(ICollection<>), typeof(IDictionary<,>) };
        XmlObjectSerializer cachedOutputSerializer;
        Type cachedOutputSerializerType;
        // bool ignoreExtensionData;
        XmlObjectSerializer[] inputSerializers;
        IList<Type> knownTypes;
        int maxItemsInObjectGraph;
        Type parameterDataContractType;
        IDataContractSurrogate surrogate;
        Object thisLock;
        MessageEncodingFormat transferMessageFormat;
        bool isParameterCollectionInterfaceDataContract;
        bool isQueryable;

        public SingleBodyParameterDataContractMessageFormatter(OperationDescription operation, Type parameterType, bool isRequestFormatter, MessageEncodingFormat transferMessageFormat, DataContractSerializerOperationBehavior dcsob)
            : base(operation, isRequestFormatter, "DataContractSerializer")
        {
            if (operation == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("operation");
            }
            if (parameterType == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("parameterType");
            }
            if (dcsob == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("dcsob");
            }
            this.parameterDataContractType = DataContractSerializerOperationFormatter.GetSubstituteDataContractType(parameterType, out isQueryable);
            this.isParameterCollectionInterfaceDataContract = IsTypeCollectionInterface(this.parameterDataContractType);
            List<Type> tmp = new List<Type>();
            if (operation.KnownTypes != null)
            {
                foreach (Type knownType in operation.KnownTypes)
                {
                    tmp.Add(knownType);
                }
            }
            Type nullableType = UnwrapNullableType(this.parameterDataContractType);
            if (nullableType != this.parameterDataContractType)
            {
                tmp.Add(nullableType);
            }
            this.surrogate = null;//dcsob.DataContractSurrogate;
            // this.ignoreExtensionData = true;// dcsob.IgnoreExtensionDataObject;
            this.maxItemsInObjectGraph = dcsob.MaxItemsInObjectGraph;
            this.knownTypes = tmp.AsReadOnly();
            ValidateType(this.parameterDataContractType, surrogate, this.knownTypes);

            this.transferMessageFormat = transferMessageFormat;
            CreateInputSerializers(this.parameterDataContractType);

            thisLock = new Object();
        }

        internal static Type UnwrapNullableType(Type type)
        {
            while (type.IsGenericType && type.GetGenericTypeDefinition() == TypeOfNullable)
            {
                type = type.GetGenericArguments()[0];
            }
            return type;
        }

        // The logic of this method should be kept the same as 
        // System.ServiceModel.Dispatcher.DataContractSerializerOperationFormatter.PartInfo.ReadObject
        protected override object ReadObject(Message message)
        {
            object val = base.ReadObject(message);
            if (this.isQueryable && val != null)
            {
                return Queryable.AsQueryable((IEnumerable)val);
            }
            return val;
        }

        protected override void AttachMessageProperties(Message message, bool isRequest)
        {
            switch (this.transferMessageFormat)
            {
                case MessageEncodingFormat.Binary:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryProperty);
                    break;
                case MessageEncodingFormat.BinaryPlusDeflate:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryPlusDeflateProperty);
                    break;
                case MessageEncodingFormat.BinaryPlusGzip:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryPlusGzipProperty);
                    break;
                case MessageEncodingFormat.FormMultipartData:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.FormMultipartDataProperty);
                    break;
                case MessageEncodingFormat.FormUrlEncoded:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.FormUrlEncodedProperty);
                    break;
                case MessageEncodingFormat.Html:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.HtmlProperty);
                    break;
                case MessageEncodingFormat.Json:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.JsonProperty);
                    break;
                case MessageEncodingFormat.Raw:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.RawProperty);
                    break;
                case MessageEncodingFormat.Soap11:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11Property);
                    break;
                case MessageEncodingFormat.Soap11WSAddressing10:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11WSAddressing10Property);
                    break;
                case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11WSAddressingAugust2004Property);
                    break;
                case MessageEncodingFormat.Soap12:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12Property);
                    break;
                case MessageEncodingFormat.Soap12WSAddressing10:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12WSAddressing10Property);
                    break;
                case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12WSAddressingAugust2004Property);
                    break;
                case MessageEncodingFormat.Text:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.TextProperty);
                    break;
                case MessageEncodingFormat.Xml:
                    message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.XmlProperty);
                    break;
            }
        }

        protected override XmlObjectSerializer[] GetInputSerializers()
        {
            return this.inputSerializers;
        }

        protected override XmlObjectSerializer GetOutputSerializer(Type type)
        {
            lock (thisLock)
            {
                // if we already have a serializer for this type reuse it
                if (this.cachedOutputSerializerType != type)
                {
                    Type typeForSerializer;
                    if (this.isParameterCollectionInterfaceDataContract)
                    {
                        // if the parameterType is a collection interface, ensure the type implements it
                        if (!this.parameterDataContractType.IsAssignableFrom(type))
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                new SerializationException(
                                    PublicSR.Format(
                                        PublicSR.TypeIsNotParameterTypeAndIsNotPresentInKnownTypes, type, this.OperationName, this.ContractName, parameterDataContractType)));
                        }
                        typeForSerializer = this.parameterDataContractType;
                    }
                    else
                    {
                        typeForSerializer = GetTypeForSerializer(type, this.parameterDataContractType, this.knownTypes);
                    }
                    this.cachedOutputSerializer = CreateSerializer(typeForSerializer);
                    this.cachedOutputSerializerType = type;
                }
                return this.cachedOutputSerializer;
            }
        }

        static bool IsTypeCollectionInterface(Type parameterType)
        {
            if (parameterType.IsGenericType && parameterType.IsInterface)
            {
                Type genericTypeDef = parameterType.GetGenericTypeDefinition();
                foreach (Type type in GenericCollectionDataContractInterfaces)
                {
                    if (genericTypeDef == type)
                    {
                        return true;
                    }
                }
            }
            foreach (Type type in CollectionDataContractInterfaces)
            {
                if (parameterType == type)
                {
                    return true;
                }
            }
            return false;
        }

        protected override void ValidateMessageFormatProperty(Message message)
        {
            object prop;
            message.Properties.TryGetValue(MessageEncodingFormatProperty.Name, out prop);
            MessageEncodingFormatProperty formatProperty = (prop as MessageEncodingFormatProperty);
            if (formatProperty == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperWarning(
                    new InvalidOperationException(
                        PublicSR.Format(
                            PublicSR.MessageFormatPropertyNotFound, this.OperationName, this.ContractName, this.ContractNs)));
            }

            if (formatProperty.Format != this.transferMessageFormat)
            { 
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperWarning(
                        new InvalidOperationException(
                            AMMMFR.Format(
                                AMMMFR.InvalidHttpMessageFormat, this.OperationName, this.ContractName, this.ContractNs, formatProperty.Format, this.transferMessageFormat)));
            }
            else
            {
                base.ValidateMessageFormatProperty(message);
            }
        }

        static void ValidateType(Type parameterType, IDataContractSurrogate surrogate, IEnumerable<Type> knownTypes)
        {
            XsdDataContractExporter dataContractExporter = new XsdDataContractExporter();
            if (surrogate != null || knownTypes != null)
            {
                ExportOptions options = new ExportOptions();
                //options.DataContractSurrogate = surrogate;
                if (knownTypes != null)
                {
                    foreach (Type knownType in knownTypes)
                    {
                        options.KnownTypes.Add(knownType);
                    }
                }
                dataContractExporter.Options = options;
            }
            dataContractExporter.GetSchemaTypeName(parameterType); // throws if parameterType is not a valid data contract
        }

        void CreateInputSerializers(Type type)
        {
            List<XmlObjectSerializer> tmp = new List<XmlObjectSerializer>();
            tmp.Add(CreateSerializer(type));
            foreach (Type knownType in this.knownTypes)
            {
                tmp.Add(CreateSerializer(knownType));
            }
            this.inputSerializers = tmp.ToArray();
        }

        XmlObjectSerializer CreateSerializer(Type type)
        {
            switch (this.transferMessageFormat)
            {
                case MessageEncodingFormat.Binary:
                case MessageEncodingFormat.BinaryPlusGzip:
                case MessageEncodingFormat.BinaryPlusDeflate:
                case MessageEncodingFormat.Soap11:
                case MessageEncodingFormat.Soap11WSAddressing10:
                case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                case MessageEncodingFormat.Soap12:
                case MessageEncodingFormat.Soap12WSAddressing10:
                case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                case MessageEncodingFormat.Raw:
                case MessageEncodingFormat.Default:
                    return new DataContractSerializer(type, this.knownTypes); //, this.maxItemsInObjectGraph, this.ignoreExtensionData, false, this.surrogate);
                case MessageEncodingFormat.Json:
                    return new DataContractJsonSerializer(type, this.knownTypes);//, this.maxItemsInObjectGraph, this.ignoreExtensionData, this.surrogate, false);
                case MessageEncodingFormat.Xml:
                case MessageEncodingFormat.Text:
                case MessageEncodingFormat.Html:
                    return new XmlAttributeOverridesSerializer(type, null, null, null, this.knownTypes.ToArray());
                case MessageEncodingFormat.FormMultipartData:
                case MessageEncodingFormat.FormUrlEncoded:
                default:
                    throw new NotImplementedException($"{nameof(CreateSerializer)} for ${this.transferMessageFormat}");
            }
        }
    }
}

