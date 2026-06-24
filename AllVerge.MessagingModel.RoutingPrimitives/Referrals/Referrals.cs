using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Xml;

using AllVerge.PolicyPrimitives;

namespace AllVerge.MessagingModel.RoutingPrimitives.Referrals
{
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions;

    using schemas.xmlsoap.org.ws._2001._10.referral;
    using www.w3.org.ns.ws_policy;

    public class Referrals : IReferrals
    {
        public static event Action<Referral, go_t> OnAddReferral;
        public static event Action<Referral> OnRemoveRefferal;

        private static ReaderWriterLock _lock = new ReaderWriterLock();
        private static referrals_t referralCache;

        static Referrals()
        {
            referralCache = new referrals_t();
        }

        public static registrationResponse_t Register(Policy policy, registration_t registration)
        {
            if (referralCache == null)
            {
                throw new InvalidOperationException(APPR.ReferralCacheNotConfigured);
            }

            if (policy == null)

                throw new ArgumentNullException("policy");

            if (registration == null)

                throw new ArgumentNullException("registration");

            ref_t @ref = registration.@ref;

            uri_t groupUri;

            if (policy.TryGetGroupAttributeValue(out string groupName))

                groupUri = new uri_t($"urn:{groupName}");

            else

                groupUri = null;

            if (@ref.HasRouting())
            {
                if (@ref.TryGetRefId(out uri_t refId))
                {
                    @ref.CheckValid(true);

                    ref_t target;

                    if (TryGetReferralFromCache(refId, out target) && !target.Expired)
                    {
                        //duplicate
                        throw new ArgumentException(
                            APPR.Format(
                                AMMRPR.ReferralAlreadyRegistered, 
                                new object[] { refId.Value }
                            ),
                            "registration.@ref"
                        );
                    }

                    IEnumerable<uri_t> excluded_prefixes;
                    IEnumerable<uri_t> new_prefixes;
                    IEnumerable<uri_t> excluded_exacts;
                    IEnumerable<uri_t> new_exacts;

                    referralCache.Put(@ref, policy, out excluded_prefixes, out new_prefixes, out excluded_exacts, out new_exacts);

                    InvokeEventDelegate(OnRemoveRefferal, groupUri, true, excluded_prefixes);
                    InvokeEventDelegate(OnRemoveRefferal, groupUri, false, excluded_exacts);
                    InvokeEventDelegate(OnAddReferral, groupUri, true, new_prefixes, @ref.go);
                    InvokeEventDelegate(OnAddReferral, groupUri, false, new_exacts, @ref.go);
                }
                else
                {
                    @ref.CheckValid(false);

                    referralCache.Add(@ref, policy);

                    InvokeEventDelegate(OnAddReferral, groupUri, true, @ref.@for.prefix, @ref.go);
                    InvokeEventDelegate(OnAddReferral, groupUri, false, @ref.@for.exact, @ref.go);
                }
            }

            registrationResponse_t registrationResponse = new registrationResponse_t();

            if (@ref.IsInvalidating())
            {
                IEnumerable<ref_t> invalidates = referralCache.@ref.Where(r => @ref.@if.invalidates.rid.Any(rid => rid == r.refId));

                List<uri_t> notInvalidated = new List<uri_t>();

                foreach (ref_t invalidate in invalidates)
                {
                    if (referralCache.Remove(invalidate))
                    {
                        InvokeEventDelegate(OnRemoveRefferal, groupUri, true, invalidate.@for.prefix);
                        InvokeEventDelegate(OnRemoveRefferal, groupUri, false, invalidate.@for.exact);
                    }
                    else

                        notInvalidated.Add(invalidate.refId);
                }

                if (notInvalidated.Count > 0)

                    registrationResponse.Add(new notInvalidated_t(notInvalidated.ToArray()));
            }

            return registrationResponse;
        }

        private static bool TryGetReferralFromCache(uri_t refId, out ref_t target)
        {
            target = referralCache[refId];

            return target != null;
        }

        private static void InvokeEventDelegate(Action<Referral> @delegate, uri_t group, bool prefixMatch, IEnumerable<uri_t> actions)
        {
            if (@delegate != null && actions != null)
            {
                foreach (uri_t uri in actions)
                {
                    @delegate.Invoke(new Referral() { GroupUri = group, Action = uri, PrefixMatch = prefixMatch } );
                }
            }
        }

        private static void InvokeEventDelegate(Action<Referral, go_t> @delegate, uri_t group, bool prefixMatch, IEnumerable<uri_t> actions, go_t go)
        {
            if (@delegate != null && actions != null)
            {
                foreach (uri_t uri in actions)
                {
                    @delegate.Invoke(new Referral() { GroupUri = group, Action = uri, PrefixMatch = prefixMatch }, go);
                }
            }
        }

        public EndpointAddress[] LookupDestinations(ref Message message, InteractionStyles interactionStyle, out string action)
        {
            return SelectDestinations(ref message, interactionStyle, out action);
        }

        public static void SetDispatchDestinations(string groupUri, string action, string messageStyle, string hostId)
        {
            if (String.IsNullOrEmpty(groupUri))

                throw new ArgumentException("Parameter null or empty.", "groupUri");

            if (String.IsNullOrEmpty(action))

                throw new ArgumentException("Parameter null or empty.", "action");

            if (String.IsNullOrEmpty(messageStyle))

                throw new ArgumentException("Parameter null or empty.", "messageStyle");

            if (hostId == null)

                throw new ArgumentNullException("hostId");

            foreach ((ref_t Referral, Policy Policy) actionReferral in referralCache.GetReferralsFor(groupUri, action, messageStyle))
            {
                foreach (uri_t via in actionReferral.Referral.go.via.Where(v => v.Value.StartsWith(ref_t.REF_VIA_DISPATCH_TOKEN_URI)))
                {
                    via.PutAttribute(
                        actionReferral.Policy.Document.CreateValueAttribute(
                            ref_t.VIA_EXT_ATTR_DISPATCH_URL,
                            ref_t.REF_EXT_NAMESPACE_URI,
                            ref_t.REF_VIA_DISPATCH_BASE_URL + hostId));
                }
            }
        }

        public static EndpointAddress[] SelectDestinations(ref Message message, InteractionStyles interactionStyle, out String action)
        {
            //https://msdn.microsoft.com/en-us/library/ms977349.aspx

            //ToDo:  there should be a lock to ensure via is not being read while written to in SetDispatchDestinations

            List<EndpointAddress> endpoints = new List<EndpointAddress>();

            action = message.GetAction(true);

            SelectDestinations(action, message.GetDocumentInteractionMessageStyle(interactionStyle), endpoints);

            SelectDestinations(action, message.GetRPCInteractionMessageStyle(interactionStyle, out message), endpoints);

            return endpoints.ToArray();
        }

        private static void SelectDestinations(string action, InteractionMessageStyle interactionMessageStyle, List<EndpointAddress> endpoints)
        {
            if (String.IsNullOrEmpty(action))

                throw new ArgumentException("Parameter must have a value.", nameof(action));

            if (interactionMessageStyle == null)

                return;

            AddressHeader addressHeader = AddressHeader.CreateAddressHeader(InteractionMessageStyle.BINDING_STYLE_NAME, interactionMessageStyle.BindingNamespace, interactionMessageStyle.BindingStyle);

            foreach ((ref_t Referral, Policy Policy) actionReferral in referralCache.GetReferralsFor(null, action, interactionMessageStyle.ToString()))
            {
                EndpointIdentity endpointIdentity;

                if (actionReferral.Policy.TryGetGroupAttribute(out XmlAttribute groupXmlAttribute))

                    endpointIdentity = new GroupEndpointIdentity(groupXmlAttribute.Value);

                else

                    endpointIdentity = null;

                foreach (uri_t via in actionReferral.Referral.go.via)
                {
                    if (via.Value.StartsWith(ref_t.REF_VIA_DISPATCH_TOKEN_URI))
                    {
                        if (via.AnyAttr.TryGetAttributeValue(ref_t.VIA_EXT_ATTR_DISPATCH_URL, ref_t.REF_EXT_NAMESPACE_URI, out string dispatchUrl))

                            endpoints.Add(new EndpointAddress(new Uri(dispatchUrl), endpointIdentity, addressHeader));
                    }
                    else

                        endpoints.Add(new EndpointAddress(new Uri(via.Value), endpointIdentity, addressHeader));
                }
            }
        }

        public static ref_t[] GetRegisteredReferrals()
        {
            List<ref_t> referrals = new List<ref_t>();

            foreach (ref_t referral in referralCache.@ref)
            {
                if (referral == null)

                    continue;

                referrals.Add(referral);
            }

            return referrals.ToArray();
        }
    }
}
