using System;
using System.ServiceModel;

namespace AllVerge.MessagingModel.ServicePrimitives
{
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using www.w3.org.ns.ws_policy;

    /// <summary>
    /// Contract on which to access policies of a service action.
    /// </summary>
    [ResourceContract(
        Namespace = ServiceModelConstants.PolicyServiceNamespace, 
        Name = ServiceModelConstants.PolicyActionServiceName)]
    public interface IPolicyActionService 
    {
        /// <summary>
        /// Sets a policy of an action.
        /// </summary>
        /// <returns>Void.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [PostMessageAction(Name = "SetActionPolicyAction")]
        [PutResourceAction("/{action}/policy", Name = "SetActionPolicy")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        void SetPolicy(string action, Policy policy);

        /// <summary>
        /// Gets the set of policies of an action.
        /// </summary>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [PostMessageAction(Name = "GetActionPoliciesAction")]
        [GetResourceAction("/{action}/policies", Name = "GetActionPolicies", ActionStyle = ResourceActionStyle.WrappedResponse)]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        Policy[] GetPolicies(string action);

        /// <summary>
        /// Gets the named policy of the action.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [PostMessageAction(Name = "GetActionPolicyAction")]
        [GetResourceAction("/{action}/policy/{policyName}", Name = "GetActionPolicy")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        Policy GetPolicy(string action, string policyName);
    }
}
