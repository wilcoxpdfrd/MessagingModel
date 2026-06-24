using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace schemas.xmlsoap.org.ws._2001._10.referral
{
    using AllVerge.PolicyPrimitives;
    using AllVerge.SystemPrimitives.Reflection;
    using www.w3.org.ns.ws_policy;

    public static class ReferralsExtensions
    {
        public static void SetPolicyReferralForAction(this Policy policy, String action, String messageStyle, String interactionStyle, String matchType, bool invalidates, ulong ttl, int concurrency, int sla, int timeout, params String[] vias)
        {
            match match;

            if (!Enum.TryParse<match>(matchType, out match))

                throw new ArgumentOutOfRangeException(nameof(matchType));

            ref_t @ref = 
                new ref_t(
                    ttl,
                    invalidates,
                    match,
                    action,
                    policy.Document.CreateValueAttribute(ref_t.REF_EXT_ATTR_INTERACTION_MESSAGE_STYLE, ref_t.REF_EXT_NAMESPACE_URI, messageStyle +'/' + interactionStyle),
                    policy.Document.CreateValueAttribute(ref_t.REF_EXT_ATTR_MAX_CONCURRENCY, ref_t.REF_EXT_NAMESPACE_URI, concurrency),
                    policy.Document.CreateValueAttribute(ref_t.REF_EXT_ATTR_DISPATCH_SLA, ref_t.REF_EXT_NAMESPACE_URI, sla),
                    policy.Document.CreateValueAttribute(ref_t.REF_EXT_ATTR_DISPATCH_TIMEOUT, ref_t.REF_EXT_NAMESPACE_URI, timeout),
                    policy.Document.CreateValueAttribute(ref_t.REF_EXT_ATTR_CREATED, ref_t.REF_EXT_NAMESPACE_URI, DateTime.Now),
                    vias);

            registration_t registration = new registration_t(@ref);

            policy.AddItem(new OperatorContentItem(ItemsChoiceType.Item, registration));
        }

        public static IEnumerable<registration_t> FilterPolicyRegistrationsForAction(this IEnumerable<registration_t> policyRegistrations, string action)
        {
            List<registration_t> registrations = new List<registration_t>();

            foreach (registration_t registration in policyRegistrations)
            {
                ref_t referral = registration.@ref;

                uri_t[] exacts = referral.@for.exact.EnsureNotNull();

                foreach (uri_t exact in exacts)
                {
                    if (exact.Value == action)
                    {
                        registrations.Add(registration);
                    }
                }

                uri_t[] prefixes = referral.@for.prefix.EnsureNotNull();

                foreach (uri_t prefix in prefixes)
                {
                    if (action.StartsWith(prefix.Value))
                    {
                        if (!registrations.Contains(registration))

                            registrations.Add(registration);
                    }
                }
            }

            return registrations;
        }

        public static void PatchPolicyRegistrations(this Policy policy, string referralId, string action, string matchType, bool invalidates, ulong ttl, int concurrency, int sla, int timeout, params String[] vias)
        {
            match match;

            if (!Enum.TryParse<match>(matchType, out match))

                throw new ArgumentOutOfRangeException(nameof(matchType));

            foreach (registration_t registration in policy[ItemsChoiceType.Item].OfType<registration_t>().Where(r => { uri_t refId; if (r.@ref.TryGetRefId(out refId)) return refId.Value == referralId; else return false; }))
            {
                if (registration.@ref.refId.Value == referralId)
                {
                    if (match == match.exact)
                    {
                        if (registration.@ref.@for.exact == null)
                        {
                            registration.@ref.@for.exact = new uri_t[] { new uri_t() { Value = registration.@ref.@for.prefix[0].Value } };
                            registration.@ref.@for.prefix = null;
                        }
                        else if (registration.@ref.@for.exact[0].Value != action)
                        {
                            registration.@ref.@for.exact = new uri_t[] { new uri_t() { Value = action } };
                            registration.@ref.@for.prefix = null;
                        }
                    }
                    else if (match == match.prefix)
                    {
                        if (registration.@ref.@for.prefix == null)
                        {
                            registration.@ref.@for.prefix = new uri_t[] { new uri_t() { Value = registration.@ref.@for.exact[0].Value } };
                            registration.@ref.@for.exact = null;
                        }
                        else if (registration.@ref.@for.prefix[0].Value != action)
                        {
                            registration.@ref.@for.prefix = new uri_t[] { new uri_t() { Value = action } };
                            registration.@ref.@for.exact = null;
                        }
                    }

                    if (registration.@ref.@if.ttl != null)
                    {
                        if (invalidates)
                        {
                            if (registration.@ref.@if.invalidates == null)

                                registration.@ref.@if.invalidates = new invalidates_t(registration.@ref.refId);

                            else if (registration.@ref.@if.ttl.Value != ttl)

                                registration.@ref.@if.ttl.Value = ttl;
                        }
                        else
                        {
                            if (registration.@ref.@if.invalidates != null)

                                registration.@ref.@if.invalidates = null;

                            if (ttl == 0)

                                registration.@ref.@if.ttl = null;

                            else if (registration.@ref.@if.ttl.Value != ttl)

                                registration.@ref.@if.ttl.Value = ttl;
                        }
                    }
                    else
                    {
                        if (invalidates)
                        {
                            if (ttl > 0)

                                registration.@ref.@if.ttl = new ttl_t(ttl);

                            registration.@ref.@if.invalidates = new invalidates_t(registration.@ref.refId);
                        }
                        else if (ttl > 0)
                        {
                            if (registration.@ref.@if.invalidates != null)

                                registration.@ref.@if.invalidates = null;

                            if (ttl > 0)

                                registration.@ref.@if = new if_t() { ttl = new ttl_t(ttl) };
                        }
                    }

                    if (registration.@ref.go != null)
                    {
                        registration.@ref.go = new go_t() { via = registration.@ref.go.via.Union(vias.Select(v => new uri_t() { Value = v })).ToArray() };
                    }
                    else
                    {
                        registration.@ref.go = new go_t() { via = vias.Select(v => new uri_t() { Value = v }).ToArray() };
                    }

                    registration.@ref.go.AnyAttr.PutAttribute(policy.Document.CreateValueAttribute(ref_t.REF_EXT_ATTR_MAX_CONCURRENCY, ref_t.REF_EXT_NAMESPACE_URI, concurrency));
                    registration.@ref.go.AnyAttr.PutAttribute(policy.Document.CreateValueAttribute(ref_t.REF_EXT_ATTR_DISPATCH_SLA, ref_t.REF_EXT_NAMESPACE_URI, sla));
                    registration.@ref.go.AnyAttr.PutAttribute(policy.Document.CreateValueAttribute(ref_t.REF_EXT_ATTR_DISPATCH_TIMEOUT, ref_t.REF_EXT_NAMESPACE_URI, timeout));
                }
            }
        }

        public static void InvalidatePolicyRegisteration(this Policy policy, string referralId)
        {
            foreach (registration_t registration in policy[ItemsChoiceType.Item].OfType<registration_t>().Where(r => { uri_t refId; if (r.@ref.TryGetRefId(out refId)) return refId.Value == referralId; else return false; }))
            {
                registration.@ref.CheckValid(true);

                registration_t invalidatingRegistration = 
                    new registration_t(
                        new ref_t(
                            referralId, 
                            policy.Document.CreateValueAttribute(ref_t.REF_EXT_ATTR_CREATED, ref_t.REF_EXT_NAMESPACE_URI, DateTime.Now)));

                invalidatingRegistration.@ref.CheckValid(false);

                policy.AddItem(new OperatorContentItem(ItemsChoiceType.Item, invalidatingRegistration));
            }
        }

        public static void DeletePolicyRegisteration(this Policy policy, string referralId)
        {
            foreach (registration_t registration in policy[ItemsChoiceType.Item].OfType<registration_t>().Where(r => { uri_t refId; if (r.@ref.TryGetRefId(out refId)) return refId.Value == referralId; else return false; }))
            {
                registration.@ref.CheckValid(true);

                policy.RemoveItem(new OperatorContentItem(ItemsChoiceType.Item, registration));
            }
        }

        public static uri_t[] EnsureNotNull(this uri_t[] uris)
        {
            if (uris == null)
                return new uri_t[0];
            return uris;
        }

        public static void AddAttribute(this ref_t @ref, XmlAttribute xmlAttribute, out int addedIndex)
        {
            @ref.AnyAttr = @ref.AnyAttr.AddAttribute(xmlAttribute, out addedIndex);
        }

        public static void PutAttribute(this ref_t @ref, XmlAttribute xmlAttribute)
        {
            @ref.AnyAttr = @ref.AnyAttr.PutAttribute(xmlAttribute);
        }

        public static void AddAttribute(this uri_t action, XmlAttribute xmlAttribute, out int addedIndex)
        {
            action.AnyAttr = action.AnyAttr.AddAttribute(xmlAttribute, out addedIndex);
        }

        public static void PutAttribute(this uri_t action, XmlAttribute xmlAttribute)
        {
            action.AnyAttr = action.AnyAttr.PutAttribute(xmlAttribute);
        }

        /// <summary>
        /// Gets the value of the attribute identified by <paramref name="name"/> and <paramref name="namespaceUri"/> from <paramref name="action"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="namespaceUri"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetNamedAttributeValue<T>(this uri_t action, String name, String namespaceUri, T defaultValue)
        {
            return action.GetNamedAttributeValue(new XmlQualifiedName(name, namespaceUri), defaultValue);
        }

        /// <summary>
        /// Gets the value of the attribute identified by <paramref name="xmlQualifiedName"/> from <paramref name="action"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlQualifiedName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetNamedAttributeValue<T>(this uri_t action, XmlQualifiedName xmlQualifiedName, T defaultValue)
        {
            if (action.AnyAttr.TryGetNamedAttribute(xmlQualifiedName, out XmlAttribute namedAttribute, out int numberAttributes))
            {
                if (namedAttribute.Value.TryParseString<T>(out T value))

                    return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the value of the attribute identified by <paramref name="name"/> and <paramref name="namespaceUri"/> from <paramref name="go"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <param name="name"></param>
        /// <param name="namespaceUri"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetNamedAttributeValue<T>(this go_t go, String name, String namespaceUri, T defaultValue)
        {
            return go.GetNamedAttributeValue(new XmlQualifiedName(name, namespaceUri), defaultValue);
        }

        /// <summary>
        /// Gets the value of the attribute identified by <paramref name="xmlQualifiedName"/> from <paramref name="go"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <param name="xmlQualifiedName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetNamedAttributeValue<T>(this go_t go, XmlQualifiedName xmlQualifiedName, T defaultValue)
        {
            if (go.AnyAttr.TryGetNamedAttribute(xmlQualifiedName, out XmlAttribute namedAttribute, out int numberAttributes))
            {
                if (namedAttribute.Value.TryParseString<T>(out T value))

                    return value;
            }

            return defaultValue;
        }

        private static bool TryGetNamedAttribute(this XmlAttribute[] anyAttr, XmlQualifiedName xmlQualifiedName, out XmlAttribute namedAttribute, out int numberAttributes)
        {
            if (anyAttr != null)
            {
                namedAttribute = anyAttr.FirstOrDefault(a => a.LocalName == xmlQualifiedName.Name && a.NamespaceURI == xmlQualifiedName.Namespace);

                numberAttributes = anyAttr.Length;
            }
            else
            {
                namedAttribute = null;

                numberAttributes = 0;
            }

            return namedAttribute != null;
        }
    }
}
