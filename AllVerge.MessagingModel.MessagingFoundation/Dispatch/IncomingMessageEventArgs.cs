using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Xml;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    using AllVerge.MessagingModel.MessagingFoundation.Description;
    using AllVerge.MessagingModel.MessagingFoundation.Http;
    using System.Diagnostics;

    /// <summary>
    /// Incoming message event arguments class.
    /// </summary>
    public class IncomingMessageEventArgs :
        EventArgs
    {
        internal const string DispatchOperationMessageFilterMatchedPropertyName = "DispatchOperationMessageFilterMatched";
        internal const string DispatchOperationUriMatchedPropertyName = "DispatchOperationUriMatched";
        internal const string DispatchOperationUriTemplateMatchResultsPropertyName = "DispatchOperationUriTemplateMatchResults";
        internal const string DispatchOperationSelectorDataPropertyName = "DispatchOperationSelectorData";
        internal const string DispatchAccessControlOperationNamePropertyName = "DispatchAccessControlOperationName";
        internal const string DispatchOperationAccessDataPropertyName = "DispatchOperationAccessData";
        internal const string DispatchOperationNamePropertyName = "DispatchOperationName";
        internal const string DispatchOperationRedirectUriPropertyName = "DispatchOperationRedirectUri";

        private (List<IDispatchMessageInspector>, object) dispatchMessageInspectors;
        private OutgoingMessageEventArgs outgoingMessageEventArgs;
        private DateTimeOffset receivedTimeUTC;
        private String receivedMethod;
        private String receivedBaseUriAndPath;
        private TimeSpan? duration;
        private MessageVersion incomingMessageVersion;
        private MessageHeaders incomingMessageHeaders;
        private MessageProperties incomingMessageProperties;
        private MessageBuffer incomingMessageBuffer;
        private string action;
        private IPrincipal principal;
        private IPEndPoint remoteIPEndpoint;
        private Uri referrer;
        private UniqueId requestId;
        private UniqueId relatesTo;
        private UniqueId correlationId;
        private String traceIdentifier;
        private String sessionId;
        private Object tag;

        /// <summary>
        /// Initializes a new instance of the <seealso cref="IncomingMessageEventArgs"/> class with values suitable to reflect a "null" event.
        /// </summary>
        /// <param name="messageVersion">The <see cref="MessageVersion"/> for the message.</param>
        /// <param name="action">The action for the message.</param>
        public IncomingMessageEventArgs(MessageVersion messageVersion, String action = null)
        {
            if (action == null) action = messageVersion.Addressing.Anonymous;
            this.outgoingMessageEventArgs = null;
            this.receivedTimeUTC = DateTime.Now;
            this.receivedMethod = null;
            this.receivedBaseUriAndPath = action;
            this.duration = null;
            this.dispatchMessageInspectors = (null, null);
            using (Message message = Message.CreateMessage(messageVersion, action))
            {
                this.SetMembersFromMessage(message);
            }
            this.action = action;
            this.principal = new GenericPrincipal(new GenericIdentity(String.Empty), Array.Empty<String>());
            this.remoteIPEndpoint = null;
            this.referrer = null;
            this.relatesTo = null;
            this.traceIdentifier = null;
            this.sessionId = null;
        }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="IncomingMessageEventArgs"/> class
        /// </summary>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="receivedMethod">The method used to receive the message.</param>
        /// <param name="receivedBaseUriAndPath">The base Uri and path for the message.</param>
        /// <param name="principal">The principal for the message.</param>
        /// <param name="traceIdentifier">The trace identifier for the message.</param>
        /// <param name="sessionId">The session Id for the message.</param>
        public IncomingMessageEventArgs(Message receivedMessage, String receivedMethod, String receivedBaseUriAndPath, IPrincipal principal, Uri referrer, IPEndPoint remoteIPEndpoint, UniqueId requestId, UniqueId relatesTo, UniqueId correlationId, String traceIdentifier, String sessionId = null)
        {
            this.outgoingMessageEventArgs = null;
            this.receivedTimeUTC = GetStartTimeOrUTCNow(receivedMessage);
            this.receivedMethod = receivedMethod;
            this.receivedBaseUriAndPath = receivedBaseUriAndPath;
            this.duration = null;
            this.dispatchMessageInspectors = (null, null);
            this.SetMembersFromMessage(receivedMessage);
            this.action = null;
            this.principal = principal;
            this.remoteIPEndpoint = remoteIPEndpoint;
            this.referrer = referrer;
            this.requestId = requestId;
            this.relatesTo = relatesTo;
            this.correlationId = correlationId;
            this.traceIdentifier = traceIdentifier;
            this.sessionId = sessionId;
        }

        private DateTimeOffset GetStartTimeOrUTCNow(Message receivedMessage)
        {
            if (receivedMessage.Properties.TryGetProperty<HttpMessagingContextProperty>(HttpMessagingContextProperty.Name, out HttpMessagingContextProperty httpMessagingContextProperty))

                return new DateTimeOffset(httpMessagingContextProperty.StartTimestamp, TimeSpan.Zero);

            return DateTime.UtcNow;

        }

        private void SetMembersFromMessage(Message message)
        {
            this.incomingMessageVersion = message.Version;
            if (this.incomingMessageVersion == MessageVersion.None)
                this.incomingMessageHeaders = null;
            else
                this.incomingMessageHeaders = message.Headers.Clone();
            this.incomingMessageProperties = message.Properties.Clone();
            this.incomingMessageBuffer = message.CreateBufferedCopy(Int32.MaxValue);
        }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="IncomingMessageEventArgs"/> class
        /// </summary>
        /// <param name="outgoingMessageEventArgs"></param>
        /// <param name="request">The request message.</param>
        /// <param name="incomingMessageVersion"></param>
        /// <param name="incomingMessageHeaders"></param>
        /// <param name="incomingMessageProperties"></param>
        public IncomingMessageEventArgs(OutgoingMessageEventArgs outgoingMessageEventArgs, Message incomingMessage)
        {
            this.outgoingMessageEventArgs = outgoingMessageEventArgs;
            this.receivedTimeUTC = GetStartTimeOrUTCNow(incomingMessage);
            this.duration = this.receivedTimeUTC.Subtract(outgoingMessageEventArgs.SentTimeUTC);
            this.dispatchMessageInspectors = (null, null);
            this.SetMembersFromMessage(incomingMessage);
            this.action = null;
            this.principal = outgoingMessageEventArgs.Principal;
            this.remoteIPEndpoint = null;
            this.referrer = null;
            this.relatesTo = null;
            this.traceIdentifier = outgoingMessageEventArgs.TraceIdentifier;
            this.sessionId = null;
            if (this.incomingMessageHeaders != null && this.incomingMessageHeaders.RelatesTo == null)
                this.incomingMessageHeaders.RelatesTo = this.outgoingMessageEventArgs.MessageId;
        }

        /// <summary>
        /// Gets the time the outgoing message was sent, or null if the incoming message event initiated the message exchange.
        /// </summary>
        public DateTimeOffset? SentTime => this.outgoingMessageEventArgs?.SentTimeUTC;

        /// <summary>
        /// Gets the time the incoming message was received.
        /// </summary>
        public DateTimeOffset ReceivedTimeUTC => this.receivedTimeUTC;

        /// <summary>
        /// Gets the method with which the incoming message was received, or null if the event initiated the message exchange.
        /// </summary>
        public string ReceivedMethod => this.receivedMethod;

        /// <summary>
        /// Gets the path at which the incoming message was received, or null if the event initiated the message exchange.
        /// </summary>
        public string ReceivedBaseUriAndPath => this.receivedBaseUriAndPath;

        /// <summary>
        /// Returns the duration between the time of receiving the incoming message and 
        /// the <see cref="SentTime"/> of the outgoing message, or null if the event 
        /// initiated the message exchange.
        /// </summary>
        public TimeSpan? Duration => this.duration;

        /// <summary>
        /// Gets the outgoing message, or null if the event initiated the message exchange.
        /// </summary>
        public MessageBuffer OutgoingMessage => this.outgoingMessageEventArgs?.OutgoingMessage;

        /// <summary>
        /// Gets the buffered incoming message.
        /// </summary>
        public MessageBuffer IncomingMessage => this.incomingMessageBuffer;

        /// <summary>
        /// Gets the <see cref="MessageVersion"/> of the incoming message.
        /// </summary>
        public MessageVersion Version => this.incomingMessageVersion;

        /// <summary>
        /// Gets the <see cref="MessageHeaders"/> of the incoming message.
        /// </summary>
        public MessageHeaders Headers => this.incomingMessageHeaders;

        /// <summary>
        /// Gets the <see cref="MessageProperties"/> of the incoming message.
        /// </summary>
        public MessageProperties Properties => this.incomingMessageProperties;

        /// <summary>
        /// Gets the action for the message.
        /// </summary>
        public string Action
        {
            get
            {
                if (this.action == null)
                {
                    if (this.incomingMessageProperties.TryGetProperty(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty requestMessageProperty))
                    {
                        this.action = requestMessageProperty.GetAction();
                    }

                    if (this.action == null)
                    {
                        MessageHeaders messageHeaders = this.incomingMessageHeaders;

                        if (messageHeaders != null)
                        {
                            Uri to = messageHeaders.To;

                            if (to != null)

                                this.action = new UriBuilder(to.Scheme, to.Host, to.Port, to.AbsolutePath).ToString();
                        }
                    }
                }

                return this.action;
            }
        }

        /// <summary>
        /// Gets the message request Id for the incoming message. 
        /// </summary>
        public UniqueId MessageId => this.incomingMessageHeaders?.MessageId;


        /// <summary>
        /// Gets the <see cref="Principal"/> for the message. 
        /// </summary>
        public IPrincipal Principal => this.principal;

        /// <summary>
        /// Gets or sets the <see cref="EndpointAddress"/> which will recieve any reply message(s).
        /// </summary>
        public EndpointAddress ReplyTo
        {
            get => this.incomingMessageHeaders?.ReplyTo;
        }

        /// <summary>
        /// Gets the <see cref="Uri"/> of the resource the referred the request.
        /// </summary>
        Uri Referrer => this.referrer;

        /// <summary>
        /// Gets the remote IP endpoint of the request.
        /// </summary>
        IPEndPoint RemoteIPEndpoint => this.remoteIPEndpoint;

        /// <summary>
        /// Gets or sets the Identifier that uniquely identifies this message.
        /// </summary>
        public UniqueId RequestId
        {
            get => this.incomingMessageHeaders?.MessageId ?? this.requestId;
        }

        /// <summary>
        /// Gets or sets the Id that relates this message to other correlated messages.
        /// </summary>
        public UniqueId RelatesTo
        {
            get => this.incomingMessageHeaders?.RelatesTo ?? this.relatesTo;
        }

        /// <summary>
        /// Gets or sets the Id that relates this message to other correlated messages.
        /// </summary>
        public UniqueId CorrelationId
        {
            get => this.correlationId;
        }

        /// <summary>
        /// Gets the <see cref="SessionId"/> for the message exchange.
        /// </summary>
        public string SessionId { get => sessionId; }

        /// <summary>
        /// Gets the <see cref="TraceIdentifier"/> for the message exchange.
        /// </summary>
        public string TraceIdentifier { get => this.traceIdentifier; }

        /// <summary>
        /// Gets or sets any additional data for the message.
        /// </summary>
        public Object Tag => this.outgoingMessageEventArgs == null ? this.tag : this.outgoingMessageEventArgs.Tag;

        internal (List<IDispatchMessageInspector> MessageInspectors, object CorrelationState) DispatchMessageInspectors => dispatchMessageInspectors;


        internal void OnAuthenticationChange(IPrincipal principal)
        {
            this.principal = principal;
        }

        internal void InspectIncomingMessage(IList<IDispatchMessageInspector> messageInspectors, object service, Func<Object, EndpointAddress, UniqueId, Message> mapOutputResult)
        {
            if (messageInspectors.Count > 0)
            {
                Message incomingMessage = this.incomingMessageBuffer.CreateMessage();

                InstanceContext instanceContext = new InstanceContext(service);

                instanceContext.Extensions.Add(new MapResultOuputMessage(mapOutputResult));

                instanceContext.Open();

                List<IDispatchMessageInspector> dispatchMessageInspectors = new List<IDispatchMessageInspector>();

                foreach (IDispatchMessageInspector dispatchMessageInspector in messageInspectors)
                {
                    Object state = dispatchMessageInspector.AfterReceiveRequest(ref incomingMessage, null, instanceContext);

                    Debug.Assert(state == instanceContext, "Dispatch message inspector did not return InstanceContext from AfterReceiveRequest.");

                    dispatchMessageInspectors.Add(dispatchMessageInspector);
                }

                this.dispatchMessageInspectors = (dispatchMessageInspectors, instanceContext);

                this.incomingMessageBuffer = incomingMessage.CreateBufferedCopy(Int32.MaxValue);
            }
        }

        public static IncomingMessageEventArgs Create(MessageVersion receivedMessageVersion, String receivedAction = null)
        {
            return new IncomingMessageEventArgs(receivedMessageVersion, receivedAction);
        }

        public static IncomingMessageEventArgs Create(Message receivedMessage, String receivedMethod, String receivedPath, Uri referrer, IPEndPoint remoteIPEndpoint, UniqueId requestId, UniqueId relatesTo, UniqueId correlationId, String traceIdentifier, string sessionId = null)
        {
            return Create(receivedMessage, receivedMethod, receivedPath, null, referrer, remoteIPEndpoint, requestId, relatesTo, correlationId, traceIdentifier, sessionId);
        }

        public static IncomingMessageEventArgs Create(Message receivedMessage, String receivedMethod, String receivedPath, IPrincipal user, Uri referrer, IPEndPoint remoteIPEndpoint, UniqueId requestId, UniqueId relatesTo, UniqueId correlationId, String traceIdentifier, string sessionId = null)
        {
            return new IncomingMessageEventArgs(receivedMessage, receivedMethod, receivedPath, user, referrer, remoteIPEndpoint, requestId, relatesTo, correlationId, traceIdentifier, sessionId);
        }
    }
}
