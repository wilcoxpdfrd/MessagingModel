using AllVerge.SystemPrimitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllVerge.MessagingModel.ServicePrimitives
{
    using AllVerge.MessagingModel.MessagingFoundation;

    /// <summary>
    /// Contains constants for services built using components provided by the <see cref="AllVerge.MessagingModel"/> namespace.
    /// </summary>
    public static class ServiceModelConstants
    {
        /// <summary>
        /// The namespace for contracts.
        /// </summary>
        public const string ContractsNamespace = MessagingModelConstants.Namespace + "Contracts/";

        /// <summary>
        /// The namespace for the diagnostics service contract.
        /// </summary>
        public const string DiagnosticsServiceNamespace = MessagingModelConstants.Namespace + "Diagnostics/";

        /// <summary>
        /// The name for the diagnostics service contract.
        /// </summary>
        public const string DiagnosticsServiceName = "DiagnosticsService";

        /// <summary>
        /// The name for the diagnostics service action contract.
        /// </summary>
        public const string DiagnosticsServiceActionName = "DiagnosticsServiceAction";

        /// <summary>
        /// The namespace for the security service contract.
        /// </summary>
        public const string SecurityServiceNamespace = MessagingModelConstants.Namespace + "Security/";

        /// <summary>
        /// The name for the security service contract.
        /// </summary>
        public const string SecurityServiceName = "SecurityService";

        /// <summary>
        /// The name for the security service action contract.
        /// </summary>
        public const string SecurityServiceActionName = "SecurityServiceAction";

        /// <summary>
        /// The namespace for the policy service contract.
        /// </summary>
        public const string PolicyServiceNamespace = MessagingModelConstants.Namespace + "Policy/";

        /// <summary>
        /// The name for the policy action service contract.
        /// </summary>
        public const string PolicyActionServiceName = "PolicyActionService";

        /// <summary>
        /// The name for the policy service contract.
        /// </summary>
        public const string PolicyServiceName = "PolicyService";

        /// <summary>
        /// The name for the policy service action contract.
        /// </summary>
        public const string PolicyServiceActionName = "PolicyServiceAction";

        /// <summary>
        /// The namespace for the host service contract.
        /// </summary>
        public const string HostServiceNamespace = MessagingModelConstants.Namespace + "Host/";

        /// <summary>
        /// The name for the host service contract.
        /// </summary>
        public const string HostServiceName = "HostService";

        /// <summary>
        /// The name for the host service action contract.
        /// </summary>
        public const string HostServiceActionName = "HostServiceAction";

        /// <summary>
        /// The host service fault action name.
        /// </summary>
        public const string HostServiceFaultActionName = HostServiceNamespace + "FaultAction";

        /// <summary>
        /// The default root namespace for service contracts.
        /// </summary>
        public const string DefaultNamespace = "http://tempuri.org/";
    }
}
