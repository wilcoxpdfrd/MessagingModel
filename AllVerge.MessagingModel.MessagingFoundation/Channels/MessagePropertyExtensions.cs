using System;
using System.Net;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    public static class MessagePropertyExtensions
    {
        public static IPEndPoint ToEndPoint(this BaseEndpointMessageProperty endpointMessageProperty)
        {
            if (IPAddress.TryParse(endpointMessageProperty.Address, out IPAddress ipAddress))
                return new IPEndPoint(ipAddress, endpointMessageProperty.Port);
            return null;
        }
    }
}
