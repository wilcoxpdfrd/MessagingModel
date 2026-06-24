using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;


namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions;
    using static AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.ResourceActions;

    /// <summary>Indicates that the decorated method defines the action of a resource endpoint in a service contract.  Provides for specifying a template for the resource action endpoint.</summary>
    public abstract class ResourceActionTemplateAttribute : ResourceActionAttribute
    {
        private string template;
        private ResourceActionStyle actionStyle;
        private bool actionStyleSetExplicitly;
        private ResourceMediaType mediaType;
        private bool mediaTypeSetExplicitly;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceActionTemplateAttribute" /> classwith the given <paramref name="methodName"/>, <paramref name="resourceAction"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="resourceAction"></param>
        /// <param name="template"></param>
        protected ResourceActionTemplateAttribute(String methodName, String resourceAction, String template) : base(methodName, resourceAction)
        {
            this.template = template;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceActionTemplateAttribute" /> class with the given <paramref name="resourceAction"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="resourceAction"></param>
        /// <param name="template"></param>
        protected ResourceActionTemplateAttribute(String resourceAction, String template) : base(resourceAction)
        {
            this.template = template;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceActionTemplateAttribute" /> class with the given <paramref name="template"/>.
        /// </summary>
        /// <param name="resourceAction"></param>
        protected ResourceActionTemplateAttribute(String template) : base()
        {
            this.template = template;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceActionTemplateAttribute" /> class.
        /// </summary>
        /// <param name="template"></param>
        protected ResourceActionTemplateAttribute() : base()
        {
        }

        /// <summary>Gets or sets a template for specifing the resource action endpoint. </summary>
        /// <exception cref="System.ArgumentNullException">The value is null.</exception>
        public string Template
        {
            get
            {
                return this.template;
            }

            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                }

                this.template = value;
            }
        }

        /// <summary>
        /// Gets and sets the action style of the media content that is exchanged via the interface method.
        /// </summary>
        /// <value>
        /// One of the <see cref="ResourceActionStyle"/> enumeration values.
        /// </value>
        public ResourceActionStyle ActionStyle
        {
            get
            {
                return this.actionStyle;
            }
            set
            {
                ValidateResourceActionStyle(value);
                this.actionStyle = value;
                this.actionStyleSetExplicitly = true;
            }
        }

        private void ValidateResourceActionStyle(ResourceActionStyle value)
        {
            if (!CanSetResourceActionStyle(value, out ResourceActionStyle[] supportedResourceActionStyles))

                /*
                <data name="BodyStyleNotSupportedByWebScript" xml:space="preserve">
                    <value>The body style '{0}' is not supported by '{1}'. Change the body style to be '{2}'.</value>
                </data>
                */

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new InvalidOperationException(PublicSR.Format(PublicSR.BodyStyleNotSupportedByWebScript, new object[]
                {
                    value,
                    this.GetType().FullName,
                    supportedResourceActionStyles == null ? "un-set": $"one of {String.Join(",", supportedResourceActionStyles)}"
                })));
        }

        protected virtual bool CanSetResourceActionStyle(ResourceActionStyle value, out ResourceActionStyle[] supportedResourceActionStyles)
        {
            AllowedHalfDuplexMessages allowedHalfDuplexMessage = ResourceActions.GetAllowedHalfDuplexMessages(this.ResourceAction);

            bool canConsumeMessage = allowedHalfDuplexMessage.HasFlag(AllowedHalfDuplexMessages.Request);
            bool canProduceMessage = allowedHalfDuplexMessage.HasFlag(AllowedHalfDuplexMessages.Response);

            if (canConsumeMessage && canProduceMessage)
            {
                supportedResourceActionStyles = new[] { ResourceActionStyle.Bare, ResourceActionStyle.Wrapped, ResourceActionStyle.WrappedRequest, ResourceActionStyle.WrappedResponse };
            }
            else if (canConsumeMessage)
            {
                supportedResourceActionStyles = new[] { ResourceActionStyle.Bare, ResourceActionStyle.Wrapped, ResourceActionStyle.WrappedRequest };
            }
            else if (canProduceMessage)
            {
                supportedResourceActionStyles = new[] { ResourceActionStyle.Bare, ResourceActionStyle.Wrapped, ResourceActionStyle.WrappedResponse };
            }
            else

                supportedResourceActionStyles = null;

            return supportedResourceActionStyles != null && supportedResourceActionStyles.Contains(value);
        }

        /// <summary>
        /// Gets the <see cref="IsActionStyleSetExplicitly"/> property.
        /// </summary>
        /// <value>
        /// A value that specifies whether the <see cref="ActionStyle"/> property is set explicitly.
        /// </value>
        public bool IsActionStyleSetExplicitly { get => actionStyleSetExplicitly; }

        /// <summary>
        /// Gets and sets the media type of the messages that are sent to and from the interface method.
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
                    this.ResourceAction,
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

        protected override MessageFilter GetMessageFilter(Uri baseAddress, OperationDescription operationDescription)
        {
            String name;

            if (String.IsNullOrEmpty(this.Name))

                name = operationDescription.OperationMethod.Name;

            else

                name = this.Name;

            String method = this.ResourceAction;
            String template = this.Template ?? String.Empty;

            if (UriTemplateHelpers.IsWildcardPath(template))

                return new WildcardTemplateFilter(baseAddress, method, name);

            return new UriTemplateFilter(new UriTemplate(template), baseAddress, method, name);
        }

        protected override void Validate(OperationDescription operationDescription)
        {
            ResourceActions.Validate(this.ResourceAction, operationDescription);
        }
    }
}
