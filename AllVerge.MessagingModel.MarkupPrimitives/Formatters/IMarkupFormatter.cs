using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AllVerge.MessagingModel.MarkupPrimitives.Formatters
{
    /// <summary>
    /// Implementations format a  <typeparamref name="DOM"/> object as a string, 
    /// or hydrate a  <typeparamref name="DOM"/> object from a formatted document,
    /// or a reader initialized from a formatted document.
    /// </summary>
    /// <typeparam name="DOM"></typeparam>
    public interface IMarkupFormatter<DOM>
    {
        Formats Format { get; }
        string ToFormattedString(DOM @object, Encoding encoding);
        DOM FromFormattedBuffer(byte[] formattedBuffer, Encoding encoding, out Exception exception);
        DOM FromFormattedStream(Stream formattedStream, Encoding encoding, out Exception exception);
        DOM FromFormattedString(String formattedString, Encoding encoding, out Exception exception);
        DOM FromFormattedSource(Uri formattedSourceUri, Uri cachePathUri, out Exception exception);
        DOM FromFormatReader(Object formatReader, out Exception exception);
    }
}
