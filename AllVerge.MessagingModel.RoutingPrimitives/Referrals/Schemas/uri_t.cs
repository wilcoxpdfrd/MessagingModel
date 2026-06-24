using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public partial class uri_t : IEquatable<uri_t>
    {
        public uri_t()
        {
        }

        public uri_t(string value)
        {
            this.Value = value;
        }

        public uri_t(uri_t value)
        {
            this.Value = value.Value;

            this.AnyAttr = value.AnyAttr.Select(a => (XmlAttribute)a.Clone()).ToArray();
        }

        public bool Equals(uri_t otherUri)
        {
            return !String.IsNullOrWhiteSpace(this.Value) && !String.IsNullOrWhiteSpace(otherUri.Value) && this.Value.Equals(otherUri.Value, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool Equals(String actionUri)
        {
            if (String.IsNullOrWhiteSpace(this.Value) || String.IsNullOrWhiteSpace(actionUri))
                return false;
            if (this.Value.Contains('#'))
            {
                if (actionUri.Contains('#'))
                    return this.Value == actionUri;
                else
                    return this.Value.Replace('#', '/') == actionUri;
            }
            return this.Value.Equals(actionUri, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool EqualsStartOfAction(String actionUri)
        {
            if (String.IsNullOrWhiteSpace(this.Value) || String.IsNullOrWhiteSpace(actionUri))
                return false;
            if (this.Value.Contains('#'))
            {
                if (actionUri.Contains('#'))
                    return actionUri.StartsWith(this.Value, StringComparison.InvariantCultureIgnoreCase);
                else
                    return actionUri.StartsWith(this.Value.Replace('#', '/'), StringComparison.InvariantCultureIgnoreCase);
            }
            return actionUri.StartsWith(this.Value, StringComparison.InvariantCultureIgnoreCase);
        }

        public uri_t Clone()
        {
            return new uri_t(this);
        }
    }
}
