using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AllVerge.Core.ServiceModel.Client;
using AllVerge.MessagingModel.ServicePrimitives;

namespace AllVerge.MessagingModel.Policy.Client
{
    /// <summary>
    /// Contains constants for services built using components provided by the <see cref="AllVerge.Core.ServiceModel.WSPolicy"/> namespace.
    /// </summary>
    public class PolicyServiceModelConstants
    {
        /// <summary>
        /// The namespace for the policy service contract.
        /// </summary>
        public const string PolicyServiceNamespace = ServiceModelConstants.Namespace + "Policy/";

        /// <summary>
        /// The name for the policy service contract.
        /// </summary>
        public const string PolicyServiceName = "PolicyService";

        /// <summary>
        /// The name for the policy action service contract.
        /// </summary>
        public const string PolicyActionServiceName = "PolicyActionService";
    }
}
