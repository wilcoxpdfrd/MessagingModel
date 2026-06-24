using AllVerge.SystemPrimitives.IO;
using AllVerge.SystemPrimitives.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.PlainText
{
    internal class XmlStringReader : XmlUTF8TextReader
    {
        public XmlStringReader() : base() { }

        protected override void OnSetInput(ref byte[] buffer, ref int offset, ref int count)
        {
            byte[] startBytes = Encoding.UTF8.GetBytes("<ms:string xmlns:ms=\"http://schemas.microsoft.com/2003/10/Serialization/\">");
            byte[] endBytes = Encoding.UTF8.GetBytes("</ms:string>");

            if (offset > 0)
                buffer = buffer.Skip(offset).ToArray();
            if (count < buffer.Length)
                buffer = buffer.Take(count).ToArray();

            buffer = startBytes.Concat(buffer).Concat(endBytes).ToArray();

            offset = 0;
            count = buffer.Length;
        }

        protected override void OnSetInput(ref Stream stream, Encoding encoding)
        {
            byte[] startBytes = Encoding.UTF8.GetBytes("<ms:string xmlns:ms=\"http://schemas.microsoft.com/2003/10/Serialization/\">");
            byte[] endBytes = Encoding.UTF8.GetBytes("</ms:string>");

            List<Stream> streams = new List<Stream>();

            streams.Add(new MemoryStream(startBytes));
            streams.Add(stream);
            streams.Add(new MemoryStream(endBytes));

            MemoryStream endStream = new MemoryStream(endBytes);

            stream = new ConcatenatedStream(streams);
        }
    }
}
