using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [XmlInclude(typeof(ref_t))]
    public partial class registration_t 
    {
        public const string TYPE_NAME = "schemas.xmlsoap.org.ws._2001._10.referral.registration_t";

        public registration_t() { }

        public registration_t(ref_t @ref) { this.@ref = @ref; }

        public uri_t[] GetInvalidating()
        {
            List<uri_t> invalidating = new List<uri_t>();

            if (this.@ref != null && this.@ref.@if != null && this.@ref.@if.invalidates != null)
            {
                foreach (uri_t refId in this.@ref.@if.invalidates.rid)
                {
                    invalidating.Add(refId);
                }
            }

            return invalidating.ToArray();
        }

        public bool HasRouting()
        {
            return (this.@ref != null && @ref.HasRouting());
        }
    }
}
