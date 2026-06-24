using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    using AllVerge.SystemPrimitives.Net.Mime;
    using global::System.ServiceModel.Channels;
    using System.Text;

    public class TransferTransportFactorySettings : ITransferTransportFactorySettings
    {
        class MessagingContentTypeMapper : IMessagingContentTypeMapper
        {
            public string GetContentTypeForTransferMessageEncoding(MessageEncodingFormat transferMessageEncoding, out MessageVersion messageVersion)
            {
                switch (transferMessageEncoding)
                {
                    case MessageEncodingFormat.Json:
                        messageVersion = MessageVersion.None;
                        return MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE;
                    case MessageEncodingFormat.Soap11WSAddressing10:
                        messageVersion = MessageVersion.Soap11WSAddressing10;
                        return MediaTypeConstants.TEXT_XML_MEDIA_TYPE;
                    case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                        messageVersion = MessageVersion.Soap11WSAddressingAugust2004;
                        return MediaTypeConstants.TEXT_XML_MEDIA_TYPE;
                    case MessageEncodingFormat.Soap11:
                        messageVersion = MessageVersion.Soap11;
                        return MediaTypeConstants.TEXT_XML_MEDIA_TYPE;
                    case MessageEncodingFormat.Soap12WSAddressing10:
                        messageVersion = MessageVersion.Soap12WSAddressing10;
                        return MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE;
                    case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                        messageVersion = MessageVersion.Soap12WSAddressingAugust2004;
                        return MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE;
                    case MessageEncodingFormat.Soap12:
                        messageVersion = MessageVersion.Soap12;
                        return MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE;
                    case MessageEncodingFormat.Xml:
                        messageVersion = MessageVersion.None;
                        return MediaTypeConstants.APPLICATION_XML_MEDIA_TYPE;
                    case MessageEncodingFormat.Binary:
                    case MessageEncodingFormat.Default:
                    default:
                        messageVersion = MessageVersion.Soap12WSAddressing10;
                        return MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_MEDIA_TYPE;
                    case MessageEncodingFormat.BinaryPlusGzip:
                        messageVersion = MessageVersion.Soap12WSAddressing10;
                        return MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_PLUS_GZIP_MEDIA_TYPE;
                    case MessageEncodingFormat.BinaryPlusDeflate:
                        messageVersion = MessageVersion.Soap12WSAddressing10;
                        return MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_PLUS_DEFLATE_MEDIA_TYPE;
                    case MessageEncodingFormat.Raw:
                        messageVersion = MessageVersion.None;
                        return MediaTypeConstants.APPLICATION_OCTET_STREAM_MEDIA_TYPE;
                }
            }

            public MessageEncodingFormat GetTransferMessageEncodingForContentType(string contentType, AddressingVersion addressingVersion = null)
            {
                String normalizedMediaType;

                String mediaType = new MediaContentType(contentType).MediaType;

                if (MediaTypes.TryGetNormalizedResourceMediaType(mediaType, out normalizedMediaType))
                {
                    switch (normalizedMediaType)
                    {
                        case MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE:
                        case MediaTypeConstants.APPLICATION_SCHEMA_PLUS_JSON_MEDIA_TYPE:
                            return MessageEncodingFormat.Json;
                        case MediaTypeConstants.TEXT_XML_MEDIA_TYPE:
                            if (addressingVersion == null)
                                return MessageEncodingFormat.Xml;
                            if (addressingVersion == AddressingVersion.WSAddressing10)
                                return MessageEncodingFormat.Soap11WSAddressing10;
                            else if (addressingVersion == AddressingVersion.WSAddressingAugust2004)
                                return MessageEncodingFormat.Soap11WSAddressingAugust2004;
                            else if (addressingVersion == AddressingVersion.None)
                                return MessageEncodingFormat.Soap11;
                            else
                                return MessageEncodingFormat.Xml;
                        case MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE:
                            if (addressingVersion == AddressingVersion.WSAddressing10)
                                return MessageEncodingFormat.Soap12WSAddressing10;
                            else if (addressingVersion == AddressingVersion.WSAddressingAugust2004)
                                return MessageEncodingFormat.Soap12WSAddressingAugust2004;
                            else if (addressingVersion == AddressingVersion.None)
                                return MessageEncodingFormat.Soap12;
                            throw new ArgumentOutOfRangeException(nameof(addressingVersion), $"{addressingVersion} not compatible with ${contentType}.");
                        case MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_MEDIA_TYPE:
                            if (addressingVersion == null || addressingVersion == AddressingVersion.WSAddressing10)
                                return MessageEncodingFormat.Binary;
                            throw new ArgumentOutOfRangeException(nameof(addressingVersion), $"{addressingVersion} not compatible with ${contentType}.");
                        case MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_PLUS_GZIP_MEDIA_TYPE:
                            if (addressingVersion == null || addressingVersion == AddressingVersion.WSAddressing10)
                                return MessageEncodingFormat.BinaryPlusGzip;
                            throw new ArgumentOutOfRangeException(nameof(addressingVersion), $"{addressingVersion} not compatible with ${contentType}.");
                        case MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_PLUS_DEFLATE_MEDIA_TYPE:
                            if (addressingVersion == null || addressingVersion == AddressingVersion.WSAddressing10)
                                return MessageEncodingFormat.BinaryPlusDeflate;
                            throw new ArgumentOutOfRangeException(nameof(addressingVersion), $"{addressingVersion} not compatible with ${contentType}.");
                        case MediaTypeConstants.APPLICATION_XML_MEDIA_TYPE:
                        case MediaTypeConstants.APPLICATION_SCHEMA_PLUS_XML_MEDIA_TYPE:
                            if (addressingVersion == null || addressingVersion == AddressingVersion.None)
                                return MessageEncodingFormat.Xml;
                            throw new ArgumentOutOfRangeException(nameof(addressingVersion), $"{addressingVersion} not compatible with ${contentType}.");
                        case MediaTypeConstants.APPLICATION_OCTET_STREAM_MEDIA_TYPE:
                            if (addressingVersion == null || addressingVersion == AddressingVersion.None)
                                return MessageEncodingFormat.Raw;
                            throw new ArgumentOutOfRangeException(nameof(addressingVersion), $"{addressingVersion} not compatible with ${contentType}.");
                    }
                }

                throw new InvalidOperationException($"Content-Type {normalizedMediaType} is not supported by the transfer transport factory.");
            }
        }

        public int MaxBufferSize => Int32.MaxValue;

        public TransferMode TransferMode => TransferMode.Buffered;

        public bool ManualAddressing => false;

        public BufferManager BufferManager => BufferManager.CreateBufferManager(Int32.MaxValue, Int32.MaxValue);

        public int MaxReadPoolSize => EncoderDefaults.MaxReadPoolSize;

        public Encoding Encoding => TextEncoderDefaults.Encoding;

        public int MaxWritePoolSize => EncoderDefaults.MaxWritePoolSize;
        
        public int MaxSessionSize => BinaryEncoderDefaults.MaxSessionSize;

        public long MaxReceivedMessageSize => TransportDefaults.MaxReceivedMessageSize;

        public MessageEncodingFormat MessageEncoding => MessageEncodingFormat.Default;

        public bool JavascriptCallbackEnabled => false;

        public IMessagingContentTypeMapper ContentTypeMapper => new MessagingContentTypeMapper();

        public MessageEncoderFactory MessageEncoderFactory => 
            new CompositeMessageEncodingFormatEncoderFactory(
                this.Encoding,
                this.MaxReadPoolSize,
                this.MaxWritePoolSize,
                new XmlDictionaryReaderQuotas(),
                this.MaxSessionSize,
                this.MaxReceivedMessageSize,
                this.MessageEncoding,
                this.JavascriptCallbackEnabled,
                this.ContentTypeMapper);

        public MessageVersion MessageVersion => MessageVersion.Default;

        public virtual TimeSpan CloseTimeout => TimeSpan.MaxValue;

        public virtual TimeSpan OpenTimeout => TimeSpan.MaxValue;

        public virtual TimeSpan ReceiveTimeout => TimeSpan.MaxValue;

        public virtual TimeSpan SendTimeout => TimeSpan.MaxValue;

        public bool KeepAliveEnabled => false;
    }
}
