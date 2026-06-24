using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.IO;

namespace AllVerge.MessagingModel.Markup.Xml
{
    /// <summary>
    /// Provides Xml transformation functions
    /// </summary>
    public static class Transformer
    {
        /// <summary>
        /// Gets an xsl compiled transform from <paramref name="transformXml"/>
        /// </summary>
        /// <param name="transformXml">The document element containing the Xsl transform.</param>
        /// <returns>The <see cref="System.Xml.Xsl.XslCompiledTransform"/></returns>
        public static XslCompiledTransform GetTransform(this XmlElement transformXml)
        {
            return GetTransform(transformXml, null);
        }

        /// <summary>
        /// Gets an Xsl compiled transform from <paramref name="transformXml"/> using <paramref name="settings"/>.
        /// </summary>
        /// <param name="transformXml">The document element containing the Xsl transform.</param>
        /// <param name="settings">The <see cref="System.Xml.Xsl.XsltSettings"/> to apply to the style sheet.</param>
        /// <returns>The <see cref="System.Xml.Xsl.XslCompiledTransform"/>.</returns>
        public static XslCompiledTransform GetTransform(this XmlElement transformXml, XsltSettings settings)
        {
            XslCompiledTransform transform =
                new XslCompiledTransform();

            if (settings == null)
                transform.Load(transformXml.CreateNavigator());
            else
                transform.Load(transformXml.CreateNavigator(), settings, new XmlUrlResolver());

            return transform;
        }

        /// <summary>
        /// Get an Xsl arguments list object for <paramref name="arguments"/>.
        /// </summary>
        /// <param name="arguments">A dictionary of <see cref="System.Xml.XmlQualifiedName"/> keyed objects containing the arguments to the Xsl transform.</param>
        /// <returns>The <see cref="System.Xml.Xsl.XsltArgumentList"/>.</returns>
        public static XsltArgumentList GetXsltArguments(this IDictionary<XmlQualifiedName, Object> arguments)
        {
            XsltArgumentList a = new XsltArgumentList();

            foreach (XmlQualifiedName key in arguments.Keys)
            {
                a.AddParam(key.Name, key.Namespace, arguments[key]);
            }

            return a;
        }

        /// <summary>
        /// Transforms <paramref name="element"/> using <paramref name="transform"/>.
        /// </summary>
        /// <param name="transform">The <see cref="System.Xml.Xsl.XslCompiledTransform"/></param>
        /// <param name="element">The document element to transform.</param>
        /// <returns>The transformed document element.</returns>
        public static XmlElement Transform(this XslCompiledTransform transform, XmlElement element)
        {
            return Transform(transform, element, null);
        }

        /// <summary>
        /// Transforms <paramref name="element"/> using <paramref name="transform"/> and <paramref name="arguments"/>.
        /// </summary>
        /// <param name="transform">The <see cref="System.Xml.Xsl.XslCompiledTransform"/></param>
        /// <param name="element">The document element to transform.</param>
        /// <param name="arguments">The arguments to the <paramref name="transform"/>.</param>
        /// <returns>The transformed document element.</returns>
        public static XmlElement Transform(this XslCompiledTransform transform, XmlElement element, XsltArgumentList arguments)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                XmlDocument elementDocument = new XmlDocument();

                elementDocument.AppendChild(elementDocument.ImportNode(element, true));

                transform.Transform(elementDocument, arguments, stream);

                stream.Position = 0;

                XmlDocument transformed = new XmlDocument();

                transformed.Load(stream);

                return transformed.DocumentElement;
            }
        }
    }
}
