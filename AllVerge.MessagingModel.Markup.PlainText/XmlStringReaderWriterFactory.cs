using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.PlainText
{
    /// <summary>Produces instances of <see cref="XmlDictionaryReader" /> or <see cref="XmlDictionaryWriter" /> that can read or write data encoded as a String to or from a stream (or buffer) and an XML Infoset.</summary>
    public static class XmlStringReaderWriterFactory
    {
        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map streams encoded as a String to an XML Infoset.</summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can read String.</returns>
        /// <param name="stream">The input <see cref="System.IO.Stream" /> from which to read.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        public static XmlDictionaryReader CreateStringReader(Stream stream, XmlDictionaryReaderQuotas quotas)
        {
            return CreateStringReader(stream, Encoding.UTF8, quotas, (OnXmlDictionaryReaderClose)null);
        }

        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map buffers encoded as a String to an XML Infoset.</summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can process String data.</returns>
        /// <param name="buffer">The input <see cref="System.Byte" /> buffer array from which to read.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        public static XmlDictionaryReader CreateStringReader(byte[] buffer, XmlDictionaryReaderQuotas quotas)
        {
            if (buffer == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(buffer));
            return CreateStringReader(buffer, 0, buffer.Length, Encoding.UTF8, quotas, (OnXmlDictionaryReaderClose)null);
        }

        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map streams encoded as a String, of a specified size and offset, to an XML Infoset.</summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can read String.</returns>
        /// <param name="stream">The input <see cref="System.IO.Stream" /> from which to read.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the reader. If null is specified as the value, the reader attempts to auto-detect the encoding.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        /// <param name="onClose">The <see cref="OnXmlDictionaryReaderClose" /> delegate to call when the reader is closed.</param>
        public static XmlDictionaryReader CreateStringReader(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            XmlStringReader xmlStringReader = new XmlStringReader();
            xmlStringReader.SetInput(stream, encoding, quotas, onClose);
            return (XmlDictionaryReader)xmlStringReader;
        }

        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map buffers encoded as a String, of a specified size and offset, to an XML Infoset.</summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can read String.</returns>
        /// <param name="buffer">The input <see cref="System.Byte" /> buffer array from which to read.</param>
        /// <param name="offset">Starting position from which to read in <paramref name="buffer" />.</param>
        /// <param name="count">Number of bytes that can be read from <paramref name="buffer" />.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        public static XmlDictionaryReader CreateStringReader(byte[] buffer, int offset, int count, XmlDictionaryReaderQuotas quotas)
        {
            return CreateStringReader(buffer, offset, count, Encoding.UTF8, quotas, (OnXmlDictionaryReaderClose)null);
        }

        /// <summary>Creates an <see cref="XmlDictionaryReader" /> that can map buffers encoded as a String, with a specified size and offset and character encoding, to an XML Infoset. </summary>
        /// <returns>An <see cref="XmlDictionaryReader" /> that can read String.</returns>
        /// <param name="buffer">The input <see cref="System.Byte" /> buffer array from which to read.</param>
        /// <param name="offset">Starting position from which to read in <paramref name="buffer" />.</param>
        /// <param name="count">Number of bytes that can be read from <paramref name="buffer" />.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the reader. If null is specified as the value, the reader attempts to auto-detect the encoding.</param>
        /// <param name="quotas">The <see cref="XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        /// <param name="onClose">The <see cref="OnXmlDictionaryReaderClose" /> delegate to call when the reader is closed. The default value is null.</param>
        public static XmlDictionaryReader CreateStringReader(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            XmlStringReader xmlStringReader = new XmlStringReader();
            xmlStringReader.SetInput(buffer, offset, count, encoding, quotas, onClose);
            return (XmlDictionaryReader)xmlStringReader;
        }

        /// <summary>Creates an <see cref="XmlDictionaryWriter" /> that writes data encoded as a String to a stream.</summary>
        /// <returns>An <see cref="XmlDictionaryWriter" /> that writes data encoded as a String to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the String writer.</param>
        public static XmlDictionaryWriter CreateStringWriter(Stream stream)
        {
            return CreateStringWriter(stream, Encoding.UTF8, true);
        }

        /// <summary>Creates an <see cref="XmlDictionaryWriter" /> that writes data encoded as a String to a stream with a specified character encoding.</summary>
        /// <returns>An <see cref="XmlDictionaryWriter" /> that writes data encoded as a String to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the String writer.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the writer. The default encoding is UTF-8.</param>
        public static XmlDictionaryWriter CreateStringWriter(Stream stream, Encoding encoding)
        {
            return CreateStringWriter(stream, encoding, true);
        }

        /// <summary>Creates an <see cref="XmlDictionaryWriter" /> that writes data encoded as a String to a stream with a specified character.</summary>
        /// <returns>An <see cref="XmlDictionaryWriter" /> that writes data encoded as a String to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the String writer.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the writer. The default encoding is UTF-8.</param>
        /// <param name="ownsStream">If true, the output stream is closed by the writer when done; otherwise false. The default value is true.</param>
        public static XmlDictionaryWriter CreateStringWriter(Stream stream, Encoding encoding, bool ownsStream)
        {
            XmlStringWriter stringWriter = new XmlStringWriter();

            stringWriter.SetOutput(stream, encoding, ownsStream);

            return stringWriter;
        }
    }
}
