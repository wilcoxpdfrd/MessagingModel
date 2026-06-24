using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public class MarkupNamespaceAttribute : MarkupAttribute
    {
        public const string XmlNameSpaceUri = "http://www.w3.org/2000/xmlns/";

        public MarkupNamespaceAttribute(string nameSpacePrefix, string nameSpace) : base(nameSpacePrefix, nameSpace)
        {
        }
    }
}
