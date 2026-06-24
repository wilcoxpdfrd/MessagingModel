using System;
using System.Xml;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public static class MessageHeaderExtensions
    {
        public static MessageHeaders Clone(this MessageHeaders messageHeaders)
        {
            return new MessageHeaders(messageHeaders);
        }

        public static UniqueId GetMessageId(this MessageHeaders messageHeaders)
        {
            UniqueId messageId = messageHeaders.MessageId;

            if (messageId == null)

                return new UniqueId(Guid.NewGuid());

            return messageId;
        }

        public static UniqueId GetRelatesTo(this MessageHeaders messageHeaders)
        {
            UniqueId relatesTo = messageHeaders.RelatesTo;

            if (relatesTo == null)

                return new UniqueId(Guid.NewGuid());

            return relatesTo;
        }

        public static bool TryGetAction(this MessageHeaders messageHeaders, out String action)
        {
            if (messageHeaders == null || messageHeaders.MessageVersion == MessageVersion.None)

                action = null;

            else

                action = messageHeaders.Action;

            return action != null;
        }

        public static string GetAction(this MessageHeaders messageHeaders)
        {
            TryGetAction(messageHeaders, out String action);

            return action;
        }

        public static string GetResponseAction(this MessageHeaders incomingMessageHeaders, bool isuDplexResponse = false)
        {
            String messageHeadersAction;

            if (incomingMessageHeaders == null || incomingMessageHeaders.MessageVersion == MessageVersion.None)

                return null;

            else

                messageHeadersAction = incomingMessageHeaders.Action;

            if (messageHeadersAction.EndsWith("Request"))

                messageHeadersAction = messageHeadersAction.Substring(0, messageHeadersAction.Length - 7);

            if (isuDplexResponse)

                return messageHeadersAction;

            return $"{messageHeadersAction}Response";
        }

        public static string GetResponseFaultAction(this MessageHeaders messageHeaders)
        {
            if (messageHeaders == null || messageHeaders.MessageVersion == MessageVersion.None || messageHeaders.MessageVersion.Addressing == null)

                return null;

            else

                return messageHeaders.MessageVersion.Addressing.FaultAction;
        }
    }
}
