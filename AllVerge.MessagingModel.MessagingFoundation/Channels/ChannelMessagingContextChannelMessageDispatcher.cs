using System;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using System.ServiceModel.Channels;

using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingFoundation;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;

    using AllVerge.SystemPrimitives.Net.Mime;

    public class ChannelMessagingContextChannelMessageDispatcher :
        MessageDispatcher<ChannelMessageContext, Message>
    {
        public ChannelMessagingContextChannelMessageDispatcher() : base() { }

        protected override bool TryPrepareIncomingMessageEventArgs(IMessagingContext<ChannelMessageContext> messagingContext, Message incomingMessage, out IncomingMessageEventArgs incomingMessageEventArgs, out Exception prepareIncomingMessageEventArgsException)
        {
            try
            {
                String receivedMethod = messagingContext.BindingContext.InteractionContext.InputVerb;
                String receivedRequestBaseUriAndPath = messagingContext.BindingContext.InteractionContext.InputLocation;
                Uri referrer = messagingContext.BindingContext.InteractionContext.InputHeaders.Referer;
                IPEndPoint remoteIPEndpoint = messagingContext.BindingContext.ConnectionContext.RemoteIPEndpoint;
                UniqueId requestId = messagingContext.RequestId;
                UniqueId relatesTo = messagingContext.RelatesTo;
                UniqueId correltionId = messagingContext.CorrelationId;
                String traceIdentifier = messagingContext.BindingContext.InteractionContext.TraceIdentifier;
                IPrincipal user = messagingContext.BindingContext.InteractionContext.User;
                String sessionId = messagingContext.SessionId;

                using (incomingMessage)
                {
                    incomingMessageEventArgs =
                        IncomingMessageEventArgs.Create(incomingMessage, receivedMethod, receivedRequestBaseUriAndPath, user, referrer, remoteIPEndpoint, requestId, relatesTo, correltionId, traceIdentifier, sessionId);
                }

                IncomingMessageEventArgs args = incomingMessageEventArgs;

                prepareIncomingMessageEventArgsException = null;
            }
            catch (Exception e)
            {
                incomingMessageEventArgs = null;

                prepareIncomingMessageEventArgsException = e;
            }

            return incomingMessageEventArgs != null;
        }

        protected override bool TryPrepareOutgoingMessage(OutgoingMessageEventArgs outgoingMessageEventArgs, out Message outgoingMessage, out Exception prepareOutgoingMessageException)
        {
            if (outgoingMessageEventArgs.Properties.TryGetProperty<MessageEncoder>("Encoder", out MessageEncoder messageEncoder))
            {
                MessageProperties outgoingMessageProperties = outgoingMessageEventArgs.Properties;

                StringValues acceptableContentTypes = outgoingMessageEventArgs.ReceivedAcceptableContentTypes;

                if (acceptableContentTypes.Count == 0)

                    acceptableContentTypes = new StringValues(MediaTypeConstants.ANY_MEDIA_TYPE);

                if (messageEncoder != null)
                {
                    String contentType = null;
                    String accept = null;

                    if (outgoingMessageProperties.TryGetProperty<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name, out HttpResponseMessageProperty httpResponseMessageProperty))
                    {
                        contentType = httpResponseMessageProperty.Headers[HttpResponseHeader.ContentType];

                        if (!string.IsNullOrWhiteSpace(contentType))

                            httpResponseMessageProperty.Headers.Remove(HttpResponseHeader.ContentType);
                    }
                    else
                    {
                        httpResponseMessageProperty = new HttpResponseMessageProperty();

                        outgoingMessageProperties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty);
                    }

                    MessageEncodingFormat transferFormat;

                    if (outgoingMessageProperties.TryGetProperty(MessageEncodingFormatProperty.Name, out MessageEncodingFormatProperty outgoingMessageFormatMessageProperty))
                    {
                        transferFormat = outgoingMessageFormatMessageProperty.Format;

                        foreach (String acceptable in acceptableContentTypes.ToArray())
                        {
                            if (acceptable == MediaTypeConstants.ANY_MEDIA_TYPE)
                            {
                                accept = CompositeMessageEncodingFormatEncoderFactory.GetContentTypeForFormat(messageEncoder, transferFormat, out bool supportedByMessageEncoder);
                            }
                            else if (messageEncoder.IsContentTypeSupported(acceptable))
                            {
                                if (string.IsNullOrEmpty(contentType))
                                {
                                    MessageEncodingFormat acceptableTransferMessageEncodings =
                                        CompositeMessageEncodingFormatEncoderFactory.GetTransferFormatForContentType(messageEncoder, acceptable, out bool IsAcceptbleSupportedByMessageEncoder);

                                    switch (acceptableTransferMessageEncodings)
                                    {
                                        case MessageEncodingFormat.Soap11:

                                            switch (transferFormat)
                                            {
                                                case MessageEncodingFormat.Soap11:
                                                case MessageEncodingFormat.Soap11WSAddressing10:
                                                case MessageEncodingFormat.Soap11WSAddressingAugust2004:

                                                    accept = acceptable;

                                                    break;
                                            }

                                            break;

                                        case MessageEncodingFormat.Soap12:

                                            switch (transferFormat)
                                            {
                                                case MessageEncodingFormat.Soap12:
                                                case MessageEncodingFormat.Soap12WSAddressing10:
                                                case MessageEncodingFormat.Soap12WSAddressingAugust2004:

                                                    accept = acceptable;

                                                    break;
                                            }

                                            break;

                                        default:

                                            if (acceptableTransferMessageEncodings == transferFormat)

                                                accept = acceptable;

                                            break;
                                    }
                                }
                                else if (acceptable == contentType)
                                {
                                    MessageEncodingFormat acceptableTransferFormat =
                                        CompositeMessageEncodingFormatEncoderFactory.GetTransferFormatForContentType(messageEncoder, acceptable, out bool isAcceptableSupportedByMessageEncoder);

                                    if (acceptableTransferFormat == transferFormat ||
                                        transferFormat == MessageEncodingFormat.Raw)
                                    {
                                        // Raw is included as it allows for message handlers to return a stream 
                                        // and at the same time set a content-type explicitly in the response 
                                        // message property ...

                                        accept = acceptable;

                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        transferFormat = MessageEncodingFormat.Default;

                        foreach (String acceptable in acceptableContentTypes.ToArray())
                        {
                            if (acceptable != MediaTypeConstants.ANY_MEDIA_TYPE && messageEncoder.IsContentTypeSupported(acceptable))
                            {
                                if (string.IsNullOrEmpty(contentType) || contentType == acceptable)
                                {
                                    transferFormat = CompositeMessageEncodingFormatEncoderFactory.GetTransferFormatForContentType(messageEncoder, acceptable, out bool isAcceptableSupportedByMessageEncoder);

                                    accept = acceptable;

                                    break;
                                }
                            }
                        }

                        if (transferFormat == MessageEncodingFormat.Default)
                        {
                            if (!outgoingMessageEventArgs.Properties.TryGetProperty(MessageEncodingFormatProperty.Name, out MessageEncodingFormatProperty transferFormatMessageProperty))
                            {
                                transferFormatMessageProperty = MessageEncodingFormatProperty.BinaryProperty;
                            }

                            transferFormat = transferFormatMessageProperty.Format;

                            accept = CompositeMessageEncodingFormatEncoderFactory.GetContentTypeForFormat(messageEncoder, transferFormat, out bool supportedByMessageEncoder);
                        }

                        switch (transferFormat)
                        {
                            case MessageEncodingFormat.Json:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.JsonProperty);

                                break;

                            case MessageEncodingFormat.Soap11WSAddressing10:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11WSAddressing10Property);

                                break;

                            case MessageEncodingFormat.Soap11WSAddressingAugust2004:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11WSAddressingAugust2004Property);

                                break;

                            case MessageEncodingFormat.Soap11:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11Property);

                                break;

                            case MessageEncodingFormat.Soap12WSAddressing10:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12WSAddressing10Property);

                                break;

                            case MessageEncodingFormat.Soap12WSAddressingAugust2004:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12WSAddressingAugust2004Property);

                                break;

                            case MessageEncodingFormat.Soap12:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12Property);

                                break;

                            case MessageEncodingFormat.Binary:
                            case MessageEncodingFormat.Default:
                            default:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryProperty);

                                break;

                            case MessageEncodingFormat.BinaryPlusGzip:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryPlusGzipProperty);

                                break;

                            case MessageEncodingFormat.BinaryPlusDeflate:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryPlusDeflateProperty);

                                break;

                            case MessageEncodingFormat.Xml:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.XmlProperty);

                                break;

                            case MessageEncodingFormat.Text:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.TextProperty);

                                break;

                            case MessageEncodingFormat.Html:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.HtmlProperty);

                                break;

                            case MessageEncodingFormat.Raw:

                                outgoingMessageProperties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.RawProperty);

                                break;
                        }
                    }

                    if (accept != null)
                    {
                        httpResponseMessageProperty.Headers.Add(HttpResponseHeader.ContentType, accept);
                    }
                }

                outgoingMessage = outgoingMessageEventArgs.OutgoingMessage.CreateMessage();

                outgoingMessage.Properties.CopyProperties(outgoingMessageProperties);

                outgoingMessage.TrySetTo(outgoingMessageEventArgs.ReceivedReplyTo?.Uri);

                outgoingMessage.TrySetRelatesTo(outgoingMessageEventArgs.RelatesTo);

                prepareOutgoingMessageException = null;
            }
            else
            {
                outgoingMessage = null;

                prepareOutgoingMessageException = new InvalidOperationException("No Encoder provided for the outgoing message.");
            }

            return outgoingMessage != null;
        }

        protected override Message PrepareOutgoingFaultMessage(Message incomingMessage, Exception exception)
        {
            return base.PrepareFaultMessage(incomingMessage.Version, exception);
        }
    }
}
