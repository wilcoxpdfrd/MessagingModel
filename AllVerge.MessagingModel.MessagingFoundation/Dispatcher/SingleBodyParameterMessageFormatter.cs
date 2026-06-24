//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatcher
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.Collections.Generic;
    using System.Xml;
    using System.Runtime.Serialization;
    using DiagnosticUtility = System.ServiceModel.DiagnosticUtility;
    using System.ServiceModel.Web;
    using System.ServiceModel.Dispatcher;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Description;
    using AllVerge.SystemPrimitives.Net.Mime;

    abstract class SingleBodyParameterMessageFormatter : IClientMessageFormatter//, IDispatchMessageFormatter
    {
        string contractName;
        string contractNs;
        bool isRequestFormatter;
        string operationName;
        string serializerType;

        protected SingleBodyParameterMessageFormatter(OperationDescription operation, bool isRequestFormatter, string serializerType)
        {
            if (operation == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("operation");
            }
            this.contractName = operation.DeclaringContract.Name;
            this.contractNs = operation.DeclaringContract.Namespace;
            this.operationName = operation.Name;
            this.isRequestFormatter = isRequestFormatter;
            this.serializerType = serializerType;
        }

        protected string ContractName
        {
            get { return this.contractName; }
        }

        protected string ContractNs
        {
            get { return this.contractNs; }
        }

        protected string OperationName
        {
            get { return this.operationName; }
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            if (isRequestFormatter)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        PublicSR.FormatterCannotBeUsedForReplyMessages));
            }
            return ReadObject(message);
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            if (!isRequestFormatter)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        PublicSR.FormatterCannotBeUsedForRequestMessages));
            }

            parameters[0] = ReadObject(message);
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            if (isRequestFormatter)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        PublicSR.FormatterCannotBeUsedForReplyMessages));
            }
            Message message = Message.CreateMessage(messageVersion, (string)null, CreateBodyWriter(result));
            if (result == null)
            {
                SuppressReplyEntityBody(message);
            }
            AttachMessageProperties(message, false);
            return message;
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            if (!isRequestFormatter)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        PublicSR.FormatterCannotBeUsedForRequestMessages));
            }
            Message message = Message.CreateMessage(messageVersion, (string)null, CreateBodyWriter(parameters[0]));
            if (parameters[0] == null)
            {
                SuppressRequestEntityBody(message);
            }
            AttachMessageProperties(message, true);
            return message;
        }

        internal static IClientMessageFormatter CreateClientFormatter(OperationDescription operation, Type type, bool isRequestFormatter, MessageEncodingFormat requestTransferFormat, UnwrappedTypesXmlSerializerManager xmlSerializerManager)
        {
            if (type == null)
            {
                return new NullMessageFormatter(false, null);
            }
            switch (requestTransferFormat)
            {
                case MessageEncodingFormat.Default:
                case MessageEncodingFormat.Binary:
                case MessageEncodingFormat.BinaryPlusGzip:
                case MessageEncodingFormat.BinaryPlusDeflate:
                case MessageEncodingFormat.Soap11WSAddressing10:
                case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                case MessageEncodingFormat.Soap11:
                case MessageEncodingFormat.Soap12WSAddressing10:
                case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                case MessageEncodingFormat.Soap12:
                case MessageEncodingFormat.Xml:
                case MessageEncodingFormat.Html:
                case MessageEncodingFormat.Text:
                    return CreateXmlFormatter(operation, type, isRequestFormatter, xmlSerializerManager);
                case MessageEncodingFormat.Json:
                    return CreateJsonFormatter(operation, type, isRequestFormatter);
                case MessageEncodingFormat.Raw:
                default:
                    throw new NotImplementedException(nameof(CreateClientFormatter));
            }
        }

        //internal static IDispatchMessageFormatter CreateDispatchFormatter(OperationDescription operation, Type type, bool isRequestFormatter, bool useJson, UnwrappedTypesXmlSerializerManager xmlSerializerManager, string callbackParameterName)
        //{
        //    if (type == null)
        //    {
        //        return new NullMessageFormatter(useJson, callbackParameterName);
        //    }
        //    else if (useJson)
        //    {
        //        return CreateJsonFormatter(operation, type, isRequestFormatter);
        //    }
        //    else
        //    {
        //        return CreateXmlFormatter(operation, type, isRequestFormatter, xmlSerializerManager);
        //    }
        //}

        internal static void SuppressReplyEntityBody(Message message)
        {
            throw new NotImplementedException("WebOperationContext");
            //WebOperationContext currentContext = WebOperationContext.Current;
            //if (currentContext != null)
            //{
            //    OutgoingWebResponseContext responseContext = currentContext.OutgoingResponse;
            //    if (responseContext != null)
            //    {
            //        responseContext.SuppressEntityBody = true;
            //    }
            //}
            //else
            //{
            //    object untypedProp;
            //    message.Properties.TryGetValue(HttpResponseMessageProperty.Name, out untypedProp);
            //    HttpResponseMessageProperty prop = untypedProp as HttpResponseMessageProperty;
            //    if (prop == null)
            //    {
            //        prop = new HttpResponseMessageProperty();
            //        message.Properties[HttpResponseMessageProperty.Name] = prop;
            //    }
            //    prop.SuppressEntityBody = true;
            //}
        }

        internal static void SuppressRequestEntityBody(Message message)
        {
            message.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object untypedProp);
            HttpRequestMessageProperty prop = untypedProp as HttpRequestMessageProperty;
            if (prop == null)
            {
                prop = new HttpRequestMessageProperty();
                message.Properties[HttpRequestMessageProperty.Name] = prop;
            }
            prop.SuppressEntityBody = true;
        }

        protected virtual void AttachMessageProperties(Message message, bool isRequest)
        {
        }

        protected abstract XmlObjectSerializer[] GetInputSerializers();

        protected abstract XmlObjectSerializer GetOutputSerializer(Type type);

        protected virtual void ValidateMessageFormatProperty(Message message)
        {
        }

        protected Type GetTypeForSerializer(Type type, Type parameterType, IList<Type> knownTypes)
        {
            if (type == parameterType)
            {
                return type;
            }
            else if (knownTypes != null)
            {
                for (int i = 0; i < knownTypes.Count; ++i)
                {
                    if (type == knownTypes[i])
                    {
                        return type;
                    }
                }
            }
            return parameterType;
        }

        public static SingleBodyParameterMessageFormatter CreateXmlFormatter(OperationDescription operation, Type type, bool isRequestFormatter, UnwrappedTypesXmlSerializerManager xmlSerializerManager)
        {
            ResourceTransferFormatSerializerOperationBehavior tsob = operation.Behaviors.Find<ResourceTransferFormatSerializerOperationBehavior>();
            if (tsob != null)
            {
                return new SingleBodyParameterDataContractMessageFormatter(operation, type, isRequestFormatter, tsob.TransferMessageSerializerFormatAttribute.Format, tsob);
            }
            DataContractSerializerOperationBehavior dcsob = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();
            if (dcsob != null)
            {
                return new SingleBodyParameterDataContractMessageFormatter(operation, type, isRequestFormatter, MessageEncodingFormat.Default, dcsob);
            }
            XmlSerializerOperationBehavior xsob = operation.Behaviors.Find<XmlSerializerOperationBehavior>();
            if (xsob != null)
            {
                return new SingleBodyParameterXmlSerializerMessageFormatter(operation, type, isRequestFormatter, xsob, xmlSerializerManager);
            }

            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                new NotSupportedException(
                    PublicSR.Format(
                        PublicSR.OnlyDataContractAndXmlSerializerTypesInUnWrappedMode, operation.Name)));
        }

        public static SingleBodyParameterMessageFormatter CreateJsonFormatter(OperationDescription operation, Type type, bool isRequestFormatter)
        {
            DataContractSerializerOperationBehavior dcsob = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();
            if (dcsob == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        PublicSR.Format(
                            PublicSR.JsonFormatRequiresDataContract, operation.Name, operation.DeclaringContract.Name, operation.DeclaringContract.Namespace)));
            }
            return new SingleBodyParameterDataContractMessageFormatter(operation, type, isRequestFormatter, MessageEncodingFormat.Json, dcsob);
        }

        BodyWriter CreateBodyWriter(object body)
        {
            XmlObjectSerializer serializer;
            if (body != null)
            {
                serializer = GetOutputSerializer(body.GetType());
                if (serializer == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new NotSupportedException(
                            PublicSR.Format(
                                PublicSR.CannotSerializeType, body.GetType(), this.operationName, this.contractName, this.contractNs, this.serializerType)));
                }
            }
            else
            {
                serializer = null;
            }
            return new SingleParameterBodyWriter(body, serializer);
        }

        protected virtual object ReadObject(Message message)
        {
            if (message.IsEmpty)
            {
                return null;
            }
            XmlObjectSerializer[] inputSerializers = GetInputSerializers();
            XmlDictionaryReader reader = message.GetReaderAtBodyContents();
            if (inputSerializers != null)
            {
                for (int i = 0; i < inputSerializers.Length; ++i)
                {
                    if (inputSerializers[i].IsStartObject(reader))
                    {
                        return inputSerializers[i].ReadObject(reader, false);
                    }
                }
            }
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new SerializationException(
                PublicSR.Format(
                    PublicSR.CannotDeserializeBody, reader.LocalName, reader.NamespaceURI, operationName, contractName, contractNs, this.serializerType)));
        }

        public static IClientMessageFormatter CreateTransferFormatClientFormatter(OperationDescription operation, Type type, bool isRequestFormatter, MessageEncodingFormat transferFormat, UnwrappedTypesXmlSerializerManager xmlSerializerManager)
        {
            IClientMessageFormatter requestFormatter = CreateClientFormatter(operation, type, isRequestFormatter, transferFormat, xmlSerializerManager);

            return new TransferFormatClientMessageFormatter(requestFormatter, transferFormat);
        }

        class NullMessageFormatter : IClientMessageFormatter//, IDispatchMessageFormatter
        {
            bool useJson;
            string callbackParameterName;

            public NullMessageFormatter(bool useJson, string callbackParameterName)
            {
                this.useJson = useJson;
                this.callbackParameterName = callbackParameterName;
            }

            public object DeserializeReply(Message message, object[] parameters)
            {
                return null;
            }

            //public void DeserializeRequest(Message message, object[] parameters)
            //{
            //}

            //public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
            //{
            //    Message reply = Message.CreateMessage(messageVersion, (string)null);
            //    SuppressReplyEntityBody(reply);
            //    if (useJson && WebHttpBehavior.TrySetupJavascriptCallback(callbackParameterName) != null)
            //    {
            //        reply.Properties.Add(WebBodyFormatMessageProperty.Name, WebBodyFormatMessageProperty.JsonProperty);
            //    }
            //    return reply;
            //}

            public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
            {
                Message request = Message.CreateMessage(messageVersion, (string)null);
                SuppressRequestEntityBody(request);
                return request;
            }
        }

        class SingleParameterBodyWriter : BodyWriter
        {
            object body;
            XmlObjectSerializer serializer;

            public SingleParameterBodyWriter(object body, XmlObjectSerializer serializer)
                : base(false)
            {
                if (body != null && serializer == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("serializer");
                }
                this.body = body;
                this.serializer = serializer;
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                if (body != null)
                {
                    this.serializer.WriteObject(writer, body);
                }
            }
        }
    }
}

