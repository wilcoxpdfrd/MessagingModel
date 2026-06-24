using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Html
{
    internal class XmlHtmlReader : XmlBaseReader, IXmlTextReaderInitializer
    {
        public override bool Read()
        {
            throw new NotImplementedException();
        }

        public void SetInput(byte[] buffer, int offset, int count, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            throw new NotImplementedException();
        }

        public void SetInput(Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, OnXmlDictionaryReaderClose onClose)
        {
            throw new NotImplementedException();
        }

        protected override XmlSigningNodeWriter CreateSigningNodeWriter()
        {
            throw new NotImplementedException();
        }
    }
}
