using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.Referrals.Policy.Client
{
    using www.w3.org.ns.ws_policy;
    using schemas.xmlsoap.org.ws._2001._10.referral;

    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.Policy.Client;
    using AllVerge.MessagingModel.RoutingPrimitives;
    using AllVerge.PolicyPrimitives;

    public class ReferralsPolicyServiceClient : PolicyServiceClient<PolicyReferralsXmlAttributeOverrides>
    {
        public ReferralsPolicyServiceClient(String remoteAddress) :
            base(new BasicHttpBinding(), new EndpointAddress(remoteAddress))
        { }

        public ReferralsPolicyServiceClient(EndpointAddress remoteAddress) :
            base(new BasicHttpBinding(), remoteAddress)
        { }

        public ReferralsPolicyServiceClient(Binding binding, String remoteAddress) :
            base(binding, new EndpointAddress(remoteAddress))
        { }

        public ReferralsPolicyServiceClient(Binding binding, EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        { }


        public Registrations GetRegistrations<Caller>(String action)
        {
            string[] policyNames;

            try
            {
                policyNames = this.GetPolicyNamesContainingItemKey("http://schemas.xmlsoap.org/ws/2001/10/referral", "registration_t", "ref-for", action);
            }
            catch (Exception e)
            {
                throw e.CreateFaultException(typeof(Caller));
            }

            Registrations registrations = new Registrations();

            foreach (string policyName in policyNames)
            {
                Policy policy;

                try
                {
                    policy = this.GetPolicy(policyName);
                }
                catch (Exception e)
                {
                    throw e.CreateFaultException(typeof(Caller));
                }

                IEnumerable<registration_t> policyRegistrations = PolicyFactory.GetElements<registration_t>(policy, ItemsChoiceType.Item);

                registrations.AddRange(policyRegistrations.FilterPolicyRegistrationsForAction(action));
            }

            return registrations;
        }

        public void Register<Caller>(string groupName, string action, string messageStyle, string interactionStyle, string matchType, bool invalidates, ulong ttl, int concurrency, int sla, int timeout, params String[] vias)
        {
            Policy policy = new Policy();

            policy.AddGroupAttribute(groupName);

            policy.SetPolicyReferralForAction(action, messageStyle, interactionStyle, matchType, invalidates, ttl, concurrency, sla, timeout, vias);

            try
            {
                this.SetPolicy(policy);
            }
            catch (Exception e)
            {
                throw e.CreateFaultException(typeof(Caller));
            }
        }

        public void UpdateRegistration<Caller>(string referralId, string action, string matchType, bool invalidates, ulong ttl, int concurrency, int sla, int timeout, params String[] vias)
        {
            match match;

            if (!Enum.TryParse<match>(matchType, out match))

                throw new ArgumentOutOfRangeException(nameof(matchType));

            string[] policyNames;

            try
            {
                policyNames = this.GetPolicyNamesContainingItemKey("http://schemas.xmlsoap.org/ws/2001/10/referral", "registration_t", "ref-refId", referralId);
            }
            catch (Exception e)
            {
                throw e.CreateFaultException(typeof(Caller));
            }

            foreach (String policyName in policyNames)
            {
                Policy policy;

                try
                {
                    policy = this.GetPolicy(policyName);
                }
                catch (Exception e)
                {
                    throw e.CreateFaultException(typeof(Caller));
                }

                policy.PatchPolicyRegistrations(referralId, action, matchType, invalidates, ttl, concurrency, sla, timeout, vias);

                try
                {
                    this.SetPolicy(policy);
                }
                catch (Exception e)
                {
                    throw e.CreateFaultException(typeof(Caller));
                }
            }
        }

        public void InvalidateRegistration<Caller>(string referralId)
        {
            string[] policyNames;

            try
            {
                policyNames = this.GetPolicyNamesContainingItemKey("http://schemas.xmlsoap.org/ws/2001/10/referral", "registration_t", "ref-refId", referralId);
            }
            catch (Exception e)
            {
                throw e.CreateFaultException(typeof(Caller));
            }

            foreach (String policyName in policyNames)
            {
                Policy policy;

                try
                {
                    policy = this.GetPolicy(policyName);
                }
                catch (Exception e)
                {
                    throw e.CreateFaultException(typeof(Caller));
                }

                policy.InvalidatePolicyRegisteration(referralId);

                try
                {
                    this.SetPolicy(policy);
                }
                catch (Exception e)
                {
                    throw e.CreateFaultException(typeof(Caller));
                }
            }
        }

        public void DeleteRegistration<Caller>(string referralId)
        {
            string[] policyNames;

            try
            {
                policyNames = this.GetPolicyNamesContainingItemKey("http://schemas.xmlsoap.org/ws/2001/10/referral", "registration_t", "ref-refId", referralId);
            }
            catch (Exception e)
            {
                throw e.CreateFaultException(typeof(Caller));
            }

            foreach (String policyName in policyNames)
            {
                Policy policy;

                try
                {
                    policy = this.GetPolicy(policyName);
                }
                catch (Exception e)
                {
                    throw e.CreateFaultException(typeof(Caller));
                }

                policy.DeletePolicyRegisteration(referralId);

                try
                {
                    this.SetPolicy(policy);
                }
                catch (Exception e)
                {
                    throw e.CreateFaultException(typeof(Caller));
                }
            }
        }
    }
}
