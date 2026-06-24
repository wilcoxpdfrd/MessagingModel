using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    using AllVerge.SystemPrimitives.Collections;

    using www.w3.org.ns.ws_policy;

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [XmlInclude(typeof(ref_t))]
    public partial class referrals_t
    {
        class Referrals : 
            KeyedCollection<uri_t, (ref_t Referral, Policy Policy)>
        {
            public Referrals()
            {
            }

            public Referrals(ref_t[] refs)
            {
                foreach (ref_t @ref in refs)
                    
                    this.Add((@ref, null));
            }

            protected override uri_t GetKeyForItem((ref_t Referral, Policy Policy) item)
            {
                return item.Referral.refId;
            }
        }

        Referrals items = null;

        private const string EMPTY_ARGUMENT_OR_PROPERTY = "Argument {0} cannot be null or not valued.{1}";
        private const string NOT_EMPTY_ARGUMENT_OR_PROPERTY = "Argument {0} must not be valued.{1}";

        public ref_t this[uri_t refId]
        {
            get
            {
                if (refId == null)

                    throw new ArgumentNullException("refId");

                if (this.items != null && this.items.Contains(refId))
                
                    return this.items[refId].Referral;

                return null;
            }
        }

        public void Add(ref_t @ref, Policy policy)
        {
            if (@ref.refId != null && !String.IsNullOrWhiteSpace(@ref.refId.Value))

                throw NotEmptyArgumentOrPropertyException(nameof(@ref.refId), "Please use Put with this value.");

            if (@ref.@for == null)

                throw EmptyArgumentOrPropertyException(nameof(@ref.@for));

            if (!@ref.HasRouting())

                throw new ArgumentException("At least one of prefix or exact is required to have at least one valued element.", "@ref.@for.prefix | @ref.@for.exact");

            if (@ref.refId == null)

                @ref.refId = new uri_t(Guid.NewGuid().ToString());

            if (this.items == null)

                this.items = new Referrals();

            this.items.Add((@ref, policy));

            this.RefreshItems();
        }

        public void Put(ref_t @ref, Policy policy, out IEnumerable<uri_t> excluded_prefixes, out IEnumerable<uri_t> new_prefixes, out IEnumerable<uri_t> excluded_exacts, out IEnumerable<uri_t> new_exacts)
        { 
            if (@ref.refId == null || String.IsNullOrWhiteSpace(@ref.refId.Value))

                throw EmptyArgumentOrPropertyException(nameof(@ref.@for), "  Please use the Add operation.");

            if (@ref.@for == null)

                throw EmptyArgumentOrPropertyException(nameof(@ref.@for));

            if (!@ref.HasRouting())

                throw EmptyArgumentOrPropertyException(nameof(@ref.@for), "  At least one of prefix or exact is required to have at least one valued element.");

            ref_t this_ref = this[@ref.refId];

            if (this_ref != null)
            {
                int length;

                excluded_prefixes = this_ref.@for.prefix.FindComplement(@ref.@for.prefix);

                new_prefixes = @ref.@for.prefix.FindComplement(this_ref.@for.prefix);

                excluded_exacts = this_ref.@for.exact.FindComplement(@ref.@for.exact);

                new_exacts = @ref.@for.exact.FindComplement(this_ref.@for.exact);

                length = @ref.@for.prefix.Length;

                this_ref.@for.prefix = new uri_t[length];

                Array.Copy(@ref.@for.prefix, this_ref.@for.prefix, length);

                length = @ref.@for.exact.Length;

                this_ref.@for.exact = new uri_t[length];

                Array.Copy(@ref.@for.exact, this_ref.@for.exact, length);

                length = @ref.go.via.Length;

                this_ref.go.via = new uri_t[length];

                Array.Copy(@ref.go.via, this_ref.go.via, length);

                this_ref.@if.ttl = @ref.@if.ttl;
            }
            else
            {
                if (this.items == null)

                    this.items = new Referrals();

                this.items.Add((@ref, policy));

                this.RefreshItems();

                if (@ref.@for.prefix == null)
                {
                    excluded_prefixes = new uri_t[0];

                    new_prefixes = new uri_t[0];
                }
                else
                {
                    excluded_prefixes = @ref.@for.prefix.Where(p => false);

                    new_prefixes = @ref.@for.prefix.Where(p => true);
                }

                if (@ref.@for.exact == null)
                {
                    excluded_exacts = new uri_t[0];

                    new_exacts = new uri_t[0];
                }
                else
                {
                    excluded_exacts = @ref.@for.exact.Where(p => false);

                    new_exacts = @ref.@for.exact.Where(p => true);
                }
            }
        }

        public bool Remove(ref_t @ref)
        {
            if (@ref.refId == null || string.IsNullOrWhiteSpace(@ref.refId.Value))

                throw EmptyArgumentOrPropertyException(nameof(@ref.refId));

            if (this.items != null && this.items.Contains(@ref.refId))
            {
                this.items.Remove(@ref.refId);

                this.RefreshItems();

                return true;
            }

            return false;
        }

        public (ref_t Referral, Policy Policy)[] GetReferralsFor(string groupUri, string action, string messageStyle)
        {
            List<(ref_t Referral, Policy Policy)> referrals = new List<(ref_t Referral, Policy Policy)>();

            if (this.items != null)
            {
                string groupName = groupUri == null ? null : new Uri(groupUri).AbsolutePath;

                foreach ((ref_t Referral, Policy Policy) item in this.items)
                {
                    if (groupUri == null || item.Policy.TryGetGroupAttributeValue(out string groupAttributeValue) && groupAttributeValue == groupName)
                    {
                        if ((item.Referral.@for.exact != null && item.Referral.@for.exact.Any(e => e.Equals(action) && e.AnyAttr.HasAttributeValue(ref_t.REF_EXT_ATTR_INTERACTION_MESSAGE_STYLE, ref_t.REF_EXT_NAMESPACE_URI, messageStyle)) ||
                            (item.Referral.@for.prefix != null && item.Referral.@for.prefix.Any(p => p.EqualsStartOfAction(action) && p.AnyAttr.HasAttributeValue(ref_t.REF_EXT_ATTR_INTERACTION_MESSAGE_STYLE, ref_t.REF_EXT_NAMESPACE_URI, messageStyle)))))

                            referrals.Add(item);
                    }
                }
            }

            return referrals.ToArray();
        }
        private void RefreshItems()
        {
            this.@ref = this.items.Select(i => i.Referral).ToArray();
        }

        private static ArgumentException NotEmptyArgumentOrPropertyException(String propertyName, String instructions = null)
        {
            return new ArgumentException(NOT_EMPTY_ARGUMENT_OR_PROPERTY.FormatString(propertyName, instructions), propertyName);
        }

        private static ArgumentException EmptyArgumentOrPropertyException(string propertyName, String instructions = null)
        {
            throw new ArgumentException(EMPTY_ARGUMENT_OR_PROPERTY.FormatString(propertyName, instructions), propertyName);
        }

    }
}
