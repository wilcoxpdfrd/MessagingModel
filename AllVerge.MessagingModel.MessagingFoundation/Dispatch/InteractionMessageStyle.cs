using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions;
    using AllVerge.MessagingModel.MarkupPrimitives.Xml;

    public class InteractionMessageStyle
    {
        public const String BINDING_KIND_SOAP = MessagingBindingConstants.SOAP_BINDING_PREFIX;
        public const String BINDING_KIND_SOAP12 = MessagingBindingConstants.SOAP12_BINDING_PREFIX;
        public const String BINDING_KIND_HTTP = MessagingBindingConstants.HTTP_BINDING_PREFIX;

        public const String BINDING_STYLE_NAME = "style";
        public const String BINDING_STYLE_RPC = "rpc";
        public const String BINDING_STYLE_DOCUMENT = "document";

        public const String BINDING_STYLE_POST = ResourceActions.POST;
        public const String BINDING_STYLE_PUT = ResourceActions.PUT;
        public const String BINDING_STYLE_PATCH = ResourceActions.PATCH;
        public const String BINDING_STYLE_GET = ResourceActions.GET;
        public const String BINDING_STYLE_DELETE = ResourceActions.DELETE;

        public const String SOAP_RPC_BINDING = BINDING_KIND_SOAP + "-" + BINDING_STYLE_RPC;
        public const String SOAP_DOCUMENT_BINDING = BINDING_KIND_SOAP + "-" + BINDING_STYLE_DOCUMENT;

        public const String SOAP12_RPC = BINDING_KIND_SOAP12 + "-" + BINDING_STYLE_RPC;
        public const String SOAP12_DOCUMENT = BINDING_KIND_SOAP12 + "-" + BINDING_STYLE_DOCUMENT;

        public const String HTTP_POST_BINDING = BINDING_KIND_HTTP + "-" + BINDING_STYLE_POST;
        public const String HTTP_PUT_BINDING = BINDING_KIND_HTTP + "-" + BINDING_STYLE_PUT;
        public const String HTTP_PATCH_BINDING = BINDING_KIND_HTTP + "-" + BINDING_STYLE_PATCH;
        public const String HTTP_GET_BINDING = BINDING_KIND_HTTP + "-" + BINDING_STYLE_GET;
        public const String HTTP_DELETE_BINDING = BINDING_KIND_HTTP + "-" + BINDING_STYLE_DELETE;

        private string interactionName;
        private InteractionStyles interactionStyle;
        private string bindingPrefix;
        private string bindingNamespace;
        private string bindingKind;
        private string bindingStyle;
        private string envelopeNamespace;

        public InteractionMessageStyle(string bindingPrefix, string bindingNamespace, string bindingStyle, string interactionName, InteractionStyles interactionStyle)
        {
            switch (bindingPrefix)
            {
                case MessagingBindingConstants.SOAP_BINDING_PREFIX:
                    if (bindingNamespace != MessagingBindingConstants.SOAP_BINDING_NAMESPACE)
                        throw new ArgumentException("The value of the parameter is unexpected.", nameof(bindingNamespace));
                    switch (bindingStyle)
                    {
                        case BINDING_STYLE_RPC:
                            if (String.IsNullOrEmpty(interactionName))
                                throw new ArgumentException("The value of the parameter is required.", nameof(interactionName));
                            break;
                        case BINDING_STYLE_DOCUMENT:
                            break;
                        default:
                            throw new ArgumentException("The value of the parameter is unexpected.", nameof(bindingStyle));
                    }
                    this.bindingKind = BINDING_KIND_SOAP;
                    this.bindingPrefix = bindingPrefix;
                    this.bindingNamespace = bindingNamespace;
                    this.envelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
                    this.bindingStyle = bindingStyle;
                    break;
                case MessagingBindingConstants.SOAP12_BINDING_PREFIX:
                    if (bindingNamespace != MessagingBindingConstants.SOAP12_BINDING_NAMESPACE)
                        throw new ArgumentException("The value of the parameter is unexpected.", nameof(bindingNamespace));
                    switch (bindingStyle)
                    {
                        case BINDING_STYLE_RPC:
                            if (String.IsNullOrEmpty(interactionName))
                                throw new ArgumentException("The value of the parameter is required.", nameof(interactionName));
                            break;
                        case BINDING_STYLE_DOCUMENT:
                            break;
                        default:
                            throw new ArgumentException("The value of the parameter is unexpected.", nameof(bindingStyle));
                    }
                    this.bindingKind = BINDING_KIND_SOAP12;
                    this.bindingPrefix = bindingPrefix;
                    this.bindingNamespace = bindingNamespace;
                    this.envelopeNamespace = "http://www.w3.org/2003/05/soap-envelope";
                    this.bindingStyle = bindingStyle;
                    break;
                case MessagingBindingConstants.HTTP_BINDING_PREFIX:
                    if (bindingNamespace != MessagingBindingConstants.HTTP_BINDING_NAMESPACE)
                        throw new ArgumentException("The value of the parameter is unexpected.", nameof(bindingNamespace));
                    switch (bindingStyle)
                    {
                        case BINDING_STYLE_POST:
                        case BINDING_STYLE_PUT:
                        case BINDING_STYLE_PATCH:
                        case BINDING_STYLE_GET:
                        case BINDING_STYLE_DELETE:
                            break;
                        default:
                            throw new ArgumentException("The value of the parameter is unexpected.", nameof(bindingStyle));
                    }
                    this.bindingKind = BINDING_KIND_HTTP;
                    this.bindingPrefix = bindingPrefix;
                    this.bindingNamespace = bindingNamespace;
                    this.envelopeNamespace = "http://schemas.microsoft.com/ws/2005/05/envelope/none";
                    this.bindingStyle = bindingStyle;
                    break;
                default:
                    throw new ArgumentException("The value of the parameter is not recognized.", nameof(bindingPrefix));
            }
            this.interactionName = interactionName;
            this.interactionStyle = interactionStyle;
        }

        public string InteractionName
        {
            get => this.interactionName;
        }

        public InteractionStyles InteractionStyle
        {
            get => this.interactionStyle;
        }

        public string Kind
        {
            get { return this.bindingKind; }
        }

        public string BindingPrefix
        {
            get => this.bindingPrefix;
        }

        public string BindingNamespace
        {
            get => this.bindingNamespace;
        }

        public string BindingStyle
        {
            get => this.bindingStyle;
        }

        public string EnvelopeNamespace
        {
            get { return this.envelopeNamespace; }
        }

        public bool IsRootOrEnvelopeNamespace(String @namespace, String rootNamespace)
        {
            return @namespace == rootNamespace || @namespace == EnvelopeNamespace;
        }

        public String GetBindingString(bool fullRpcStyle = true)
        {
            if (this.bindingStyle == BINDING_STYLE_RPC && fullRpcStyle)
            
                return $"{this.bindingKind}-{this.bindingStyle}:{this.interactionName}";

            return $"{this.bindingKind}-{this.bindingStyle}";
        }

        public string GetInteractionString()
        {
            if (this.interactionStyle.TryGetXmlEnumAttributeNameFromEnum(out String interactionStyleName))

                return interactionStyleName;

            return this.interactionStyle.ToString();
        }

        /// <summary>
        /// Returns a <see cref="String"/> that represents the current <see cref="InteractionMessageStyle"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.GetBindingString()}/{this.GetInteractionString()}";
        }

        public static InteractionMessageStyle Parse(String interactionMessageStyle)
        {
            if (interactionMessageStyle == null)

                throw new ArgumentNullException(nameof(interactionMessageStyle));

            String[] segments = interactionMessageStyle.Split('/');

            if (segments.Length != 2)

                throw new ArgumentException("The format of the parameter is invalid.", nameof(interactionMessageStyle));

            if (!segments[1].TryGetEnumFromXmlEnumAttributeName<InteractionStyles>(out InteractionStyles interactionStyle) &&
                !Enum.TryParse<InteractionStyles>(segments[1], out interactionStyle))

                throw new ArgumentException("The value of the parameter is invalid.", nameof(interactionMessageStyle));

            String interactionName;

            segments = segments[0].Split(':');

            if (segments.Length == 2)

                interactionName = segments[1];

            else if (segments.Length == 1)

                interactionName = null;

            else

                throw new ArgumentException("The format of the parameter is invalid.", nameof(interactionMessageStyle));


            segments = segments[0].Split('-');

            if (segments.Length != 2)

                throw new ArgumentException("The format of the parameter is invalid.", nameof(interactionMessageStyle));

            switch (segments[0])
            {
                case MessagingBindingConstants.SOAP_BINDING_PREFIX:
                    return new InteractionMessageStyle(MessagingBindingConstants.SOAP_BINDING_PREFIX, MessagingBindingConstants.SOAP_BINDING_NAMESPACE, segments[1], interactionName, interactionStyle);
                case MessagingBindingConstants.SOAP12_BINDING_PREFIX:
                    return new InteractionMessageStyle(MessagingBindingConstants.SOAP12_BINDING_PREFIX, MessagingBindingConstants.SOAP12_BINDING_NAMESPACE, segments[1], interactionName, interactionStyle);
                case MessagingBindingConstants.HTTP_BINDING_PREFIX:
                    return new InteractionMessageStyle(MessagingBindingConstants.HTTP_BINDING_PREFIX, MessagingBindingConstants.HTTP_BINDING_NAMESPACE, segments[1], interactionName, interactionStyle);
            }

            throw new ArgumentException("The value of the parameter is not recognized.", nameof(interactionMessageStyle));
        }
    }
}
