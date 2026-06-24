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
    public partial class registrationResponse_t 
    {
        public registrationResponse_t()
        {
        }

        public registrationResponse_t(XmlElement faultElement)
        {
            Add(faultElement);
        }

        public void Add(XmlElement faultElement)
        {
            if (faultElement is duplicate_t || faultElement is notInvalidated_t)
            {
                this.Any = new XmlElement[] { faultElement };
            }
            else
                throw new ArgumentException(String.Format("Fault element must be either of type {0} or {1}.", typeof(duplicate_t).FullName, typeof(notInvalidated_t).FullName), "faultElement");
        }
    }
}
