using System.Xml;
using Xunit;

namespace AllVerge.MessagingModel.Markup.PlainText.Tests
{
    using System.Runtime.Serialization.PlainText;
    using System.Text;

    public class PlainTextTests
    {
        [Fact]
        public void XmlStreamStringWriterTest()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                XmlWriter xmlWriter = XmlStringReaderWriterFactory.CreateStringWriter(ms);

                xmlWriter.WriteStartDocument();

                xmlWriter.WriteStartElement("root");

                xmlWriter.WriteStartAttribute("my", "test", "uri:my-namespace");

                xmlWriter.WriteString("root-attribute-value");

                xmlWriter.WriteEndAttribute();

                xmlWriter.WriteStartElement("content");

                xmlWriter.WriteString("content text.");

                xmlWriter.WriteString("  some more text");

                xmlWriter.WriteEndElement();

                xmlWriter.WriteStartElement("attribute-only-content");

                xmlWriter.WriteStartAttribute("my-attr", "test", "uri:my-namespace/my-attr");

                xmlWriter.WriteEndAttribute();

                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndDocument();

                xmlWriter.Flush();

                String result = Encoding.Default.GetString(ms.ToArray());
                
                Assert.Equal("﻿root (0):\n\tcontent:\n\tcontent text.  some more text\nattribute-only-content (1)\n\nfootnotes:\n\t\n0 my:test: root-attribute-value\n1 my-attr:test\n\nglossary:\n\t\nmy->uri:my-namespace\nmy-attr->uri:my-namespace/my-attr\n\n", result);
            }
        }

        [Fact]
        public void XmlStreamStringReaderTest()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                StreamWriter streamWriter = new StreamWriter(ms);

                string expectedValue = "﻿root (0):\n\tcontent:\n\tcontent text.  some more text\nattribute-only-content (1)\n\nfootnotes:\n\t\n0 my:test: root-attribute-value\n1 my-attr:test\n\nglossary:\n\t\nmy->uri:my-namespace\nmy-attr->uri:my-namespace/my-attr\n\n";

                streamWriter.Write(expectedValue);

                streamWriter.Flush();

                ms.Seek(0, SeekOrigin.Begin);

                using (XmlReader xmlReader = XmlStringReaderWriterFactory.CreateStringReader(ms, XmlDictionaryReaderQuotas.Max))
                {
                    xmlReader.Read();

                    if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "string" && xmlReader.NamespaceURI == "http://schemas.microsoft.com/2003/10/Serialization/")
                    {
                        StringBuilder stringBuilder = new StringBuilder();

                        while (xmlReader.Read() && xmlReader.NodeType == XmlNodeType.Text || xmlReader.NodeType == XmlNodeType.Whitespace)

                            stringBuilder.Append(xmlReader.Value);

                        if (xmlReader.NodeType != XmlNodeType.EndElement)

                            Assert.Fail($"Unexpected NodeType: {xmlReader.NodeType}");

                        string actualValue = stringBuilder.ToString();

                        Assert.Equal(expectedValue, actualValue);
                    }
                    else

                        Assert.Fail("Failed to read string element node.");

                    if (xmlReader.NodeType != XmlNodeType.EndElement || !(xmlReader.LocalName == "string" && xmlReader.NamespaceURI == "http://schemas.microsoft.com/2003/10/Serialization/"))

                        Assert.Fail("Failed to read string end element node.");

                    xmlReader.Read();

                    if (xmlReader.NodeType != XmlNodeType.None)

                        Assert.Fail("Failed to read EOF.");

                    Assert.True(xmlReader.EOF);
                }
            }
        }

        [Fact]
        public void XmlBufferStringReaderTest()
        {
            string expectedValue = "﻿root (0):\n\tcontent:\n\tcontent text.  some more text\nattribute-only-content (1)\n\nfootnotes:\n\t\n0 my:test: root-attribute-value\n1 my-attr:test\n\nglossary:\n\t\nmy->uri:my-namespace\nmy-attr->uri:my-namespace/my-attr\n\n";

            byte[] buffer = Encoding.UTF8.GetBytes(expectedValue);

            using (XmlReader xmlReader = XmlStringReaderWriterFactory.CreateStringReader(buffer, XmlDictionaryReaderQuotas.Max))
            {
                xmlReader.Read();

                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.LocalName == "string" && xmlReader.NamespaceURI == "http://schemas.microsoft.com/2003/10/Serialization/")
                {
                    StringBuilder stringBuilder = new StringBuilder();

                    while (xmlReader.Read() && xmlReader.NodeType == XmlNodeType.Text || xmlReader.NodeType == XmlNodeType.Whitespace)

                        stringBuilder.Append(xmlReader.Value);

                    if (xmlReader.NodeType != XmlNodeType.EndElement)

                        Assert.Fail($"Unexpected NodeType: {xmlReader.NodeType}");

                    string actualValue = stringBuilder.ToString();

                    Assert.Equal(expectedValue, actualValue);
                }
                else

                    Assert.Fail("Failed to read string element node.");

                if (xmlReader.NodeType != XmlNodeType.EndElement || !(xmlReader.LocalName == "string" && xmlReader.NamespaceURI == "http://schemas.microsoft.com/2003/10/Serialization/"))

                    Assert.Fail("Failed to read string end element node.");

                xmlReader.Read();

                if (xmlReader.NodeType != XmlNodeType.None)

                    Assert.Fail("Failed to read EOF.");

                Assert.True(xmlReader.EOF);
            }
        }
    }
}