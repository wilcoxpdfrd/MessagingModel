using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes
{
    /// <summary>Indicates that a method defines a POST message action that is part of a service contract in an application.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PostMessageActionAttribute : MessageActionAttribute
    {
        public static readonly string ACTION_NAME = ResourceActions.POST;

        private bool isOneWay;
        private Type callbackContractType;

        /// <summary>Initializes a new instance of the <see cref="PostMessageActionAttribute" /> class.</summary>
        public PostMessageActionAttribute() : base(ACTION_NAME)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PostMessageActionAttribute" /> class with the given <paramref name="messageAction"/>.</summary>
        public PostMessageActionAttribute(string messageAction) : base(ACTION_NAME, messageAction)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PostMessageActionAttribute" /> class with the given <paramref name="methodName"/> and <paramref name="messageAction"/>.</summary>
        public PostMessageActionAttribute(string methodName, string messageAction) : base(methodName, ACTION_NAME, messageAction)
        {
        }

        public override bool GetIsOneWay()
        {
            return IsOneWay;
        }

        /// <summary>Gets or sets a value that indicates whether an operation returns a reply message using a half-duplex transport channel.  The default is false.  When set false, <see cref="CallbackContractType"/> is set null.</summary>
        /// <returns>true if this method receives a request message and returns either no reply message or returns a reply message using a one-way duplex reply channel; otherwise, false.</returns>
        public bool IsOneWay
        {
            get
            {
                return isOneWay;
            }

            set
            {
                if (!value)

                    CallbackContractType = null;

                isOneWay = value;
            }
        }

        /// <summary>Gets or sets the contract type for the callback when the messaging interaction is duplex and a ServiceChannel is present in the InteractionChannel.  The default is null.  When set to a non-null value, <see cref="IsOneWay"/> is set true.</summary>
        /// <returns>A <see cref="Type" /> that includes a method decorated with a <see cref="PostMessageActionAttribute"/> attribute specifying the callback action.</returns>
        public Type CallbackContractType
        {
            get
            {
                return callbackContractType;
            }

            set
            {
                if (value != null)
                {
                    if (!value.IsInterface && !value.IsMarshalByRef)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(AMMMFR.Format(AMMMFR.SFxInvalidCallbackContractType, value.Name)));
                    }

                    if (ServiceReflector.GetSingleAttribute<ResourceContractAttribute>(value) == null)

                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(AMMMFR.Format(AMMMFR.SFxInvalidCallbackContractType, value.Name)));

                    isOneWay = true;
                }

                callbackContractType = value;
            }
        }

        /// <summary>
        /// Gets the name of a method defined on the callback contract (specified by <see cref="CallbackContractType"/>)
        /// that specifies the action to use for the callback. 
        /// </summary>
        /// <remarks>
        /// Required if there is more than one method of the callback contract decorated with <see cref="PostMessageActionAttribute"/> 
        /// that is constrained as one-way.
        /// </remarks>
        public string CallbackContractMethodName { get; set; }

        protected override void Validate(OperationDescription operationDescription)
        {
        }
    }
}
