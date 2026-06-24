using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Mime;
using System.Runtime;
using System.Runtime.Serialization.PlainText;
using System.ServiceModel.Channels;
using System.ServiceModel.Diagnostics;
using System.ServiceModel;
using System.Text;
using System.Xml;

using System.ServiceModel.Diagnostics.Application;

using AllVerge.SystemPrimitives.Net.Mime;
using System.Runtime.Serialization.Html;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    internal class PlainTextMessageEncoderFactory : MessageEncoderFactory
    {
        private PlainTextMessageEncoder messageEncoder;
        internal const string PlainTextMediaType = MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE;

        public PlainTextMessageEncoderFactory(Encoding writeEncoding, int maxReadPoolSize, int maxWritePoolSize, XmlDictionaryReaderQuotas quotas)
        {
            messageEncoder = new PlainTextMessageEncoder(writeEncoding, maxReadPoolSize, maxWritePoolSize, quotas);
        }

        public override MessageEncoder Encoder => messageEncoder;

        public override MessageVersion MessageVersion => MessageVersion.None;

        internal class ContentEncoding
        {
            internal string contentType;
            internal Encoding encoding;
        }

        private class PlainTextMessageEncoder : MessageEncoder
        {
            private string contentType;
            private int maxReadPoolSize;
            private int maxWritePoolSize;
            private OnXmlDictionaryReaderClose onStreamedReaderClose;
            private XmlDictionaryReaderQuotas readerQuotas;
            private SynchronizedPool<XmlDictionaryReader> streamedReaderPool;
            private SynchronizedPool<XmlDictionaryWriter> streamedWriterPool;
            private object thisLock;
            private Encoding writeEncoding;
            public override string ContentType => contentType;
            public override string MediaType => MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE;
            public override MessageVersion MessageVersion => MessageVersion.None;
            ContentEncoding[] contentEncodingMap = GetContentEncodingMap(MessageVersion.None);

            public PlainTextMessageEncoder(Encoding writeEncoding, int maxReadPoolSize, int maxWritePoolSize, XmlDictionaryReaderQuotas quotas)
            {
                if (writeEncoding == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(writeEncoding));
                }
                thisLock = new object();
                TextEncoderDefaults.ValidateEncoding(writeEncoding);
                this.writeEncoding = writeEncoding;
                this.maxReadPoolSize = maxReadPoolSize;
                this.maxWritePoolSize = maxWritePoolSize;
                this.readerQuotas = new XmlDictionaryReaderQuotas();
                onStreamedReaderClose = ReturnStreamedReader;
                quotas.CopyTo(readerQuotas);
                contentType = GetContentType(MediaType, writeEncoding);
            }

            //private bool IsCharSetSupported(string charSet)
            //{
            //    Encoding tmp;
            //    return TextEncoderDefaults.TryGetEncoding(charSet, out tmp);
            //}

            public override bool IsContentTypeSupported(string contentType)
            {
                if (contentType == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("contentType");
                }

                if (MessageVersion == MessageVersion.None)
                {
                    if (this.IsContentTypeSupported(contentType, MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE, MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
            {
                if (!contentType.StartsWith(this.MediaType))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(contentType)));
                }

                XmlDictionaryReader reader = XmlStringReaderWriterFactory.CreateStringReader(buffer.AsSpan().ToArray(), XmlDictionaryReaderQuotas.Max);

                reader.Read();

                Message message = Message.CreateMessage(this.MessageVersion, null, reader);

                //message.Properties.Add(TransferFormatMessageProperty.Name, TransferFormatMessageProperty.TextProperty);
                message.Properties.Encoder = this;

                return message;
            }

            public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
            {
                if (stream == null)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("stream"));

                if (DiagnosticsAppTD.TextMessageDecodingStartIsEnabled())
                {
                    DiagnosticsAppTD.TextMessageDecodingStart();
                }

                XmlReader reader = TakeStreamedReader(stream, GetEncodingFromContentType(contentType, this.contentEncodingMap));
                Message message = Message.CreateMessage(reader, maxSizeOfHeaders, this.MessageVersion);
                message.Properties.Encoder = this;

                if (DiagnosticsAppTD.StreamedMessageReadByEncoderIsEnabled())
                {
                    DiagnosticsAppTD.StreamedMessageReadByEncoder(EventTraceActivityHelper.TryExtractActivity(message, true));
                }

                if (MessageLogger.LogMessagesAtTransportLevel)
                    MessageLogger.LogMessage(ref message, MessageLoggingSource.TransportReceive);
                return message;
            }

            internal static ContentEncoding[] GetContentEncodingMap(MessageVersion version)
            {
                Encoding[] readEncodings = PlainTextMessageEncoderFactory.GetSupportedEncodings();
                string media = GetMediaType(version);
                ContentEncoding[] map = new ContentEncoding[readEncodings.Length];
                for (int i = 0; i < readEncodings.Length; i++)
                {
                    ContentEncoding contentEncoding = new ContentEncoding();
                    contentEncoding.contentType = GetContentType(media, readEncodings[i]);
                    contentEncoding.encoding = readEncodings[i];
                    map[i] = contentEncoding;
                }
                return map;
            }

            internal static string GetMediaType(MessageVersion version)
            {
                string mediaType = null;
                if (version.Envelope == EnvelopeVersion.None)
                {
                    mediaType = PlainTextMessageEncoderFactory.PlainTextMediaType;
                }
                else
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(
                        PublicSR.Format(PublicSR.EnvelopeVersionNotSupported, version.Envelope)));
                }
                return mediaType;
            }

            internal static Encoding GetEncodingFromContentType(string contentType, ContentEncoding[] contentMap)
            {
                if (contentType == null)
                {
                    return null;
                }

                // Check for known/expected content types
                for (int i = 0; i < contentMap.Length; i++)
                {
                    if (contentMap[i].contentType == contentType)
                    {
                        return contentMap[i].encoding;
                    }
                }

                // then some heuristic matches (since System.Mime.ContentType is a performance hit)
                // start by looking for a parameter. 

                // If none exists, we don't have an encoding
                int semiColonIndex = contentType.IndexOf(';');
                if (semiColonIndex == -1)
                {
                    return null;
                }

                // optimize for charset being the first parameter
                int charsetValueIndex = -1;

                // for Indigo scenarios, we'll have "; charset=", so check for the c
                if ((contentType.Length > semiColonIndex + 11) // need room for parameter + charset + '=' 
                    && contentType[semiColonIndex + 2] == 'c'
                    && string.Compare("charset=", 0, contentType, semiColonIndex + 2, 8, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    charsetValueIndex = semiColonIndex + 10;
                }
                else
                {
                    // look for charset= somewhere else in the message
                    int paramIndex = contentType.IndexOf("charset=", semiColonIndex + 1, StringComparison.OrdinalIgnoreCase);
                    if (paramIndex != -1)
                    {
                        // validate there's only whitespace or semi-colons beforehand
                        for (int i = paramIndex - 1; i >= semiColonIndex; i--)
                        {
                            if (contentType[i] == ';')
                            {
                                charsetValueIndex = paramIndex + 8;
                                break;
                            }

                            if (contentType[i] == '\n')
                            {
                                if (i == semiColonIndex || contentType[i - 1] != '\r')
                                {
                                    break;
                                }

                                i--;
                                continue;
                            }

                            if (contentType[i] != ' '
                                && contentType[i] != '\t')
                            {
                                break;
                            }
                        }
                    }
                }

                string charSet;
                Encoding enc;

                // we have a possible charset value. If it's easy to parse, do so
                if (charsetValueIndex != -1)
                {
                    // get the next semicolon
                    semiColonIndex = contentType.IndexOf(';', charsetValueIndex);
                    if (semiColonIndex == -1)
                    {
                        charSet = contentType.Substring(charsetValueIndex);
                    }
                    else
                    {
                        charSet = contentType.Substring(charsetValueIndex, semiColonIndex - charsetValueIndex);
                    }

                    // and some minimal quote stripping
                    if (charSet.Length > 2 && charSet[0] == '"' && charSet[charSet.Length - 1] == '"')
                    {
                        charSet = charSet.Substring(1, charSet.Length - 2);
                    }

                    Fx.Assert(charSet == (new ContentType(contentType)).CharSet,
                            "CharSet parsing failed to correctly parse the ContentType header.");

                    if (TryGetEncodingFromCharSet(charSet, out enc))
                    {
                        return enc;
                    }
                }

                // our quick heuristics failed. fall back to System.Net
                try
                {
                    ContentType parsedContentType = new ContentType(contentType);
                    charSet = parsedContentType.CharSet;
                }
                catch (FormatException e)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ProtocolException(PublicSR.EncoderBadContentType, e));
                }

                if (TryGetEncodingFromCharSet(charSet, out enc))
                    return enc;

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ProtocolException(PublicSR.Format(PublicSR.EncoderUnrecognizedCharSet, charSet)));
            }

            internal static bool TryGetEncodingFromCharSet(string charSet, out Encoding encoding)
            {
                encoding = null;
                if (charSet == null || charSet.Length == 0)
                    return true;

                return TextEncoderDefaults.TryGetEncoding(charSet, out encoding);
            }

            XmlReader TakeStreamedReader(Stream stream, Encoding enc)
            {
                if (streamedReaderPool == null)
                {
                    lock (ThisLock)
                    {
                        if (streamedReaderPool == null)
                        {
                            streamedReaderPool = new SynchronizedPool<XmlDictionaryReader>(maxReadPoolSize);
                        }
                    }
                }
                XmlDictionaryReader xmlReader = streamedReaderPool.Take();
                if (xmlReader == null)
                {
                    xmlReader = XmlStringReaderWriterFactory.CreateStringReader(stream, enc, this.readerQuotas, null);
                    if (DiagnosticsAppTD.ReadPoolMissIsEnabled())
                    {
                        DiagnosticsAppTD.ReadPoolMiss(xmlReader.GetType().Name);
                    }
                }
                else
                {
                    ((IXmlTextReaderInitializer)xmlReader).SetInput(stream, enc, this.readerQuotas, onStreamedReaderClose);
                }
                return xmlReader;
            }

            void ReturnStreamedReader(XmlDictionaryReader xmlReader)
            {
                streamedReaderPool.Return(xmlReader);
            }

            public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
            {
                throw new NotImplementedException();
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
                this.ThrowIfMismatchedMessageVersion(message);
                message.Properties.Encoder = this;
                XmlDictionaryWriter xmlDictionaryWriter = TakeStreamedWriter(stream);
                xmlDictionaryWriter.WriteStartDocument();
                message.WriteMessage(xmlDictionaryWriter);
                xmlDictionaryWriter.WriteEndDocument();
                xmlDictionaryWriter.Flush();
                ReturnStreamedWriter(xmlDictionaryWriter);
                if (MessageLogger.LoggingEnabled && MessageLogger.LogMessagesAtTransportLevel)
                {
                    MessageLogger.LogMessage(ref message, MessageLoggingSource.TransportSend);
                }
            }

            private object ThisLock => thisLock;

            private void ReturnStreamedWriter(XmlWriter xmlWriter)
            {
                xmlWriter.Close();
                streamedWriterPool.Return((XmlDictionaryWriter)xmlWriter);
            }

            //private void ReturnStreamedReader(XmlDictionaryReader xmlReader)
            //{
            //    streamedReaderPool.Return(xmlReader);
            //}

            private XmlDictionaryWriter TakeStreamedWriter(Stream stream)
            {
                if (streamedWriterPool == null)
                {
                    lock (ThisLock)
                    {
                        if (streamedWriterPool == null)
                        {
                            streamedWriterPool = new SynchronizedPool<XmlDictionaryWriter>(maxWritePoolSize);
                        }
                    }
                }
                XmlDictionaryWriter xmlDictionaryWriter = streamedWriterPool.Take();
                if (xmlDictionaryWriter == null)
                {
                    xmlDictionaryWriter = XmlStringReaderWriterFactory.CreateStringWriter(stream, writeEncoding, ownsStream: false);
                }
                else
                {
                    ((IXmlTextWriterInitializer)xmlDictionaryWriter).SetOutput(stream, writeEncoding, ownsStream: false);
                }
                return xmlDictionaryWriter;
            }
        }

        public static Encoding[] GetSupportedEncodings()
        {
            Encoding[] supported = TextEncoderDefaults.SupportedEncodings;
            Encoding[] enc = new Encoding[supported.Length];
            Array.Copy(supported, enc, supported.Length);
            return enc;
        }

        private static string GetContentType(string mediaType, Encoding encoding)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}; charset={1}", new object[2]
            {
                mediaType,
                TextEncoderDefaults.EncodingToCharSet(encoding)
            });
        }
    }
}
