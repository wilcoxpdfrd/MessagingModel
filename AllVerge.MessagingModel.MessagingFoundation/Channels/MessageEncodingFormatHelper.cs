using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    internal static class MessageEncodingFormatHelper
    {
        internal static bool IsDefined(MessageEncodingFormat messageEncodingFormat)
        {
            return
                messageEncodingFormat == MessageEncodingFormat.Default ||
                messageEncodingFormat == MessageEncodingFormat.Binary ||
                messageEncodingFormat == MessageEncodingFormat.BinaryPlusGzip ||
                messageEncodingFormat == MessageEncodingFormat.BinaryPlusDeflate ||
                messageEncodingFormat == MessageEncodingFormat.Soap11WSAddressing10 ||
                messageEncodingFormat == MessageEncodingFormat.Soap11WSAddressingAugust2004 ||
                messageEncodingFormat == MessageEncodingFormat.Soap11 ||
                messageEncodingFormat == MessageEncodingFormat.Soap12WSAddressing10 ||
                messageEncodingFormat == MessageEncodingFormat.Soap12WSAddressingAugust2004 ||
                messageEncodingFormat == MessageEncodingFormat.Soap12 ||
                messageEncodingFormat == MessageEncodingFormat.Xml ||
                messageEncodingFormat == MessageEncodingFormat.Text ||
                messageEncodingFormat == MessageEncodingFormat.Html ||
                messageEncodingFormat == MessageEncodingFormat.Json ||
                messageEncodingFormat == MessageEncodingFormat.Raw;
        }
    }
}
