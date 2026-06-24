using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection;
using System.Text;

using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes
{
    /// <summary>
    /// Indicates the decorated method defines a POST resource endpoint action in a service contract.
    /// See remarks regarding handling Form Url-Encoded POST messages.
    /// </summary>
    /// <remarks>
    /// The <see cref="Http"/> runtime maps <see cref="AllVerge.Core.ServiceModel.Channels.MessageFormats.FormUrlEncoded"/> 
    /// parameters as "querystring parameters" for the purpose of template matching and binding the form parameters to the arguments of the method this attribute is applied to.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PostResourceActionAttribute : PotentResourceActionTemplateAttribute
    {
        public static readonly string ACTION_NAME = ResourceActions.POST;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostResourceActionAttribute" /> class with the given <paramref name="methodName"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="template"></param>
        public PostResourceActionAttribute(string methodName, string template) : base(methodName, ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostResourceActionAttribute" /> class with the given <paramref name="template"/>.
        /// </summary>
        /// <param name="template"></param>
        public PostResourceActionAttribute(string template) : base(ACTION_NAME, template)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostResourceActionAttribute" /> class.
        /// </summary>
        public PostResourceActionAttribute() : base(ACTION_NAME)
        {
        }
    }
}
