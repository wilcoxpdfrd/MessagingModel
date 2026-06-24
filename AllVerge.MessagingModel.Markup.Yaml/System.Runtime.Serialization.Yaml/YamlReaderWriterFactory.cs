using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Yaml
{
    /// <summary>Produces instances of <see cref="System.Xml.XmlDictionaryReader" /> or <see cref="System.Xml.XmlDictionaryWriter" /> that can read or write data encoded with Yet Another Markup Language (YAML) to or from a stream (or buffer) and an XML Infoset.</summary>
    public static class YamlReaderWriterFactory
    {
        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryReader" /> that can map streams encoded with Yet Another Markup Language (YAML) to an XML Infoset.</summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryReader" /> that can read Yet Another Markup Language (YAML).</returns>
        /// <param name="stream">The input <see cref="System.IO.Stream" /> from which to read.</param>
        /// <param name="quotas">The <see cref="System.Xml.XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        public static XmlDictionaryReader CreateYamlReader(Stream stream, XmlDictionaryReaderQuotas quotas)
        {
            return CreateYamlReader(stream, (Encoding)null, quotas, (OnXmlDictionaryReaderClose)null);
        }

        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryReader" /> that can map buffers encoded with Yet Another Markup Language (YAML) to an XML Infoset.</summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryReader" /> that can process Yet Another Markup Language (YAML) data.</returns>
        /// <param name="buffer">The input <see cref="System.Byte" /> buffer array from which to read.</param>
        /// <param name="quotas">The <see cref="System.Xml.XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        public static XmlDictionaryReader CreateYamlReader(byte[] buffer, XmlDictionaryReaderQuotas quotas)
        {
            if (buffer == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(buffer));
            return CreateYamlReader(buffer, 0, buffer.Length, (Encoding)null, quotas, (OnXmlDictionaryReaderClose)null);
        }

        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryReader" /> that can map streams encoded with Yet Another Markup Language (YAML), of a specified size and offset, to an XML Infoset.</summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryReader" /> that can read Yet Another Markup Language (YAML).</returns>
        /// <param name="stream">The input <see cref="System.IO.Stream" /> from which to read.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the reader. If null is specified as the value, the reader attempts to auto-detect the encoding.</param>
        /// <param name="quotas">The <see cref="System.Xml.XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        /// <param name="onClose">The <see cref="System.Xml.OnXmlDictionaryReaderClose" /> delegate to call when the reader is closed.</param>
        public static XmlDictionaryReader CreateYamlReader(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            XmlYamlReader xmlYamlReader = new XmlYamlReader();
            xmlYamlReader.SetInput(stream, encoding, quotas, onClose);
            return (XmlDictionaryReader)xmlYamlReader;
        }

        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryReader" /> that can map buffers encoded with Yet Another Markup Language (YAML), of a specified size and offset, to an XML Infoset.</summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryReader" /> that can read Yet Another Markup Language (YAML).</returns>
        /// <param name="buffer">The input <see cref="System.Byte" /> buffer array from which to read.</param>
        /// <param name="offset">Starting position from which to read in <paramref name="buffer" />.</param>
        /// <param name="count">Number of bytes that can be read from <paramref name="buffer" />.</param>
        /// <param name="quotas">The <see cref="System.Xml.XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        public static XmlDictionaryReader CreateYamlReader(byte[] buffer, int offset, int count, XmlDictionaryReaderQuotas quotas)
        {
            return CreateYamlReader(buffer, offset, count, (Encoding)null, quotas, (OnXmlDictionaryReaderClose)null);
        }

        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryReader" /> that can map buffers encoded with Yet Another Markup Language (YAML), with a specified size and offset and character encoding, to an XML Infoset. </summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryReader" /> that can read Yet Another Markup Language (YAML).</returns>
        /// <param name="buffer">The input <see cref="System.Byte" /> buffer array from which to read.</param>
        /// <param name="offset">Starting position from which to read in <paramref name="buffer" />.</param>
        /// <param name="count">Number of bytes that can be read from <paramref name="buffer" />.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the reader. If null is specified as the value, the reader attempts to auto-detect the encoding.</param>
        /// <param name="quotas">The <see cref="System.Xml.XmlDictionaryReaderQuotas" /> used to prevent Denial of Service attacks when reading untrusted data. </param>
        /// <param name="onClose">The <see cref="System.Xml.OnXmlDictionaryReaderClose" /> delegate to call when the reader is closed. The default value is null.</param>
        public static XmlDictionaryReader CreateYamlReader(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            XmlYamlReader xmlYamlReader = new XmlYamlReader();
            xmlYamlReader.SetInput(buffer, offset, count, encoding, quotas, onClose);
            return (XmlDictionaryReader)xmlYamlReader;
        }

        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to a stream.</summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the YAML writer.</param>
        public static XmlDictionaryWriter CreateYamlWriter(Stream stream)
        {
            return CreateYamlWriter(stream, Encoding.UTF8, true);
        }

        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to a stream with a specified character encoding.</summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the YAML writer.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the writer. The default encoding is UTF-8.</param>
        public static XmlDictionaryWriter CreateYamlWriter(Stream stream, Encoding encoding)
        {
            return CreateYamlWriter(stream, encoding, true);
        }

        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to a stream with a specified character encoding.</summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the YAML writer.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the writer. The default encoding is UTF-8.</param>
        /// <param name="ownsStream">If true, the output stream is closed by the writer when done; otherwise false. The default value is true.</param>
        public static XmlDictionaryWriter CreateYamlWriter(Stream stream, Encoding encoding, bool ownsStream)
        {
            return CreateYamlWriter(stream, encoding, ownsStream, false);
        }

        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to a stream with a specified character.</summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the YAML writer.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the writer. The default encoding is UTF-8.</param>
        /// <param name="ownsStream">If true, the output stream is closed by the writer when done; otherwise false. The default value is true.</param>
        /// <param name="indent">If true, the output uses multiline format, indenting each level properly; otherwise, false. </param>
        public static XmlDictionaryWriter CreateYamlWriter(Stream stream, Encoding encoding, bool ownsStream, bool indent)
        {
            return CreateYamlWriter(stream, encoding, ownsStream, indent, "  ");
        }

        /// <summary>Creates an <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to a stream with a specified character.</summary>
        /// <returns>An <see cref="System.Xml.XmlDictionaryWriter" /> that writes data encoded with YAML to the stream based on an XML Infoset.</returns>
        /// <param name="stream">The output <see cref="System.IO.Stream" /> for the YAML writer.</param>
        /// <param name="encoding">The <see cref="System.Text.Encoding" /> that specifies the character encoding used by the writer. The default encoding is UTF-8.</param>
        /// <param name="ownsStream">If true, the output stream is closed by the writer when done; otherwise false. The default value is true.</param>
        /// <param name="indent">If true, the output uses multiline format, indenting each level properly; otherwise, false.</param>
        /// <param name="indentChars">The string used to indent each level.</param>
        public static XmlDictionaryWriter CreateYamlWriter(Stream stream, Encoding encoding, bool ownsStream, bool indent, string indentChars)
        {
            XmlYamlWriter xmlYamlWriter = new XmlYamlWriter(indent, indentChars);
            xmlYamlWriter.SetOutput(stream, encoding, ownsStream);
            return (XmlDictionaryWriter)xmlYamlWriter;
        }
    }
}
