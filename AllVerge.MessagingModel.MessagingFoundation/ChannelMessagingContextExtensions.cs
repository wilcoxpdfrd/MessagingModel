using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml.Linq;

using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingFoundation.Channels;
using AllVerge.MessagingModel.MessagingFoundation.Faults;

using AllVerge.SystemPrimitives.Collections;

using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    public static class ChannelMessagingContextExtensions
    {
        public static ChannelMessageContext GetRejectedMessageContext(this IMessagingContext<ChannelMessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders, string faultAction = null)
        {
            MessageVersion messageVersion = messagingContext.InputContext?.Message.Version ?? MessageVersion.None;

            AddressingVersion addressingVersion = null;

            if (messageVersion.Addressing == AddressingVersion.WSAddressing10)
                addressingVersion = AddressingVersion.WSAddressing10;
            else if (messageVersion.Addressing == AddressingVersion.WSAddressingAugust2004)
                addressingVersion = AddressingVersion.WSAddressingAugust2004;
            else if (messageVersion.Addressing == AddressingVersion.None)
                addressingVersion = AddressingVersion.None;
            else
                addressingVersion = AddressingVersion.None;

            string addressingVersionNs = PublicXD.GetDictionaryString(addressingVersion, "Namespace").Value;

            switch (rejectionCode)
            {
                default:
                case RejectCode.BindingUnreachable:
                    return ChannelMessageContext.Create(
                        messagingContext.InputContext,
                        Message.CreateMessage(
                            messageVersion,
                            RootFaultCode.CreateSenderFaultCode(
                                new FaultCode(
                                    PublicXD.AddressingStringConstants.DestinationUnreachable,
                                    addressingVersionNs)).WrapFaultCode(messageVersion.Envelope),
                            String.Empty,
                            faultAction));
                case RejectCode.NotAuthorized:
                    return ChannelMessageContext.Create(
                        messagingContext.InputContext,
                        Message.CreateMessage(
                            messageVersion,
                            RootFaultCode.CreateSenderFaultCode(
                                new FaultCode(
                                    PublicXD.Addressing200408StringConstants.MessageInformationHeaderRequired,
                                    addressingVersionNs)).WrapFaultCode(messageVersion.Envelope),
                            "A required message information header is not present.",
                            rejectionHeaders[RejectHeaders.Authenticate],
                            faultAction));
                case RejectCode.TooBusy:
                    return ChannelMessageContext.Create(
                        messagingContext.InputContext,
                        Message.CreateMessage(
                            messageVersion,
                            RootFaultCode.CreateSenderFaultCode(
                                new FaultCode(
                                    PublicXD.AddressingStringConstants.EndpointUnavailable,
                                    addressingVersionNs)).WrapFaultCode(messageVersion.Envelope),
                            "The endpoint is unable to process the message at this time.",
                            XDocument.Parse($"<wsa:RetryAfter xmlns:wsa={addressingVersionNs}>{rejectionHeaders[RejectHeaders.RetryAfter]}</wsa:RetryAfter>"),
                            faultAction));
                case RejectCode.Timeout:
                    return ChannelMessageContext.Create(
                        messagingContext.InputContext,
                        Message.CreateMessage(
                            messageVersion,
                            RootFaultCode.CreateReceiverFaultCode(
                                new FaultCode("Timeout")).WrapFaultCode(messageVersion.Envelope),
                            "The endpoint timed out before completely processing the message.",
                            faultAction));
                case RejectCode.Faulted:
                    return GetFaultMessageContext(messagingContext, faultAction);
                case RejectCode.NotHandled:
                    return ChannelMessageContext.Create(
                        messagingContext.InputContext,
                        Message.CreateMessage(
                            messageVersion,
                            RootFaultCode.CreateReceiverFaultCode(
                                new FaultCode("NotHandled")).WrapFaultCode(messageVersion.Envelope),
                            "The endpoint accepted the message for handling but .",
                            faultAction));
            }
        }

        private static ChannelMessageContext GetFaultMessageContext(IMessagingContext<ChannelMessageContext> messagingContext, string faultAction = null)
        {
            if (!messagingContext.Items.TryGetValue(out Exception e))

                e = new Exception("Fault details not available.");

            Message receivedMessage = messagingContext.InputContext.Message;

            MessageVersion messageVersion = messagingContext.InputContext.Message.Version;

            RootFaultCode rootFaultCode = RootFaultCode.CreateReceiverFaultCode();

            if (faultAction == null) faultAction = receivedMessage.Headers.FaultTo?.Uri.AbsoluteUri;

            return ChannelMessageContext.Create(messagingContext.InputContext, e.CreateFaultMessage(rootFaultCode, messageVersion, faultAction));
        }

    }
}
