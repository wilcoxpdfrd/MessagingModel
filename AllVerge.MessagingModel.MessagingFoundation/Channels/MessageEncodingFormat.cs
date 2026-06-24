using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    /// <summary>Specifies the supported message encoding formats.</summary>
	public enum MessageEncodingFormat
    {
        /// <summary>Defaults to the Binary format.</summary>
        Default,
        /// <summary>The Soap11 format.</summary>
        Soap11,
        /// <summary>The Soap11 and WS-Addressing submitted August 2004 format.</summary>
        Soap11WSAddressingAugust2004,
        /// <summary>The Soap11 and WS-Addressing 1.0 format.</summary>
        Soap11WSAddressing10,
        /// <summary>The Soap12 format.</summary>
        Soap12,
        /// <summary>The Soap11 and WS-Addressing submitted August 2004 format.</summary>
        Soap12WSAddressingAugust2004,
        /// <summary>The Soap11 and WS-Addressing 1.0 format.</summary>
        Soap12WSAddressing10,
        /// <summary>The JSON format.</summary>
        Json,
        /// <summary>The XML format.</summary>
        Xml,
        /// <summary>The Plain Text format</summary>
        Text,
        /// <summary>The Html format</summary>
        Html,
        /// <summary>The Form UrlEncoded format.</summary>
        FormUrlEncoded,
        /// <summary>The Multipart Form-Data format.</summary>
        FormMultipartData,
        /// <summary>The "Binary" format.</summary>
        Binary,
        /// <summary>The "Binary plus gzip" format.</summary>
        BinaryPlusGzip,
        /// <summary>The "Binary plus deflate" format.</summary>
        BinaryPlusDeflate,
        /// <summary>The "Raw" (byte stream) format.</summary>
        Raw
    }
}
