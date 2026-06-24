using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions
{
    public struct InteractionStyle
    {
        public const String None = "none";
        public const String Notification = "notification";
        public const String ReliableNotification = "reliable-notification";
        public const String MulticastNotification = "multicast-notification";
        public const String ReliableMulticastNotification = "reliable-multicast-notification";
        public const String SolicitResponse = "solicit-response";
        public const String SolicitResponseWithAck = "solicit-response-with-acknowledgement";
        public const String Request = "request";
        public const String RequestResponse = "request-response";
        public const String DuplexMessaging = "duplex-messaging";
        public const String NotAvailable = "not-available";
    }

    /// <summary>
    /// Interaction styles enumeration.  
    /// </summary>
    public enum InteractionStyles
    {
        /// <summary>
        /// Indicates that the connector does not send or receive any messages.
        /// </summary>
        [XmlEnum(InteractionStyle.None)]
        None,
        /// <summary>
        /// Indicates that the connector can send messages to a receiver.
        /// </summary>
        [XmlEnum(InteractionStyle.Notification)]
        Notification,
        /// <summary>
        /// Indicates that the connector can reliably send (ordered) messages to a receiver.  Retries may occur.
        /// </summary>
        [XmlEnum(InteractionStyle.ReliableNotification)]
        ReliableNotification,
        /// <summary>
        /// Indicates that the connector can send messages to (possibly) multiple receivers.
        /// </summary>
        [XmlEnum(InteractionStyle.MulticastNotification)]
        MulticastNotification,
        /// <summary>
        /// Indicates that the connector can reliably send (ordered) messages to (possibly) multiple receivers.  Retries may occur.
        /// </summary>
        [XmlEnum(InteractionStyle.ReliableMulticastNotification)]
        ReliableMulticastNotification,
        /// <summary>
        /// Indicates that the connector can send messages to a receiver and receive correlated response messages in reply.
        /// </summary>
        [XmlEnum(InteractionStyle.SolicitResponse)]
        SolicitResponse,
        /// <summary>
        /// Same as <see cref="SolicitResponse"/>, but in addition, an acknowledgement is sent to the receiver by the connector upon receipt of the of the correlated reply message.
        /// </summary>
        [XmlEnum(InteractionStyle.SolicitResponseWithAck)]
        SolicitResponseWithAck,
        /// <summary>
        /// Indicates that the connector can receive request messages.
        /// </summary>
        [XmlEnum(InteractionStyle.Request)]
        Request,
        /// <summary>
        /// Indicates that the connector can receive request messages, and send correlated messages in reply.
        /// </summary>
        [XmlEnum(InteractionStyle.RequestResponse)]
        RequestResponse,
        /// <summary>
        /// Indicates that the connector can both receive and send messages.  The messages may or may not be correlated.
        /// </summary>
        [XmlEnum(InteractionStyle.DuplexMessaging)]
        DuplexMessaging,
        /// <summary>
        /// Indicates that an interaction style is not available.
        /// </summary>
        [XmlEnum(InteractionStyle.NotAvailable)]
        NotAvailable
    }
}
