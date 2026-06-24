using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AllVerge.Core.Resource;

using AllVerge.Core.ServiceModel.Description.Adapters;
using AllVerge.Core.ServiceModel.Description.Model;

namespace AllVerge.Core.ServiceModel.Description.Raml
{
    public class Raml10Writer : IDescriptionWriter
    {
        private string descriptionExportsPath;

        public Raml10Writer(String descriptionExportsPath)
        {
            this.descriptionExportsPath = descriptionExportsPath;
        }

        public void WriteDescription(ProtocolDescription description, string connectorNameOrIndex, string connectionNameOrIndex, string behaviorNameOrIndex, string hostName)
        {
            Uri documentUri = new Uri(description.DocumentUrl);

            Uri documentCacheUri = documentUri.GetCachePathUri(new Uri(this.descriptionExportsPath));

            switch (description.DocumentType)
            {
                case DocumentType.RAML10:

                    break;

                default:

                    if (!documentCacheUri.TryAppendSuffixToResourceName("raml", out documentCacheUri))

                        throw new InvalidOperationException($"{documentUri.AbsoluteUri} is not a full resource identifier.");

                    break;
            }

            if (documentCacheUri.IsFile)

                Directory.CreateDirectory(Directory.GetParent(documentCacheUri.LocalPath).FullName);

            throw new NotImplementedException($"Wrinting to {documentCacheUri.LocalPath}");
        }
    }
}
