using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class NilpotentResourceActionTemplateAttribute : ResourceActionTemplateAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NilpotentResourceActionTemplateAttribute" /> classwith the given <paramref name="methodName"/>, <paramref name="resourceAction"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="resourceAction"></param>
        /// <param name="template"></param>
        protected NilpotentResourceActionTemplateAttribute(String methodName, String resourceAction, String template) : 
            base(methodName, resourceAction, template) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="NilpotentResourceActionTemplateAttribute" /> classwith the given <paramref name="resourceAction"/> and <paramref name="template"/>.
        /// </summary>
        /// <param name="resourceAction"></param>
        /// <param name="template"></param>
        protected NilpotentResourceActionTemplateAttribute(String resourceAction, String template) :
            base(resourceAction, template)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="NilpotentResourceActionTemplateAttribute" /> classwith the given <paramref name="resourceAction"/>.
        /// </summary>
        /// <param name="resourceAction"></param>
        protected NilpotentResourceActionTemplateAttribute(String resourceAction) :
            base(resourceAction)
        { }
    }
}
