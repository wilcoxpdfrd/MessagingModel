using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Xml.Serialization;

using AllVerge.MessagingModel.MessagingFoundation.Description;
using AllVerge.MessagingModel.MessagingFoundation.Client;

namespace AllVerge.MessagingModel.Policy.Client
{
    using www.w3.org.ns.ws_policy;

    public class PolicyServiceClient<XmlAttributeOverridesType> : ResourceClient<IPolicyService>, IPolicyService where XmlAttributeOverridesType : XmlAttributeOverrides, new()
    {
        public PolicyServiceClient(String remoteAddress) : 
            base(new BasicHttpBinding(), new EndpointAddress(remoteAddress)) { }

        public PolicyServiceClient(EndpointAddress remoteAddress) :
            base(new BasicHttpBinding(), remoteAddress)
        { }

        public PolicyServiceClient(Binding binding, String remoteAddress) : 
            base(binding, new EndpointAddress(remoteAddress)) { }

        public PolicyServiceClient(Binding binding, EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
        { }

        protected override void SetChannelFactoryConfiguration(ChannelFactory<IPolicyService> channelFactory)
        {
            global::System.Diagnostics.Debug.WriteLine("PolicyServiceClient:SetChannelFactoryConfiguration");

            XmlAttributeOverrides overrides = new XmlAttributeOverridesType();

            foreach (OperationDescription operation in channelFactory.Endpoint.Contract.Operations)
            {
                XmlAttributeOverridesSerializerOperationBehavior.ApplyTo(operation, overrides);

                Debug.Write("XmlMessageSerializerOperationBehavior::ApplyTo:");
                Debug.Write(operation.Name);
                Debug.WriteLine(".");
            }

            base.SetChannelFactoryConfiguration(channelFactory);
        }

        public Policy[] GetPolicies()
        {
            using (OperationContextScope scope = new OperationContextScope((IContextChannel)base.Channel))
            {
                return base.Channel.GetPolicies();
            }
        }

        public string[] GetPolicyNamesContainingItem(string itemTypeNamespaceUri, string itemTypeName)
        {
            using (OperationContextScope scope = new OperationContextScope((IContextChannel)base.Channel))
            {
                return base.Channel.GetPolicyNamesContainingItem(base.Base64UrlEncode(itemTypeNamespaceUri), itemTypeName);
            }
        }

        public string[] GetPolicyNamesContainingItemKey(string itemTypeNamespaceUri, string itemTypeName, string itemKeyTarget, string itemKeyUri)
        {
            using (OperationContextScope scope = new OperationContextScope((IContextChannel)base.Channel))
            {
                return base.Channel.GetPolicyNamesContainingItemKey(base.Base64UrlEncode(itemTypeNamespaceUri), itemTypeName, itemKeyTarget, base.Base64UrlEncode(itemKeyUri));
            }
        }

        public Policy GetPolicy(string name)
        {
            using (OperationContextScope scope = new OperationContextScope((IContextChannel)base.Channel))
            {
                return base.Channel.GetPolicy(name);
            }
        }

        public void SetPolicy(Policy policy)
        {
            using (OperationContextScope scope = new OperationContextScope((IContextChannel)base.Channel))
            {
                base.Channel.SetPolicy(policy);
            }
        }
    }
}
