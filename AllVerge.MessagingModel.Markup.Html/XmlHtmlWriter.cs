using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.ServiceModel;
using System.Text;
using System.Web;
using System.Xml;

namespace System.Runtime.Serialization.Html
{
    using System.Runtime.Serialization;

    internal class XmlHtmlWriter : XmlDictionaryWriter, IXmlTextWriterInitializer
    {
        const string XMLNS_NS = "http://www.w3.org/2000/xmlns/";

        XmlWriter writer;
        NamespaceManager nsMgr;

        private int depth;
        private Element[] elements;

        private class Element
        {
            private string prefix;

            private string localName;

            private int prefixId;

            public string Prefix
            {
                get
                {
                    return prefix;
                }
                set
                {
                    prefix = value;
                }
            }

            public string LocalName
            {
                get
                {
                    return localName;
                }
                set
                {
                    localName = value;
                }
            }

            public int PrefixId
            {
                get
                {
                    return prefixId;
                }
                set
                {
                    prefixId = value;
                }
            }

            public void Clear()
            {
                prefix = null;
                localName = null;
                prefixId = 0;
            }
        }

        class NamespaceManager : KeyedCollection<String, (String Prefix, String Namespace)>
        {
            private Dictionary<String, String> nsMap = new Dictionary<string, string>();
            private int depth;

            protected override string GetKeyForItem((string Prefix, string Namespace) item)
            {
                return item.Prefix;
            }

            internal new void Clear()
            {
                base.Clear();

                nsMap.Clear();

                depth = 0;
            }

            internal string LookupPrefix(string ns)
            {
                if (nsMap.ContainsKey(ns))
                
                    return nsMap[ns];

                return null;
            }

            internal string LookupNamespace(string prefix)
            {
                if (this.Contains(prefix))

                    return this[prefix].Namespace;

                return null;
            }

            internal string LookupAttributePrefix(string ns)
            {
                return this.nsMap[ns];
            }

            internal void AddNamespaceIfNotDeclared(string prefix, string ns)
            {
                if (LookupNamespace(prefix) != ns)
                {
                    AddNamespace(prefix, ns);
                }
            }

            internal void AddNamespace(string prefix, string ns)
            {
                this.Add((prefix, ns));

                this.nsMap.Add(ns, prefix);
            }

            internal void Close()
            {
                depth = 0;
            }

            internal void ExitScope()
            {
                depth--;

            }

            internal void EnterScope()
            {
                depth++;
            }
        }

        public XmlHtmlWriter()
        {
            this.writer = null;
            this.nsMgr = new NamespaceManager();
        }

        public override WriteState WriteState => writer == null ? WriteState.Closed : writer.WriteState;

        public void SetOutput(Stream stream, Encoding encoding, bool ownsStream)
        {
            if (writer != null)
            {
                if (writer.WriteState != WriteState.Closed)

                    writer.Close();

                writer = null;
            }

            if (stream == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("stream");
            
            if (encoding == null)
            
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("encoding");
            
            if (encoding.WebName != Encoding.UTF8.WebName)
            {
                stream = new EncodingStreamWrapper(stream, encoding, true);
            }

            if (writer == null)
            {
                XmlWriterSettings settings = new XmlWriterSettings() { OmitXmlDeclaration = true, Encoding = encoding  };

                settings.GetType().GetProperty("OutputMethod").GetSetMethod(true).Invoke(settings, new object[] { XmlOutputMethod.Html });

                writer = XmlWriter.Create(stream, settings);
            }

            nsMgr.Clear();

            if (depth != 0)
            {
                elements = null;
                depth = 0;
            }
        }

        public override void Flush()
        {
            this.writer.Flush();
        }

        public override string LookupPrefix(string ns)
        {
            if (this.writer == null)
            
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            if (ns == XMLNS_NS)

                return "xmlns";

            return nsMgr.LookupPrefix(ns);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            this.writer.WriteBase64(buffer, index, count);
        }

        public override void WriteCData(string text)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteStartElement("span");
            writer.WriteString(text);
            writer.WriteEndElement();
        }

        public override void WriteCharEntity(char ch)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            throw new NotImplementedException();
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            this.writer.WriteChars(buffer, index, count);
        }

        public override void WriteComment(string text)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteComment(text);
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteDocType("html", null, null, null);
        }

        public override void WriteEndAttribute()
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        public override void WriteEndDocument()
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteEndElement();
        }

        public override void WriteEndElement()
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteEndElement();

            writer.WriteEndElement();

            ExitScope();
        }

        public override void WriteEntityRef(string name)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteEntityRef(name);
        }

        public override void WriteFullEndElement()
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteFullEndElement();
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            throw new NotImplementedException();
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteRaw(data);
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            bool writeXmlns = !String.IsNullOrWhiteSpace(ns) && LookupPrefix(ns) == null;

            StartAttribute(prefix, localName, ns);

            writer.WriteStartElement("fieldset");

            writer.WriteElementString("legend", "Attribute");

            bool hasPrefix = !String.IsNullOrWhiteSpace(prefix);

            if (writeXmlns)
            {
                string xmlnsName;

                if (hasPrefix)

                    xmlnsName = $"xmlns:{prefix}";

                else

                    xmlnsName = prefix;

                WriteLabelFor(xmlnsName, $"{xmlnsName}=");

                writer.WriteStartElement("span");

                writer.WriteAttributeString("id", xmlnsName);

                writer.WriteString(ns);

                writer.WriteEndElement();

                writer.WriteElementString("br", null);
            }

            string name;

            if (hasPrefix)

                name = $"{prefix}:{localName}";

            else

                name = localName;

            WriteLabelFor(name, $"{name}=");

            writer.WriteStartElement("div");

            writer.WriteAttributeString("id", name);
        }

        private void StartAttribute(string prefix, string localName, string ns)
        {
            if (prefix == null)
            {
                if (ns == XMLNS_NS && localName != "xmlns")
                {
                    prefix = "xmlns";
                }
                else
                {
                    prefix = string.Empty;
                }
            }
            if (prefix.Length == 0 && localName == "xmlns")
            {
                prefix = "xmlns";
                localName = string.Empty;
            }
            if (prefix == "xmlns")
            {
                if (ns != null && ns != XMLNS_NS)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(PublicSR.Format(PublicSR.XmlPrefixBoundToNamespace, "xmlns", XMLNS_NS, ns), "ns"));
                }
            }
            else if (ns == null)
            {
                if (prefix.Length == 0)
                {
                    ns = string.Empty;
                }
                else
                {
                    ns = nsMgr.LookupNamespace(prefix);
                    if (ns == null)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(PublicSR.Format(PublicSR.XmlUndefinedPrefix, prefix), "prefix"));
                    }
                }
            }
            else if (ns.Length == 0)
            {
                if (prefix.Length != 0)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(PublicSR.XmlEmptyNamespaceRequiresNullPrefix, "prefix"));
                }
            }
            else if (prefix.Length == 0)
            {
                prefix = nsMgr.LookupAttributePrefix(ns);

                if (prefix == null)
                {
                    if (ns.Length == XMLNS_NS.Length && ns == XMLNS_NS)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(PublicSR.Format(PublicSR.XmlSpecificBindingNamespace, "xmlns", ns)));
                    }
                
                    prefix = GeneratePrefix(ns);
                }
            }
            else
            {
                nsMgr.AddNamespaceIfNotDeclared(prefix, ns);
            }
        }

        private string GeneratePrefix(string ns)
        {
            string prefix;

            do
            {
                prefix = string.Concat(str3: elements[depth].PrefixId++.ToString(CultureInfo.InvariantCulture), str0: "d", str1: depth.ToString(CultureInfo.InvariantCulture), str2: "p");
            }
            while (nsMgr.LookupNamespace(prefix) != null);
            
            nsMgr.AddNamespace(prefix, ns);
            
            return prefix;
        }

        public override void WriteStartDocument()
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteStartElement("html");

            writer.WriteStartElement("head");

            writer.WriteElementString("title", "AllVerge Html Document");

            writer.WriteEndElement();

            WriteCData("ToDo: Prepare and link style sheet to accompany this document.");
        }

        public override void WriteStartDocument(bool standalone)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteStartElement("html");

            WriteCData("ToDo: Prepare and link style sheet to accompany this document.");
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            bool writeXmlns = !String.IsNullOrWhiteSpace(ns) && LookupPrefix(ns) == null;

            StartElement(ref prefix, localName, ns);

            if (writeXmlns)
            {
                WriteAttributeString("xmlns", prefix, XMLNS_NS, ns);
            }

            String name;

            if (!String.IsNullOrEmpty(prefix))

                name = $"{prefix}-{localName}";

            else

                name = localName;

            writer.WriteStartElement("fieldset");

            writer.WriteElementString("legend", name);

            writer.WriteStartElement("div");
        }

        private void StartElement(ref string prefix, string localName, string ns)
        {
            if (localName == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("localName"));
            }
            if (localName.Length == 0)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(PublicSR.InvalidLocalNameEmpty, "localName"));
            }
            Element element = EnterScope();
            if (ns == null)
            {
                if (prefix == null)
                {
                    prefix = string.Empty;
                }
                ns = nsMgr.LookupNamespace(prefix);
                if (ns == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(PublicSR.Format(PublicSR.XmlUndefinedPrefix, prefix), "prefix"));
                }
            }
            else if (prefix == null)
            {
                prefix = nsMgr.LookupPrefix(ns);

                if (prefix == null)
                {
                    prefix = GeneratePrefix(ns);
                }
            }
            else
            {
                nsMgr.AddNamespaceIfNotDeclared(prefix, ns);
            }
        }

        private void WriteLabelFor(string name, string label)
        {
            writer.WriteStartElement("label");

            writer.WriteAttributeString("for", name);

            writer.WriteString(label);

            writer.WriteEndElement();
        }

        private Element EnterScope()
        {
            nsMgr.EnterScope();
            depth++;
            if (elements == null)
            {
                elements = new Element[4];
            }
            else if (elements.Length == depth)
            {
                Element[] destinationArray = new Element[depth * 2];
                Array.Copy(elements, destinationArray, depth);
                elements = destinationArray;
            }
            Element element = elements[depth];
            if (element == null)
            {
                element = new Element();
                elements[depth] = element;
            }
            return element;
        }

        private void ExitScope()
        {
            elements[depth].Clear();
            depth--;
            nsMgr.ExitScope();
        }

        public override void WriteString(string text)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteString(text);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteWhitespace(string ws)
        {
            if (this.writer == null)

                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NullReferenceException(nameof(this.writer)));

            writer.WriteWhitespace(ws);
        }

        public override void Close()
        {
            base.Close();

            nsMgr.Close();

            if (depth != 0)
            {
                elements = null;

                depth = 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (writer != null)
                {
                    if (writer.WriteState != WriteState.Closed)

                        writer.Close();

                    writer = null;
                }
            }
        }
    }
}
