using AllVerge.Core.ServiceModel.Transfer;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public class ZeroMQResponseMessage : ResponseMessageBase
    {
        internal ZeroMQResponseMessage(Message response, ZeroMQRequestMessage requestMessage) : 
            base(response, requestMessage)
        {
        }
    }
}