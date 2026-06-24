using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace AllVerge.MessagingModel.Markup.Xml.Schema
{
    using AllVerge.SystemPrimitives.Reflection;

    /// <summary>
    /// Contains Xml Character helper methods.
    /// </summary>
    public static class XsCharHelper
    {
        private static unsafe volatile byte* charProperties;

        unsafe static XsCharHelper()
        {
            byte* arg_3B_0 = ((UnmanagedMemoryStream)typeof(XmlSchema).Assembly.GetEmbeddedStream("XmlCharType.bin")).PositionPointer;

            Thread.MemoryBarrier();

            charProperties = arg_3B_0;
        }

        /// <summary>
        /// Checks whether the character is a valid whitespace character.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static unsafe bool IsWhiteSpaceChar(this char ch)
        {
            return (charProperties[ch] & 1) > 0;
        }
    }
}
