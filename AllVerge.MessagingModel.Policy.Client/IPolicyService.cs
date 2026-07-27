using System.ServiceModel;

namespace AllVerge.MessagingModel.Policy.Client
{
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.ServicePrimitives;
    using www.w3.org.ns.ws_policy;

    /// <summary>
    /// Contract on which to manage policies of a service.
    /// </summary>
    [ServiceContract(
        Namespace = ServiceModelConstants.PolicyServiceNamespace,
        Name = ServiceModelConstants.PolicyServiceName)]
    public interface IPolicyService
    {
        /// <summary>
        /// Gets the set of policies of the service.
        /// </summary>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="System.ServiceModel.FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [OperationContract(Name = "GetPoliciesAction")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        Policy[] GetPolicies();

        /// <summary>
        /// Gets the named policy of the service.
        /// </summary>
        /// <param name="name">The Uniform Resource Name for the policy.</param>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="System.ServiceModel.FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [OperationContract(Name = "GetPolicyAction")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        Policy GetPolicy(string name);

        /// <summary>
        /// Gets the names of the service policies that contain items of <paramref name="itemTypeName" /> in <paramref name="itemNamespaceUri"/>.
        /// </summary>
        /// <param name="itemTypeNamespaceUri">The type namespace Uri for the policy item.</param>
        /// <param name="itemTypeName">The type name for the policy item.</param>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="System.ServiceModel.FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [OperationContract(Name = "GetPolicyNamesContainingItemAction")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        string[] GetPolicyNamesContainingItem(string itemTypeNamespaceUri, string itemTypeName);

        /// <summary>
        /// Gets the names of the service policies that contain items of <paramref name="itemTypeName" /> in <paramref name="itemNamespaceUri"/> for <paramref name="itemKeyUri"/>.
        /// </summary>
        /// <param name="itemTypeNamespaceUri">The type namespace Uri for the policy item.</param>
        /// <param name="itemTypeName">The type name for the policy item.</param>
        /// <param name="itemKeyTarget">The target property or attribute of the item.</param>
        /// <param name="itemKeyUri">A key that identifies a property or attribute of the item.</param>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="System.ServiceModel.FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [OperationContract(Name = "GetPolicyNamesContainingItemKeyAction")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        string[] GetPolicyNamesContainingItemKey(string itemTypeNamespaceUri, string itemTypeName, string itemKeyTarget, string itemKeyUri);

        /// <summary>
        /// Sets a policy of the service.
        /// </summary>
        /// <returns>Void.</returns>
        /// <exception cref="System.ServiceModel.FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        [OperationContract(Name = "SetPolicyAction")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        void SetPolicy(Policy policy);
    }
}