using System;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes
{
    /// <summary>Indicates the decorated method defines a DELETE resource endpoint action in a service contract.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DeleteResourceActionAttribute : IdempotentResourceActionTemplateAttribute
    {
        public static readonly string ACTION_NAME = ResourceActions.DELETE;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteResourceActionAttribute" /> class with the given <paramref name="methodName"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="template"></param>
        public DeleteResourceActionAttribute(string methodName, string template) : 
            base(methodName, ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteResourceActionAttribute" /> class with the given <paramref name="template"/>.
        /// </summary>
        /// <param name="template"></param>
        public DeleteResourceActionAttribute(string template) : base(ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteResourceActionAttribute" /> class.
        /// </summary>
        /// <param name="template"></param>
        public DeleteResourceActionAttribute() : base(ACTION_NAME)
        {
        }

        protected override void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            base.ApplyClientBehavior(operationDescription, clientOperation);
        }
    }
}
