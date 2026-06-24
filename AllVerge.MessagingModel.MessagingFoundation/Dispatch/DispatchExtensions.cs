using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public static class DispatchExtensions
    {
        public static bool TryGetMessageFilterMatchedDispatchOperation(this IncomingMessageEventArgs incomingMessageEventArgs, out DispatchOperationDescription dispatchOperation)
        {
            if (incomingMessageEventArgs.Properties.TryGetProperty(IncomingMessageEventArgs.DispatchOperationMessageFilterMatchedPropertyName, out MessageFilterMatch messageFilterMatch))
            {
                dispatchOperation = (DispatchOperationDescription)messageFilterMatch.Data;
            }
            else
            {
                dispatchOperation = null;
            }

            return dispatchOperation != null;
        }

        public static bool TryGetUriTemplateMatchedDispatchOperation(this IncomingMessageEventArgs incomingMessageEventArgs, out DispatchOperationDescription dispatchOperation, out NameObjectCollectionBase dispatchMethodVariables)
        {
            if (incomingMessageEventArgs.Properties.TryGetProperty(IncomingMessageEventArgs.DispatchOperationUriTemplateMatchResultsPropertyName, out UriTemplateMatch uriTemplateMatch))
            {
                dispatchOperation = (DispatchOperationDescription)uriTemplateMatch.Data;

                dispatchMethodVariables = uriTemplateMatch.BoundVariables;
            }
            else
            {
                dispatchOperation = null;

                dispatchMethodVariables = null;
            }

            return dispatchOperation != null;
        }
    }
}
