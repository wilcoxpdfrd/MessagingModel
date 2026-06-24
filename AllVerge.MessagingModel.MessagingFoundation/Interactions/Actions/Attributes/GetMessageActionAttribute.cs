using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes
{
    /// <summary>Indicates that a method defines a GET Resource Action that is part of a service contract in an application.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class GetMessageActionAttribute : MessageActionAttribute
    {
        public static readonly string ACTION_NAME = ResourceActions.GET;

        /// <summary>Initializes a new instance of the <see cref="GetMessageActionAttribute" /> class.</summary>
        public GetMessageActionAttribute() : base(ACTION_NAME)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="GetMessageActionAttribute" /> class with the given <paramref name="messageAction"/>.</summary>
        public GetMessageActionAttribute(string messageAction) : base(ACTION_NAME, messageAction)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="GetMessageActionAttribute" /> class with the given <paramref name="methodName"/> and <paramref name="messageAction"/>.</summary>
        public GetMessageActionAttribute(string methodName, string messageAction) : base(methodName, ACTION_NAME, messageAction)
        {
        }

        public override bool GetIsOneWay() => false;

        protected override void Validate(OperationDescription operationDescription)
        {
        }

        protected override void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            base.ApplyClientBehavior(operationDescription, clientOperation);
        }
    }
}
