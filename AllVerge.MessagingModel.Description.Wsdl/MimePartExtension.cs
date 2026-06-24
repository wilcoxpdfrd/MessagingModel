using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web.Services.Configuration;
using System.Web.Services.Description;
using System.Xml.Serialization;

namespace AllVerge.Core.ServiceModel.Description.Wsdl
{
    [XmlFormatExtension("mimePart", MimeContentBinding.Namespace, typeof(MimePartCollection))]
    [XmlFormatExtensionPoint("Extensions")]
    public sealed class MimePartExtension : ServiceDescriptionFormatExtension
    {
        private MimePart mimePart;

        public MimePartExtension(MimePart mimePart)
        {
            if (mimePart == null)

                throw new ArgumentNullException("mimePart");

            this.mimePart = mimePart;
        }

        /// <summary>Gets the collection of MIME extensibility elements for the part of the <see cref="T:System.Web.Services.Description.MimeMultipartRelatedBinding" /> of which the <see cref="T:System.Web.Services.Description.MimePart" /> is a member.</summary>
        /// <returns>A <see cref="T:System.Web.Services.Description.ServiceDescriptionFormatExtensionCollection" />.</returns>
        [XmlIgnore]
        public ServiceDescriptionFormatExtensionCollection Extensions
        {
            get
            {
                return this.mimePart.Extensions;
            }
        }
    }
}
