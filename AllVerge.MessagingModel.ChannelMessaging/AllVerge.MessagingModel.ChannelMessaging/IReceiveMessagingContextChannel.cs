using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    using AllVerge.MessagingModel.ChannelMessaging.Channels;

    /// <summary>
    /// Defines the supported messaging interaction patterns.
    /// </summary>
    public enum MessagingInteractions
    {
        /// <summary>
        /// A one-way messaging interaction
        /// </summary>
        Simplex,
        /// <summary>
        /// A one-way messaging interaction, followed by an responsive one-way messaging interaction
        /// </summary>
        HalfDuplex,
        /// <summary>
        /// An asynchronous messaging interaction
        /// </summary>
        Duplex,
    }

    /// <summary>
    /// Defines the application messaging interaction pattern that is supported by the messaging server.
    /// </summary>
    /// <remarks>
    /// Messaging interactions are typically <em>not</em> initiated by the messaging server.  Then <see cref="IsInitiating"/> is false, and:
    /// <list type="bullet">
    /// <item>The <see cref="MessagingInteractions.Simplex"/> (Receive) pattern is where a one-way message is received by the messaging server</item>
    /// <item>The <see cref="MessagingInteractions.HalfDuplex"/> (RequestResponse) pattern is where a request message is received by the messaging server, and a response message is subsequently sent by the messaging server</item>
    /// <item>The <see cref="MessagingInteractions.Duplex"/> (AsynchronousRequestResponse) pattern is where a message is received by the messaging server, and a response message is asynchronously sent by the messaging server</item>
    /// </list>
    /// Messaging interactions <em>can</em> be initiated by the messaging server, however.  Then <see cref="IsInitiating"/> is true, and:
    /// <list type="bullet">
    /// <item>The <see cref="MessagingInteractions.Simplex"/> (Send) pattern is where a one-way message is sent by the messaging server</item>
    /// <item>The <see cref="MessagingInteractions.HalfDuplex"/> (SolicitResponse) pattern is where a solicit message is sent by the messaging server, and a response message is subsequently received by the messaging server</item>
    /// <item>The <see cref="MessagingInteractions.Duplex"/> (AsynchronousSolicitResponse) pattern is where a solicit message is sent by the messaging server, and a response message is asynchronously received by the messaging server</item>
    /// </list>
    /// Note: Composite messaging interactions can be implemented using the basic <see cref="MessagingInteractions"/>, for example:
    /// <list type="bullet">
    /// <item>Polling can be implemented using SolicitResponse pattern, where the receiver of the solict message can respond with any available message(s), or a NULL message if none are available, followed by a Send message to to acknowledge receipt of the message(s)</item>
    /// <item>Subscriptions can be implemented using AsyncSolicitResponse with <see cref="IsInitiating"/> is true and the <see cref="MessagingInteractions.Duplex"/> pattern, where the receiver of the solicit message begins asynchronously publishing response messages as any are available</item>
    /// </list>
    /// </remarks>
    public struct MessagingChannelInteractions
    {
        public static readonly MessagingChannelInteractions Send = new MessagingChannelInteractions(true, MessagingInteractions.Simplex);
        public static readonly MessagingChannelInteractions Received = new MessagingChannelInteractions(false, MessagingInteractions.Simplex);
        public static readonly MessagingChannelInteractions RequestResponse = new MessagingChannelInteractions(false, MessagingInteractions.HalfDuplex);
        public static readonly MessagingChannelInteractions AsynchronousRequestResponse = new MessagingChannelInteractions(false, MessagingInteractions.Duplex);
        public static readonly MessagingChannelInteractions SolicitResponse = new MessagingChannelInteractions(true, MessagingInteractions.HalfDuplex);
        public static readonly MessagingChannelInteractions AsynchronousSolicitResponse = new MessagingChannelInteractions(true, MessagingInteractions.Duplex);
        public static readonly MessagingChannelInteractions Poll = new MessagingChannelInteractions(true, MessagingInteractions.HalfDuplex, MessagingInteractions.Simplex);

        public MessagingChannelInteractions(bool isInitiating, params MessagingInteractions[] interactions)
        {
            this.Interactions = interactions;
            this.IsInitiating = isInitiating;
        }

        /// <summary>
        /// Indicates whether the messaging interaction is initiated on the messaging channel server.
        /// </summary>
        public bool IsInitiating { get; }

        /// <summary>
        /// An ordered set of <see cref="MessagingInteractions"/>.
        /// </summary>
        public MessagingInteractions[] Interactions { get; }

        /// <summary>
        /// Indicates whether <paramref name="comparand"/> has the same <see cref="IsInitiating"/> and <see cref="Interactions"/> as the instance.  If <paramref name="considerExtensionEqual"/> is true, then ignores any extra <see cref="Interactions"/> items in <paramref name="comparand"/> beyond those in the instance.
        /// </summary>
        /// <param name="comparand"></param>
        /// <param name="considerExtensionEqual"></param>
        /// <returns></returns>
        public bool IsEqual(MessagingChannelInteractions comparand, bool considerExtensionEqual = false)
        {
            if (this.IsInitiating == comparand.IsInitiating)
            {
                if (considerExtensionEqual)
                {
                    if (comparand.Interactions.Length > this.Interactions.Length)
                        return this.Interactions.SequenceEqual(comparand.Interactions.Take(this.Interactions.Length));
                    return false;
                }
                return this.Interactions.SequenceEqual(comparand.Interactions) && this.IsInitiating == comparand.IsInitiating;
            }
            return false;
        }

        /// <summary>
        /// Returns a string representation of the instance, including whether the channel initiates messaging on the messaging server, and the <see cref="Interactions"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{(this.IsInitiating ? "Initiating:  " : "")}{String.Join(", ", this.Interactions.Select(i => i.ToString()))}";
        }

        public static MessagingChannelInteractions ValidateIsExtending(MessagingChannelInteractions interactions, MessagingChannelInteractions extendingInteractions)
        {
            if (interactions.IsEqual(extendingInteractions, true))
            {
                return extendingInteractions;
            }

            throw new ArgumentException();
        }
    }

    /// <summary>
    /// Defines an abstract messaging context channel.
    /// </summary>
    /// <remarks>
    /// Defines common messaging context channel members but not any particular channel messaging exchange pattern;
    /// classes that wish to implement a messaging context channel with this interface and a defined messaging 
    /// exchange pattern should derive from an abstract class such as 
    /// <see cref="AbstractRequestResponseMessagingContextChannel{MessageContext}"/>, 
    /// <see cref="AbstractAsynchronousRequestResponseMessagingContextChannel{MessageContext}"/> or 
    /// <see cref="AbstractSolicitResponseMessagingContextChannel{MessageContext}"/>, etc.
    /// </remarks>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IReceiveMessagingContextChannel<MessageContext> :
        IMessagingContextChannel<MessageContext>
    {
        /// <summary>
        /// The <see cref="Uri"/> the channel listens on.
        /// </summary>
        Uri ListenUri { get; }
        /// <summary>
        /// Configures <paramref name="messagingContext"/> with any channel level properties used for binding to the messaging middleware pipeline.
        /// </summary>
        /// <param name="receivedMessagingContext"></param>
        void ConfigureChannelProperties(IMessagingContext<MessageContext> messagingContext);
        /// <summary>
        /// A callback invoked after <paramref name="receivedMessagingContext"/> has been handled by the messaging middleware pipeline.
        /// </summary>
        /// <param name="messagingContext"></param>
        /// <returns></returns>
        Task HandledMessagingCallBackAsync(IMessagingContext<MessageContext> messagingContext);
    }
}
