using System.ServiceModel;

using ws_policy = www.w3.org.ns.ws_policy;

namespace AllVerge.MessagingModel.Policy.Client
{
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.ServicePrimitives;

    /// <summary>
    /// Contract on which to access policies of an action.
    /// </summary>
    [ServiceContract(
        Namespace = ServiceModelConstants.PolicyServiceNamespace,
        Name = ServiceModelConstants.PolicyActionServiceName)]
    public interface IPolicyActionService
    {

        /// <summary>
        /// Sets a policy of a service action.
        /// </summary>
        /// <returns>Void.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [OperationContract(Name = "SetActionPolicyAction")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        void SetPolicy(string action, ws_policy.Policy policy);

        /// <summary>
        /// Gets the set of policies of the service.
        /// </summary>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [OperationContract(Name = "GetActionPoliciesAction")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        ws_policy.Policy[] GetPolicies(string action);

        /// <summary>
        /// Gets the named policy of the service.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [OperationContract(Name = "GetActionPolicyAction")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        ws_policy.Policy GetPolicy(string action, string policyName);
    }
}