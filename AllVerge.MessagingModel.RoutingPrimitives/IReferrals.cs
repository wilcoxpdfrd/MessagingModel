using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.RoutingPrimitives
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions;
    using schemas.xmlsoap.org.ws._2001._10.referral;

    /// <summary>
    /// Interface provides a method to lookup a (set of) forwarding <see cref="EndpointAddress"/>.
    /// </summary>
    public interface IReferrals
    {
        /// <summary>
        /// Looks up a (set of) <see cref="EndpointAddress"/> that the message is to be forwarded to, 
        /// and an action for the message.
        /// </summary>
        /// <param name="message">The <see cref="Message"/> to be processed.</param>
        /// <param name="interactionStyle">The <see cref="InteractionStyles"/> value that represents the style of message interaction usded by the endpoints.</param>
        /// <param name="action">The action for the message.</param>
        /// 
        /// <returns></returns>
        EndpointAddress[] LookupDestinations(ref Message message, InteractionStyles interactionStyle, out String action);
    }
}
