using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    /// <summary>
    /// Indicates the decorated method defines the action of a resource endpoint in a service contract.  
    /// The action can induce a state change of the resource (repeating the action can have unique side effects).
    /// Provides for specifiying a template for the resource action endpoint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class PotentResourceActionTemplateAttribute : ResourceActionTemplateAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PotentResourceActionTemplateAttribute" /> class with the given <paramref name="methodName"/>, <paramref name="resourceAction"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="resourceAction"></param>
        /// <param name="template"></param>
        protected PotentResourceActionTemplateAttribute(String methodName, String resourceAction, String template) : 
            base(methodName, resourceAction, template) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PotentResourceActionTemplateAttribute" /> class with the given <paramref name="resourceAction"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="resourceAction"></param>
        /// <param name="template"></param>
        protected PotentResourceActionTemplateAttribute(String resourceAction, String template) :
            base(resourceAction, template)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PotentResourceActionTemplateAttribute" /> class with the given <paramref name="resourceAction"/>.
        /// </summary>
        /// <param name="resourceAction"></param>
        protected PotentResourceActionTemplateAttribute(String resourceAction) :
            base(resourceAction)
        { }
    }
}
