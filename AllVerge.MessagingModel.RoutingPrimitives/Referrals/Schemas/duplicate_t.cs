using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public partial class duplicate_t : XmlElement
    {
        private static readonly XmlDocument doc;
        private static readonly string typeName;
        private static readonly string typeNamespace;

        static duplicate_t()
        {
            Type type = typeof(duplicate_t);

            XmlTypeAttribute typeAttr = (XmlTypeAttribute)type.GetCustomAttributes(typeof(XmlTypeAttribute), false).FirstOrDefault();

            doc = new XmlDocument();

            typeName = typeAttr.TypeName;
            typeNamespace = typeAttr.Namespace;
        }

        public duplicate_t() : base(null, typeName, typeNamespace, doc)
        {
        }
    }
}
