using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;
using System.Text;

using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes
{
    /// <summary>Indicates the decorated method defines a PUT resource endpoint action in a service contract.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PutResourceActionAttribute : IdempotentResourceActionTemplateAttribute
    {
        public static readonly string ACTION_NAME = ResourceActions.PUT;

        /// <summary>
        /// Initializes a new instance of the <see cref="PutResourceActionAttribute" /> class with the given <paramref name="methodName"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="template"></param>
        public PutResourceActionAttribute(string methodName, string template) : base(methodName, ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PutResourceActionAttribute" /> class with the given <paramref name="template"/>.
        /// </summary>
        /// <param name="template"></param>
        public PutResourceActionAttribute(string template) : base(ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PutResourceActionAttribute" /> class.
        /// </summary>
        public PutResourceActionAttribute() : base(ACTION_NAME)
        {
        }
    }
}
