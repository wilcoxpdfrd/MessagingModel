using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace AllVerge.MessagingModel.MarkupPrimitives.Formatters
{
    using AllVerge.SystemPrimitives.Net.Mime;

    /// <summary>
    /// Provides methods to format a DOM Object <typeparamref name="O"/> as a string 
    /// or to hydrate a DOM Object from a formatted string 
    /// or a reader initialized with a formatted stream or string.
    /// </summary>
    /// <typeparam name="O"></typeparam>
    public class MarkupFormatter<O>
    {
        class MarkupFormatterCollection : KeyedCollection<Formats, IMarkupFormatter<O>>
        {
            protected override Formats GetKeyForItem(IMarkupFormatter<O> item)
            {
                return item.Format;
            }
        }

        public static bool TryRegister(IMarkupFormatter<O> markupFormatter)
        {
            if (!markupFormatters.Contains(markupFormatter.Format))
            {
                lock (markupFormatters)
                {
                    if (!markupFormatters.Contains(markupFormatter.Format))

                        markupFormatters.Add(markupFormatter);
                }
                return true;
            }

            return false;
        }

        private static MarkupFormatterCollection markupFormatters = new MarkupFormatterCollection();

        public static string ToFormattedString(O @object, string contentType, out Exception exception)
        {
            if (contentType == null)

                contentType = MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE;

            String mediaType = MediaTypes.ParseMediaType(contentType, out String resourceMediaTypeVariant, out Encoding resourceEncoding);

            String normalizedMediaType;

            if (MediaTypes.TryGetNormalizedResourceMediaType(mediaType, out normalizedMediaType))
            {
                switch (normalizedMediaType)
                {
                    case MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE:
                        return ToFormattedString(@object, Formats.PlainText, resourceMediaTypeVariant, resourceEncoding, out exception);
                    case MediaTypeConstants.TEXT_MARKDOWN_MEDIA_TYPE:
                        return ToFormattedString(@object, Formats.Markdown, resourceMediaTypeVariant, resourceEncoding, out exception);
                    case MediaTypeConstants.APPLICATION_XML_MEDIA_TYPE:
                        return ToFormattedString(@object, Formats.XML, resourceMediaTypeVariant, resourceEncoding, out exception);
                    case MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE:
                        return ToFormattedString(@object, Formats.JSON, resourceMediaTypeVariant, resourceEncoding, out exception);
                }
            }

            exception = new NotImplementedException(contentType);

            return null;
        }

        //private string ToPlainText(String resourceMediaTypeVariant, Encoding resourceEncoding)
        //{
        //    StringBuilder sb = new StringBuilder();

        //    foreach (IRepresentation item in this)
        //    {
        //        sb.AppendLine(item.ToFormattedString());
        //    }

        //    return sb.ToString();
        //}

        //private string ToMarkdownRepresentation(String resourceMediaTypeVariant, Encoding resourceEncoding)
        //{
        //    throw new NotImplementedException(nameof(ToMarkdownRepresentation));
        //}

        //private string ToXMLRepresentation(String resourceMediaTypeVariant, Encoding resourceEncoding)
        //{
        //    return this.Serialize(XmlSerialization.EmptyNSMap).OuterXml;
        //}

        //private string ToJSONRepresentation(String resourceMediaTypeVariant, Encoding resourceEncoding)
        //{
        //    return this.SerializeAsJson();
        //}

        public static string ToFormattedString(O @object, Formats format, String formatVariant, Encoding encoding, out Exception exception)
        {
            exception = null;

            if (markupFormatters.Contains(format))

                return markupFormatters[format].ToFormattedString(@object, encoding);

            exception = CreateNoRegisteredFormatterException(@object, format);

            return null;
        }

        public static O FromFormattedBuffer(byte[] formattedBuffer, Formats format, Encoding encoding, out Exception exception)
        {
            if (markupFormatters.Contains(format))

                return markupFormatters[format].FromFormattedBuffer(formattedBuffer, encoding, out exception);

            exception = CreateNoRegisteredFormatterException<O>(format);

            return default(O);
        }

        public static O FromFormattedStream(Stream formattedStream, Formats format, Encoding encoding, out Exception exception)
        {
            if (markupFormatters.Contains(format))

                return markupFormatters[format].FromFormattedStream(formattedStream, encoding, out exception);

            exception = CreateNoRegisteredFormatterException<O>(format);

            return default(O);
        }

        public static O FromFormattedString(string formattedString, string contentType, out Exception exception)
        {
            if (contentType == null)

                contentType = MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE;

            String mediaType = MediaTypes.ParseMediaType(contentType, out String mediaTypevariant, out Encoding encoding);

            String normalizedMediaType;

            if (MediaTypes.TryGetNormalizedResourceMediaType(mediaType, out normalizedMediaType))
            {
                switch (normalizedMediaType)
                {
                    case MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE:
                        return FromFormattedString(formattedString, Formats.PlainText, mediaTypevariant, encoding, out exception);
                    case MediaTypeConstants.TEXT_MARKDOWN_MEDIA_TYPE:
                        return FromFormattedString(formattedString, Formats.Markdown, mediaTypevariant, encoding, out exception);
                    case MediaTypeConstants.APPLICATION_XML_MEDIA_TYPE:
                        return FromFormattedString(formattedString, Formats.XML, mediaTypevariant, encoding, out exception);
                    case MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE:
                        return FromFormattedString(formattedString, Formats.JSON, mediaTypevariant, encoding, out exception);
                }
            }

            throw new NotImplementedException(contentType);
        }

        public static O FromFormattedString(string formattedString, Formats format, String formatVariant, Encoding encoding, out Exception exception)
        {
            if (markupFormatters.Contains(format))

                return markupFormatters[format].FromFormattedString(formattedString, encoding, out exception);

            exception = CreateNoRegisteredFormatterException<O>(format);

            return default(O);
        }

        public static O FromFormatReader(Object formatReader, string contentType, out Exception exception)
        {
            if (contentType == null)

                contentType = MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE;

            String mediaType = MediaTypes.ParseMediaType(contentType, out String mediaTypevariant, out Encoding encoding);

            String normalizedMediaType;

            if (MediaTypes.TryGetNormalizedResourceMediaType(mediaType, out normalizedMediaType))
            {
                switch (normalizedMediaType)
                {
                    case MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE:
                        return FromFormatReader(formatReader, Formats.PlainText, mediaTypevariant, out exception);
                    case MediaTypeConstants.TEXT_MARKDOWN_MEDIA_TYPE:
                        return FromFormatReader(formatReader, Formats.Markdown, mediaTypevariant, out exception);
                    case MediaTypeConstants.APPLICATION_XML_MEDIA_TYPE:
                        return FromFormatReader(formatReader, Formats.XML, mediaTypevariant, out exception);
                    case MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE:
                        return FromFormatReader(formatReader, Formats.JSON, mediaTypevariant, out exception);
                }
            }

            throw new NotImplementedException(contentType);
        }

        public static O FromFormatReader(Object formatReader, Formats format, String formatVariant, out Exception exception)
        {
            if (markupFormatters.Contains(format))

                return markupFormatters[format].FromFormatReader(formatReader, out exception);

            exception = CreateNoRegisteredFormatterException<O>(format);

            return default(O);
        }

        public static O FromFormattedSource(Uri formattedSourceUri, Uri cachePathUri, Formats format, out Exception exception)
        {
            if (markupFormatters.Contains(format))

                return markupFormatters[format].FromFormattedSource(formattedSourceUri, cachePathUri, out exception);

            exception = CreateNoRegisteredFormatterException<O>(format);

            return default(O);
        }

        private static Exception CreateNoRegisteredFormatterException<T>(Formats format)
        {
            return CreateNoRegisteredFormatterException(typeof(T), format);
        }

        private static Exception CreateNoRegisteredFormatterException(O @object, Formats format)
        {
            return CreateNoRegisteredFormatterException(@object.GetType(), format);
        }

        private static Exception CreateNoRegisteredFormatterException(Type objectType, Formats format)
        {
            return new InvalidOperationException($"No formatter of '{format}' is registered for '{objectType}'.");
        }
    }
}
