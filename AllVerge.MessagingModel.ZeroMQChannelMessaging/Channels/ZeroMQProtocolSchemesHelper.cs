using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.SystemPrimitives.Net;

    public struct ZeroMQProtocolSchemesHelper
    {
        private const string ZEROMQ_NORMAL_IPC_SCHEME_DELIMITED = "inproc://";
        private const string ZEROMQ_NORMAL_TCP_SCHEME_DELIMITED = "tcp://";

        internal static string NormalizeServerAddress(String serverAddress, bool forListener = false, bool forRequestResponseSockets = false)
        {
            if (serverAddress.StartsWith(TransportProtocolSchemes.ZEROMQ_IPC_DELIMITED))

                serverAddress = $"{ZEROMQ_NORMAL_IPC_SCHEME_DELIMITED}{serverAddress.Substring(TransportProtocolSchemes.ZEROMQ_IPC_DELIMITED.Length).TrimEnd('/')}";

            else if (serverAddress.StartsWith(TransportProtocolSchemes.ZEROMQ_TCP_DELIMITED))
            {
                serverAddress = $"{ZEROMQ_NORMAL_TCP_SCHEME_DELIMITED}{serverAddress.Substring(TransportProtocolSchemes.ZEROMQ_TCP_DELIMITED.Length).TrimEnd('/')}";

                if (forListener)

                    serverAddress = serverAddress.Replace("localhost", "*").TrimEnd('/');
            }
            else if (serverAddress.StartsWith("zeromq."))

                serverAddress = serverAddress.Substring(7);

            // https://netmq.readthedocs.io/en/latest/request-response/#how-it-works

            if (forRequestResponseSockets)
            {
                if (forListener)

                    return "@" + serverAddress;

                else

                    return ">" + serverAddress;
            }

            return serverAddress;
        }

        internal static string DenormalizeServerAddress(String serverAddress, bool forListener, bool forRequestResponseSockets = false)
        {
            if (forRequestResponseSockets)
            {
                if (forListener)

                    serverAddress = serverAddress.TrimStart('@');

                else

                    serverAddress = serverAddress.TrimStart('>');
            }

            if (serverAddress.StartsWith(ZEROMQ_NORMAL_IPC_SCHEME_DELIMITED))

                serverAddress = $"{TransportProtocolSchemes.ZEROMQ_IPC_DELIMITED}{serverAddress.Substring(ZEROMQ_NORMAL_IPC_SCHEME_DELIMITED.Length)}";

            else if (serverAddress.StartsWith(ZEROMQ_NORMAL_TCP_SCHEME_DELIMITED))
            {
                serverAddress = $"{TransportProtocolSchemes.ZEROMQ_TCP_DELIMITED}{serverAddress.Substring(TransportProtocolSchemes.ZEROMQ_TCP_DELIMITED.Length).TrimEnd('/')}";

                if (forListener)

                    serverAddress = serverAddress.Replace("*", "localhost");
            }
            else if (!serverAddress.StartsWith("zeromq."))

                serverAddress = $"zeromq.{serverAddress}";

            return serverAddress;
        }
    }

    public static class ZeroMQProtocolSchemesExtensions
    {
        public static Uri ReceivedZeroMQRequestBaseUri(this Uri requestUri)
        {
            if (requestUri != null && requestUri.IsAbsoluteUri)
            {
                String serverAddress = ZeroMQProtocolSchemesHelper.DenormalizeServerAddress(requestUri.AbsoluteUri, true);

                if (serverAddress.StartsWith(TransportProtocolSchemes.ZEROMQ_IPC_DELIMITED))

                    return new Uri(new Uri(serverAddress).GetLeftPart(UriPartial.Authority));

                return new Uri(serverAddress);
            }

            return null;
        }

        public static String ReceivedZeroMQRequestUriPath(this Uri requestUri)
        {
            if (requestUri != null && requestUri.IsAbsoluteUri)
            {
                String serverAddress = ZeroMQProtocolSchemesHelper.DenormalizeServerAddress(requestUri.AbsoluteUri, true);

                return new Uri(serverAddress).AbsolutePath;
            }

            return null;
        }
    }
}
