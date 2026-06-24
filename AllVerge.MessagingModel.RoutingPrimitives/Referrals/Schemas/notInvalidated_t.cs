using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    public partial class notInvalidated_t : XmlElement
    {
        private static readonly XmlDocument doc;
        private static readonly string localName;
        private static readonly string namespaceURI;

        static notInvalidated_t()
        {
            Type type = typeof(notInvalidated_t);

            XmlRootAttribute rootAttr = (XmlRootAttribute)type.GetCustomAttributes(typeof(XmlRootAttribute), false).FirstOrDefault();

            doc = new XmlDocument();

            localName = rootAttr.ElementName;
            namespaceURI = rootAttr.Namespace;
        }

        public notInvalidated_t() : base(null, localName, namespaceURI, doc) { }

        public notInvalidated_t(params uri_t[] rid) : base(null, localName, namespaceURI, doc)
        {
            this.rid = rid;
        }

        public static bool IsInstance(object obj)
        {
            if (obj is XmlElement)
            {
                XmlElement el = (XmlElement)obj;

                return el.LocalName == localName && el.NamespaceURI == namespaceURI;
            }

            return false;
        }
    }
}
