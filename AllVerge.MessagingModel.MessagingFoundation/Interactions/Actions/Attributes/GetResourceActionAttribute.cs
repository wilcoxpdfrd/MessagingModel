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
    /// <summary>Indicates that a method defines a GET operation that is part of a service contract.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class GetResourceActionAttribute : NilpotentResourceActionTemplateAttribute
    {
        public static readonly string ACTION_NAME = ResourceActions.GET;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetResourceActionAttribute" /> class with the given <paramref name="methodName"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="template"></param>
        public GetResourceActionAttribute(string methodName, string template) : base(methodName, ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetResourceActionAttribute" /> class with the given <paramref name="template"/>.
        /// </summary>
        /// <param name="template"></param>
        public GetResourceActionAttribute(string template) : base(ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetResourceActionAttribute" /> class.
        /// </summary>
        /// <param name="template"></param>
        public GetResourceActionAttribute() : base(ACTION_NAME)
        {
        }

        protected override void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            base.ApplyClientBehavior(operationDescription, clientOperation);
        }
    }
}
