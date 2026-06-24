using AllVerge.MessagingModel.MessagingFoundation.Channels;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    [AttributeUsage(ServiceModelAttributeTargets.ServiceContract | ServiceModelAttributeTargets.OperationContract, Inherited = false, AllowMultiple = false)]
    public sealed class ResourceTransferFormatSerializerAttribute : Attribute
    {
        public ResourceTransferFormatSerializerAttribute()
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

        internal MessageEncodingFormat Format { get; set; }

        internal DataContractFormatAttribute DataContractFormatAttribute { get; }
    }
}
