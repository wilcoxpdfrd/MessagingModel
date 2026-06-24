using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

    /// <summary>Indicates the decorated method defines a OPTIONS resource endpoint action in a service contract.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OptionsResourceActionAttribute : NilpotentResourceActionTemplateAttribute
    {
        public static readonly string ACTION_NAME = ResourceActions.OPTIONS;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsResourceActionAttribute" /> class with the given <paramref name="methodName"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="template"></param>
        public OptionsResourceActionAttribute(string methodName, string template) : base(methodName, ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsResourceActionAttribute" /> class with the given <paramref name="template"/>.
        /// </summary>
        /// <param name="template"></param>
        public OptionsResourceActionAttribute(string template) : base(ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsResourceActionAttribute" /> class with the given <paramref name="template"/>.
        /// </summary>
        public OptionsResourceActionAttribute() : base(ACTION_NAME)
        {
        }
    }
}
