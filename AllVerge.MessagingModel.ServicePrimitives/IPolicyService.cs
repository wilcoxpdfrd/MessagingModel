using System;
using System.ServiceModel;

namespace AllVerge.MessagingModel.ServicePrimitives
{
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using www.w3.org.ns.ws_policy;

    /// <summary>
    /// Contract on which to manage policies of a service.
    /// </summary>
    [ResourceContract(
        Namespace = ServiceModelConstants.PolicyServiceNamespace, 
        Name = ServiceModelConstants.PolicyServiceName)]
    public interface IPolicyService 
    {
        /// <summary>
        /// Sets a policy of the service.
        /// </summary>
        /// <returns>Void.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [PostMessageAction(Name = "SetPolicyAction")]
        [PutResourceAction("/policy")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        void SetPolicy(Policy policy);

        /// <summary>
        /// Gets the set of policies of the service.
        /// </summary>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [PostMessageAction(Name = "GetPoliciesAction")]
        [GetResourceAction("/policy/all", ActionStyle = ResourceActionStyle.WrappedResponse)]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        Policy[] GetPolicies();

        /// <summary>
        /// Gets the names of the service policies that contain items of <paramref name="itemTypeName" /> in <paramref name="itemNamespaceUri"/>.
        /// </summary>
        /// <param name="itemTypeNamespaceUri">The type namespace Uri for the policy item.</param>
        /// <param name="itemTypeName">The type name for the policy item.</param>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [PostMessageAction(Name = "GetPolicyNamesContainingItemAction")]
        [GetResourceAction("/policy/{itemTypeNamespaceUri}/{itemTypeName}", ActionStyle = ResourceActionStyle.WrappedResponse)]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        String[] GetPolicyNamesContainingItem(string itemTypeNamespaceUri, string itemTypeName);

        /// <summary>
        /// Gets the names of the service policies that contain items of <paramref name="itemTypeName" /> in <paramref name="itemNamespaceUri"/> for <paramref name="itemKeyUri"/>.
        /// </summary>
        /// <param name="itemTypeNamespaceUri">The type namespace Uri for the policy item.</param>
        /// <param name="itemTypeName">The type name for the policy item.</param>
        /// <param name="itemKeyTarget">The target property or attribute of the item.</param>
        /// <param name="itemKeyUri">A key that identifies a property or attribute of the item.</param>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [PostMessageAction(Name = "GetPolicyNamesContainingItemKeyAction")]
        [GetResourceAction("/policy/{itemTypeNamespaceUri}/{itemTypeName}/{itemKeyTarget}/{itemKeyUri}", ActionStyle = ResourceActionStyle.WrappedResponse)]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        String[] GetPolicyNamesContainingItemKey(string itemTypeNamespaceUri, string itemTypeName, string itemKeyTarget, string itemKeyUri);

        /// <summary>
        /// Gets the named policy of the service.
        /// </summary>
        /// <param name="name">The Uniform Resource Name for the policy.</param>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [PostMessageAction(Name = "GetPolicyAction")]
        [GetResourceAction("/policy/{name}")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        Policy GetPolicy(String name);
    }
}
