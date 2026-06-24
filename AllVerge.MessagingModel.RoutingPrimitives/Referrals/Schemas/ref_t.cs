using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml;
using System.Xml.Serialization;
using AllVerge.PolicyPrimitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    public enum match
    {
        exact,
        prefix
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [XmlInclude(typeof(for_t))]
    [XmlInclude(typeof(if_t))]
    [XmlInclude(typeof(go_t))]
    [XmlInclude(typeof(uri_t))]
    [XmlInclude(typeof(desc_t))]
    public partial class ref_t
    {
        public const string REF_EXT_NAMESPACE_URI = "http://allverge/connector/referral/";

        public const string REF_EXT_ATTR_CREATED = "created";
        public const string REF_EXT_ATTR_INTERACTION_MESSAGE_STYLE = "messageStyle";
        public const string REF_EXT_ATTR_MAX_CONCURRENCY = "maxConcurrentCalls";
        public const string REF_EXT_ATTR_DISPATCH_SLA = "sla";
        public const string REF_EXT_ATTR_DISPATCH_TIMEOUT = "timeout";

        public const string VIA_EXT_ATTR_DISPATCH_URL = "dispatchUrl";
        public const string REF_VIA_DISPATCH_BASE_URL = "zeromq.tcp://dispatch/via/";

        public const string REF_VIA_DISPATCH_TOKEN_URI = REF_EXT_NAMESPACE_URI + "dispatch/";
        public const string REF_VIA_DISPATCH_PACKAGE_TEMPLATE = "{packageId}/{version}/{framework}";

        /// <summary>
        /// For deserialization only.
        /// </summary>
        public ref_t() {}

        public ref_t(ulong ttl, bool invalidates, match match, string @for, XmlAttribute messageStyleAttribute, XmlAttribute concurrencyAttribute, XmlAttribute slaAttribute, XmlAttribute timeoutAttribute, XmlAttribute createdAttribute, params string[] vias)
        {
            if (@for == null)

                throw new ArgumentNullException(nameof(@for));

            if (vias == null)

                throw new ArgumentNullException(nameof(vias));

            if (vias.Length == 0)

                throw new ArgumentException("At least one value of the parameter is required.", nameof(vias));

            this.AddAttribute(createdAttribute, out int createdIndex);
            
            this.refId = new uri_t(Guid.NewGuid().ToString());

            if (match == match.exact)
            {
                this.@for = new for_t()
                {
                    exact = new uri_t[] { new uri_t() { Value = @for, AnyAttr = new XmlAttribute[] { messageStyleAttribute } } }
                };
            }
            else
            {
                this.@for = new for_t()
                {
                    prefix = new uri_t[] { new uri_t() { Value = @for, AnyAttr = new XmlAttribute[] { messageStyleAttribute } } }
                };
            }
            if (ttl > 0)
                this.@if = new if_t() { ttl = new ttl_t(ttl) };
            else
                this.@if = new if_t();
            if (invalidates)
            {
                this.@if.invalidates = new invalidates_t(this.refId);
            }
            this.go = new go_t() { via = vias.Select(v => new uri_t() { Value = v }).ToArray(), AnyAttr = new XmlAttribute[] { concurrencyAttribute, slaAttribute, timeoutAttribute } };
            this.desc = null;
            this.refId = new uri_t() { Value = null };
        }

        public ref_t(String invalidateRefId, XmlAttribute createdAttribute) 
        {
            if (invalidateRefId == null)

                throw new ArgumentNullException("invalidateRefId");

            this.AddAttribute(createdAttribute, out int createdIndex);
            this.@for = new for_t();
            this.@if = new if_t() { invalidates = new invalidates_t(new uri_t() { Value = invalidateRefId }) };
            this.go = new go_t() { via = new uri_t[0] };
            this.desc = null;
            this.refId = new uri_t() { Value = null };
        }

        /// <remarks/>
        [XmlIgnore]
        DateTime? Created
        {
            get
            {
                if (this.AnyAttr.TryGetAttribute(REF_EXT_ATTR_CREATED, ref_t.REF_EXT_NAMESPACE_URI, out XmlAttribute createdAttribute))

                    return DateTime.Parse(createdAttribute.InnerText);

                return null;
            }
        }

        public bool Expired
        {
            get
            {
                if (this.@if != null && this.@if.ttl != null && this.@if.ttl.Value > 0L && this.Created.HasValue)
                {
                    DateTime created = this.Created.Value;
                    ulong ttl = this.@if.ttl.Value;
                    double totalMilliseconds = DateTime.MaxValue.Subtract(created).TotalMilliseconds;
                    DateTime t = DateTime.MaxValue;
                    if (totalMilliseconds > (double)ttl)
                    {
                        t = created.AddMilliseconds((double)ttl);
                    }
                    return t < DateTime.UtcNow;
                }
                return false;
            }
        }

        public bool TryGetRefId(out uri_t refId)
        {
            if (this.refId == null || String.IsNullOrWhiteSpace(this.refId.Value))
            {
                refId = null;
                return false;
            }
            refId = this.refId;
            return true;
        }

        public bool HasRouting()
        {
            return this.@for != null && (HasRoutingValues(this.@for.exact) || HasRoutingValues(this.@for.prefix));
        }
        
        private static bool HasRoutingValues(uri_t[] elements)
        {
            return (elements != null && elements.Length > 0 && !elements.All(p => String.IsNullOrWhiteSpace(p.Value)));
        }

        public bool IsInvalidating()
        {
            return this.@if != null && this.@if.invalidates != null;
        }

        public void CheckValid(bool mustHaveRefId)
        {
            if (mustHaveRefId && (this.refId == null || String.IsNullOrWhiteSpace(this.refId.Value)))
            {
                throw new ReferralFormatException(
                    APPR.Format(
                        APPR.InvalidElementBadChild, 
                        new object[]
                        {
                            "ref",
                            "refId"
                        })
                    );
            }
            if (this.@for == null)
            {
                throw new ReferralFormatException(
                    this.refId,
                    APPR.Format(
                        APPR.InvalidElementBadChild, 
                        new object[]
                        {
                            "ref",
                            "for"
                        }
                    )
                );
            }
            if (this.@if == null)
            {
                throw new ReferralFormatException(
                    this.refId,
                    APPR.Format(
                        APPR.InvalidElementBadChild, 
                        new object[]
                        {
                            "ref",
                            "if"
                        }
                    )
                );
            }
            string[] schemes;
            if (null != this.@for.exact)

                schemes = this.@for.exact.Where(u => u.Value != null).Select(u => new Uri(u.Value).Scheme).ToArray();

            else if (null != this.@for.prefix)

                schemes = this.@for.prefix.Where(u => u.Value != null).Select(u => new Uri(u.Value).Scheme).ToArray();

            else if (null != this.@if.invalidates)

                schemes = new string[0];

            else

                throw new ReferralFormatException(this.refId, ReferralFormatException.BadMatchCombination);

            if (this.@if.invalidates != null)
            {
                foreach (uri_t refId in this.@if.invalidates.rid)
                {
                    if (this.refId.Equals(refId))
                    {
                        throw new ReferralFormatException(this.refId, ReferralFormatException.BadRidValue);
                    }
                }
            }
            if (this.@if.ttl != null && this.@if.ttl.Value < 0L)
            {
                throw new ReferralFormatException(this.refId, ReferralFormatException.BadTtlValue);
            }
            if (this.go == null)
            {
                throw new ReferralFormatException(
                    this.refId,
                    APPR.Format(
                        APPR.InvalidElementBadChild, 
                        new object[]
                        {
                            "ref",
                            "go"
                        }
                    )
                );
            }
            if (this.go.via != null)
            {
                foreach (uri_t via in this.go.via)
                {
                    if (null == via.Value)
                    {
                        throw new ReferralFormatException(this.refId, ReferralFormatException.BadViaValue);
                    }
                }
                if (schemes.Any(s1 => s1 == Uri.UriSchemeHttps && !this.go.via.Select(u => new Uri(u.Value).Scheme).Any(s2 => s2 == Uri.UriSchemeHttps)))

                    throw new ReferralFormatException(this.refId, ReferralFormatException.BadTransport);
            }
        }
    }
}
