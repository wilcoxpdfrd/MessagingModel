using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes
{
    /// <summary>
    /// Identifies the method of a resource endpoint contract with which to wait for and obtain a message for sending a one-way POST resource action call back response after recieving a one-way POST resource action message in a duplex message exchange.
    /// </summary>
    /// <remarks>
    /// The identified method must be parameterless, and be async: either returning a Task or is the Begin method of an APM pair on the type decorated with <see cref="ResourceContractAttribute"/>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class WaitForReplyMessageAsyncMethodAttribute : ResourceEndpointAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForReplyMessageAsyncMethodAttribute" /> class with the given <paramref name="methodName"/>.
        /// </summary>
        /// <param name="methodName"></param>
        public WaitForReplyMessageAsyncMethodAttribute(String methodName) : base(methodName) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForReplyMessageAsyncMethodAttribute" /> class.
        /// </summary>
        public WaitForReplyMessageAsyncMethodAttribute() : base() { }

        /// <summary>
        /// The name of the method that recieves a message in a one-way POST Resource Action.
        /// </summary>
        /// <remarks>
        /// Required if there is more than one method of the contract decorated with a <see cref="PostMessageActionAttribute"/>.
        /// </remarks>
        public string ForReceivePostMethod { get; set; }

        protected override void Validate(OperationDescription operationDescription)
        {
            if (operationDescription.TaskMethod != null &&
                operationDescription.TaskMethod.ReturnType.IsGenericType)
            {
                if (ValidateMessages(operationDescription))
                    return;
            }

            if (operationDescription.BeginMethod != null)
            {
                if (operationDescription.EndMethod.ReturnType != typeof(void))
                {
                    if (ValidateMessages(operationDescription))
                        return;
                }
            }

            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                new InvalidOperationException($"{nameof(WaitForReplyMessageAsyncMethodAttribute)} can only decorate methods with no arguments, that return a generic task or is the Begin method of an APM pair whose End method returns the response value."));
        }

        private static bool ValidateMessages(OperationDescription operationDescription)
        {
            return
                operationDescription.Messages.Count == 2 &&
                operationDescription.Messages[0].Direction == MessageDirection.Input &&
                operationDescription.Messages[0].Body.Parts.Count == 0 &&
                operationDescription.Messages[1].Direction == MessageDirection.Output &&
                operationDescription.Messages[1].Body.Parts.Count == 0 && 
                operationDescription.Messages[1].Body.ReturnValue != null;
        }
    }
}
