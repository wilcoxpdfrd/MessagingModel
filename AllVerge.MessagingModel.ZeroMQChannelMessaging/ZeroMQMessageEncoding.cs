using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public enum ZeroMQMessageEncoding
    {
        /// <summary>
        /// Indicates using Binary message encoder.
        /// </summary>
        Binary,

        /// <summary>
        /// Indicates using Text message encoder.
        /// </summary>
        // Text,
    }

    static partial class ZeroMQMessageEncodingHelper
    {
        internal static bool IsDefined(ZeroMQMessageEncoding value)
        {
            return
                value == ZeroMQMessageEncoding.Binary 
                //|| value == ZeroMQMessageEncoding.Text
                ;
        }
    }
}
