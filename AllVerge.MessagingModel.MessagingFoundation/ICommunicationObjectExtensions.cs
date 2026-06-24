using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Diagnostics;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    public static class ICommunicationObjectExtensions
    {
        public static void ThrowIfDisposedOrNotOpen(this ICommunicationObject communicationObject)
        {
            CommunicationObject _communicationObject = communicationObject as CommunicationObject;

            if (_communicationObject != null)
            {
                switch (_communicationObject.State)
                {
                    case CommunicationState.Opened:
                        break;
                    case CommunicationState.Created:
                        throw TraceUtility.ThrowHelperError(CreateNotOpenException(_communicationObject), Guid.Empty, _communicationObject);
                    case CommunicationState.Opening:
                        throw TraceUtility.ThrowHelperError(CreateNotOpenException(_communicationObject), Guid.Empty, _communicationObject);
                    case CommunicationState.Closing:
                        throw TraceUtility.ThrowHelperError(CreateClosedException(_communicationObject), Guid.Empty, _communicationObject);
                    case CommunicationState.Closed:
                        throw TraceUtility.ThrowHelperError(CreateClosedException(_communicationObject), Guid.Empty, _communicationObject);
                    case CommunicationState.Faulted:
                        throw TraceUtility.ThrowHelperError(CreateFaultedException(_communicationObject), Guid.Empty, _communicationObject);
                    default:
                        throw Fx.AssertAndThrow("ThrowIfDisposedOrNotOpen: Unknown CommunicationObject.state");
                }
            }
        }

        private static Exception CreateNotOpenException(ICommunicationObject communicationObject)
        {
            return new InvalidOperationException(PublicSR.Format(PublicSR.CommunicationObjectCannotBeUsed, communicationObject.GetType().ToString(), communicationObject.State.ToString()));
        }

        private static Exception CreateClosedException(ICommunicationObject communicationObject)
        {
            CommunicationObject commObject = communicationObject as CommunicationObject;

            if (commObject != null)
            {
                if (commObject.Aborted)
                {
                    return CreateAbortedException(communicationObject);
                }
            }
            return new ObjectDisposedException(communicationObject.GetType().ToString());
        }

        private static Exception CreateAbortedException(ICommunicationObject communicationObject)
        {
            return new CommunicationObjectAbortedException(PublicSR.Format(PublicSR.CommunicationObjectAborted1, communicationObject.GetType().ToString()));
        }


        private static Exception CreateFaultedException(ICommunicationObject communicationObject)
        {
            string message = PublicSR.Format(PublicSR.CommunicationObjectFaulted1, communicationObject.GetType().ToString());
            return new CommunicationObjectFaultedException(message);
        }
    }
}
