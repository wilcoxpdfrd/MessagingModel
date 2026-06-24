using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

using AllVerge.Core.Model;
using AllVerge.Core.Model.IETFTypes;
using AllVerge.Core.Model.JsonSchema;
using AllVerge.Core.Model.LexicalTypes;
using AllVerge.Core.Model.SwaggerTypes;
using AllVerge.Core.Resource;
using AllVerge.Core.ServiceModel.Description.Adapters;
using AllVerge.Core.ServiceModel.Description.Model;

namespace AllVerge.Core.ServiceModel.Description.Swagger
{
    internal class Swagger20Writer : IDescriptionWriter
    {
        //private class Definition
        //{
        //    private Potential potential;

        //    public Definition(Potential potential)
        //    {
        //        this.potential = potential;
        //    }

        //    public Potential Potential { get { return this.potential; } }
        //}

        private string descriptionExportsPath;

        public Swagger20Writer(String descriptionExportsPath)
        {
            this.descriptionExportsPath = descriptionExportsPath;
        }

        public void WriteDescription(ProtocolDescription description, String connectorNameOrIndex, String connectionNameOrIndex, String behaviorNameOrIndex, String hostName)
        {
            QualifiedName.ClearCurrentNamespaceManager();

            String descriptionNamespace = description.QualifiedName.Namespace;

            if (!QualifiedName.TrySetCurrentNamespaceManager(descriptionNamespace))
            {
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());

                foreach (String @namespace in description.Resources)
                {
                    QualifiedName qualifiedName = QualifiedName.CreateImmutableFromFQN(@namespace);

                    namespaceManager.AddNamespace(qualifiedName.Name, qualifiedName.Namespace);
                }

                QualifiedName.SetCurrentNamespaceManager(descriptionNamespace, namespaceManager);
            }

            String descriptionNamespacePrefix;

            if (!QualifiedName.TryLookupNamespacePrefix(descriptionNamespace, out descriptionNamespacePrefix))

                throw new InvalidOperationException(String.Format("Resource targetNamespace '{0}' is not a registered namespace.", descriptionNamespace));

            SwaggerDataTypes.InitializeBuiltInTypes();
            JsonSchemaTypes.TryInitializeBuiltInTypes();
            IETFDataTypes.TryInitializeBuiltInTypes();

            Connector connector;
            
            if (!description.TryGetConnector(connectorNameOrIndex, out connector))

                throw new ArgumentOutOfRangeException(nameof(connectorNameOrIndex), connectorNameOrIndex);

            Connection connection;
            
            if (!connector.TryGetConnection(connectionNameOrIndex, out connection))

                throw new ArgumentOutOfRangeException(nameof(connectionNameOrIndex), connectionNameOrIndex);

            Uri documentUri = new Uri(description.DocumentUrl);

            Uri documentCacheUri = documentUri.GetCachePathUri(new Uri(this.descriptionExportsPath));

            switch (description.DocumentType)
            {
                case DocumentType.SWAGGER20:

                    break;

                default:

                    if (!documentCacheUri.TryAppendSuffixToResourceName("swagger.json", out documentCacheUri))

                        throw new InvalidOperationException($"{documentUri.AbsoluteUri} is not a full resource identifier.");

                    break;
            }

            if (documentCacheUri.IsFile)

                Directory.CreateDirectory(Directory.GetParent(documentCacheUri.LocalPath).FullName);

            using (FileStream fs = new FileStream(documentCacheUri.LocalPath, FileMode.Create))
            {
                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(fs, Encoding.UTF8, false, true))
                {
                    writer.WriteStartDocument();

                    writer.WriteStartElement("root");

                    writer.WriteAttributeString("type", "object");

                    writer.WriteStartElement(SwaggerTokens.SWAGGER);

                    writer.WriteString(SwaggerTokens.VERSION_20);

                    writer.WriteEndElement();

                    description.WriteInfoAndExternalDocsNodes(writer);

                    connection.WriteHostAndBasePathAndSchemeNodes(hostName, writer);

                    IEnumerable<String> tags = description.WriteTags(connection, behaviorNameOrIndex, writer);

                    ReferenceManagers references = new ReferenceManagers();

                    connection.WritePathNodes(behaviorNameOrIndex, writer, references, tags);

                    references.Definitions.WriteKnownReferencesNodes(writer);

                    references.Parameters.WriteKnownReferencesNodes(writer);

                    references.Responses.WriteKnownReferencesNodes(writer);

                    writer.WriteEndElement();

                    writer.WriteEndDocument();

                    writer.Flush();
                }

                fs.Flush();
            }
        }
    }
}