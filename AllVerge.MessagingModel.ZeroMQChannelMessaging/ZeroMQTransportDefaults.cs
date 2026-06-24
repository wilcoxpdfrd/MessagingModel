using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration;

    static class ZeroMQReliableSessionDefaults
    {
        //internal const string AcknowledgementIntervalString = "00:00:00.2";
        //internal static TimeSpan AcknowledgementInterval { get { return TimeSpanHelper.FromMilliseconds(200, AcknowledgementIntervalString); } }
        internal const bool Enabled = false;
        //internal const bool FlowControlEnabled = true;
        internal const string InactivityTimeoutString = "00:10:00";
        internal static TimeSpan InactivityTimeout { get { return TimeSpanHelper.FromMinutes(10, InactivityTimeoutString); } }
        //internal const int MaxPendingChannels = 4;
        //internal const int MaxRetryCount = 8;
        //internal const int MaxTransferWindowSize = 8;
        internal const bool Ordered = true;
        //internal static ReliableMessagingVersion ReliableMessagingVersion { get { return System.ServiceModel.ReliableMessagingVersion.WSReliableMessagingFebruary2005; } }
        //internal const string ReliableMessagingVersionString = "WSReliableMessagingFebruary2005";
    }

    static class ZeroMQTransportDefaults 
    {
        public const bool TransactionsEnabled = false;
        public const ZeroMQTransportProtocols ZeroMQTransportProtocol = ZeroMQTransportProtocols.TCP;
        public const TransferMode TransferMode = System.ServiceModel.TransferMode.Buffered;
        internal static TransactionProtocol TransactionProtocol
        {
            get { return TransactionProtocol.Default; }
        }
    }

    //static class ZeroMQTransferTransportDefaults
    //{
    //    public const HostNameComparisonMode HostNameComparisonMode = global::System.ServiceModel.HostNameComparisonMode.StrongWildcard;
    //    public const bool KeepAliveEnabled = true;
    //    public const string Realm = "";
    //    public const ZeroMQTransportProtocols ZeroMQTransportProtocol = ZeroMQTransportProtocols.TCP;
    //    public const TransferMode TransferMode = global::System.ServiceModel.TransferMode.Streamed;
    //    public const bool ManualAddressing = false;
    //    public const int DefaultMaxPendingAccepts = 0;
    //    public const int MaxPendingAcceptsUpperLimit = 100000;
        
    //    // We use 0 as the default value of the MaxPendingAccepts property on HttpTransportBindingElement. In 4.5 we always
    //    // use 10 under the hood if the default value is picked. In future releases, we could adjust the underlying default
    //    // value when we have the dynamic expending pattern of BeginGetContext call implemented and the heap fragmentation issue
    //    // from NCL layer solved.
    //    const int PendingAcceptsConstant = 10;

    //    public static TimeSpan RequestInitializationTimeout => TimeSpanHelper.FromMilliseconds(0, RequestInitializationTimeoutString);
    //    public const string RequestInitializationTimeoutString = "00:00:00";

    //    public static int GetEffectiveMaxPendingAccepts(int maxPendingAccepts)
    //    {
    //        return maxPendingAccepts == ZeroMQTransferTransportDefaults.DefaultMaxPendingAccepts ?
    //                                    PendingAcceptsConstant :
    //                                    maxPendingAccepts;
    //    }

    //    public static MessageEncoderFactory GetDefaultMessageEncoderFactory()
    //    {
    //        return new TextMessageEncoderFactory(MessageVersion.Default, TextEncoderDefaults.Encoding, EncoderDefaults.MaxReadPoolSize, EncoderDefaults.MaxWritePoolSize, EncoderDefaults.ReaderQuotas);
    //    }

    //}
}
