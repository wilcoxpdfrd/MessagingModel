using System;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Diagnostics;
using AllVerge.SystemPrimitives.Net.Mime;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    class CompositeMessageEncodingFormatEncoderFactory : MessageEncoderFactory, IContentTypeMessageEncoderFactorySelector
    {
        private class CompositeMessageEncodingFormatEncoder : MessageEncoder
        {
            string mediaType;
            string contentType;
            IMessagingContentTypeMapper contentTypeMapper;
            MessageVersion messageVersion;

            // Double-checked locking pattern requires volatile for read/write synchronization
            Encoding writeEncoding;

            int maxReadPoolSize;
            int maxWritePoolSize;
            XmlDictionaryReaderQuotas readerQuotas;
            int maxSessionSize;
            long maxReceivedMessageSize;
            bool javascriptCallbackEnabled;

            volatile MessageEncoderFactory nullMessageEncoderFactory;
            volatile MessageEncoderFactory jsonMessageEncoderFactory;
            volatile MessageEncoderFactory rawMessageEncoderFactory;
            volatile MessageEncoderFactory textMessageEncoderFactory;
            volatile MessageEncoderFactory plainTextMessageEncoderFactory;
            volatile MessageEncoderFactory markupMessageEncoderFactory;
            volatile MessageEncoderFactory binaryMessageEncoderFactory;
            volatile MessageEncoderFactory binaryPlusGzipMessageEncoderFactory;
            volatile MessageEncoderFactory binaryPlusDeflateMessageEncoderFactory;
            volatile MessageEncoderFactory soap12WSAddressing10Factory;
            volatile MessageEncoderFactory soap12WSAddressingAugust2004MessageEncoderFactory;
            volatile MessageEncoderFactory soap12MessageEncoderFactory;
            volatile MessageEncoderFactory soap11WSAddressing10MessageEncoderFactoryFactory;
            volatile MessageEncoderFactory soap11WSAddressingAugust2004MessageEncoderFactory;
            volatile MessageEncoderFactory soap11MessageEncoderFactory;

            object thisLock;


            public CompositeMessageEncodingFormatEncoder(Encoding writeEncoding, int maxReadPoolSize, int maxWritePoolSize, XmlDictionaryReaderQuotas quotas, int maxSessionSize, long maxReceivedMessageSize, bool javascriptCallbackEnabled = false, MessageEncodingFormat transferFormat = MessageEncodingFormat.Default, IMessagingContentTypeMapper contentTypeMapper = null)
            {
                if (writeEncoding == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("writeEncoding");
                }

                this.thisLock = new object();

                TextEncoderDefaults.ValidateEncoding(writeEncoding);
                this.writeEncoding = writeEncoding;

                this.maxReadPoolSize = maxReadPoolSize;
                this.maxWritePoolSize = maxWritePoolSize;
                this.readerQuotas = new XmlDictionaryReaderQuotas();
                quotas.CopyTo(this.readerQuotas);
                this.maxSessionSize = maxSessionSize;
                this.maxReceivedMessageSize = maxReceivedMessageSize;
                this.javascriptCallbackEnabled = javascriptCallbackEnabled;
                this.contentTypeMapper = contentTypeMapper;

                this.mediaType = this.GetContentTypeForFormat(transferFormat, out this.messageVersion);

                this.contentType = GetContentType(mediaType, writeEncoding);
            }

            public override string MediaType
            {
                get { return this.mediaType; }
            }

            public override string ContentType
            {
                get { return this.contentType; }
            }

            public override MessageVersion MessageVersion
            {
                get { return this.messageVersion; }
            }

            public MessageEncoderFactory NullMessageEncoderFactory
            {
                get
                {
                    if (nullMessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (nullMessageEncoderFactory == null)
                            {
                                nullMessageEncoderFactory =
                                    new NullMessageEncoderFactory();
                            }
                        }
                    }
                    return nullMessageEncoderFactory;
                }
            }

            public MessageEncoderFactory JsonMessageEncoderFactory
            {
                get
                {
                    if (jsonMessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (jsonMessageEncoderFactory == null)
                            {
                                jsonMessageEncoderFactory = 
                                    new JsonMessageEncoderFactory(
                                        writeEncoding, 
                                        maxReadPoolSize, 
                                        maxWritePoolSize, 
                                        readerQuotas, 
                                        javascriptCallbackEnabled);
                            }
                        }
                    }
                    return jsonMessageEncoderFactory;
                }
            }

            public MessageEncoderFactory TextMessageEncoderFactory
            {
                get
                {
                    if (textMessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (textMessageEncoderFactory == null)
                            {
                                textMessageEncoderFactory = 
                                    new TextMessageEncoderFactory(
                                        MessageVersion.None, 
                                        writeEncoding, 
                                        maxReadPoolSize, 
                                        maxWritePoolSize, 
                                        readerQuotas);
                            }
                        }
                    }
                    return textMessageEncoderFactory;
                }
            }

            public MessageEncoderFactory PlainTextMessageEncoderFactory
            {
                get
                {
                    if (plainTextMessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (plainTextMessageEncoderFactory == null)
                            {
                                plainTextMessageEncoderFactory =
                                    new PlainTextMessageEncoderFactory(
                                        writeEncoding,
                                        maxReadPoolSize,
                                        maxWritePoolSize,
                                        readerQuotas);
                            }
                        }
                    }
                    return plainTextMessageEncoderFactory;
                }
            }

            public MessageEncoderFactory MarkupMessageEncoderFactory
            {
                get
                {
                    if (markupMessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (markupMessageEncoderFactory == null)
                            {
                                markupMessageEncoderFactory = 
                                    new HtmlMessageEncoderFactory(
                                        writeEncoding, 
                                        maxReadPoolSize, 
                                        maxWritePoolSize, 
                                        readerQuotas);
                            }
                        }
                    }
                    return markupMessageEncoderFactory;
                }
            }

            public MessageEncoderFactory Soap11WSAddressing10MessageEncoderFactory
            {
                get
                {
                    if (soap11WSAddressing10MessageEncoderFactoryFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (soap11WSAddressing10MessageEncoderFactoryFactory == null)
                            {
                                soap11WSAddressing10MessageEncoderFactoryFactory = 
                                    new TextMessageEncoderFactory(
                                        MessageVersion.Soap11WSAddressing10, 
                                        writeEncoding, 
                                        maxReadPoolSize, 
                                        maxWritePoolSize, 
                                        readerQuotas);
                            }
                        }
                    }
                    return soap11WSAddressing10MessageEncoderFactoryFactory;
                }
            }

            public MessageEncoderFactory BinaryMessageEncoderFactory
            {
                get
                {
                    if (binaryMessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (binaryMessageEncoderFactory == null)
                            {
                                binaryMessageEncoderFactory = 
                                    new BinaryMessageEncoderFactory(
                                        MessageVersion.Soap12, 
                                        maxReadPoolSize,
                                        maxWritePoolSize,
                                        maxSessionSize, 
                                        readerQuotas, 
                                        maxReceivedMessageSize, 
                                        BinaryVersion.Version1, 
                                        CompressionFormat.None);
                            }
                        }
                    }
                    return binaryMessageEncoderFactory;
                }
            }

            public MessageEncoderFactory BinaryPlusGzipMessageEncoderFactory
            {
                get
                {
                    if (binaryPlusGzipMessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (binaryPlusGzipMessageEncoderFactory == null)
                            {
                                binaryPlusGzipMessageEncoderFactory =
                                    new BinaryMessageEncoderFactory(
                                        MessageVersion.Soap12,
                                        maxReadPoolSize,
                                        maxWritePoolSize,
                                        maxSessionSize,
                                        readerQuotas,
                                        maxReceivedMessageSize,
                                        BinaryVersion.GZipVersion1,
                                        CompressionFormat.GZip);
                            }
                        }
                    }
                    return binaryPlusGzipMessageEncoderFactory;
                }
            }

            public MessageEncoderFactory BinaryPlusDeflateMessageEncoderFactory
            {
                get
                {
                    if (binaryPlusDeflateMessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (binaryPlusDeflateMessageEncoderFactory == null)
                            {
                                binaryPlusDeflateMessageEncoderFactory =
                                    new BinaryMessageEncoderFactory(
                                        MessageVersion.Soap12,
                                        maxReadPoolSize,
                                        maxWritePoolSize,
                                        maxSessionSize,
                                        readerQuotas,
                                        maxReceivedMessageSize,
                                        BinaryVersion.DeflateVersion1,
                                        CompressionFormat.Deflate);
                            }
                        }
                    }
                    return binaryPlusDeflateMessageEncoderFactory;
                }
            }

            public MessageEncoderFactory Soap11WSAddressingAugust2004MessageEncoderFactory
            {
                get
                {
                    if (soap11WSAddressingAugust2004MessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (soap11WSAddressingAugust2004MessageEncoderFactory == null)
                            {
                                soap11WSAddressingAugust2004MessageEncoderFactory = 
                                    new TextMessageEncoderFactory(
                                        MessageVersion.Soap11WSAddressingAugust2004, 
                                        writeEncoding, 
                                        maxReadPoolSize, 
                                        maxWritePoolSize, 
                                        readerQuotas);
                            }
                        }
                    }
                    return soap11WSAddressingAugust2004MessageEncoderFactory;
                }
            }

            public MessageEncoderFactory Soap11MessageEncoderFactory
            {
                get
                {
                    if (soap11MessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (soap11MessageEncoderFactory == null)
                            {
                                soap11MessageEncoderFactory = 
                                    new TextMessageEncoderFactory(
                                        MessageVersion.Soap11, 
                                        writeEncoding, 
                                        maxReadPoolSize, 
                                        maxWritePoolSize, 
                                        readerQuotas);
                            }
                        }
                    }
                    return soap11MessageEncoderFactory;
                }
            }

            public MessageEncoderFactory Soap12WSAddressing10MessageEncoderFactory
            {
                get
                {
                    if (soap12WSAddressing10Factory == null)
                    {
                        lock (ThisLock)
                        {
                            if (soap12WSAddressing10Factory == null)
                            {
                                soap12WSAddressing10Factory = 
                                    new TextMessageEncoderFactory(
                                        MessageVersion.Soap12WSAddressing10, 
                                        writeEncoding, 
                                        maxReadPoolSize, 
                                        maxWritePoolSize, 
                                        readerQuotas);
                            }
                        }
                    }
                    return soap12WSAddressing10Factory;
                }
            }

            public MessageEncoderFactory Soap12WSAddressingAugust2004MessageEncoderFactory
            {
                get
                {
                    if (soap12WSAddressingAugust2004MessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (soap12WSAddressingAugust2004MessageEncoderFactory == null)
                            {
                                soap12WSAddressingAugust2004MessageEncoderFactory = 
                                    new TextMessageEncoderFactory(
                                        MessageVersion.Soap12WSAddressingAugust2004, 
                                        writeEncoding, 
                                        maxReadPoolSize, 
                                        maxWritePoolSize, 
                                        readerQuotas);
                            }
                        }
                    }
                    return soap12WSAddressingAugust2004MessageEncoderFactory;
                }
            }

            public MessageEncoderFactory Soap12MessageEncoderFactory
            {
                get
                {
                    if (soap12MessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (soap12MessageEncoderFactory == null)
                            {
                                soap12MessageEncoderFactory = 
                                    new TextMessageEncoderFactory(
                                        MessageVersion.Soap12, 
                                        writeEncoding, 
                                        maxReadPoolSize, 
                                        maxWritePoolSize, 
                                        readerQuotas);
                            }
                        }
                    }
                    return soap12MessageEncoderFactory;
                }
            }

            public MessageEncoderFactory RawMessageEncoderFactory
            {
                get
                {
                    if (rawMessageEncoderFactory == null)
                    {
                        lock (ThisLock)
                        {
                            if (rawMessageEncoderFactory == null)
                            {
                                rawMessageEncoderFactory =
                                    new ByteStreamMessageEncodingBindingElement(readerQuotas).
                                        CreateMessageEncoderFactory();

                                // see the comments in IWebMessageEncoderHelper ...
                                ((IWebMessageEncoderHelper)rawMessageEncoderFactory.Encoder).EnableBodyReaderMoveToContent();
                            }
                        }
                    }
                    return rawMessageEncoderFactory;
                }
            }

            object ThisLock
            {
                get { return thisLock; }
            }

            public override bool IsContentTypeSupported(string contentType)
            {
                if (contentType == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("contentType");
                }

                if (TryGetContentTypeMapping(
                    contentType,
                    out MessageEncodingFormat transferFormat))
                {
                    return true;
                }

                return
                    JsonMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    MarkupMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    Soap11MessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    Soap11WSAddressing10MessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    Soap11WSAddressingAugust2004MessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    Soap12MessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    Soap12WSAddressing10MessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    Soap12WSAddressingAugust2004MessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    BinaryMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    BinaryPlusGzipMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    BinaryPlusDeflateMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    TextMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType) ||
                    RawMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType);
            }

            public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
            {
                if (bufferManager == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("bufferManager"));
                }

                MessageEncodingFormat transferFormat = GetTransferFormatForContentType(contentType);

                Message message;

                switch (transferFormat)
                {
                    case MessageEncodingFormat.Json:
                        
                        message = JsonMessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);
                        
                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.JsonProperty);
                        
                        break;

                    case MessageEncodingFormat.Xml:
                        
                        message = TextMessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);
                        
                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.XmlProperty);
                        
                        break;

                    case MessageEncodingFormat.Text:

                        message = PlainTextMessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.TextProperty);

                        break;

                    case MessageEncodingFormat.Html:
                        
                        message = MarkupMessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);
                        
                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.HtmlProperty);
                        
                        break;

                    case MessageEncodingFormat.Soap11WSAddressing10:

                        message = Soap11WSAddressing10MessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        if (message.Headers.Action != null)
                        {
                            message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11WSAddressing10Property);
                        }

                        break;

                    case MessageEncodingFormat.Soap11WSAddressingAugust2004:

                        message = Soap11WSAddressingAugust2004MessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        if (message.Headers.Action != null)
                        {
                            message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11WSAddressingAugust2004Property);
                        }

                        break;

                    case MessageEncodingFormat.Soap11:

                        message = Soap11MessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11Property);

                        break;

                    case MessageEncodingFormat.Soap12WSAddressing10:

                        message = Soap12WSAddressing10MessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        if (message.Headers.Action != null)
                        {
                            message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12WSAddressing10Property);
                        }

                        break;

                    case MessageEncodingFormat.Soap12WSAddressingAugust2004:

                        message = Soap12WSAddressingAugust2004MessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        if (message.Headers.Action != null)
                        {
                            message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12WSAddressingAugust2004Property);
                        }

                        break;

                    case MessageEncodingFormat.Soap12:

                        message = Soap12MessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12Property);

                        break;

                    case MessageEncodingFormat.Binary:

                        message = BinaryMessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryProperty);

                        break;

                    case MessageEncodingFormat.BinaryPlusGzip:

                        message = BinaryPlusGzipMessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryPlusGzipProperty);

                        break;

                    case MessageEncodingFormat.BinaryPlusDeflate:

                        message = BinaryPlusDeflateMessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryPlusDeflateProperty);

                        break;

                    case MessageEncodingFormat.Raw:

                        message = RawMessageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType);
                        
                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.RawProperty);

                        break;

                    default:

                        throw Fx.AssertAndThrow("This should never get hit because GetFormatForContentType shouldn't return a WebContentFormat other than Json, Xml, and Raw");
                }
                return message;
            }

            public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
            {
                if (stream == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("stream"));
                }

                MessageEncodingFormat transferFormat = GetTransferFormatForContentType(contentType);

                Message message;

                switch (transferFormat)
                {
                    case MessageEncodingFormat.Json:
                        
                        message = JsonMessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);
                        
                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.JsonProperty);
                        
                        break;

                    case MessageEncodingFormat.Xml:
                        
                        message = TextMessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);
                        
                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.XmlProperty);
                        
                        break;

                    case MessageEncodingFormat.Text:

                        message = PlainTextMessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.TextProperty);

                        break;

                    case MessageEncodingFormat.Html:
                        
                        message = MarkupMessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);
                        
                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.HtmlProperty);
                        
                        break;

                    case MessageEncodingFormat.Soap11WSAddressing10:

                        message = Soap11WSAddressing10MessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        if (message.Headers.Action != null)
                        {
                            message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11WSAddressing10Property);
                        }

                        break;

                    case MessageEncodingFormat.Soap11WSAddressingAugust2004:

                        message = Soap11WSAddressingAugust2004MessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        if (message.Headers.Action != null)
                        {
                            message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11WSAddressingAugust2004Property);
                        }

                        break;

                    case MessageEncodingFormat.Soap11:

                        message = Soap11MessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap11Property);

                        break;

                    case MessageEncodingFormat.Soap12WSAddressing10:

                        message = Soap12WSAddressing10MessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        if (message.Headers.Action != null)
                        {
                            message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12WSAddressing10Property);
                        }

                        break;

                    case MessageEncodingFormat.Soap12WSAddressingAugust2004:

                        message = Soap12WSAddressingAugust2004MessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        if (message.Headers.Action != null)
                        {
                            message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12WSAddressingAugust2004Property);
                        }

                        break;

                    case MessageEncodingFormat.Soap12:

                        message = Soap12MessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.Soap12Property);

                        break;

                    case MessageEncodingFormat.Binary:

                        message = BinaryMessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryProperty);

                        break;

                    case MessageEncodingFormat.BinaryPlusGzip:

                        message = BinaryPlusGzipMessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryPlusGzipProperty);

                        break;

                    case MessageEncodingFormat.BinaryPlusDeflate:

                        message = BinaryPlusDeflateMessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.BinaryPlusDeflateProperty);

                        break;

                    case MessageEncodingFormat.Raw:

                        message = RawMessageEncoderFactory.Encoder.ReadMessage(stream, maxSizeOfHeaders, contentType);

                        message.Properties.Add(MessageEncodingFormatProperty.Name, MessageEncodingFormatProperty.RawProperty);

                        break;

                    default:

                        throw Fx.AssertAndThrow("This should never get hit because GetFormatForContentType shouldn't return a WebContentFormat other than Json, Xml, and Raw");
                }
                return message;
            }

            public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
            {
                if (message == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("message"));
                }
                if (bufferManager == null)
                {
                    throw TraceUtility.ThrowHelperError(new ArgumentNullException("bufferManager"), message);
                }
                if (maxMessageSize < 0)
                {
                    throw TraceUtility.ThrowHelperError(new ArgumentOutOfRangeException("maxMessageSize", maxMessageSize,
                        PublicSR.Format(PublicSR.ValueMustBeNonNegative)), message);
                }
                if (messageOffset < 0 || messageOffset > maxMessageSize)
                {
                    throw TraceUtility.ThrowHelperError(new ArgumentOutOfRangeException("messageOffset", messageOffset,
                        PublicSR.Format(PublicSR.JsonValueMustBeInRange, 0, maxMessageSize)), message);
                }

                MessageEncodingFormat transferFormat = GetTransferMessageEncoding(message);

                JavascriptCallbackResponseMessageProperty javascriptResponseMessageProperty;
                switch (transferFormat)
                {
                    case MessageEncodingFormat.Json:
                        JsonMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return JsonMessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Xml:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        TextMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return TextMessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Text:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        PlainTextMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return PlainTextMessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Html:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        MarkupMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return MarkupMessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Soap11WSAddressing10:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap11WSAddressing10MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return Soap11WSAddressing10MessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap11WSAddressingAugust2004MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return Soap11WSAddressingAugust2004MessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Soap11:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap11MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return Soap11MessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Soap12WSAddressing10:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap12WSAddressing10MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return Soap12WSAddressing10MessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap12WSAddressingAugust2004MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return Soap12WSAddressingAugust2004MessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Soap12:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap12MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return Soap12MessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Binary:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        BinaryMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return BinaryMessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.BinaryPlusGzip:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        BinaryPlusGzipMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return BinaryPlusGzipMessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.BinaryPlusDeflate:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        BinaryPlusDeflateMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return BinaryPlusDeflateMessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    case MessageEncodingFormat.Raw:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        RawMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        return RawMessageEncoderFactory.Encoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
                    default:
                        throw Fx.AssertAndThrow("This should never get hit because GetFormatForContentType shouldn't return a WebContentFormat other than Json, Xml, Soap11, Soap12 and Raw");
                }
            }

            public override void WriteMessage(Message message, Stream stream)
            {
                if (message == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("message"));
                }
                if (stream == null)
                {
                    throw TraceUtility.ThrowHelperError(new ArgumentNullException("stream"), message);
                }

                MessageEncodingFormat transferFormat = GetTransferMessageEncoding(message);

                JavascriptCallbackResponseMessageProperty javascriptResponseMessageProperty;
                switch (transferFormat)
                {
                    case MessageEncodingFormat.Json:
                        JsonMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        JsonMessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Xml:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        TextMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        TextMessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Text:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        PlainTextMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        PlainTextMessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Html:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        MarkupMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        MarkupMessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Soap11WSAddressing10:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap11WSAddressing10MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        Soap11WSAddressing10MessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap11WSAddressingAugust2004MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        Soap11WSAddressingAugust2004MessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Soap11:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap11MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        Soap11MessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Soap12WSAddressing10:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap12WSAddressing10MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        Soap12WSAddressing10MessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap12WSAddressingAugust2004MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        Soap12WSAddressingAugust2004MessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Soap12:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        Soap12MessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        Soap12MessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Binary:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        BinaryMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        BinaryMessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.BinaryPlusGzip:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        BinaryPlusGzipMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        BinaryPlusGzipMessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.BinaryPlusDeflate:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        BinaryPlusDeflateMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        BinaryPlusDeflateMessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    case MessageEncodingFormat.Raw:
                        if (message.Properties.TryGetProperty(JavascriptCallbackResponseMessageProperty.Name, out javascriptResponseMessageProperty) &&
                            javascriptResponseMessageProperty != null &&
                            !string.IsNullOrEmpty(javascriptResponseMessageProperty.CallbackFunctionName))
                        {
                            throw TraceUtility.ThrowHelperError(new InvalidOperationException(PublicSR.JavascriptCallbackNotsupported), message);
                        }
                        RawMessageEncoderFactory.Encoder.ThrowIfMismatchedMessageVersion(message);
                        RawMessageEncoderFactory.Encoder.WriteMessage(message, stream);
                        break;
                    default:
                        throw Fx.AssertAndThrow("This should never get hit because GetFormatForContentType shouldn't return a WebContentFormat other than Json, Xml, and Raw");
                }
            }

            private MessageEncodingFormat GetTransferMessageEncoding(Message message)
            {
                MessageEncodingFormat transferFormat;

                if (message.Properties.TryGetProperty<MessageEncodingFormatProperty>(MessageEncodingFormatProperty.Name, out MessageEncodingFormatProperty transferMessageEncodingMessageProperty))
                {
                    transferFormat = transferMessageEncodingMessageProperty.Format;
                }
                else if (message.Properties.TryGetProperty<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name, out HttpResponseMessageProperty httpResponseMessageProperty))
                {
                    transferFormat = this.GetTransferFormatForContentType(httpResponseMessageProperty.HttpResponseMessage.Content.Headers.ContentType.MediaType, message.Version.Addressing);
                }
                else

                    transferFormat = this.GetTransferFormatForContentType(this.MediaType, this.MessageVersion.Addressing);
                
                return transferFormat;
            }

            //public override IAsyncResult BeginWriteMessage(Message message, Stream stream, AsyncCallback callback, object state)
            //{
            //    if (message == null)
            //    {
            //        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("message"));
            //    }
            //    if (stream == null)
            //    {
            //        throw TraceUtility.ThrowHelperError(new ArgumentNullException("stream"), message);
            //    }

            //    ThrowIfMismatchedMessageVersion(message);

            //    return new WriteMessageAsyncResult(message, stream, this, callback, state);
            //}

            //public override void EndWriteMessage(IAsyncResult result)
            //{
            //    WriteMessageAsyncResult.End(result);
            //}

            //internal override bool IsCharSetSupported(string charSet)
            //{
            //    Encoding tmp;
            //    return TextEncoderDefaults.TryGetEncoding(charSet, out tmp);
            //}

            //internal void ThrowIfMismatchedMessageVersion(Message message)
            //{
            //    if (message.Version != MessageVersion)
            //    {
            //        throw TraceUtility.ThrowHelperError(
            //            new ProtocolException(ServiceModelSR.GetString(ServiceModelSR.EncoderMessageVersionMismatch, message.Version, MessageVersion)),
            //            message);
            //    }
            //}

            internal MessageEncodingFormat GetTransferFormatForContentType(string contentType, AddressingVersion addressingVersion = null)
            {
                if (TryGetContentTypeMapping(
                    contentType,
                    addressingVersion,
                    out MessageEncodingFormat transferFormat))
                {
                    if (DiagnosticUtility.ShouldTraceInformation)
                    {
                        if (string.IsNullOrEmpty(contentType))
                        {
                            contentType = "<null>";
                        }
                        TraceUtility.TraceEvent(TraceEventType.Information,
                            TraceCode.RequestFormatSelectedFromContentTypeMapper,
                            PublicSR.Format(PublicSR.TraceCodeRequestFormatSelectedFromContentTypeMapper, transferFormat.ToString(), contentType));
                    }
                    return transferFormat;
                }

                // Don't pass on null content types to IsContentTypeSupported methods -- they might throw.
                // If null content type isn't already mapped, return the default format of Raw.

                if (contentType == null)
                {
                    transferFormat = MessageEncodingFormat.Raw;
                }
                else if (this.JsonMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType))
                {
                    transferFormat = MessageEncodingFormat.Json;
                }
                // TextMessageEncoderFactory will also "support" text/plain ... perform plain text check first ...
                else if (this.PlainTextMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType))
                {
                    transferFormat = MessageEncodingFormat.Text;
                }
                // TextMessageEncoderFactory will also "support" text/html ... perform markup check first ...
                else if (this.MarkupMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType)) 
                {
                    transferFormat = MessageEncodingFormat.Html;
                }
                // text/xml will be supported by both the text and soap11 encoders ... 
                // choose text only when addressing version is null.
                else if (addressingVersion == null && this.TextMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType)) 
                {
                    transferFormat = MessageEncodingFormat.Xml;
                }
                else if (this.Soap11MessageEncoderFactory.Encoder.IsContentTypeSupported(contentType))
                {
                    if (addressingVersion == AddressingVersion.None)

                        transferFormat = MessageEncodingFormat.Soap11;

                    else if (addressingVersion == AddressingVersion.WSAddressing10)

                        transferFormat = MessageEncodingFormat.Soap11WSAddressing10;

                    else if (addressingVersion == AddressingVersion.WSAddressingAugust2004)

                        transferFormat = MessageEncodingFormat.Soap11WSAddressingAugust2004;
                }
                else if (this.Soap12MessageEncoderFactory.Encoder.IsContentTypeSupported(contentType))
                {
                    if (addressingVersion == null || addressingVersion == AddressingVersion.None)

                        transferFormat = MessageEncodingFormat.Soap12;

                    else if (addressingVersion == AddressingVersion.WSAddressing10)

                        transferFormat = MessageEncodingFormat.Soap12WSAddressing10;

                    else if (addressingVersion == AddressingVersion.WSAddressingAugust2004)

                        transferFormat = MessageEncodingFormat.Soap12WSAddressingAugust2004;
                }
                else if (this.BinaryMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType))
                {
                    transferFormat = MessageEncodingFormat.Binary;
                }
                else if (this.BinaryPlusGzipMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType))
                {
                    transferFormat = MessageEncodingFormat.BinaryPlusGzip;
                }
                else if (this.BinaryPlusDeflateMessageEncoderFactory.Encoder.IsContentTypeSupported(contentType))
                {
                    transferFormat = MessageEncodingFormat.BinaryPlusDeflate;
                }
                else
                {
                    transferFormat = MessageEncodingFormat.Raw;
                }

                if (DiagnosticUtility.ShouldTraceInformation)
                {
                    TraceUtility.TraceEvent(TraceEventType.Information,
                        TraceCode.RequestFormatSelectedByEncoderDefaults,
                        PublicSR.Format(PublicSR.TraceCodeRequestFormatSelectedByEncoderDefaults, transferFormat.ToString(), contentType));
                }

                return transferFormat;
            }

            internal string GetContentTypeForFormat(MessageEncodingFormat transferFormat, out MessageVersion messageVersion)
            {
                if (this.TryGetMappedContentTypeForFormat(transferFormat, out string contentType, out messageVersion))
                {
                    return contentType;
                }
                switch (transferFormat)
                {
                    case MessageEncodingFormat.Json:
                        contentType = this.JsonMessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.None;
                        break;
                    case MessageEncodingFormat.Xml:
                        contentType = this.TextMessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.None;
                        break;
                    case MessageEncodingFormat.Text:
                        contentType = this.PlainTextMessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.None;
                        break;
                    case MessageEncodingFormat.Html:
                        contentType = this.MarkupMessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.None;
                        break;
                    case MessageEncodingFormat.Soap11WSAddressing10:
                        contentType = this.Soap11WSAddressing10MessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.Soap11WSAddressing10;
                        break;
                    case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                        contentType = this.Soap11WSAddressingAugust2004MessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.Soap11WSAddressingAugust2004;
                        break;
                    case MessageEncodingFormat.Soap11:
                        contentType = this.Soap11MessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.Soap11;
                        break;
                    case MessageEncodingFormat.Soap12WSAddressing10:
                        contentType = this.Soap12WSAddressing10MessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.Soap12WSAddressing10;
                        break;
                    case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                        contentType = this.Soap12WSAddressingAugust2004MessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.Soap12WSAddressingAugust2004;
                        break;
                    case MessageEncodingFormat.Soap12:
                        contentType = this.Soap12MessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.Soap12;
                        break;
                    case MessageEncodingFormat.Raw:
                        contentType = this.RawMessageEncoderFactory.Encoder.ContentType;
                        break;
                    case MessageEncodingFormat.Default:
                    case MessageEncodingFormat.Binary:
                    default:
                        contentType = this.BinaryMessageEncoderFactory.Encoder.ContentType;
                        messageVersion = MessageVersion.Soap12;
                        break;
                }

                return contentType;
            }

            private bool TryGetMappedContentTypeForFormat(MessageEncodingFormat transferFormat, out string contentType, out MessageVersion messageVersion)
            {
                if (this.contentTypeMapper == null)
                {
                    contentType = null;
                    messageVersion = null;
                    return false;
                }
                bool result;
                try
                {
                    if (!MessageEncodingFormatHelper.IsDefined(transferFormat))
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(
                            AMMMFR.Format(AMMMFR.UnknownTransferEncodingFormat, null, transferFormat));
                    }
                    contentType = this.contentTypeMapper.GetContentTypeForTransferMessageEncoding(transferFormat, out messageVersion);
                    result = contentType != null;
                }
                catch (Exception ex)
                {
                    if (Fx.IsFatal(ex))
                    {
                        throw;
                    }
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new CommunicationException(
                            PublicSR.Format(PublicSR.ErrorEncounteredInContentTypeMapper), ex));
                }
                return result;
            }

            private bool TryGetContentTypeMapping(string contentType, out MessageEncodingFormat transferFormat)
            {
                return TryGetContentTypeMapping(contentType, null, out transferFormat);
            }

            private bool TryGetContentTypeMapping(string contentType, AddressingVersion addressingVersion, out MessageEncodingFormat transferFormat)
            {
                if (contentTypeMapper == null)
                {
                    transferFormat = MessageEncodingFormat.Default;
                    return false;
                }

                try
                {
                    transferFormat = contentTypeMapper.GetTransferMessageEncodingForContentType(contentType, addressingVersion);
                    if (!MessageEncodingFormatHelper.IsDefined(transferFormat))
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(
                            AMMMFR.Format(AMMMFR.UnknownTransferEncodingFormat, contentType, transferFormat));
                    }
                    return true;
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new CommunicationException(
                            PublicSR.Format(PublicSR.ErrorEncounteredInContentTypeMapper), e));
                }
            }
        }

        CompositeMessageEncodingFormatEncoder messageEncoder;

        public CompositeMessageEncodingFormatEncoderFactory(Encoding writeEncoding, int maxReadPoolSize, int maxWritePoolSize, XmlDictionaryReaderQuotas quotas, int maxSessionSize, long maxReceivedMessageSize, MessageEncodingFormat transferFormat = MessageEncodingFormat.Default, bool javascriptCallbackEnabled = false, IMessagingContentTypeMapper contentTypeMapper = null)
        {
            messageEncoder = 
                new CompositeMessageEncodingFormatEncoder(
                    writeEncoding,
                    maxReadPoolSize,
                    maxWritePoolSize,
                    quotas,
                    maxSessionSize,
                    maxReceivedMessageSize,
                    javascriptCallbackEnabled,
                    transferFormat,
                    contentTypeMapper);
        }

        public override MessageEncoder Encoder
        {
            get { return messageEncoder; }
        }

        public override MessageVersion MessageVersion
        {
            get { return messageEncoder.MessageVersion; }
        }

        public override MessageEncoder CreateSessionEncoder()
        {
            return base.CreateSessionEncoder();
        }

        public bool IsContentTypeSupported(String contentType)
        {
            return (this.Encoder as CompositeMessageEncodingFormatEncoder).IsContentTypeSupported(contentType);
        }

        public MessageEncodingFormat GetTransferFormatForContentType(String contentType)
        {
            return GetTransferFormatForContentType(this.Encoder, contentType, out bool _);
        }

        internal String GetContentTypeForFormat(MessageEncodingFormat transferFormat)
        {
            return GetContentTypeForFormat(this.Encoder, transferFormat, out bool _);
        }

        internal static MessageEncodingFormat GetTransferFormatForContentType(MessageEncoder messageEncoder, string contentType, out bool isContentTypeSupportedByMessageEncoder)
        {
            isContentTypeSupportedByMessageEncoder = messageEncoder.IsContentTypeSupported(contentType);

            if (messageEncoder is CompositeMessageEncodingFormatEncoder)
            {
                return (messageEncoder as CompositeMessageEncodingFormatEncoder).GetTransferFormatForContentType(contentType);
            }
            else
            {
                return ResourceTransferBinding.DefaultBindingElement.ContentTypeMapper.GetTransferMessageEncodingForContentType(contentType);
            }
        }

        internal static string GetContentTypeForFormat(MessageEncoder messageEncoder, MessageEncodingFormat transferFormat, out bool isContentTypeSupportedByMessageEncoder)
        {
            if (messageEncoder is CompositeMessageEncodingFormatEncoder)
            {
                isContentTypeSupportedByMessageEncoder = true;

                return (messageEncoder as CompositeMessageEncodingFormatEncoder).GetContentTypeForFormat(transferFormat, out _);
            }
            else
            {
                String mediaType = transferFormat.CreateMessageContentType(out _).NormalizedMediaType;

                isContentTypeSupportedByMessageEncoder = messageEncoder.IsContentTypeSupported(mediaType);

                return mediaType;
            }
        }

        internal static string GetContentType(string mediaType, Encoding encoding)
        {
            string charset = TextEncoderDefaults.EncodingToCharSet(encoding);
            if (!string.IsNullOrEmpty(charset))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}; charset={1}", mediaType, charset);
            }
            return mediaType;
        }

        public bool TryGetMessageEncoderFactory(string contentType, out MessageEncoderFactory messageEncoderFactory)
        {
            if (contentType == null)
            {
                messageEncoderFactory = new NullMessageEncoderFactory();

                return true;
            }

            CompositeMessageEncodingFormatEncoder encoder = this.Encoder as CompositeMessageEncodingFormatEncoder;

            MessageEncodingFormat transferFormat = encoder.GetTransferFormatForContentType(contentType);

            switch (transferFormat)
            {
                case MessageEncodingFormat.Json:

                    messageEncoderFactory = encoder.JsonMessageEncoderFactory;

                    return true;

                case MessageEncodingFormat.Xml:

                    messageEncoderFactory = encoder.TextMessageEncoderFactory;

                    return true;

                case MessageEncodingFormat.Text:

                    messageEncoderFactory = encoder.PlainTextMessageEncoderFactory;

                    return true;

                case MessageEncodingFormat.Html:

                    messageEncoderFactory = encoder.MarkupMessageEncoderFactory;

                    return true;

                case MessageEncodingFormat.Soap11WSAddressing10:

                    messageEncoderFactory = encoder.Soap11WSAddressing10MessageEncoderFactory;;

                    return true;

                case MessageEncodingFormat.Soap11WSAddressingAugust2004:

                    messageEncoderFactory = encoder.Soap11WSAddressingAugust2004MessageEncoderFactory;;

                    return true;

                case MessageEncodingFormat.Soap11:

                    messageEncoderFactory = encoder.Soap11MessageEncoderFactory;;

                    return true;

                case MessageEncodingFormat.Soap12WSAddressing10:

                    messageEncoderFactory = encoder.Soap12WSAddressing10MessageEncoderFactory;;

                    return true;

                case MessageEncodingFormat.Soap12WSAddressingAugust2004:

                    messageEncoderFactory = encoder.Soap12WSAddressingAugust2004MessageEncoderFactory;

                    return true;

                case MessageEncodingFormat.Soap12:

                    messageEncoderFactory = encoder.Soap12MessageEncoderFactory;

                    return true;

                case MessageEncodingFormat.Binary:

                    messageEncoderFactory = encoder.BinaryMessageEncoderFactory;

                    return true;

                case MessageEncodingFormat.BinaryPlusGzip:

                    messageEncoderFactory = encoder.BinaryPlusGzipMessageEncoderFactory;

                    return true;

                case MessageEncodingFormat.BinaryPlusDeflate:

                    messageEncoderFactory = encoder.BinaryPlusDeflateMessageEncoderFactory;

                    return true;

                case MessageEncodingFormat.Raw:

                    messageEncoderFactory = encoder.RawMessageEncoderFactory;

                    return true;

                default:

                    messageEncoderFactory = null;

                    return false;
            }
        }
    }
}
