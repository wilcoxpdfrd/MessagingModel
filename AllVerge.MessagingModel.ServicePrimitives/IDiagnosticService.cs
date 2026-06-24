using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.ServiceModel;

namespace AllVerge.MessagingModel.ServicePrimitives
{
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using AllVerge.SystemPrimitives.Diagnostics;

    /// <summary>
    /// Contract on which to instrument a service.
    /// </summary>
    [ResourceContract(
        Namespace = ServiceModelConstants.DiagnosticsServiceNamespace,
        Name = ServiceModelConstants.DiagnosticsServiceName)]
    [ServiceContract(
        Namespace = ServiceModelConstants.DiagnosticsServiceNamespace,
        Name = ServiceModelConstants.DiagnosticsServiceName)]
    public interface IDiagnosticService
    {
        /// <summary>
        /// Replies with the date and time the ping was received.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> of when the ping was received.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails"/>.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [OperationContract]
        [GetResourceAction("/ping")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        DateTime Ping();

        /// <summary>
        /// Retrieves all indicators current values for the service.
        /// </summary>
        /// <returns>An <see cref="Array"/> of all <see cref="IIndicator"/> for the service.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails"/>.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [OperationContract]
        [GetResourceAction("/indicators", ActionStyle = ResourceActionStyle.WrappedResponse)]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        IIndicator[] GetIndicators();

        /// <summary>
        /// Retrieves an indicators current value for the service.
        /// </summary>
        /// <param name="indicatorName">The name of the indicator to be returned.</param>
        /// <returns>The <see cref="IIndicator"/> of <paramref name="indicatorName"/> associated with the service, if found.  Otherwise, null.</returns>
        /// <exception cref="FaultException{TDetail}">TDetail is <see cref="FaultDetails"/>.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [OperationContract]
        [GetResourceAction("/indicator/{indicatorName}")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        IIndicator GetIndicator(string indicatorName);
    }
}
