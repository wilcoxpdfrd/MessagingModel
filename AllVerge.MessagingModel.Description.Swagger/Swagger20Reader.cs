using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

using AllVerge.Core.Resource;

using AllVerge.Core.Markup;
using AllVerge.Core.Markup.Document;
using AllVerge.Core.Markup.Formatters;
using AllVerge.Core.Markup.Json;

using AllVerge.Core.ServiceModel.Description.Adapters;
using AllVerge.Core.ServiceModel.Description.Model;

namespace AllVerge.Core.ServiceModel.Description.Swagger
{
    internal class Swagger20Reader : IDescriptionReader
    {
        private Uri descriptionCachePathUri;

        static Swagger20Reader()
        {
            MarkupFormatter<MarkupNode>.TryRegister(new JsonMarkupFormatter());
        }

        public Swagger20Reader(String descriptionImportsCachePath)
        {
            this.descriptionCachePathUri = new Uri(descriptionImportsCachePath);
        }

        public ProtocolDescription ReadDescription(string descriptionLocator, bool canReadFromCache = true)
        {
            Uri descriptionLocatorUri = new Uri(descriptionLocator);

            MarkupNode rootNode = MarkupFormatter<MarkupNode>.FromFormattedSource(descriptionLocatorUri, canReadFromCache ? this.descriptionCachePathUri : null, Formats.JSON, out Exception exception);

            MarkupNode swaggerNode;
            DocumentType documentType;

            if (rootNode.TryGetChildNodeByName(SwaggerTokens.SWAGGER, out swaggerNode))
            {
                if (swaggerNode.Text == SwaggerTokens.VERSION_20)

                    documentType = DocumentType.SWAGGER20;

                else

                    throw new InvalidOperationException("SWAGGER version not supported.");
            }
            else

                throw new InvalidOperationException("SWAGGER node not found.");

            return rootNode.ReadFullDescription(descriptionLocator, descriptionCachePathUri, documentType);
        }
    }
}
