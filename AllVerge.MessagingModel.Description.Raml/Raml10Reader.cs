using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

using System.Runtime.Serialization.Yaml;

using AllVerge.Core.Resource;

using AllVerge.Core.Markup.Document;
using AllVerge.Core.Markup.Yaml;
using AllVerge.Core.Markup.Formatters;

using AllVerge.Core.Model.Actuals;
using AllVerge.Core.Model.XML;

using AllVerge.Core.ServiceModel.Description.Model;
using AllVerge.Core.ServiceModel.Description.Adapters;
using AllVerge.Core.Markup;

namespace AllVerge.Core.ServiceModel.Description.Raml
{
    internal class Raml10Reader : IDescriptionReader
    {
        private Uri descriptionCachePathUri;

        static Raml10Reader()
        {
            MarkupFormatter<MarkupNode>.TryRegister(new YamlMarkupFormatter());
            MarkupFormatter<IRepresentation>.TryRegister(new XmlRepresentationFormatter());
        }

        public Raml10Reader(String descriptionCachePathUri)
        {
            this.descriptionCachePathUri = new Uri(descriptionCachePathUri);
        }

        public ProtocolDescription ReadDescription(string descriptionLocator, bool canReadFromCache = true)
        {
            Uri descriptionLocatorUri = new Uri(descriptionLocator);

            MarkupNode rootNode = MarkupFormatter<MarkupNode>.FromFormattedSource(descriptionLocatorUri, canReadFromCache ? this.descriptionCachePathUri : null, Formats.YAML, out Exception exception);

            DocumentType documentType;
            MarkupNode versionLine;

            if (rootNode.TryFindFirstChildNode((childNode) =>
                childNode.Name == MarkupTokens.COMMENT_NODE_NAME &&
                childNode.Text.StartsWith(RamlTokens.VERSION_LINE_RAML_PREAMBLE),
                out versionLine))
            {
                if (versionLine.Text.StartsWith(RamlTokens.VERSION_LINE_RAML_1_0_PREAMBLE))

                    documentType = DocumentType.RAML10;

                else

                    throw new InvalidOperationException("RAML version not supported.");
            }
            else

                throw new InvalidOperationException("RAML version line not found.");

            String fragmentIdentifier = versionLine.Text.Substring(RamlTokens.VERSION_LINE_RAML_1_0_PREAMBLE_LENGTH).Trim();

            if (fragmentIdentifier == String.Empty)

                return rootNode.ReadFullDescription(descriptionLocator, this.descriptionCachePathUri, documentType);

            else

                throw new InvalidOperationException(String.Format("RAML version line includes fragment identifer {0}.  Description locator must point to a full description.", fragmentIdentifier));
        }
    }
}
