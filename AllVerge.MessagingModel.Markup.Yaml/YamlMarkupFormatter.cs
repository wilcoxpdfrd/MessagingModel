using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Yaml;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.Markup.Yaml
{
    using AllVerge.MessagingModel.MarkupPrimitives;
    using AllVerge.MessagingModel.MarkupPrimitives.Document;
    using AllVerge.MessagingModel.MarkupPrimitives.Formatters;

    using AllVerge.SystemPrimitives.Net;

    public class YamlMarkupFormatter : IMarkupFormatter<MarkupNode>
    {
        public Formats Format => Formats.YAML;

        public MarkupNode FromFormatReader(object formatReader, out Exception exception)
        {
            if (formatReader == null)
            {
                exception = new ArgumentNullException(nameof(formatReader));

                return null;
            }

            if (formatReader is XmlDictionaryReader)
            {
                return FromDictionaryReader((XmlDictionaryReader)formatReader, out exception);
            }

            exception = new NotSupportedException($"{nameof(formatReader)} of type '{formatReader.GetType()}' is not supported.");

            return null;
        }

        public MarkupNode FromFormattedBuffer(byte[] formattedBuffer, Encoding encoding, out Exception exception)
        {
            if (formattedBuffer == null)
            {
                exception = new ArgumentNullException(nameof(formattedBuffer));

                return null;
            }

            using (XmlDictionaryReader reader =
                YamlReaderWriterFactory.CreateYamlReader(formattedBuffer, XmlDictionaryReaderQuotas.Max))
            {
                return FromDictionaryReader(reader, out exception);
            }
        }

        public MarkupNode FromFormattedStream(Stream formattedStream, Encoding encoding, out Exception exception)
        {
            if (formattedStream == null)
            {
                exception = new ArgumentNullException(nameof(formattedStream));

                return null;
            }

            using (XmlDictionaryReader reader =
                YamlReaderWriterFactory.CreateYamlReader(formattedStream, XmlDictionaryReaderQuotas.Max))
            {
                return FromDictionaryReader(reader, out exception);
            }
        }

        public MarkupNode FromFormattedSource(Uri formattedSourceUri, Uri cachePathUri, out Exception exception)
        {
            try
            {
                Stream jsonStream;
                String jsonMediaType;
                String jsonMediaTypeVariant;
                Encoding jsonEncoding;

                if (cachePathUri != null)
                {
                    if (!formattedSourceUri.TryStreamCachedResource(cachePathUri, out jsonStream, out jsonMediaType))

                        formattedSourceUri.DownloadResourceAndGetResourceMediaType(
                            cachePathUri,
                            out jsonStream,
                            out jsonMediaType,
                            out jsonMediaTypeVariant,
                            out jsonEncoding);
                }
                else

                    formattedSourceUri.DownloadResourceAndGetResourceMediaType(
                        null,
                        out jsonStream,
                        out jsonMediaType,
                        out jsonMediaTypeVariant,
                        out jsonEncoding);

                exception = null;

                using (jsonStream)
                {
                    using (XmlDictionaryReader reader = YamlReaderWriterFactory.CreateYamlReader(jsonStream, XmlDictionaryReaderQuotas.Max))
                    {
                        return FromDictionaryReader(reader, out exception);
                    }
                }
            }
            catch (Exception e)
            {
                exception = e;
            }

            return null;
        }

        private static MarkupNode FromDictionaryReader(XmlDictionaryReader reader, out Exception exception)
        {
            try
            {
                exception = null;

                return reader.ReadMarkup();
            }
            catch (Exception ex)
            {
                exception = ex;

                return null;
            }
        }

        public MarkupNode FromFormattedString(string formattedString, Encoding encoding, out Exception exception)
        {
            throw new NotImplementedException(nameof(FromFormattedString));
        }

        public string ToFormattedString(MarkupNode @object, Encoding encoding)
        {
            throw new NotImplementedException(nameof(ToFormattedString));
        }
    }
}
