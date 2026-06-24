using AllVerge.MessagingModel.MessagingFoundation.Channels;
using AllVerge.MessagingModel.MessagingFoundation.Description;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
using System;
using System.Net.Security;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public class DispatchOperationDescription
    {
        private Type resourceType;
        private XmlQualifiedName resourceContractName;
        private int operationDescriptionHashCode;
        private SerializerOperationBehavior serializerOperationBehavior;
        internal ResourceActionAttribute resourceMethodAttribute;
        private MethodInfo resourceMethod;
        private object singletonDispatcher;

        public class SerializerOperationBehavior
        {
            public delegate Object GetFormatterFunc(OperationDescription operationDescription, out bool formatRequest, out bool formatReply, bool isProxy);
            public delegate Object GetFaultFormatterFunc();
            private GetFormatterFunc getFormatter;
            private GetFaultFormatterFunc getFaultFormatter;

            public SerializerOperationBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
            {
                DataContractSerializerOperationBehavior dataContractSerializerOperationBehavior = operationDescription.Behaviors.Find<DataContractSerializerOperationBehavior>();
                XmlSerializerOperationBehavior xmlSerializerOperationBehavior = operationDescription.Behaviors.Find<XmlSerializerOperationBehavior>();

                if (dataContractSerializerOperationBehavior != null)
                {
                    (dataContractSerializerOperationBehavior as IOperationBehavior).ApplyDispatchBehavior(operationDescription, dispatchOperation);

                    this.getFormatter = dataContractSerializerOperationBehavior.GetFormatter;
                    this.getFaultFormatter = () => dispatchOperation.FaultFormatter;
                }

                if (xmlSerializerOperationBehavior != null)
                {
                    (xmlSerializerOperationBehavior as IOperationBehavior).ApplyDispatchBehavior(operationDescription, dispatchOperation);

                    this.getFormatter = (OperationDescription OperationDescription, out bool formatRequest, out bool formatReply, bool isProxy) => {

                        formatRequest = dispatchOperation.DeserializeRequest;
                        formatReply = dispatchOperation.SerializeReply;

                        return dispatchOperation.Formatter;
                    };
                    this.getFaultFormatter = () => dispatchOperation.FaultFormatter;
                }
            }

            public object GetFormatter(OperationDescription operationDescription, out bool formatRequest, out bool formatReply, bool isProxy)
            {
                return this.getFormatter(operationDescription, out formatRequest, out formatReply, isProxy);
            }

            public object GetFaultFormatter()
            {
                return this.getFaultFormatter();
            }
        }

        public DispatchOperationDescription(Type resourceType, XmlQualifiedName resourceContractName, OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            this.resourceType = resourceType;
            this.resourceContractName = resourceContractName;
            this.operationDescriptionHashCode = operationDescription.GetHashCode();
            this.serializerOperationBehavior = new SerializerOperationBehavior(operationDescription, dispatchOperation);
            this.resourceMethodAttribute = operationDescription.Behaviors.Find<ResourceActionAttribute>();
            this.resourceMethod = operationDescription.OperationMethod;
        }

        public Type ResourceType => this.resourceType;
        public XmlQualifiedName ResourceContractName => this.resourceContractName;
        public String Name => this.resourceMethodAttribute?.Name ?? this.resourceMethod.Name;
        public String Method => this.resourceMethodAttribute?.ResourceAction;
        public MethodInfo MethodInfo => this.resourceMethod;

        internal SerializerOperationBehavior SerializerOperationDescription { get => serializerOperationBehavior; }
        public DispatchOperationDescription DuplexOutputOperation { get; internal set; }

        internal object GetSingletonDispatcher(Action<object> configureSingletonDispatcherAction)
        {
            if (this.singletonDispatcher == null)
            {
                this.singletonDispatcher = Activator.CreateInstance(this.resourceType);

                configureSingletonDispatcherAction(this.singletonDispatcher);
            }

            return this.singletonDispatcher;
        }

        public override int GetHashCode()
        {
            return this.operationDescriptionHashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is OperationDescription)
                return this.GetHashCode() == (obj as OperationDescription).GetHashCode();
            return base.Equals(obj);
        }

        public static bool operator !=(DispatchOperationDescription left, OperationDescription right)
        {
            return !(left == right);
        }

        public static bool operator == (DispatchOperationDescription left, OperationDescription right)
        {
            if (object.ReferenceEquals(null, left))
                return object.ReferenceEquals(null, right);
            return left.Equals(right);
        }
    }

    public static class DispathOperationInfoExtensions
    {
        public static bool CanResourceMethodHandleContentFormat(this DispatchOperationDescription dispatchOperation, MessageEncodingFormat transferFormat)
        {
            if (dispatchOperation.resourceMethodAttribute is IdempotentResourceActionTemplateAttribute ||
                dispatchOperation.resourceMethodAttribute is PotentResourceActionTemplateAttribute)
            {
                switch (transferFormat)
                {
                    case MessageEncodingFormat.Soap11WSAddressing10:
                    case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                    case MessageEncodingFormat.Soap11:
                    case MessageEncodingFormat.Soap12WSAddressing10:
                    case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                    case MessageEncodingFormat.Soap12:
                        return false;
                    default:
                        return true;
                }
            }
            else
            {
                switch (transferFormat)
                {
                    case MessageEncodingFormat.Soap11WSAddressing10:
                    case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                    case MessageEncodingFormat.Soap11:
                    case MessageEncodingFormat.Soap12WSAddressing10:
                    case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                    case MessageEncodingFormat.Soap12:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public static bool GetIsWrapResponse(this DispatchOperationDescription dispatchOperation, MessageVersion messageVersion, MessageHeaders incomingHeaders, out XmlQualifiedName responseActionQualifiedName, out string responseAction)
        {
            bool isDuplexResponse = dispatchOperation.GetIsDuplexResponse(out _);

            responseAction = incomingHeaders.GetResponseAction(isDuplexResponse);

            bool isWrapResponse = dispatchOperation.GetIsWrapResponse(out bool? isEnvelopeVersionNone, out String replyActionName);

            if (isEnvelopeVersionNone == true && messageVersion != MessageVersion.None)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(
                    AMMMFR.Format(AMMMFR.SFxMessageVersionNotSupportedOperation, messageVersion.Envelope)));

            if (isWrapResponse)
            {
                if (replyActionName != null)

                    responseActionQualifiedName = new XmlQualifiedName(replyActionName, dispatchOperation.ResourceContractName.Namespace);
                
                else

                    responseActionQualifiedName = new XmlQualifiedName(dispatchOperation.Name + ResourceTypeLoader.ResponseSuffix, dispatchOperation.ResourceContractName.Namespace);
            }
            else

                responseActionQualifiedName = null;

            return isWrapResponse;
        }

        public static bool GetIsDuplexResponse(this DispatchOperationDescription dispatchOperation, out Type callbackResourceContractType)
        {
            return dispatchOperation.resourceMethodAttribute.GetIsDuplexResponse(out callbackResourceContractType);
        }

        private static bool GetIsWrapResponse(this DispatchOperationDescription dispatchOperation, out bool? isEnvelopeVersionNone, out String replyActionName)
        {
            return dispatchOperation.resourceMethodAttribute.GetIsWrapResponse(out isEnvelopeVersionNone, out replyActionName, out ProtectionLevel protectionLevel);
        }
   }
}
