using AllVerge.Core.ServiceModel.Transfer;
using System;
using System.ServiceModel;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQChannelUtilities
    {
        internal static Exception CreateCommunicationException(ZeroMQListenerException listenerException)
        {
            return new CommunicationException(listenerException.Message, listenerException);
        }

        internal static void EnsureRequestMessageContentNotNull(ZeroMQRequestMessage requestMessage)
        {
            if (requestMessage.Content == null)
            {
                requestMessage.Content = new ByteArrayMessageContent(requestMessage.RequestContext, EmptyArray<byte>.Instance);
            }
        }

        public static bool IsEmpty(ZeroMQResponseMessage responseMessage)
        {
            return
                responseMessage.Content == null ||
                (responseMessage.Content.ContentLength.HasValue &&
                responseMessage.Content.ContentLength.Value == 0);
        }
    }
}