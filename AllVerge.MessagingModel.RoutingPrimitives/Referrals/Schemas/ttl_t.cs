using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public partial class ttl_t
    {
        public ttl_t() { }

        public ttl_t(ulong ttl)
        {
            this.valueField = ttl;
        }
    }
}