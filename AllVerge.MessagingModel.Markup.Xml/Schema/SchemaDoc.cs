using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace AllVerge.MessagingModel.Markup.Xml.Schema
{
    public class SchemaDoc
    {
        private string documentation;
        private string language;
        private XmlNode[] markup;
        private XmlAttribute[] anyAttributes;

        public SchemaDoc(string documentation)
        {
            this.documentation = documentation;
            this.language = CultureInfo.InvariantCulture.ToString();
        }

        public SchemaDoc(string documentation, string language)
        {
            this.documentation = documentation;
            this.language = language;
            this.markup = null;
            this.anyAttributes = null;
        }

        public SchemaDoc(XmlNode[] markup)
        {
            this.documentation = null;
            this.language = CultureInfo.InvariantCulture.ToString();
            this.markup = markup;
            this.anyAttributes = null;
        }

        public SchemaDoc(XmlNode[] markup, XmlAttribute[] anyAttributes)
        {
            this.documentation = null;
            this.language = CultureInfo.InvariantCulture.ToString();
            this.markup = markup;
            this.anyAttributes = anyAttributes;
        }

        public SchemaDoc(string documentation, XmlNode[] any, XmlAttribute[] anyAttributes)
        {
            this.documentation = documentation;
            this.language = CultureInfo.InvariantCulture.ToString();
            this.markup = any;
            this.anyAttributes = anyAttributes;
        }

        public SchemaDoc(string documentation, string language, XmlNode[] markup, XmlAttribute[] anyAttributes)
        {
            this.documentation = documentation;
            this.language = language;
            this.markup = markup;
            this.anyAttributes = anyAttributes;
        }

        /// <summary>Gets or sets the text documentation for the instance of the <see cref="T:System.Web.Services.Description.DocumentableItem" />.</summary>
        /// <returns>A string that represents the documentation for the <see cref="T:System.Web.Services.Description.DocumentableItem" />.</returns>
        public string Text
        {
            get
            {
                if (this.documentation != null)

                    return this.documentation;

                return null;
            }
        }

        public string Language
        {
            get
            {
                return language;
            }
        }

        public XmlNode[] Markup
        {
            get
            {
                return this.markup;
            }
        }

        /// <summary>Gets or sets an array of type <see cref="T:System.Xml.XmlAttribute" /> that represents attribute extensions of WSDL to comply with Web Services Interoperability (WS-I) Basic Profile 1.1.</summary>
        /// <returns>An array of type <see cref="T:System.Xml.XmlAttribute" /> that represents attribute extensions of WSDL to comply with Web Services Interoperability (WS-I) Basic Profile 1.1.</returns>
        public XmlAttribute[] Attributes
        {
            get
            {
                return this.anyAttributes;
            }
        }
    }
}
