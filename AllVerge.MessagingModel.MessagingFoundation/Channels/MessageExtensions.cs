using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.MessagingModel.MessagingFoundation.Http;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions;

    using AllVerge.SystemPrimitives.Net;

    public static class MessageExtensions
    {
        static MethodInfo MessageGenericGetBodyMethod = typeof(Message).GetMethod("GetBody", Type.EmptyTypes).GetGenericMethodDefinition();

        public static MessageBuffer TranslateToVersionAndBuffer(this Message message, MessageVersion targetMessageVersion, out MessageVersion fromMessageVersion)
        {
            return TranslateToVersion(message, targetMessageVersion, out fromMessageVersion).CreateBufferedCopy(Int32.MaxValue);
        }

        public static Message TranslateToVersion(this Message message, MessageVersion targetMessageVersion, out MessageVersion fromMessageVersion)
        {
            Message toMessage = null;

            if (message != null)
            {
                fromMessageVersion = message.Version;

                string action = message.GetAction(true);

                if (message.IsEmpty)

                    toMessage = Message.CreateMessage(targetMessageVersion, action);

                else

                    toMessage = Message.CreateMessage(targetMessageVersion, action, message.GetReaderAtBodyContents());

                MessageHeaders receivedMessageHeaders = message.Headers;

                if (receivedMessageHeaders != null)
                {
                    MessageHeaders messageHeaders = toMessage.Headers;

                    if (receivedMessageHeaders.MessageId != null)

                        messageHeaders.MessageId = new UniqueId(receivedMessageHeaders.MessageId.ToString());

                    if (receivedMessageHeaders.RelatesTo != null)

                        messageHeaders.RelatesTo = new UniqueId(receivedMessageHeaders.RelatesTo.ToString());

                    if (receivedMessageHeaders.From != null)

                        messageHeaders.ReplyTo = new EndpointAddress(receivedMessageHeaders.From.Uri, receivedMessageHeaders.From.Identity, receivedMessageHeaders.From.Headers.ToArray());

                    if (receivedMessageHeaders.ReplyTo != null)

                        messageHeaders.ReplyTo = new EndpointAddress(receivedMessageHeaders.ReplyTo.Uri, receivedMessageHeaders.ReplyTo.Identity, receivedMessageHeaders.ReplyTo.Headers.ToArray());

                    if (targetMessageVersion != MessageVersion.None && targetMessageVersion.Addressing != AddressingVersion.None)
                    {
                        messageHeaders.To = receivedMessageHeaders.To;

                        messageHeaders.FaultTo = receivedMessageHeaders.FaultTo;
                    }
                }

                MessageProperties receivedProperties = message.Properties;

                MessageHeaders headers = toMessage.Headers;

                if (receivedProperties.ContainsKey("Via"))

                    headers.Add(MessageHeader.CreateHeader("Via", MessagingModelConstants.Namespace, receivedProperties["Via"]));

                if (receivedProperties.ContainsKey(HttpRequestMessageProperty.Name))
                {
                    HttpRequestMessageProperty httpRequestMessageProperty = (HttpRequestMessageProperty)receivedProperties[HttpRequestMessageProperty.Name];

                    foreach (String key in httpRequestMessageProperty.Headers.Keys)
                    {
                        headers.Add(MessageHeader.CreateHeader("HttpRequestHeader-" + key, MessagingModelConstants.Namespace, httpRequestMessageProperty.Headers[key]));
                    }
                }
            }
            else

                fromMessageVersion = null;

            return toMessage;
        }

        public static Message CopyMessage(this Message message, out Message createdMessage, out Message copiedMessage)
        {
            MessageBuffer messageBuffer = message.CreateBufferedCopy(Int32.MaxValue);

            createdMessage = messageBuffer.CreateMessage();

            copiedMessage = messageBuffer.CreateMessage();

            return createdMessage;
        }

        public static XmlDictionaryWriter GetBodyContentWriter(this Stream stream, MessageEncodingFormat transferFormat)
        {
            if (transferFormat == MessageEncodingFormat.Json)
                return JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false);
            else
                return XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, false);
        }

        public static XmlDictionaryReader GetBodyContentReader(this Stream stream, MessageEncodingFormat transferFormat)
        {
            if (transferFormat == MessageEncodingFormat.Json)
                return JsonReaderWriterFactory.CreateJsonReader(stream, Encoding.UTF8, XmlDictionaryReaderQuotas.Max, null);
            else
                return XmlDictionaryReader.CreateTextReader(stream, Encoding.UTF8, XmlDictionaryReaderQuotas.Max, null);
        }

        public static String GetAction(this Message message, bool isIntermediateMessage)
        {
            string action = message.Headers.Action;

            if (action == null && message.Properties.TryGetProperty(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))

                action = httpRequestMessageProperty.GetAction();

            if (action == null)

                action = message.Headers.To?.ToString();

            if (action == null)
            {
                if (isIntermediateMessage)
                {
                    Uri via = message.Properties.Via;

                    if (via != null)

                        return new UriBuilder(via.Scheme, via.Segments[1].TrimEnd('/'), 80, String.Join("", via.Segments.Skip(2).ToArray())).Uri.AbsoluteUri;
                }
                else

                    action = message.Properties.Via?.ToString();
            }

            return action;
        }

        static Dictionary<String, String> actionPrefixSuffixPairs = new Dictionary<string, string>();
        static Dictionary<String, String> actionSuffixPairs = new Dictionary<string, string>();
        static MessageExtensions()
        {
            actionSuffixPairs.Add("Input", "Output");
            actionSuffixPairs.Add("Request", "Response");
            actionPrefixSuffixPairs.Add("Delete", "Ack");
            actionPrefixSuffixPairs.Add("Get", "");
            actionPrefixSuffixPairs.Add("", "Response");
        }

        public static string GetResponseAction(this Message message)
        {
            String action = message.GetAction(false);

            if (action == null)

                return action;

            if (!new Uri(action).TryGetResourceName(out Uri resourceUriAndPath, out String inputName))

                inputName = "Request";

            string outputName = null;

            foreach (KeyValuePair<String, String> kvp in actionSuffixPairs)
            {
                if (inputName.EndsWith(kvp.Key))
                {
                    outputName = $"{inputName.Substring(0, kvp.Key.Length)}{kvp.Value}";

                    break;
                }
            }

            if (outputName == null)
            {
                foreach (KeyValuePair<String, String> kvp in actionPrefixSuffixPairs)
                {
                    if (inputName.StartsWith(kvp.Key))
                    {
                        outputName = $"{inputName.Substring(kvp.Key.Length)}{kvp.Value}";

                        break;
                    }
                }
            }

            return new Uri(resourceUriAndPath, outputName).AbsoluteUri;
        }

        public static string GetFaultAction(this Message message)
        {
            return message.Version.Addressing.FaultAction;
        }

        public static InteractionMessageStyle GetDocumentInteractionMessageStyle(this Message message, InteractionStyles interactionStyle)
        {
            if (message == null)

                throw new ArgumentNullException(nameof(message));

            if (message.Version.Envelope == EnvelopeVersion.Soap11)

                return new InteractionMessageStyle(
                    MessagingBindingConstants.SOAP_BINDING_PREFIX,
                    MessagingBindingConstants.SOAP_BINDING_NAMESPACE,
                    InteractionMessageStyle.BINDING_STYLE_DOCUMENT,
                    null,
                    interactionStyle);

            else if (message.Version.Envelope == EnvelopeVersion.Soap12)

                return new InteractionMessageStyle(
                    MessagingBindingConstants.SOAP12_BINDING_PREFIX,
                    MessagingBindingConstants.SOAP12_BINDING_NAMESPACE,
                    InteractionMessageStyle.BINDING_STYLE_DOCUMENT,
                    null,
                    interactionStyle);

            else if (message.Properties.TryGetProperty(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))

                return new InteractionMessageStyle(
                    MessagingBindingConstants.HTTP_BINDING_PREFIX,
                    MessagingBindingConstants.HTTP_BINDING_NAMESPACE,
                    httpRequestMessageProperty.Method.ToUpper(),
                    null,
                    interactionStyle);

            else

                throw new InvalidOperationException(
                    $"Could not determine message style of message {message.Headers?.MessageId}.");
        }

        public static InteractionMessageStyle GetRPCInteractionMessageStyle(this Message message, InteractionStyles interactionStyle, out Message createdMessage)
        {
            if (message == null)

                throw new ArgumentNullException(nameof(message));

            if (message.Version.Envelope == EnvelopeVersion.None)
            {
                createdMessage = message;

                return null;
            }
            else
            {
                String interactionName = message.GetBodyName(out createdMessage);

                if (message.Version.Envelope == EnvelopeVersion.Soap11)

                    return new InteractionMessageStyle(
                        MessagingBindingConstants.SOAP_BINDING_PREFIX,
                        MessagingBindingConstants.SOAP_BINDING_NAMESPACE,
                        InteractionMessageStyle.BINDING_STYLE_RPC,
                        interactionName,
                        interactionStyle);

                else if (message.Version.Envelope == EnvelopeVersion.Soap12)

                    return new InteractionMessageStyle(
                        MessagingBindingConstants.SOAP12_BINDING_PREFIX,
                        MessagingBindingConstants.SOAP12_BINDING_NAMESPACE,
                        InteractionMessageStyle.BINDING_STYLE_RPC,
                        interactionName,
                        interactionStyle);
            }

            throw new InvalidOperationException(
                $"Could not determine message style of message {message.Headers?.MessageId}.");
        }

        private static String GetBodyName(this Message message, out Message createdMessage)
        {
            String interactionName;

            if (message.IsEmpty)
            {
                createdMessage = message;

                interactionName = null;
            }
            else
            {
                createdMessage = message.CopyMessage(out _, out Message copy);

                using (var reader = copy.GetReaderAtBodyContents())
                {
                    interactionName = reader.LocalName;
                }
            }

            return interactionName;
        }

        public static MessageType TrySetTo<MessageType>(this MessageType message, Uri to, bool force = false) where MessageType : Message
        {
            if ((message.Headers.To == null || force) && to != null)

                message.Headers.To = to;

            return message;
        }

        public static MessageType TrySetRelatesTo<MessageType>(this MessageType message, UniqueId relatesTo, bool force = false) where MessageType : Message
        {
            if ((message.Headers.RelatesTo == null || force) && relatesTo != null)

                message.Headers.RelatesTo = relatesTo;

            return message;
        }

        public static MessageType TrySetReplyTo<MessageType>(this MessageType message, EndpointAddress replyToAddress, bool force = false) where MessageType : Message
        {
            if (message != null)
            {
                if ((message.Headers.ReplyTo == null || force) && replyToAddress != null)

                    message.Headers.ReplyTo = replyToAddress;
            }

            return message;
        }
    }
}
