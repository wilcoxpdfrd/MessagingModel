using System;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Xml;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Http;

    /// <summary>
    /// Outgoing message event arguments class.
    /// </summary>
    public class OutgoingMessageEventArgs :
        EventArgs
    {
        private IncomingMessageEventArgs incomingMessageEventArgs;
        private HttpRequestMessageProperty incomingRequestMessageProperty;
        private DateTimeOffset? sentTimeUTC;
        private TimeSpan? duration;
        private String action;
        private IPrincipal principal;
        private String traceIdentifier;
        private MessageBuffer outgoingMessageBuffer;
        private MessageVersion outgoingMessageVersion;
        private MessageHeaders outgoingMessageHeaders;
        private MessageProperties outgoingMessageProperties;
        private bool outgoingMessageIsFault;
        private Object tag;

        /// <summary>
        /// Initializes a new instance of the <seealso cref="OutgoingMessageEventArgs"/> class.
        /// </summary>
        /// <param name="outgoingMessage"></param>
        /// <param name="principal"></param>
        /// <param name="traceIdentifier"></param>
        private OutgoingMessageEventArgs(Message outgoingMessage, IPrincipal principal, String traceIdentifier)
        {
            this.incomingMessageEventArgs = null;
            this.sentTimeUTC = null;
            this.duration = null;
            this.SetMembersFromMessage(outgoingMessage);
            this.principal = principal;
            this.traceIdentifier = traceIdentifier;
        }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="OutgoingMessageEventArgs"/> class
        /// </summary>
        /// <param name="outgoingMessage">The outgoing message.</param>
        /// <param name="outgoingMessageVersion"></param>
        /// <param name="outgoingMessageHeaders"></param>
        /// <param name="outgoingMessageProperties"></param>
        /// <param name="principal"></param>
        [Obsolete]
        public OutgoingMessageEventArgs(MessageBuffer outgoingMessage, MessageVersion outgoingMessageVersion, MessageHeaders outgoingMessageHeaders, MessageProperties outgoingMessageProperties, bool outgoingMessageIsFault, IPrincipal principal, String traceIdentifier)
        {
            this.incomingMessageEventArgs = null;
            this.sentTimeUTC = null;
            this.duration = null;
            this.outgoingMessageBuffer = outgoingMessage;
            this.outgoingMessageVersion = outgoingMessageVersion;
            this.outgoingMessageHeaders = outgoingMessageHeaders;
            this.outgoingMessageProperties = outgoingMessageProperties;
            this.outgoingMessageProperties.GetPropertyOrDefault<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name);
            this.outgoingMessageIsFault = outgoingMessageIsFault;
            this.principal = principal;
            this.traceIdentifier = traceIdentifier;
            if (outgoingMessageVersion != MessageVersion.None)
            {
                this.action = outgoingMessageHeaders.GetAction();
                if (outgoingMessageHeaders.MessageId == null)
                    outgoingMessageHeaders.MessageId = new UniqueId(Guid.NewGuid());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="incomingMessageEventArgs"></param>
        /// <param name="outgoingMessage"></param>
        private OutgoingMessageEventArgs(IncomingMessageEventArgs incomingMessageEventArgs, Message outgoingMessage)
        {
            this.sentTimeUTC = null;
            this.duration = null;
            this.incomingMessageEventArgs = incomingMessageEventArgs;
            HttpResponseMessageProperty httpResponseMessageProperty = 
                this.SetMembersFromMessage(outgoingMessage);
            if (this.outgoingMessageHeaders != null && this.outgoingMessageHeaders.RelatesTo == null)
                this.outgoingMessageHeaders.RelatesTo = incomingMessageEventArgs.MessageId;
            this.outgoingMessageProperties.CopyProperties(incomingMessageEventArgs.Properties);

            if (this.outgoingMessageIsFault)
            {
                if (outgoingMessageVersion == MessageVersion.None)
                {
                    MessageFault messageFault = MessageFault.CreateFault(this.outgoingMessageBuffer.CreateMessage(), Int32.MaxValue);

                    if (messageFault.Code.IsSenderFault)
                    {
                        httpResponseMessageProperty.StatusCode = HttpStatusCode.BadRequest;
                        httpResponseMessageProperty.StatusDescription = messageFault.Reason.GetMatchingTranslation().Text;
                    }
                    else if (messageFault.Code.IsReceiverFault)
                    {
                        httpResponseMessageProperty.StatusCode = HttpStatusCode.InternalServerError;
                        httpResponseMessageProperty.StatusDescription = messageFault.Reason.GetMatchingTranslation().Text;
                    }
                    else
                    {
                        httpResponseMessageProperty.StatusCode = HttpStatusCode.InternalServerError;
                        httpResponseMessageProperty.StatusDescription = httpResponseMessageProperty.StatusCode.SupplyStatusCodeDescription();
                    }
                }
                else
                {
                    httpResponseMessageProperty.StatusCode = HttpStatusCode.InternalServerError;
                    httpResponseMessageProperty.StatusDescription = httpResponseMessageProperty.StatusCode.SupplyStatusCodeDescription();
                }
            }
            else
                this.outgoingMessageIsFault = false;
            this.principal = null;
            this.traceIdentifier = incomingMessageEventArgs.TraceIdentifier;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OutgoingMessageEventArgs"/> class
        /// </summary>
        /// <param name="incomingMessageEventArgs">The <see cref="IncomingMessageEventArgs"/> instance containing incoming message state.</param>
        /// <param name="outgoingMessage">The outgoing message.</param>
        /// <param name="outgoingMessageVersion"></param>
        /// <param name="outgoingMessageHeaders"></param>
        /// <param name="outgoingMessageProperties"></param>
        [Obsolete]
        public OutgoingMessageEventArgs(IncomingMessageEventArgs incomingMessageEventArgs, MessageBuffer outgoingMessage, MessageVersion outgoingMessageVersion, MessageHeaders outgoingMessageHeaders, MessageProperties outgoingMessageProperties, bool outgoingMessageIsFault)
        {
            this.incomingMessageEventArgs = incomingMessageEventArgs;
            this.sentTimeUTC = null;
            this.duration = null;
            this.outgoingMessageBuffer = outgoingMessage;
            this.outgoingMessageVersion = outgoingMessageVersion;
            this.outgoingMessageHeaders = outgoingMessageHeaders;
            this.outgoingMessageProperties = outgoingMessageProperties;
            this.outgoingMessageProperties.CopyProperties(incomingMessageEventArgs.Properties);
            HttpResponseMessageProperty httpExtendedResponseMessageProperty = 
                this.outgoingMessageProperties.GetPropertyOrDefault<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name);
            if (outgoingMessageIsFault)
            {
                this.outgoingMessageIsFault = true;
                if (outgoingMessageVersion == MessageVersion.None)
                {
                    MessageFault messageFault = MessageFault.CreateFault(outgoingMessage.CreateMessage(), Int32.MaxValue);

                    if (messageFault.Code.IsSenderFault)
                    {
                        httpExtendedResponseMessageProperty.StatusCode = HttpStatusCode.BadRequest;
                        httpExtendedResponseMessageProperty.StatusDescription = messageFault.Reason.GetMatchingTranslation().Text;
                    }
                    else if (messageFault.Code.IsReceiverFault)
                    {
                        httpExtendedResponseMessageProperty.StatusCode = HttpStatusCode.InternalServerError;
                        httpExtendedResponseMessageProperty.StatusDescription = messageFault.Reason.GetMatchingTranslation().Text;
                    }
                    else
                    {
                        httpExtendedResponseMessageProperty.StatusCode = HttpStatusCode.InternalServerError;
                        httpExtendedResponseMessageProperty.StatusDescription = httpExtendedResponseMessageProperty.StatusCode.SupplyStatusCodeDescription();
                    }
                }
                else
                {
                    httpExtendedResponseMessageProperty.StatusCode = HttpStatusCode.InternalServerError;
                    httpExtendedResponseMessageProperty.StatusDescription = httpExtendedResponseMessageProperty.StatusCode.SupplyStatusCodeDescription();
                }
            }
            else
                this.outgoingMessageIsFault = false;
            this.principal = null;
            this.traceIdentifier = incomingMessageEventArgs.TraceIdentifier;
            if (outgoingMessageVersion != MessageVersion.None)
            {
                this.action = outgoingMessageHeaders.GetAction();
                if (outgoingMessageHeaders.MessageId == null)
                    outgoingMessageHeaders.MessageId = new UniqueId(Guid.NewGuid());
                if (outgoingMessageHeaders.RelatesTo == null)
                    outgoingMessageHeaders.RelatesTo = incomingMessageEventArgs.MessageId;
            }
        }

        private HttpResponseMessageProperty SetMembersFromMessage(Message message)
        {
            this.outgoingMessageVersion = message.Version;
            if (this.outgoingMessageVersion.Addressing != AddressingVersion.None)
            {
                this.outgoingMessageHeaders = message.Headers.Clone();
                this.action = outgoingMessageHeaders.GetAction();
                if (this.outgoingMessageHeaders.MessageId == null)
                    this.outgoingMessageHeaders.MessageId = new UniqueId(Guid.NewGuid());
            }
            this.outgoingMessageProperties = message.Properties.Clone();
            this.outgoingMessageIsFault = message.IsFault;
            this.outgoingMessageBuffer = message.CreateBufferedCopy(Int32.MaxValue);
            return this.outgoingMessageProperties.GetPropertyOrDefault<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name);
        }

        /// <summary>
        /// Gets the time the incoming message was received, or null if the outgoing message event is initiating the message exchange.
        /// </summary>
        public DateTimeOffset? ReceivedTimeUTC => this.incomingMessageEventArgs?.ReceivedTimeUTC;

        /// <summary>
        /// Gets the time the outgoing message was sent.
        /// </summary>
        public DateTimeOffset SentTimeUTC 
        {
            get
            {
                if (this.sentTimeUTC == null)

                    this.sentTimeUTC = DateTimeOffset.UtcNow;

                return this.sentTimeUTC.Value;
            }
        }

        /// <summary>
        /// Returns the duration between the time of dispatching the outgoing message and 
        /// the <see cref="ReceivedTimeUTC"/> of the incoming message, 
        /// or null if the outgoing message event is initiating the message exchange.
        /// </summary>
        public TimeSpan? Duration
        {
            get
            {
                if (this.duration == null && this.incomingMessageEventArgs != null)

                    this.duration = this.SentTimeUTC.Subtract(incomingMessageEventArgs.ReceivedTimeUTC);

                return this.duration;
            }
        }

        /// <summary>
        /// Gets the messaging <see cref="Principal"/>.
        /// </summary>
        public IPrincipal Principal => this.incomingMessageEventArgs?.Principal ?? this.principal;

        /// <summary>
        /// Gets the messaging authentication change delegate.  Called when authentication 
        /// causes the <see cref="Principal"/> to change.  Exposed here primarily to allow 
        /// dispatchers to unsubscribe from this delegate 
        /// via <see cref="global::ServiceModel.MessagingApplication.MessagingProtocolHandlerContext{MessagingContext}.RemoveAuthenticationChangeListener"/> event.
        /// </summary>
        internal Action<IPrincipal> OnAuthenticationChange => incomingMessageEventArgs.OnAuthenticationChange;

        /// <summary>
        /// Gets the <see cref="TraceIdentifier"/> for the message exchange.
        /// </summary>
        public string TraceIdentifier { get => this.traceIdentifier; }

        /// <summary>
        /// Gets the action of the received message.  
        /// Returns null if not an action message, or if the event is initiating the message exchange.
        /// </summary>
        public String ReceivedAction => this.incomingMessageEventArgs?.Action;

        /// <summary>
        /// Gets the set of acceptable content-types specified by the received message for the outgoing message, 
        /// or null if the event is initiating the message exchange.
        /// </summary>
        public StringValues ReceivedAcceptableContentTypes
        {
            get
            {
                if (this.incomingRequestMessageProperty == null)
                {
                    if (this.incomingMessageEventArgs != null)
                    {
                        this.incomingMessageEventArgs.Properties.TryGetProperty(HttpRequestMessageProperty.Name,
                            out this.incomingRequestMessageProperty);
                    }
                }

                if (this.incomingRequestMessageProperty != null)
                {
                    string acceptableCharset;

                    if (incomingRequestMessageProperty.Headers.TryGetHeaderValues(HttpHeaderNames.AcceptCharset, out String[] acceptableCharsets))

                        acceptableCharset = AcceptCharsetHeaderHelper.SelectAcceptableCharset(acceptableCharsets).HeaderName;

                    else

                        acceptableCharset = Encoding.UTF8.HeaderName;

                    if (incomingRequestMessageProperty.Headers.TryGetHeaderValues(HttpHeaderNames.Accept, out String[] acceptable))

                        return new StringValues(acceptable.Select(a => new MediaContentType(a, (MediaContentType.PARAMETER_KEY_CHARSET, acceptableCharset, false)).ToMediaTypePlusCharSet()).ToArray());
                }

                return StringValues.Empty;
            }
        }

        /// <summary>
        /// Gets the Reply-To header of the received message, or null if the event is initiating the message exchange.
        /// </summary>
        public EndpointAddress ReceivedReplyTo
        {
            get
            {
                if (this.incomingMessageEventArgs != null)
                {
                    return this.incomingMessageEventArgs.Headers.ReplyTo;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the message action.
        /// </summary>
        public String Action => this.action;

        /// <summary>
        /// Gets the message request Id for the message.
        /// </summary>
        public UniqueId MessageId => outgoingMessageHeaders?.MessageId;

        /// <summary>
        /// Gets the Id that relates the request message to another correlated message.
        /// </summary>
        public UniqueId RelatesTo => outgoingMessageHeaders?.RelatesTo;

        /// <summary>
        /// Gets the imcoming message, or null if the outgoing message event is initiating the message exchange.
        /// </summary>
        public MessageBuffer IncomingMessage => this.incomingMessageEventArgs?.IncomingMessage;

        /// <summary>
        /// Gets the outgoing message. 
        /// </summary>
        public MessageBuffer OutgoingMessage => this.outgoingMessageBuffer;

        /// <summary>
        /// Gets the <see cref="MessageVersion"/> of the outgoing message.
        /// </summary>
        public MessageVersion Version => this.outgoingMessageVersion;

        /// <summary>
        /// Gets the <see cref="MessageHeaders"/> of the outgoing message.
        /// </summary>
        public MessageHeaders Headers => this.outgoingMessageHeaders;

        /// <summary>
        /// Gets the outgoing message properties.
        /// </summary>
        public MessageProperties Properties => this.outgoingMessageProperties;

        /// <summary>
        /// Indicates whether the outgoing message is a fault message.
        /// </summary>
        public bool OutgoingMessageIsFault => this.outgoingMessageIsFault;

        /// <summary>
        /// Gets any additional data for the message.
        /// </summary>
        public Object Tag => this.incomingMessageEventArgs == null ? this.tag : this.incomingMessageEventArgs.Tag;

        internal void InspectOutgoingMessage()
        {
            if (this.incomingMessageEventArgs != null && this.incomingMessageEventArgs.DispatchMessageInspectors.MessageInspectors != null)
            {
                Message outgoingMessage = this.outgoingMessageBuffer.CreateMessage();

                Object correlationState = this.incomingMessageEventArgs.DispatchMessageInspectors.CorrelationState;

                foreach (IDispatchMessageInspector messageInspector in this.incomingMessageEventArgs.DispatchMessageInspectors.MessageInspectors)
                {
                    messageInspector.BeforeSendReply(ref outgoingMessage, correlationState);
                }

                Debug.Assert(correlationState is CommunicationObject, "corrlelationState is not a CommunicationObject.");

                (correlationState as CommunicationObject).Close();

                this.outgoingMessageBuffer = outgoingMessage.CreateBufferedCopy(Int32.MaxValue);
            }
        }

        internal static OutgoingMessageEventArgs Create(Message outgoingMessage, IPrincipal user, string traceIdentifier)
        {
            return new OutgoingMessageEventArgs(outgoingMessage, user, traceIdentifier);
        }

        internal static OutgoingMessageEventArgs Create(IncomingMessageEventArgs incomingMessageEventArgs, Message outgoingMessage)
        {
            return new OutgoingMessageEventArgs(incomingMessageEventArgs, outgoingMessage);
        }
    }
}
