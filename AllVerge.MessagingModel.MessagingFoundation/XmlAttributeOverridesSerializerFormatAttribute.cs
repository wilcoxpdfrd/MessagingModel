using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    [AttributeUsage(ServiceModelAttributeTargets.ServiceContract | ServiceModelAttributeTargets.OperationContract, Inherited = false, AllowMultiple = false)]
    public sealed class XmlAttributeOverridesSerializerFormatAttribute : Attribute
    {
        public XmlAttributeOverridesSerializerFormatAttribute()
        {
            this.DataContractFormatAttribute = new DataContractFormatAttribute();
        }

        public OperationFormatStyle Style
        {
            get { return DataContractFormatAttribute.Style; }
            set
            {
                DataContractFormatAttribute.Style = value;
            }
        }

        public Type XmlAttributeOverridesType {
            get
            {
                return XmlAttributeOverrides?.GetType();
            }
            set
            {
                ValidateXmlAttributeOverridesType(value);

                this.XmlAttributeOverrides = (XmlAttributeOverrides)Activator.CreateInstance(value);
            }
        }

        internal DataContractFormatAttribute DataContractFormatAttribute { get; }

        internal XmlAttributeOverrides XmlAttributeOverrides { get; private set; }

        private void ValidateXmlAttributeOverridesType(Type xmlAttributeOverridesType)
        {
            if (xmlAttributeOverridesType == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(xmlAttributeOverridesType)));

            if (!typeof(XmlAttributeOverrides).IsAssignableFrom(xmlAttributeOverridesType))

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException($"Parameter is not a type that derives from ${nameof(XmlAttributeOverrides)}", nameof(xmlAttributeOverridesType)));
        }
    }
}
