using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace AllVerge.MessagingModel.ServicePrimitives
{
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using AllVerge.SystemPrimitives.Diagnostics;

    /// <summary>
    /// Contract on which to instrument the actions of a service.
    /// </summary>
    [ResourceContract(
        Namespace = ServiceModelConstants.DiagnosticsServiceNamespace, 
        Name = ServiceModelConstants.DiagnosticsServiceActionName)]
    public interface IDiagnosticServiceAction
    {
        /// <summary>
        /// Sets the set of actions for the service.
        /// </summary>
        /// <param name="actions"></param>
        void SetActions(params String[] actions);

        /// <summary>
        /// The trace of the service action.
        /// </summary>
        /// <param name="action">The service action receiving the trace request.</param>
        /// <returns>The echoed trace request of the service action.</returns>
        /// <exception cref="FaultCode{TDetail}">TDetail is <see cref="ServiceDetailedException"/>.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [OperationContract]
        [GetResourceAction("/trace/{action}", Name = "TraceAction")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        String Trace(string action);

        /// <summary>
        /// Retrieves all indicators current values for the service action.
        /// </summary>
        /// <param name="action">The service action receiving the trace.</param>
        /// <returns>An <see cref="Array"/> of all <see cref="IIndicator"/> for the service <paramref name="action"/>.</returns>
        /// <exception cref="FaultCode{TDetail}">TDetail is <see cref="ServiceDetailedException"/>.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [OperationContract]
        [GetResourceAction("/{action}/indicators", Name = "GetActionIndicators", ActionStyle = ResourceActionStyle.WrappedResponse)]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        IIndicator[] GetIndicators(string action);

        /// <summary>
        /// Retrieves an indicators current value for the service.
        /// </summary>
        /// <param name="action">The service action.</param>
        /// <param name="indicatorName">The name of the indicator to be returned.</param>
        /// <returns>The <see cref="IIndicator"/> of <paramref name="indicatorName"/> associated with the service  <paramref name="action"/>, if found.  Otherwise, null.</returns>
        /// <exception cref="FaultCode{TDetail}">TDetail is <see cref="ServiceDetailedException"/>.  See also <see cref="FaultDetail"/> and <see cref="FaultCode"/>.</exception>
        [OperationContract]
        [GetResourceAction("/{action}/indicator/{indicatorName}", Name = "GetActionIndicator")]
        [FaultContract(
            typeof(FaultDetail),
            Action = FaultExceptionsConstants.Namespace)]
        IIndicator GetIndicator(string action, string indicatorName);
    }
}
