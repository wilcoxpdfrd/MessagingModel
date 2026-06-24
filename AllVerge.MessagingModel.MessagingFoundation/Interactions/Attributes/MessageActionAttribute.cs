using System;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.ServiceModel;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Description;

    using AllVerge.SystemPrimitives.Net;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions;
    using static AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.ResourceActions;

    /// <summary>
    /// Indicates that the decorated method defines the message action of a resource endpoint in a service contract.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class MessageActionAttribute : ResourceActionAttribute, IResourceMessageActionAttribute
    {
        private string messageAction;
        private string replyAction;
        private ResourceMediaType mediaType;
        private bool mediaTypeSetExplicitly;
        private bool isInitiating = true;
        private bool isTerminating;
        private ProtectionLevel protectionLevel;
        private bool hasProtectionLevel;
        internal const string ActionPropertyName = "Action";
        internal const string ProtectionLevelPropertyName = "ProtectionLevel";
        internal const string ReplyActionPropertyName = "ReplyAction";

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageActionAttribute" /> class with the given <paramref name="methodName"/>, <paramref name="resourceAction"/> and <paramref name="messageAction"/>.
        /// </summary>
        /// <param name="messageAction"></param>
        protected MessageActionAttribute(String methodName, String resourceAction, String messageAction) : 
            base(methodName, resourceAction)
        {
            this.messageAction = messageAction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageActionAttribute" /> class with the given <paramref name="resourceAction"/> and <paramref name="messageAction"/>.
        /// </summary>
        /// <param name="messageAction"></param>
        protected MessageActionAttribute(String resourceAction, String messageAction) : 
            base(resourceAction)
        {
            this.messageAction = messageAction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageActionAttribute" /> class with the given <paramref name="resourceAction"/>.
        /// </summary>
        /// <param name="action"></param>
        protected MessageActionAttribute(String resourceAction) :
            base(resourceAction)
        {
        }

        /// <summary>
        /// Gets or sets an indication that the method can handle messages without an WS-Addressing Action Header; 
        /// allows dispatching to the method when a binding with a "none" Addressing is used.
        /// Used when more than one method of a contract is decorated with an attribute derived from <see cref="MessageActionAttribute"/>;
        /// only one such method can specify <see cref="IsUnAddressedAction"/> true.
        /// </summary>
        public virtual bool IsUnAddressedAction { get; set; }

        /// <summary>
        /// Gets or sets the WS-Addressing action of the request message.
        /// </summary>
        /// <returns>
        /// The action to use in generating the WS-Addressing Action header.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">The value is null.</exception>
        public string Action
        {

            get
            {
                return this.messageAction;
            }

            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                }

                this.messageAction = value;
            }
        }
        
        /// <summary>
        /// Gets or sets a value that specifies whether the messages of an operation must be encrypted, signed, or both.
        /// </summary>
        /// <returns>
        /// One of the <see cref="System.Net.Security.ProtectionLevel" /> values. The default is <see cref="ProtectionLevel.None" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is not one of the <see cref="ProtectionLevel" /> values.</exception>
        public ProtectionLevel ProtectionLevel
        {
            get
            {
                return this.protectionLevel;
            }
            set
            {
                this.protectionLevel = value;
                this.hasProtectionLevel = true;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the messages for this operation must be encrypted, signed, or both.
        /// </summary>
        /// <returns>
        /// true if the <see cref="OperationContractAttribute.ProtectionLevel" /> property is set to a value other than <see cref="ProtectionLevel.None" />; otherwise, false. The default is false.
        /// </returns>
        public bool HasProtectionLevel
        {
            get
            {
                return this.hasProtectionLevel;
            }
        }

        /// <summary>
        /// Gets or sets the value of the SOAP action for the reply message of the operation.
        /// </summary>
        /// <returns>
        /// The value of the SOAP action for the reply message.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <see cref="OperationContractAttribute.ReplyAction" /> is null.
        /// </exception>
        public string ReplyAction
        {

            get
            {
                return this.replyAction;
            }

            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                }
                this.replyAction = value;
            }
        }

        /// <summary>
        /// Gets and sets the body style of the messages that are sent to and from the interface method.
        /// </summary>
        /// <value>
        /// One of the <see cref="ResourceMediaType"/> enumeration values.
        /// </value>
        public ResourceMediaType MediaType
        {
            get
            {
                return this.mediaType;
            }
            set
            {
                ValidateMediaType(value);
                this.mediaType = value;
                this.mediaTypeSetExplicitly = true;
            }
        }

        private void ValidateMediaType(ResourceMediaType value)
        {
            if (!CanSetMediaType(value))

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(PublicSR.Format(PublicSR.OperationDoesNotSupportFormat, new object[]
                {
                    this.Action,
                    value,
                })));
        }

        protected virtual bool CanSetMediaType(ResourceMediaType value)
        {
            if (value != ResourceMediaType.None)
            {
                AllowedHalfDuplexMessages allowedHalfDuplexMessages = ResourceActions.GetAllowedHalfDuplexMessages(this.ResourceAction);

                return allowedHalfDuplexMessages > 0;
            }

            return true;
        }

        /// <summary>
        /// Gets the <see cref="IsMediaTypeSetExplicitly"/> property.
        /// </summary>
        /// <value>
        /// A value that specifies whether the <see cref="MediaType"/> property is set explicitly.
        /// </value>
        public bool IsMediaTypeSetExplicitly { get => mediaTypeSetExplicitly; }

        /// <summary>
        /// Override to get a value that indicates whether an operation returns a reply message using a half-duplex transport channel.
        /// </summary>
        public abstract bool GetIsOneWay();

        /// <summary>
        /// Gets or sets a value that indicates whether the method implements an operation that can initiate a session on the server (if such a session exists).
        /// </summary>
        /// <returns>
        /// true if the operation is permitted to initiate a session on the server, otherwise, false. The default is true.
        /// </returns>
        public bool IsInitiating
        {
            get
            {
                return this.isInitiating;
            }
            set
            {
                this.isInitiating = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the service operation causes the server to close the session after the reply message, if any, is sent.
        /// </summary>
        /// <returns>
        /// true if the operation causes the server to close the session, otherwise, false. The default is false.
        /// </returns>
        public bool IsTerminating
        {
            get
            {
                return this.isTerminating;
            }
            set
            {
                this.isTerminating = value;
            }
        }

        public bool IsSessionOpenNotificationEnabled
        {
            get
            {
                return this.Action == "http://schemas.microsoft.com/2011/02/session/onopen";
            }
        }

        internal void EnsureInvariants(MethodInfo methodInfo, string operationName)
        {
            if (this.IsSessionOpenNotificationEnabled && (!this.GetIsOneWay() || !this.IsInitiating || methodInfo.GetParameters().Length != 0))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(PublicSR.Format(PublicSR.ContractIsNotSelfConsistentWhenIsSessionOpenNotificationEnabled, new object[]
                {
                    operationName,
                    "Action",
                    "http://schemas.microsoft.com/2011/02/session/onopen",
                    "IsOneWay",
                    "IsInitiating"
                })));
            }
        }

        protected override MessageFilter GetMessageFilter(Uri baseAddress, OperationDescription operationDescription)
        {
            ContractDescription contractDescription = operationDescription.DeclaringContract;

            String name;
            String action;

            if (String.IsNullOrEmpty(this.Name))

                name = operationDescription.OperationMethod.Name;

            else

                name = this.Name;

            if (String.IsNullOrWhiteSpace(this.Action))
            {
                String contractDescriptionNamespace;
                String contractDescriptionName;

                if (String.IsNullOrWhiteSpace(contractDescription.Namespace))

                    contractDescriptionNamespace = UriUtils.TEMP_URI.AbsoluteUri;

                else

                    contractDescriptionNamespace = contractDescription.Namespace;

                if (String.IsNullOrWhiteSpace(contractDescription.Name))

                    contractDescriptionName = contractDescription.ContractType.Name;

                else

                    contractDescriptionName = contractDescription.Name;

                action = UriUtils.CreateUri(contractDescriptionNamespace, contractDescriptionName, this.Name);
            }
            else

                action = this.messageAction;

            return 
                new ActionMessageFilter(
                    UriUtils.CreateUriOrWildCard(
                        action,
                        name));
        }
    }
}
