using AllVerge.MessagingModel.MessagingFoundation.Channels;
using AllVerge.MessagingModel.MessagingFoundation.Description;
using AllVerge.MessagingModel.MessagingFoundation.Dispatcher;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
using AllVerge.SystemPrimitives.Net.Mime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Client.Resource
{
    internal class ResourceEndpointAttributeChannelActionBehavior : IEndpointBehavior
    {
        internal class MessagePassthroughFormatter : IClientMessageFormatter//, IDispatchMessageFormatter
        {
            public object DeserializeReply(Message message, object[] parameters)
            {
                return message;
            }

            //public void DeserializeRequest(Message message, object[] parameters)
            //{
            //    parameters[0] = message;
            //}

            //public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
            //{
            //    return result as Message;
            //}

            public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
            {
                return parameters[0] as Message;
            }
        }

        private MessageEncodingFormat defaultOutgoingRequestFormat;
        private MessageEncodingFormat defaultOutgoingReplyFormat;
        private UnwrappedTypesXmlSerializerManager xmlSerializerManager;

        public ResourceEndpointAttributeChannelActionBehavior()
        {
            this.defaultOutgoingRequestFormat = MessageEncodingFormat.Default;
            this.defaultOutgoingReplyFormat = MessageEncodingFormat.Default;
            this.xmlSerializerManager = new UnwrappedTypesXmlSerializerManager();
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            if (endpoint == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("endpoint");
            }
            if (clientRuntime == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("clientRuntime");
            }

            Type channelType = endpoint.Contract.ContractType;

            Dictionary<String, String> map = new Dictionary<string, string>();
            
            foreach (OperationDescription od in endpoint.Contract.Operations)
            {
                ClientOperation cop = clientRuntime.ClientOperations.FirstOrDefault(o => o.Name == od.Name);

                if (cop != null)
                {
                    IClientMessageFormatter requestClient = GetRequestClientFormatter(od, endpoint, clientRuntime);
                    IClientMessageFormatter replyClient = GetReplyClientFormatter(od, endpoint, clientRuntime);
                    cop.Formatter = new CompositeClientFormatter(requestClient, replyClient);
                    cop.SerializeRequest = true;
                    cop.DeserializeReply = od.Messages.Count > 1 && !IsUntypedMessage(od.Messages[1]);
                }

            //    var x = od.Behaviors.Find<ResourceEndpointAttribute>();

            //    MethodInfo mi = od.OperationMethod;

            //    IEnumerable<OperationContractAttribute> operationContracts = ServiceReflector.GetOperationContractAttributes<OperationContractAttribute>(mi, out Dictionary<OperationContractAttribute, Attribute> providers);

            //    foreach (OperationContractAttribute operationContract in operationContracts)
            //    {
            //        if (providers.TryGetValue(operationContract, out Attribute attribute))
            //        {
            //            ResourceEndpointAttribute resourceEndpointAttribute = attribute as ResourceEndpointAttribute;

            //            if (resourceEndpointAttribute != null)
            //            {
            //                ResourceActionAttribute resourceActionAttribute = resourceEndpointAttribute as ResourceActionAttribute;

            //                if (resourceActionAttribute != null)
            //                {
            //                    String action = operationContract.Action ?? $"{new Uri(new Uri(od.DeclaringContract.Namespace), od.DeclaringContract.Name)}/{od.Name}";

            //                    map.Add(action, resourceActionAttribute.ResourceAction);
            //                }
            //            }
            //        }
            //    }
            }

            //clientRuntime.MessageInspectors.Add(new ResourceEndpointAttributeChannelActionMessageInspector(map));
        }

        private IClientMessageFormatter GetReplyClientFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            if (operationDescription.Messages.Count < 2)
            {
                return null;
            }

            ResourceActionAttribute resourceActionAttribute = operationDescription.Behaviors.Find<ResourceActionAttribute>();

            MessageEncodingFormatBindingElement transferMessageEncodingBindingElement = endpoint.Binding.CreateBindingElements().Find<MessageEncodingFormatBindingElement>();

            UriTemplateClientFormatter uriTemplate = new UriTemplateClientFormatter(resourceActionAttribute, operationDescription, null, GetQueryStringConverter(operationDescription), endpoint.Address.Uri, false, endpoint.Contract.Name);

            bool isBareRequest = !resourceActionAttribute.GetIsWrapRequest(out _, out _, out _);
            bool isBareResponse = !resourceActionAttribute.GetIsWrapResponse(out _, out _, out _);

            ValidateBodyParameters(operationDescription, uriTemplate, isBareRequest, isBareResponse, false);

            if (TryGetStreamParameterType(operationDescription.Messages[1], operationDescription, false, out Type type))
            {
                return new HttpStreamFormatter(operationDescription);
            }
            if (IsUntypedMessage(operationDescription.Messages[1]))
            {
                return new MessagePassthroughFormatter();
            }

            MessageEncodingFormat? requestTransferFormat = GetRequestFormat(uriTemplate.SuppressRequestBody, transferMessageEncodingBindingElement);
            MessageEncodingFormat? responseTransferFormat = GetResponseFormat(uriTemplate.SuppressResponseBody, transferMessageEncodingBindingElement);

            if (responseTransferFormat.HasValue && isBareResponse && TryGetNonMessageParameterType(operationDescription.Messages[1], operationDescription, false, out Type parameterType))
            {
                return SingleBodyParameterMessageFormatter.CreateTransferFormatClientFormatter(operationDescription, parameterType, false, responseTransferFormat.Value, this.xmlSerializerManager);
            }
            else
            {
                MessageDescription temp = operationDescription.Messages[0];
                operationDescription.Messages[0] = MakeDummyMessageDescription(MessageDirection.Input);
                IClientMessageFormatter result;
                result = GetDefaultTransferFormatClientFormatter(operationDescription, clientRuntime, responseTransferFormat,!isBareResponse);
                operationDescription.Messages[0] = temp;
                return result;
            }
        }

        IClientMessageFormatter GetDefaultTransferFormatClientFormatter(OperationDescription od, ClientRuntime clientRuntime, MessageEncodingFormat? transferFormat, bool isWrapped)
        {
            IClientMessageFormatter TransferFormatFormatter = GetDefaultClientFormatter(od, clientRuntime, transferFormat, isWrapped);

            if (transferFormat != null)
            
                return new TransferFormatClientMessageFormatter(TransferFormatFormatter, transferFormat.Value);

            return new TransferFormatClientMessageFormatter(TransferFormatFormatter);
        }

        private IClientMessageFormatter GetRequestClientFormatter(OperationDescription operationDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            IClientMessageFormatter innerFormatter = null;

            // get some validation errors by creating "throwAway" formatter

            // validate that endpoint.Address is not null before accessing the endpoint.Address.Uri. This is to avoid throwing a NullRefException while constructing a UriTemplateClientFormatter
            if (endpoint.Address == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        PublicSR.Format(
                            PublicSR.ServiceEndpointMustHaveNonNullAddress, typeof(ServiceEndpoint), typeof(ChannelFactory), typeof(WebHttpEndpoint), nameof(ServiceEndpoint.Address), typeof(ServiceEndpoint))));
            }

            ResourceActionAttribute resourceActionAttribute = operationDescription.Behaviors.Find<ResourceActionAttribute>();

            MessageEncodingFormatBindingElement messageEncodingFormatBindingElement = endpoint.Binding.CreateBindingElements().Find<MessageEncodingFormatBindingElement>();

            UriTemplateClientFormatter uriTemplate = new UriTemplateClientFormatter(resourceActionAttribute, operationDescription, null, GetQueryStringConverter(operationDescription), endpoint.Address.Uri, false, endpoint.Contract.Name);

            MessageEncodingFormat? requestTransferFormat = GetRequestFormat(uriTemplate.SuppressRequestBody, messageEncodingFormatBindingElement);
            MessageEncodingFormat? responseTransferFormat = GetResponseFormat(uriTemplate.SuppressResponseBody, messageEncodingFormatBindingElement);

            int uriVariablesCount = uriTemplate.pathMapping.Count + uriTemplate.queryMapping.Count;

            bool isStream = false;

            HideReplyMessage(operationDescription, () => 
            {
                bool isBareRequest = !resourceActionAttribute.GetIsWrapRequest(out _, out _, out _);
                bool isBareResponse = !resourceActionAttribute.GetIsWrapResponse(out _, out _, out _);

                bool isUntypedWhenUriParamsNotConsidered = false;
                Action doBodyFormatter = () => 
                {
                    if (uriVariablesCount != 0)
                    {
                        this.EnsureNotUntypedMessageNorMessageContract(operationDescription, uriTemplate);
                    }
                    // get body formatter
                    ValidateBodyParameters(operationDescription, uriTemplate, isBareRequest, isBareResponse, true);
                    IClientMessageFormatter baseFormatter;
                    Type parameterType;
                    if (TryGetStreamParameterType(operationDescription.Messages[0], operationDescription, true, out parameterType))
                    {
                        isStream = true;
                        baseFormatter = new HttpStreamFormatter(operationDescription);
                    }
                    else if (requestTransferFormat.HasValue)
                    {
                        if (UseBareRequestFormatter(isBareRequest, operationDescription, out parameterType))
                        {
                            baseFormatter = SingleBodyParameterMessageFormatter.CreateClientFormatter(operationDescription, parameterType, true, requestTransferFormat.Value, this.xmlSerializerManager);
                        }
                        else
                        {
                            baseFormatter = GetDefaultClientFormatter(operationDescription, clientRuntime, requestTransferFormat.Value, !isBareRequest);
                        }
                    }
                    else
                    
                        baseFormatter = new NullMessageClientFormatter();

                    innerFormatter = baseFormatter;
                    isUntypedWhenUriParamsNotConsidered = IsUntypedMessage(operationDescription.Messages[0]);
                };
                if (uriVariablesCount == 0)
                {
                    if (IsUntypedMessage(operationDescription.Messages[0]))
                    {
                        ValidateBodyParameters(operationDescription, uriTemplate, isBareRequest, isBareResponse, true);
                        innerFormatter = new MessagePassthroughFormatter();
                        isUntypedWhenUriParamsNotConsidered = true;
                    }
                    else if (IsTypedMessage(operationDescription.Messages[0]))
                    {
                        ValidateBodyParameters(operationDescription, uriTemplate, isBareRequest, isBareResponse, true);
                        innerFormatter = GetDefaultClientFormatter(operationDescription, clientRuntime, requestTransferFormat.Value, !isBareRequest);
                    }
                    else
                    {
                        doBodyFormatter();
                    }
                }
                else
                {
                    HideRequestUriTemplateParameters(operationDescription, uriTemplate, () => 
                    {
                        CloneMessageDescriptionsBeforeActing(operationDescription, delegate ()
                        {
                            doBodyFormatter();
                        });
                    });
                }
                innerFormatter = new UriTemplateClientFormatter(resourceActionAttribute, operationDescription, innerFormatter, GetQueryStringConverter(operationDescription), endpoint.Address.Uri, isUntypedWhenUriParamsNotConsidered, endpoint.Contract.Name);
            });

            requestTransferFormat = GetRequestTransferFormat(isStream, requestTransferFormat);
            MessageEncodingFormat[] acceptTransferFormats = GetAccepts(responseTransferFormat);
            if (requestTransferFormat.HasValue || acceptTransferFormats.Length > 0)
            {
                innerFormatter = new TransferFormatClientMessageFormatter(innerFormatter, requestTransferFormat, acceptTransferFormats);
            }
            return innerFormatter;
        }

        private static MessageEncodingFormat? GetRequestTransferFormat(bool isStream, MessageEncodingFormat? requestFormat)
        {
            if (isStream)
            {
                return MessageEncodingFormat.Raw;
            }
            else if (requestFormat.HasValue)
            {
                if (requestFormat == MessageEncodingFormat.Default)
                    return MessageEncodingFormat.Binary;
                return requestFormat.Value;
            }
            return null;
        }

        private static MessageEncodingFormat[] GetAccepts(MessageEncodingFormat? transferFormat)
        {
            if (transferFormat.HasValue)
            {
                if (transferFormat == MessageEncodingFormat.Default)
                    return new MessageEncodingFormat[] { MessageEncodingFormat.Binary };
                return new MessageEncodingFormat[] { transferFormat.Value };
            }

            return Array.Empty<MessageEncodingFormat>();
        }

        protected virtual QueryStringConverter GetQueryStringConverter(OperationDescription operationDescription)
        {
            return new QueryStringConverter();
        }

        internal MessageEncodingFormat? GetRequestFormat(bool suppressRequestBody, MessageEncodingFormatBindingElement transferMessageEncodingBindingElement)
        {
            if (suppressRequestBody)

                return null;

            if (transferMessageEncodingBindingElement.Format == MessageEncodingFormat.Default)
                return MessageEncodingFormat.Binary;
            return transferMessageEncodingBindingElement.Format;
        }

        internal MessageEncodingFormat? GetResponseFormat(bool suppressResponseBody, MessageEncodingFormatBindingElement transferMessageEncodingBindingElement)
        {
            if (suppressResponseBody)

                return null;

            if (transferMessageEncodingBindingElement.Format == MessageEncodingFormat.Default)
                return MessageEncodingFormat.Binary;
            return transferMessageEncodingBindingElement.Format;
        }

        static void HideRequestUriTemplateParameters(OperationDescription operationDescription, UriTemplateClientFormatter template, Action effect)
        {
            HideRequestUriTemplateParameters(operationDescription, template.pathMapping, template.queryMapping, effect);
        }

        static void HideRequestUriTemplateParameters(OperationDescription operationDescription, Dictionary<int, string> pathMapping, Dictionary<int, KeyValuePair<string, Type>> queryMapping, Action effect)
        {
            // mutate description to hide UriTemplate parameters
            Collection<MessagePartDescription> originalParts = CloneParts(operationDescription.Messages[0]);
            Collection<MessagePartDescription> parts = CloneParts(operationDescription.Messages[0]);
            operationDescription.Messages[0].Body.Parts.Clear();
            int newIndex = 0;
            for (int i = 0; i < parts.Count; ++i)
            {
                if (!pathMapping.ContainsKey(i) && !queryMapping.ContainsKey(i))
                {
                    operationDescription.Messages[0].Body.Parts.Add(parts[i]);
                    parts[i].Index = newIndex++;
                }
            }
            effect();
            // unmutate description
            operationDescription.Messages[0].Body.Parts.Clear();
            for (int i = 0; i < originalParts.Count; ++i)
            {
                operationDescription.Messages[0].Body.Parts.Add(originalParts[i]);
            }
        }

        internal virtual bool UseBareRequestFormatter(bool isBareRequest, OperationDescription operationDescription, out Type parameterType)
        {
            parameterType = null;
            return isBareRequest && TryGetNonMessageParameterType(operationDescription.Messages[0], operationDescription, true, out parameterType);
        }

        static void CloneMessageDescriptionsBeforeActing(OperationDescription operationDescription, Action effect)
        {
            MessageDescription originalRequest = operationDescription.Messages[0];
            bool thereIsAReply = operationDescription.Messages.Count > 1;
            MessageDescription originalReply = thereIsAReply ? operationDescription.Messages[1] : null;
            operationDescription.Messages[0] = originalRequest.Clone();
            if (thereIsAReply)
            {
                operationDescription.Messages[1] = originalReply.Clone();
            }
            effect();
            operationDescription.Messages[0] = originalRequest;
            if (thereIsAReply)
            {
                operationDescription.Messages[1] = originalReply;
            }
        }

        static Collection<MessagePartDescription> CloneParts(MessageDescription md)
        {
            MessagePartDescriptionCollection bodyParameters = md.Body.Parts;
            Collection<MessagePartDescription> bodyParametersClone = new Collection<MessagePartDescription>();
            for (int i = 0; i < bodyParameters.Count; ++i)
            {
                MessagePartDescription copy = bodyParameters[i].Clone();
                bodyParametersClone.Add(copy);
            }
            return bodyParametersClone;
        }

        static void HideReplyMessage(OperationDescription operationDescription, Action effect)
        {
            MessageDescription temp = null;
            if (operationDescription.Messages.Count > 1)
            {
                temp = operationDescription.Messages[1];
                operationDescription.Messages[1] = MakeDummyMessageDescription(MessageDirection.Output);
            }
            effect();
            if (operationDescription.Messages.Count > 1)
            {
                operationDescription.Messages[1] = temp;
            }
        }

        void EnsureNotUntypedMessageNorMessageContract(OperationDescription operationDescription, UriTemplateClientFormatter template)
        {
            // Called when there are UriTemplate parameters.  UT does not compose with Message
            // or MessageContract because the SOAP and REST programming models must be uniform here.
            bool isUnadornedSuppressRequestEntity = false;

            if (template.SuppressRequestBody && !template.HasUriTemplate)
            {
                isUnadornedSuppressRequestEntity = true;
            }
            if (IsTypedMessage(operationDescription.Messages[0]))
            {
                if (isUnadornedSuppressRequestEntity)
                {
                    // WebGet will give you UriTemplate parameters by default.
                    // We need a special error message for this case to prevent confusion.
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new InvalidOperationException(
                            AMMMFR.Format(
                                AMMMFR.MethodCannotHaveMCParameter, operationDescription.Name, operationDescription.DeclaringContract.Name, template.Method, operationDescription.Messages[0].MessageType.Name)));
                }
                else
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new InvalidOperationException(
                            PublicSR.Format(
                                PublicSR.UTParamsDoNotComposeWithMessageContract, operationDescription.Name, operationDescription.DeclaringContract.Name)));
                }
            }

            if (IsUntypedMessage(operationDescription.Messages[0]))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(
                        PublicSR.Format(
                            PublicSR.UTParamsDoNotComposeWithMessage, operationDescription.Name, operationDescription.DeclaringContract.Name)));
            }
        }

        void ValidateBodyParameters(OperationDescription operation, UriTemplateClientFormatter template, bool isBareRequest, bool isBareResponse, bool request)
        {
            if (request)
            {
                ValidateSuppressEntityBodyHasNoBody(operation, template);
            }
            // validate that if bare is chosen for request/response, then at most 1 parameter is possible
            ValidateBodyStyle(operation, isBareRequest, isBareResponse, request);
            // validate if the request or response body is a stream, no other body parameters
            // can be specified
            ValidateAtMostOneStreamParameter(operation, request);
        }

        void ValidateBodyStyle(OperationDescription operation, bool isBareRequest, bool isBareResponse, bool request)
        {
            Type dummy;
            if (request && isBareRequest)
            {
                TryGetNonMessageParameterType(operation.Messages[0], operation, true, out dummy);
            }
            if (!request && operation.Messages.Count > 1 && isBareResponse)
            {
                TryGetNonMessageParameterType(operation.Messages[1], operation, false, out dummy);
            }
        }

        internal static bool TryGetNonMessageParameterType(MessageDescription message, OperationDescription declaringOperation, bool isRequest, out Type type)
        {
            type = null;
            if (message == null)
            {
                return true;
            }
            if (IsTypedMessage(message) || IsUntypedMessage(message))
            {
                return false;
            }
            if (isRequest)
            {
                if (message.Body.Parts.Count > 1)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new InvalidOperationException(
                            PublicSR.Format(
                                PublicSR.AtMostOneRequestBodyParameterAllowedForUnwrappedMessages, declaringOperation.Name, declaringOperation.DeclaringContract.Name)));
                }
                if (message.Body.Parts.Count == 1 && message.Body.Parts[0].Type != typeof(void))
                {
                    type = message.Body.Parts[0].Type;
                }
                return true;
            }
            else
            {
                if (message.Body.Parts.Count > 0)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new InvalidOperationException(
                            PublicSR.Format(
                                PublicSR.OnlyReturnValueBodyParameterAllowedForUnwrappedMessages, declaringOperation.Name, declaringOperation.DeclaringContract.Name)));
                }
                if (message.Body.ReturnValue != null && message.Body.ReturnValue.Type != typeof(void))
                {
                    type = message.Body.ReturnValue.Type;
                }
                return true;
            }
        }

        static bool TryGetStreamParameterType(MessageDescription message, OperationDescription declaringOperation, bool isRequest, out Type type)
        {
            type = null;
            if (message == null || IsTypedMessage(message) || IsUntypedMessage(message))
            {
                return false;
            }
            if (isRequest)
            {
                bool hasStream = false;
                for (int i = 0; i < message.Body.Parts.Count; ++i)
                {
                    if (typeof(Stream) == message.Body.Parts[i].Type)
                    {
                        type = message.Body.Parts[i].Type;
                        hasStream = true;
                        break;
                    }

                }
                if (hasStream && message.Body.Parts.Count > 1)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new ArgumentException(
                            PublicSR.Format(
                                PublicSR.AtMostOneRequestBodyParameterAllowedForStream, declaringOperation.Name, declaringOperation.DeclaringContract.Name)));
                }
                return hasStream;
            }
            else
            {
                // validate that the stream is not an out or ref param
                for (int i = 0; i < message.Body.Parts.Count; ++i)
                {
                    if (typeof(Stream) == message.Body.Parts[i].Type)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new ArgumentException(
                                PublicSR.Format(
                                    PublicSR.NoOutOrRefStreamParametersAllowed, message.Body.Parts[i].Name, declaringOperation.Name, declaringOperation.DeclaringContract.Name)));
                    }
                }
                if (message.Body.ReturnValue != null && typeof(Stream) == message.Body.ReturnValue.Type)
                {
                    // validate that there are no out or ref params
                    if (message.Body.Parts.Count > 0)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new ArgumentException(
                                PublicSR.Format(
                                    PublicSR.NoOutOrRefParametersAllowedWithStreamResult, declaringOperation.Name, declaringOperation.DeclaringContract.Name)));
                    }
                    type = message.Body.ReturnValue.Type;
                    return true;
                }

                else
                {
                    return false;
                }
            }
        }

        static void ValidateAtMostOneStreamParameter(OperationDescription operation, bool request)
        {
            Type dummy;
            if (request)
            {
                TryGetStreamParameterType(operation.Messages[0], operation, true, out dummy);
            }
            else
            {
                if (operation.Messages.Count > 1)
                {
                    TryGetStreamParameterType(operation.Messages[1], operation, false, out dummy);
                }
            }
        }

        static void ValidateSuppressEntityBodyHasNoBody(OperationDescription operation, UriTemplateClientFormatter template)
        {
            if (template.SuppressRequestBody)
            {
                if (!IsUntypedMessage(operation.Messages[0]) && operation.Messages[0].Body.Parts.Count != 0)
                {
                    if (!IsTypedMessage(operation.Messages[0]))
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new InvalidOperationException(
                                AMMMFR.Format(
                                    AMMMFR.MethodCannotHaveBody, operation.Name, operation.DeclaringContract.Name, template.Method, operation.Messages[0].Body.Parts[0].Name)));
                    }
                    else
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new InvalidOperationException(
                                AMMMFR.Format(
                                    AMMMFR.MethodCannotHaveMCParameter, operation.Name, operation.DeclaringContract.Name, template.Method, operation.Messages[0].MessageType.Name)));
                    }
                }
            }
        }

        internal bool GetIsWrapResponse(OperationDescription od, ResourceActionAttribute resourceActionAttribute)
        {
            return resourceActionAttribute.GetIsWrapResponse(out bool? isEnvelopeVersionNone, out String actionName, out ProtectionLevel protectionLevel);
        }

        static bool IsTypedMessage(MessageDescription message)
        {
            return message != null && message.IsTypedMessage;
        }

        static bool IsUntypedMessage(MessageDescription message)
        {
            return message != null && message.IsUntypedMessage;
        }

        static MessageDescription MakeDummyMessageDescription(MessageDirection direction)
        {
            MessageDescription messageDescription = new MessageDescription("urn:dummyAction", direction);
            return messageDescription;
        }

        internal IClientMessageFormatter GetDefaultClientFormatter(OperationDescription od, ClientRuntime clientRuntime, MessageEncodingFormat? requestMessageFormat, bool isWrapped)
        {
            DataContractSerializerOperationBehavior dcsob = od.Behaviors.Find<DataContractSerializerOperationBehavior>();
            ClientOperation cop = new ClientOperation(clientRuntime, "dummyClient", "urn:dummy");
            cop.Formatter = null;

            if (dcsob != null)
            {
                if (dcsob is ResourceTransferFormatSerializerOperationBehavior)
                {
                    ResourceTransferFormatSerializerOperationBehavior tsob = dcsob as ResourceTransferFormatSerializerOperationBehavior;
                    tsob.TransferMessageSerializerFormatAttribute.Format = requestMessageFormat ?? MessageEncodingFormat.Default;
                    tsob = new ResourceTransferFormatSerializerOperationBehavior(od, tsob.TransferMessageSerializerFormatAttribute);
                    (tsob as IOperationBehavior).ApplyClientBehavior(od, cop);
                    return cop.Formatter;
                }
                else
                {
                    (dcsob as IOperationBehavior).ApplyClientBehavior(od, cop);
                    return cop.Formatter;
                }
            }
            XmlSerializerOperationBehavior xsob = od.Behaviors.Find<XmlSerializerOperationBehavior>();
            if (xsob != null)
            {
                xsob = new XmlSerializerOperationBehavior(od, xsob.XmlSerializerFormatAttribute);
                (xsob as IOperationBehavior).ApplyClientBehavior(od, cop);
                return cop.Formatter;
            }

            return null;
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}