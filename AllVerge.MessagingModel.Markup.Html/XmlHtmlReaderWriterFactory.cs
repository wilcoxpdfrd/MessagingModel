using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Html
{
    /// <summary>Produces instances of <see cref="XmlDictionaryReader" /> or <see cref="XmlDictionaryWriter" /> that can read or write data encoded with Html to or from a stream (or buffer) and an XML Infoset.</summary>
    public static class XmlHtmlReaderWriterFactory
    {
        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map streams encoded with HTML to an XML Infoset.</summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can read HTML.</returns>
        /// <param name="stream">The input <see cref="System.IO.Stream" /> from which to read.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        public static XmlDictionaryReader CreateHtmlReader(Stream stream, XmlDictionaryReaderQuotas quotas)
        {
            return CreateHtmlReader(stream, (Encoding)null, quotas, (OnXmlDictionaryReaderClose)null);
        }

        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map buffers encoded with HTML to an XML Infoset.</summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can process HTML data.</returns>
        /// <param name="buffer">The input <see cref="System.Byte" /> buffer array from which to read.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        public static XmlDictionaryReader CreateHtmlReader(byte[] buffer, XmlDictionaryReaderQuotas quotas)
        {
            if (buffer == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(buffer));
            return CreateHtmlReader(buffer, 0, buffer.Length, (Encoding)null, quotas, (OnXmlDictionaryReaderClose)null);
        }

        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map streams encoded with HTML, of a specified size and offset, to an XML Infoset.</summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can read HTML.</returns>
        /// <param name="stream">The input <see cref="System.IO.Stream" /> from which to read.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the reader. If null is specified as the value, the reader attempts to auto-detect the encoding.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        /// <param name="onClose">The <see cref="OnXmlDictionaryReaderClose" /> delegate to call when the reader is closed.</param>
        public static XmlDictionaryReader CreateHtmlReader(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            XmlHtmlReader XmlHtmlReader = new XmlHtmlReader();
            XmlHtmlReader.SetInput(stream, encoding, quotas, onClose);
            return (XmlDictionaryReader)XmlHtmlReader;
        }

        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map buffers encoded with HTML, of a specified size and offset, to an XML Infoset.</summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can read HTML.</returns>
        /// <param name="buffer">The input <see cref="System.Byte" /> buffer array from which to read.</param>
        /// <param name="offset">Starting position from which to read in <paramref name="buffer" />.</param>
        /// <param name="count">Number of bytes that can be read from <paramref name="buffer" />.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        public static XmlDictionaryReader CreateHtmlReader(byte[] buffer, int offset, int count, XmlDictionaryReaderQuotas quotas)
        {
            return CreateHtmlReader(buffer, offset, count, (Encoding)null, quotas, (OnXmlDictionaryReaderClose)null);
        }

        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map buffers encoded with HTML, with a specified size and offset and character encoding, to an XML Infoset. </summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can read HTML.</returns>
        /// <param name="buffer">The input <see cref="System.Byte" /> buffer array from which to read.</param>
        /// <param name="offset">Starting position from which to read in <paramref name="buffer" />.</param>
        /// <param name="count">Number of bytes that can be read from <paramref name="buffer" />.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the reader. If null is specified as the value, the reader attempts to auto-detect the encoding.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        /// <param name="onClose">The <see cref="OnXmlDictionaryReaderClose" /> delegate to call when the reader is closed. The default value is null.</param>
        public static XmlDictionaryReader CreateHtmlReader(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            XmlHtmlReader xmlHtmlReader = new XmlHtmlReader();
            xmlHtmlReader.SetInput(buffer, offset, count, encoding, quotas, onClose);
            return (XmlDictionaryReader)xmlHtmlReader;
        }

        /// <summary>Creates an <see cref="XmlDictionaryWriter" /> that writes data encoded with HTML to a stream.</summary>
        /// <returns>An <see cref="XmlDictionaryWriter" /> that writes data encoded with HTML to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the HTML writer.</param>
        public static XmlDictionaryWriter CreateHtmlWriter(Stream stream)
        {
            return CreateHtmlWriter(stream, Encoding.UTF8, true);
        }

        /// <summary>Creates an <see cref="XmlDictionaryWriter" /> that writes data encoded with HTML to a stream with a specified character encoding.</summary>
        /// <returns>An <see cref="XmlDictionaryWriter" /> that writes data encoded with HTML to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the HTML writer.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the writer. The default encoding is UTF-8.</param>
        public static XmlDictionaryWriter CreateHtmlWriter(Stream stream, Encoding encoding)
        {
            return CreateHtmlWriter(stream, encoding, true);
        }

        /// <summary>Creates an <see cref="XmlDictionaryWriter" /> that writes data encoded with HTML to a stream with a specified character.</summary>
        /// <returns>An <see cref="XmlDictionaryWriter" /> that writes data encoded with HTML to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the HTML writer.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the writer. The default encoding is UTF-8.</param>
        /// <param name="ownsStream">If true, the output stream is closed by the writer when done; otherwise false. The default value is true.</param>
        public static XmlDictionaryWriter CreateHtmlWriter(Stream stream, Encoding encoding, bool ownsStream)
        {
            XmlHtmlWriter htmlWriter = new XmlHtmlWriter();

            htmlWriter.SetOutput(stream, encoding, ownsStream);

            return htmlWriter;
        }
    }
}
