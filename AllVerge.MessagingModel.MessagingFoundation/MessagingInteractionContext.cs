using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Xml;
using System.Security.Principal;
using System.ServiceModel;

using AllVerge.MessagingModel.MessagingFoundation.Channels;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
using AllVerge.MessagingModel.MessagingFoundation.Dispatch;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    using global::System.ServiceModel.Channels;
    using System.ComponentModel.Design;

    /// <summary>
    /// Provides state for the messaging interaction life cycle.
    /// </summary>
    public abstract class MessagingInteractionContext
    {
        private static readonly string PROPERTY_NAME = nameof(MessagingInteractionContext);

        private IServiceContainer services;
        private IncomingMessageEventArgs incomingMessageEventArgs;
        private OutgoingMessageEventArgs outgoingMessageEventArgs;
        private IInteractionContextChannel channel;

        private IPrincipal ambientPrincipal = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingInteractionContext"/> class
        /// </summary>
        /// <param name="incomingMessageEventArgs">The <see cref="IncomingMessageEventArgs"/> instance containing the request state.</param>
        protected MessagingInteractionContext(IServiceContainer services, IncomingMessageEventArgs incomingMessageEventArgs, IChannel channel = null)
        {
            this.services = services;
            this.incomingMessageEventArgs = incomingMessageEventArgs;
            this.outgoingMessageEventArgs = null;
            if (channel == null)
            {
                Uri to;
                if (incomingMessageEventArgs.Version.Addressing == AddressingVersion.None)
                    to = incomingMessageEventArgs.Properties.Via;
                else
                    to = incomingMessageEventArgs.Headers.To;
                if (incomingMessageEventArgs.Headers.ReplyTo != null)
                    this.channel =
                        InteractionChannel.CreateChannel(
                            incomingMessageEventArgs.Version,
                            new EndpointAddress(incomingMessageEventArgs.Headers.ReplyTo.Uri),
                            to);
                else if (incomingMessageEventArgs.Headers.From != null)
                    this.channel =
                        InteractionChannel.CreateChannel(
                            incomingMessageEventArgs.Version,
                            new EndpointAddress(incomingMessageEventArgs.Headers.From.Uri),
                            to);
                else
                {
                    String address;
                    if (incomingMessageEventArgs.Version.Addressing == AddressingVersion.WSAddressing10)
                        address = PublicXD.GetDictionaryString(PublicXD.Dictionaries.Addressing10Dictionary, "Anonymous").Value;
                    else if (incomingMessageEventArgs.Version.Addressing == AddressingVersion.WSAddressingAugust2004)
                        address = PublicXD.GetDictionaryString(PublicXD.Dictionaries.Addressing200408Dictionary, "Anonymous").Value;
                    else
                        address = PublicXD.GetDictionaryString(PublicXD.Dictionaries.AddressingNoneDictionary, "Namespace").Value;
                    this.channel =
                            InteractionChannel.CreateChannel(
                                incomingMessageEventArgs.Version,
                                new EndpointAddress(address),
                                to);
                }
            }
            else

                this.channel = 
                    InteractionChannel.CreateChannel(channel);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingInteractionContext"/> class
        /// </summary>
        /// <param name="outgoingMessageEventArgs">The <see cref="IncomingMessageEventArgs"/> instance containing the request state.</param>
        protected MessagingInteractionContext(OutgoingMessageEventArgs outgoingMessageEventArgs)
        {
            this.services = null;
            this.outgoingMessageEventArgs = outgoingMessageEventArgs;
            this.incomingMessageEventArgs = null;
            if (outgoingMessageEventArgs.Headers.From != null)
                this.channel =
                    InteractionChannel.CreateChannel(
                        outgoingMessageEventArgs.Version,
                        new EndpointAddress(outgoingMessageEventArgs.Headers.To),
                        outgoingMessageEventArgs.Headers.From.Uri);
            else if (outgoingMessageEventArgs.Headers.ReplyTo != null)
                this.channel =
                    InteractionChannel.CreateChannel(
                        outgoingMessageEventArgs.Version,
                        new EndpointAddress(outgoingMessageEventArgs.Headers.To),
                        outgoingMessageEventArgs.Headers.ReplyTo.Uri);
            else
                this.channel =
                    InteractionChannel.CreateChannel(
                        outgoingMessageEventArgs.Version,
                        new EndpointAddress(outgoingMessageEventArgs.Headers.To),
                        null);
        }

        public IInteractionContextChannel Channel
        {
            get
            {
                return this.channel;
            }
        }

        public IMessagingDispatcher Dispatcher { get; set; }

        /// <summary>Gets the message properties for the incoming message in the <see cref="MessagingInteractionContext" />.</summary>
        /// <returns>A <see cref="MessageProperties" /> object that contains the message properties for the incoming message.</returns>
        public virtual MessageProperties IncomingMessageProperties => this.incomingMessageEventArgs?.Properties;

        /// <summary>Gets the message properties for the outbound message in the active <see cref="T:MessageContext" />.</summary>
        /// <returns>A <see cref="MessageProperties" /> object that contains the message properties on the outbound message.</returns>
        public virtual MessageProperties OutgoingMessageProperties => this.outgoingMessageEventArgs?.Properties;

        /// <summary>
        /// Gets the start time of the request.
        /// </summary>
        public virtual DateTimeOffset StartTimeUTC { get; }

        /// <summary>
        /// Gets the <see cref="Principal"/> for the request. 
        /// </summary>
        public virtual IPrincipal Principal { get; }

        /// <summary>
        /// Gets the action for the request.
        /// </summary>
        public virtual string Action { get; }

        /// <summary>
        /// Gets the Id for the request message.
        /// </summary>
        public virtual UniqueId MessageId { get; }

        /// <summary>
        /// Gets the Id that relates this message to other correlated messages.
        /// </summary>
        public virtual UniqueId RelatedTo { get; }

        /// <summary>
        /// Gets the <see cref="MessageVersion"/> of the incoming message.
        /// </summary>
        public MessageVersion IncomingVersion => this.incomingMessageEventArgs?.Version;

        /// <summary>
        /// Gets the incoming message.
        /// </summary>
        public MessageBuffer IncomingMessage
        {
            get
            {
                if (this.incomingMessageEventArgs != null)

                    return this.incomingMessageEventArgs.IncomingMessage;

                if (this.outgoingMessageEventArgs != null)

                    return this.outgoingMessageEventArgs.IncomingMessage;

                return null;
            }
        }

        public Stream IncomingMessageBodyStream
        {
            get
            {
                return this.IncomingMessage?.CreateMessage().GetBodyContentsAsCharacterStream();
            }
        }

        /// <summary>
        /// Gets the <see cref="MessageVersion"/> of the outgoing message.
        /// </summary>
        public MessageVersion OutgoingVersion => this.outgoingMessageEventArgs?.Version;

        /// <summary>
        /// Gets the outgoing message.
        /// </summary>
        public MessageBuffer OutgoingMessage
        {
            get
            {
                if (this.incomingMessageEventArgs != null)

                    return this.incomingMessageEventArgs.OutgoingMessage;

                if (this.outgoingMessageEventArgs != null)

                    return this.outgoingMessageEventArgs.OutgoingMessage;

                return null;
            }
        }

        /// <summary>
        /// Gets any additional data for the request.
        /// </summary>
        public virtual Object Tag { get; }

        internal IncomingMessageEventArgs IncomingMessageEventArgs
        {
            get => this.incomingMessageEventArgs;

            set
            {
                if (value != null)
                {
                    if (CanSetIncomingMessageEventArgs(value))

                        this.incomingMessageEventArgs = value;
                }
            }
        }

        protected virtual bool CanSetIncomingMessageEventArgs(IncomingMessageEventArgs incomingMessageEventArgs)
        {
            return false;
        }

        internal OutgoingMessageEventArgs OutgoingMessageEventArgs
        {
            get => this.outgoingMessageEventArgs;

            set
            {
                if (value != null)
                {
                    if (CanSetOutgoingMessageEventArgs(value))

                        this.outgoingMessageEventArgs = value;
                }
            }
        }

        protected virtual bool CanSetOutgoingMessageEventArgs(OutgoingMessageEventArgs outgoingMessageEventArgs)
        {
            return false;
        }

        /// <summary>
        /// Returns the call back channel <typeparamref name="T"/> when a <see cref="ServiceChannel"/> is present.  Otherwise returns null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        public T GetCallbackChannel<T>() where T : class
        {
            if (this.Channel != null)
            {
                return this.Channel.GetCallbackChannel<T>(this.Dispatcher);
            }

            return null;
        }

        /// <summary>
        /// Gets the <see cref="MessagingInteractionContext"/> for the current request.
        /// </summary>
        public static MessagingInteractionContext Current
        {
            get
            {
                if ((MessagingInteractionContextAccessor.Current != null) && MessagingInteractionContextAccessor.Current.InteractionContext != null)

                    return MessagingInteractionContextAccessor.Current.InteractionContext;

                return null;
            }
        }

        public IServiceContainer Services { get => services; }

        // ToDo: remove dependency on thread currentprincipal; thread can change from use of Tasks ...
        // better to create a context object in allverge.core.threading that uses
        // both threadlocal and async local to hold user (check both), this can pass through to that.
        // LogEvent can just use that ...
        private void SetThreadCurrentPrincipal()
        {
            if (this.ambientPrincipal == null)

                this.ambientPrincipal = Thread.CurrentPrincipal;

            Thread.CurrentPrincipal = this.Principal;
        }

        public void ResetThreadCurrentPrincipal()
        {
            Thread.CurrentPrincipal = this.ambientPrincipal;

            this.ambientPrincipal = null;
        }

        internal static MessagingInteractionContext SetCurrentMessageContext(IServiceContainer services, IncomingMessageEventArgs incomingMessageEventArgs, IChannel channel = null)
        {
            if (MessagingInteractionContextAccessor.Current == null)
            {
                MessagingInteractionContextAccessor.Current = new MessagingInteractionContextAccessor();
            }

            if (incomingMessageEventArgs == null)
            {
                throw new ArgumentNullException(nameof(incomingMessageEventArgs));
            }

            if (Current != null)
            {
                if (Current.IncomingMessageEventArgs != null)

                    throw new InvalidOperationException($"Invalid attempt to re-set {PROPERTY_NAME}::{nameof(IncomingMessageEventArgs)} for the request.");

                Current.IncomingMessageEventArgs = incomingMessageEventArgs;
            }
            else
            {
                MessagingInteractionContext messageContext = new IncomingMessageInteractionContext(services, incomingMessageEventArgs, channel);

                messageContext.SetThreadCurrentPrincipal();

                MessagingInteractionContextAccessor.Current.SetInteractionContext(messageContext);
            }

            return Current;
        }

        internal static MessagingInteractionContext SetCurrentMessageContext(OutgoingMessageEventArgs outgoingMessageEventArgs)
        {
            if (MessagingInteractionContextAccessor.Current == null)
            {
                MessagingInteractionContextAccessor.Current = new MessagingInteractionContextAccessor();
            }

            if (Current != null)
            {
                if (Current.OutgoingMessageEventArgs != null)

                    throw new InvalidOperationException($"Invalid attempt to re-set {PROPERTY_NAME}::{nameof(OutgoingMessageEventArgs)} for the request.");

                // outgoingMessageEventArgs may be null, 
                // if there was no dispatcher found to route the request to ...

                Current.OutgoingMessageEventArgs = outgoingMessageEventArgs;
            }
            else
            {
                if (outgoingMessageEventArgs == null)
                {
                    throw new ArgumentNullException(nameof(outgoingMessageEventArgs));
                }

                MessagingInteractionContext interactionContext = new OutgoingMessageInteractionContext(outgoingMessageEventArgs);

                interactionContext.SetThreadCurrentPrincipal();

                MessagingInteractionContextAccessor.Current.SetInteractionContext(interactionContext);
            }

            return Current;
        }

        internal static void ClearCurrentMessageContext()
        {
            if (Current != null)
            {
                Current.ResetThreadCurrentPrincipal();

                MessagingInteractionContextAccessor.Current.SetInteractionContext(null);
            }
        }
    }

    public static class MessagingContextExtensions
    {
        public static bool TryGetIncomingMessageDispatchAccessControlOperationName(this MessagingInteractionContext interactionContext, out String dispatchAccessControlOperationName)
        {
            return interactionContext.IncomingMessageProperties.TryGetProperty(IncomingMessageEventArgs.DispatchAccessControlOperationNamePropertyName, out dispatchAccessControlOperationName);
        }

        public static bool TryGetIncomingMessageDispatchOperationAccessData(this MessagingInteractionContext interactionContext, out DispatchOperationAccessData dispatchOperationAccessData)
        {
            return interactionContext.IncomingMessageProperties.TryGetProperty(IncomingMessageEventArgs.DispatchOperationAccessDataPropertyName, out dispatchOperationAccessData);
        }

        public static bool TryGetIncomingMessageDispatchOperationName(this MessagingInteractionContext interactionContext, out String dispatchOperationName)
        {
            return interactionContext.IncomingMessageProperties.TryGetProperty(IncomingMessageEventArgs.DispatchOperationNamePropertyName, out dispatchOperationName);
        }
    }
}
