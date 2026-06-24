using System;

using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MarkupPrimitives.Xml.Serialization
{
    /// <summary>
    /// Specifies a method with which to obtain an <see cref="XmlAttributeOverridesProvider"/> instance (see remarks).
    /// </summary>
    /// <remarks>
    /// An <see cref="XmlAttributeOverridesProvider"/> can be cast to a new instance of a <see cref="XmlAttributeOverrides"/> 
    /// type to pass as an argument when constructing an <see cref="XmlSerializer"/> for serializing or deserializing a 
    /// given type.  The provider can therefore be provisioned with a set of standard overrides, and each instance 
    /// of an  <see cref="XmlAttributeOverrides"/> obtained from it via a cast can be modified without changing the 
    /// underlying provider.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class XmlAttributeOverridesProviderAttribute : Attribute
    {
        private string _methodName;

        public string MethodName => _methodName;

        private XmlAttributeOverridesProviderAttribute()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        public XmlAttributeOverridesProviderAttribute(string methodName)
        {
            _methodName = methodName;
        }
    }
}
