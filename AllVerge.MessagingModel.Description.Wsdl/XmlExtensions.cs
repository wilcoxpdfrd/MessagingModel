using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace AllVerge.Core.ServiceModel.Description.Wsdl
{

    public static class XmlExtensions
    {
        public static XmlQualifiedName ToSystem(this Microsoft.Xml.XmlQualifiedName xmlQualifiedName)
        {
            return new XmlQualifiedName(xmlQualifiedName.Name, xmlQualifiedName.Namespace);
        }

        public static IEnumerable<XmlQualifiedName> ToSystemQualifiedNames(this Microsoft.Xml.Serialization.XmlSerializerNamespaces xmlSerializerNamespaces)
        {
            return xmlSerializerNamespaces.ToArray().Select(n => new XmlQualifiedName(n.Name, n.Namespace));
        }

        public static Microsoft.Xml.XmlQualifiedName ToMicrosoft(this XmlQualifiedName xmlQualifiedName)
        {
            return new Microsoft.Xml.XmlQualifiedName(xmlQualifiedName.Name, xmlQualifiedName.Namespace);
        }

        public static XmlSerializerNamespaces ToSystem(this Microsoft.Xml.Serialization.XmlSerializerNamespaces xmlSerializerNamespaces)
        {
            return xmlSerializerNamespaces.ToArray().Aggregate(new XmlSerializerNamespaces(), (s, n) => { s.Add(n.Name, n.Namespace); return s; });
        }

        public static void AddFromSystem(this Microsoft.Xml.Serialization.XmlSchemas schemas, XmlSchema schema, out IEnumerable<Microsoft.Xml.Schema.ValidationEventArgs> validationEventArgs)
        {
            MemoryStream ms = new MemoryStream();

            schema.Write(ms);

            ms.Seek(0, SeekOrigin.Begin);

            List<Microsoft.Xml.Schema.ValidationEventArgs> eventArgsSink = new List<Microsoft.Xml.Schema.ValidationEventArgs>();

            Microsoft.Xml.Schema.XmlSchema msSchema = Microsoft.Xml.Schema.XmlSchema.Read(ms, (s, e) => eventArgsSink.Add(e));

            if (eventArgsSink.Count == 0)
            {
                schemas.Add(msSchema);
            }
            validationEventArgs = eventArgsSink;
        }
    }
}
