using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.Markup.Xml.Schema
{
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.SystemPrimitives.Net;

    public class SchemaInclude
    {
        private string baseLocation;
        private string location;
        private Uri importsCachePathUri;
        private IEnumerable<XmlAttribute> anyAttr;
        IEnumerable<SchemaDoc> documentation;

        public SchemaInclude(string baseLocation, string location, Uri importsCachePathUri = null)
        {
            this.baseLocation = baseLocation;
            this.location = location;
            this.importsCachePathUri = importsCachePathUri;
            this.anyAttr = CollectionUtils.ToEnumerable<XmlAttribute>();
            this.documentation = CollectionUtils.ToEnumerable<SchemaDoc>();
        }

        public SchemaInclude(string baseLocation, string location, XmlAttribute[] anyAttr, IEnumerable<SchemaDoc> documentation)
        {
            this.baseLocation = baseLocation;
            this.location = location;
            this.anyAttr = anyAttr;
            this.documentation = documentation;
        }

        public SchemaDoc[] Documentation
        {
            get
            {
                return documentation.ToArray();
            }
        }

        public IEnumerable<XmlAttribute> AnyAttr
        {
            get
            {
                return anyAttr;
            }
        }

        public Uri AbsoluteUri
        {
            get
            {
                Uri absoluteUri;

                if (UriUtils.TryCreateAbsoluteUri(this.location, this.baseLocation, out absoluteUri))

                    return absoluteUri;

                throw new UriFormatException("Cannot resolve path to included schema.");
            }
        }

        public Uri ImportsCachePathUri
        {
            get
            {
                return importsCachePathUri;
            }
        }
    }
}
