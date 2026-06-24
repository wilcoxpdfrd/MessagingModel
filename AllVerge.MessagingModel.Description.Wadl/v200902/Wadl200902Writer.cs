using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using AllVerge.Core.Resource;

using AllVerge.Core.ServiceModel.Description.Adapters;
using AllVerge.Core.ServiceModel.Description.Model;

namespace AllVerge.Core.ServiceModel.Description.Wadl.v200902
{
    internal class Wadl200902Writer : IDescriptionWriter
    {
        private string descriptionExportsPath;

        public Wadl200902Writer(String descriptionExportsPath)
        {
            this.descriptionExportsPath = descriptionExportsPath;
        }

        public void WriteDescription(ProtocolDescription description, string connectorNameOrIndex, string connectionNameOrIndex, string behaviorNameOrIndex, string hostName)
        {
            Connector connector;

            if (!description.TryGetConnector(connectorNameOrIndex, out connector))

                throw new ArgumentOutOfRangeException(nameof(connectorNameOrIndex), "Not found.");

            Connection connection;

            if (!connector.TryGetConnection(connectionNameOrIndex, out connection))

                throw new ArgumentOutOfRangeException(nameof(connectionNameOrIndex), "Not found.");

            Uri documentUri = new Uri(description.DocumentUrl);

            Uri documentCacheUri = documentUri.GetCachePathUri(new Uri(this.descriptionExportsPath));

            switch (description.DocumentType)
            {
                case DocumentType.WADL200610:
                case DocumentType.WADL200902:

                    break;

                default:

                    if (!documentCacheUri.TryAppendSuffixToResourceName("wadl", out documentCacheUri))

                        throw new Exception();

                    break;
            }

            if (documentCacheUri.IsFile)

                Directory.CreateDirectory(Directory.GetParent(documentCacheUri.LocalPath).FullName);

            using (FileStream fs = new FileStream(documentCacheUri.LocalPath, FileMode.Create))
            {
                using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateDictionaryWriter(XmlWriter.Create(fs, new XmlWriterSettings())))
                {
                    ;
                }
            }
        }
    }
}
