using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [XmlInclude(typeof(uri_t))]
    public partial class for_t
    {
    }
}
