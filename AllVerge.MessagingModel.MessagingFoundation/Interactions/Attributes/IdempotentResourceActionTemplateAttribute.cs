using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    /// <summary>
    /// Indicates the decorated method defines the action of a resource endpoint in a service contract.  
    /// The action will induce a state change of the resource at most once (repeating the action will have no effect - no side effects).
    /// Provides for specifiying a template for the resource action endpoint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class IdempotentResourceActionTemplateAttribute : ResourceActionTemplateAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceActionTemplateAttribute" /> classwith the given <paramref name="methodName"/>, <paramref name="resourceAction"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="resourceAction"></param>
        /// <param name="template"></param>
        protected IdempotentResourceActionTemplateAttribute(String methodName, String resourceAction, String template) : 
            base(methodName, resourceAction, template) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceActionTemplateAttribute" /> classwith the given <paramref name="resourceAction"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="resourceAction"></param>
        /// <param name="template"></param>
        protected IdempotentResourceActionTemplateAttribute(String resourceAction, String template) :
            base(resourceAction, template)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceActionTemplateAttribute" /> classwith the given <paramref name="resourceAction"/>.
        /// </summary>
        /// <param name="resourceAction"></param>
        protected IdempotentResourceActionTemplateAttribute(String resourceAction) :
            base(resourceAction)
        { }
    }
}
