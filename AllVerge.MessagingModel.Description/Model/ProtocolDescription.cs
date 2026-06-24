using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.Description.Model
{
    using AllVerge.DataModel.Primitives;
    using AllVerge.DataModel.Primitives.Actuals;
    using AllVerge.DataModel.Primitives.DataTypes;
    using AllVerge.DataModel.Primitives.DataTypes.Abstractions;

    using AllVerge.DataModel.RamlTypes;
    using AllVerge.DataModel.SwaggerTypes;
    using AllVerge.DataModel.XMLSchema;
    using AllVerge.DataModel.YamlTypes;
    using AllVerge.DataModel.IETFTypes;
    using AllVerge.DataModel.JsonSchema;
    using AllVerge.DataModel.XML;
    using AllVerge.DataModel.Json;

    using AllVerge.MessagingModel.Description.Adapters;
    using AllVerge.MessagingModel.Markup.Yaml;
    using AllVerge.MessagingModel.MarkupPrimitives;
    using AllVerge.MessagingModel.MarkupPrimitives.Formatters;
    using AllVerge.MessagingModel.MarkupPrimitives.Json;
    using AllVerge.MessagingModel.MarkupPrimitives.Xml;
    
    using AllVerge.SystemPrimitives;
    using AllVerge.SystemPrimitives.Net;
    
    using Newtonsoft.Json;
    
    public class ProtocolDescription : QualifiedAgent
    {
        public const String ModelNamespace = "http://allverge.com/model";

        public static readonly String DocumentTypeNone = "none";
	
	    public static readonly String DocumentTypeWsdl11 = "wsdl11";
	    public static readonly String DocumentTypeUriWsdl11 = "http://schemas.xmlsoap.org/wsdl/";

	    public static readonly String DocumentTypeWsdl20 = "wsdl20";
	    public static readonly String DocumentTypeUriWsdl20 = "http://www.w3.org/ns/wsdl";
	
	    public static readonly String DocumentTypeWadl = "wadl";
	    public static readonly String DocumentTypeUriWadl = "http://wadl.dev.java.net/2009/02";
	
	    public static readonly String BindingWsdlUriPrefixSoap11 = "soap11";
	    public static readonly String BindingWsdlUriSoap11 = "http://schemas.xmlsoap.org/wsdl/soap/";

	    public static readonly String BindingWsdlUriPrefixSoap12 = "soap12";
	    public static readonly String BindingWsdlUriSoap12 = "http://schemas.xmlsoap.org/wsdl/soap12/";

        public static readonly String BindingWsdlUriPrefixHttp = "http";
	    public static readonly String BindingWsdlUriHttp = "http://schemas.xmlsoap.org/wsdl/http/";

	    public static readonly String BindingWsdlUriPrefixMime = "mime";
	    public static readonly String BindingWsdlUriMime = "http://schemas.xmlsoap.org/wsdl/mime/";
	
	    public static readonly String BindingWsdlUriPrefixEncodingSoap11 = "soapenc";

        public static readonly String BindingWsdlUriEncodingSoap11 = "http://schemas.xmlsoap.org/soap/encoding/"; 

	    public static readonly String BindingWsdlUriPrefixEncodingSoap12 = "soap-enc"; 
	    public static readonly String BindingWsdlUriEncodingSoap12 = "http://www.w3.org/2001/12/soap-encoding";

        public const String ATTRIBUTE_RESOURCE_URI = "resourceUri";
        public const String JSON_ATTRIBUTE_RESOURCE_URI = "@" + ATTRIBUTE_RESOURCE_URI;
        public const String JSON_PROPERTY_RESOURCES = "resources";
        public const String PROPERTY_RESOURCES = "Resources";
        public const String PROPERTY_RESOURCES_ITEMS = "Uri";
        public const String ATTRIBUTE_DOCUMENT_NAME = "qualifiedName";
        public const String ATTRIBUTE_DOCUMENT_TYPE = "documentType";
        public const String JSON_ATTRIBUTE_DOCUMENT_TYPE = "@" + ATTRIBUTE_DOCUMENT_TYPE;
        public const String ATTRIBUTE_DOCUMENT_URL = "documentUrl";
        public const String JSON_ATTRIBUTE_DOCUMENT_URL = "@" + ATTRIBUTE_DOCUMENT_URL;
        public const String JSON_PROPERTY_CONNECTORS = "connectors";
        public const String PROPERTY_CONNECTORS = "Connectors";
        public const String PROPERTY_CONNECTORS_ITEMS = nameof(Connector);

        private DocumentType documentType;
        private string documentUrl;
        private QualifiedName[] resourceIds;
        private List<Connector> connectors;

        public ProtocolDescription() :
            base(Fixable.FixOnRead)
        {
            SetLocalFields(null, null, null);
        }

        [JsonConstructor]
        protected ProtocolDescription(Fixable fixable) :
            base(fixable)
        {
            this.SetLocalFields(null, null, null);
        }

        public ProtocolDescription(QualifiedName qualifiedName, Attributes attributes, Annotations annotations, DocumentType documentType, string documentUrl, QualifiedName[] resourceIds, params Connector[] connectors) :
            base(qualifiedName, attributes, annotations)
        {
            SetLocalFields(documentType, documentUrl, resourceIds, connectors);
        }

        private void SetLocalFields(DocumentType? documentType, string documentUrl, QualifiedName[] resourceIds, params Connector[] connectors)
        {
            this.documentType = documentType ?? DocumentType.DEFAULT;
            this.documentUrl = documentUrl ?? String.Empty;
            this.resourceIds = resourceIds ?? new QualifiedName[0];
            this.connectors = new List<Connector>(connectors);

            base.SetHandledAttributeNames(ATTRIBUTE_RESOURCE_URI, ATTRIBUTE_DOCUMENT_TYPE, ATTRIBUTE_DOCUMENT_URL);
        }

        [XmlAttribute(ATTRIBUTE_RESOURCE_URI)]
        [JsonProperty(JSON_ATTRIBUTE_RESOURCE_URI)]
        public String ResourceUri
        {
            get
            {
                this.Fixed.OnRead();

                return ProtocolDescription.ModelNamespace;
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(ResourceUri));

                if (value != ProtocolDescription.ModelNamespace)

                    throw new MemberAccessException("Property value is invalid.");
            }
        }

        [XmlAttribute(ATTRIBUTE_DOCUMENT_TYPE)]
        [JsonProperty(JSON_ATTRIBUTE_DOCUMENT_TYPE)]
        public DocumentType DocumentType
        {
            get
            {
                this.Fixed.OnRead();

                return this.documentType;
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(DocumentType));

                this.documentType = value;
            }
        }

        [XmlAttribute(ATTRIBUTE_DOCUMENT_URL)]
        [JsonProperty(JSON_ATTRIBUTE_DOCUMENT_URL)]
        public string DocumentUrl
        {
            get
            {
                this.Fixed.OnRead();

                return this.documentUrl;
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(DocumentUrl));

                this.documentUrl = value;
            }
        }

        [JsonProperty(JSON_PROPERTY_RESOURCES)]
        [XmlArray(PROPERTY_RESOURCES)]
        [XmlArrayItem(PROPERTY_RESOURCES_ITEMS)]
        public String[] Resources
        {
            get
            {
                this.Fixed.OnRead();

                return this.resourceIds.Select(n => n.FullyQualifiedName).ToArray();
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(Resources));

                this.resourceIds = value.Select(u => QualifiedName.CreateImmutableFromFQN(u)).ToArray();
            }
        }

        [JsonProperty(JSON_PROPERTY_CONNECTORS)]
        [XmlArray(PROPERTY_CONNECTORS)]
        [XmlArrayItem(PROPERTY_CONNECTORS_ITEMS)]
        public Connector[] Connectors
        {
            get
            {
                this.Fixed.OnRead();

                return this.connectors.ToArray();
            }

            set
            {
                this.Fixed.ThrowIfNotWriteable(this.AttributableTypeName, nameof(Connectors));

                this.connectors.Clear();

                if (value != null)

                    this.connectors.AddRange(value);
            }
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            this.Fixed.OnDeserialized(streamingContext);
        }

        protected override void OnWriteAttributes(XmlWriter writer)
        {
            base.OnWriteAttributes(writer);

            writer.WriteAttributeString(ATTRIBUTE_RESOURCE_URI, this.ResourceUri);

            writer.WriteAttributeString(ATTRIBUTE_DOCUMENT_TYPE, this.DocumentType.ToString());

            writer.WriteAttributeString(ATTRIBUTE_DOCUMENT_URL, this.DocumentUrl);
        }

        protected override void OnReadAttributes(XmlReader reader)
        {
            base.OnReadAttributes(reader);

            if (reader.MoveToAttribute(ATTRIBUTE_RESOURCE_URI))
            {
                this.ResourceUri = reader.Value;

                reader.MoveToElement();
            }

            if (reader.MoveToAttribute(ATTRIBUTE_DOCUMENT_TYPE))
            {
                this.DocumentType = (DocumentType)Enum.Parse(typeof(DocumentType), reader.Value);

                reader.MoveToElement();
            }

            if (reader.MoveToAttribute(ATTRIBUTE_DOCUMENT_URL))
            {
                this.DocumentUrl = reader.Value;

                reader.MoveToElement();
            }
        }

        protected override void OnWriteProperties(XmlWriter writer)
        {
            base.OnWriteProperties(writer);

            writer.WriteStartElement(PROPERTY_RESOURCES);

            foreach (String resourceId in this.Resources)

                writer.WriteElementString(PROPERTY_RESOURCES_ITEMS, resourceId);

            writer.WriteEndElement();

            writer.WriteStartElement(PROPERTY_CONNECTORS);

            foreach (Connector connector in this.Connectors)

                writer.WriteRaw(connector.Serialize(XmlSerialization.EmptyNSMap).OuterXml);

            writer.WriteEndElement();
        }

        protected override void OnReadProperties(XmlReader reader, string elementName)
        {
            base.OnReadProperties(reader, elementName);

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_RESOURCES)
            {
                List<string> resourceIds = new List<string>();

                reader.Read();

                while (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_RESOURCES_ITEMS)
                {
                    resourceIds.Add(reader.ReadElementContentAsString());
                }

                this.Resources = resourceIds.ToArray();

                reader.ReadEmptyOrEndElement(PROPERTY_RESOURCES);
            }

            if (reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_CONNECTORS)
            {
                while (reader.Read() && reader.NodeType == XmlNodeType.Element && reader.Name == PROPERTY_CONNECTORS_ITEMS)
                {
                    using (XmlReader r = reader.ReadSubtree())
                    {
                        this.connectors.Add(r.Deserialize<Connector>());
                    }
                }
            }

            reader.ReadEmptyOrEndElement(PROPERTY_CONNECTORS);
        }

        [XmlIgnore]
        public XmlNamespaceManager NamespaceManager { get; set; }

        public void AddConnector(Connector connector)
        {
            this.connectors.Add(connector);
        }

        public bool TryGetConnector(string connectorQualifiedNameOrIndex, out Connector connector)
        {
            int serviceIndex;

            if (int.TryParse(connectorQualifiedNameOrIndex, out serviceIndex))
            {
                if (serviceIndex >= 0 && serviceIndex < this.Connectors.Length)

                    connector = this.Connectors[serviceIndex];

                else

                    connector = null;
            }
            else

                connector = this.Connectors.FirstOrDefault(s => s.QualifiedNameToken == connectorQualifiedNameOrIndex);

            return connector != null;
        }

        public ProtocolDescription ShallowClone()
        {
            return 
                new ProtocolDescription(
                    this.QualifiedName, 
                    this.Attributes.Clone(), 
                    this.Annotations.Clone(), 
                    this.DocumentType, 
                    this.DocumentUrl, 
                    this.resourceIds, 
                    this.Connectors.ShallowClone());
        }

        public String ToString(Formats format)
        {
            String s = null;

            switch (format)
            {
                case Formats.XML:

                    s = this.Serialize().OuterXml;

                    break;

                case Formats.JSON:

                    s = this.SerializeAsJsonString();

                    break;

                case Formats.YAML:

                    s = this.SerializeAsYaml();

                    break;
            }

            return s;
        }

        public Stream ToResourceStream(Formats format = Formats.JSON, bool omitBOM = false)
        {
            if (format != Formats.JSON)

                throw new NotImplementedException(format.ToString());

            MemoryStream ms = new MemoryStream();

            Encoding encoding;

            if (omitBOM)

                encoding = new UTF8Encoding();

            else

                encoding = Encoding.UTF8;

            using (StreamWriter sw = new StreamWriter(ms, encoding, 1024, true))
            {
                sw.NewLine = "";

                sw.WriteLine("{");

                DataTypeCollection.WriteJsonPropertyName(sw, "description", true);

                this.SerializeAsJson(sw);

                sw.WriteLine(",");

                DataTypeCollection.WriteJsonPropertyName(sw, "resourceDataTypesMap", true);

                DataTypeCollection.GetJsonFormsMap(sw, this.Resources.Select(r => QualifiedName.CreateImmutableFromFQN(r)));

                sw.WriteLine("}");

                sw.Flush();

                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }
        }

        public void Save(String descriptionCachePath, bool backup = true)
        {
            Uri documentUri = new Uri(this.documentUrl);

            Uri documentCacheUri = documentUri.GetCachePathUri(new Uri(descriptionCachePath));

            Uri protocolCacheUri;

            if (documentCacheUri.TryAppendSuffixToResourceName("protocol.xml", out protocolCacheUri))
            {
                if (protocolCacheUri.IsFile)
                {
                    Directory.CreateDirectory(Directory.GetParent(protocolCacheUri.LocalPath).FullName);

                    protocolCacheUri.TryBackup(10);

                    using (FileStream fs = File.Open(protocolCacheUri.LocalPath, FileMode.Create))
                    {
                        Exception e;

                        fs.WriteXml(this.Serialize(), out e);

                        if (e != null)

                            throw e;
                    }
                }
                else

                    throw new InvalidOperationException("Could not prepare a cache Uri for the resource.");
            }
            else

                throw new InvalidOperationException("Could not form a cache Uri for the resource.");
        }

        public static ProtocolDescription LoadFromCache(String cachePath, String protocolDescriptionUrl, out String targetNamespace)
        {
            Uri protocolDescriptionUri = new Uri(protocolDescriptionUrl);

            Uri protocolDescriptionCacheUri = protocolDescriptionUri.GetCachePathUri(new Uri(cachePath));

            Uri protocolDocumentCacheUri;

            if (protocolDescriptionCacheUri.TryAppendSuffixToResourceName("protocol.xml", out protocolDocumentCacheUri))
            {
                if (protocolDocumentCacheUri.IsFile)

                    return Load(protocolDocumentCacheUri.LocalPath, out targetNamespace);

                else

                    return Load(protocolDocumentCacheUri.AbsolutePath, out targetNamespace);
            }

            targetNamespace = null;

            return null;
        }

        public static ProtocolDescription Load(string protocolDescriptionFile, out string targetNamespace)
        {
            if (File.Exists(protocolDescriptionFile))
            {
                XmlElement element;

                using (FileStream fs = File.OpenRead(protocolDescriptionFile))
                {
                    Exception e;

                    fs.ReadXml(out element, out e);

                    if (e != null)

                        throw e;
                }

                return Load(element, out targetNamespace);
            }

            targetNamespace = null;

            return null;
        }

        public static ProtocolDescription Load(XmlElement protocolDescriptionElement, out String documentTargetNamespace)
        {
            if (protocolDescriptionElement == null)

                throw new ArgumentNullException(nameof(protocolDescriptionElement));

            MarkupFormatter<IRepresentation>.TryRegister(new XmlRepresentationFormatter());
            MarkupFormatter<IRepresentation>.TryRegister(new JsonRepresentationFormatter());

            BindingConstants.InitializeBindingConstants();

            QualifiedName.ClearCurrentNamespaceManager();

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(protocolDescriptionElement.OwnerDocument.NameTable);

            AbstractDataTypes.TryInitializeBuiltInTypes();

            foreach (XmlNode namespaceNode in protocolDescriptionElement.SelectNodes(PROPERTY_RESOURCES +"/" + PROPERTY_RESOURCES_ITEMS))
            {
                QualifiedName qualifiedName = QualifiedName.CreateImmutableFromFQN(namespaceNode.InnerText);

                namespaceManager.AddNamespace(qualifiedName.LocalName, qualifiedName.Namespace);

                switch (qualifiedName.Namespace)
                {
                    case IETFDataTypes.RFC2616_NAMESPACE:

                        IETFDataTypes.InitializeRFC2616BuiltInTypes(qualifiedName.LocalName);

                        break;

                    case IETFDataTypes.RFC3339_NAMESPACE:

                        IETFDataTypes.InitializeRFC3339BuiltInTypes(qualifiedName.LocalName);

                        break;

                    case IETFDataTypes.RFC3986_NAMESPACE:

                        IETFDataTypes.InitializeRFC3986BuiltInTypes(qualifiedName.LocalName);

                        break;

                    case IETFDataTypes.RFC3987_NAMESPACE:

                        IETFDataTypes.InitializeRFC3987BuiltInTypes(qualifiedName.LocalName);

                        break;

                    case IETFDataTypes.RFC5322_NAMESPACE:

                        IETFDataTypes.InitializeRFC5322BuiltInTypes(qualifiedName.LocalName);

                        break;

                    case JsonSchemaTypes.NAMESPACE:

                        JsonSchemaTypes.TryInitializeBuiltInTypes(qualifiedName.LocalName);

                        break;

                    case RamlDataTypes.NAMESPACE:

                        RamlDataTypes.TryInitializeBuiltInTypes(qualifiedName.LocalName);

                        break;

                    case SwaggerDataTypes.NAMESPACE:

                        SwaggerDataTypes.InitializeBuiltInTypes(qualifiedName.LocalName);

                        break;

                    case XsDataType.NAMESPACE:

                        XsDataType.InitializeXsdBuiltInTypes(qualifiedName.LocalName);

                        break;

                    case XsDataType.INSTANCE_NAMESPACE:

                        XsDataType.InitializeXsiBuiltInTypes(qualifiedName.LocalName);

                        break;

                    case YamlTypes.NAMESPACE:

                        YamlTypes.TryInitializeBuiltInTypes();

                        break;
                }
            }

            XmlNode documentNameNode = protocolDescriptionElement.SelectSingleNode("@"+ ATTRIBUTE_DOCUMENT_NAME);

            String documentPrefix;
            String documentLocalName;

            QualifiedName.ParsePrefixedName(documentNameNode.InnerText, out documentPrefix, out documentLocalName);

            documentTargetNamespace = namespaceManager.LookupNamespace(documentPrefix);

            String fullyQualifiedDocumentName = QualifiedName.CreateFullyQualifiedName(documentTargetNamespace, documentLocalName);

            if (!QualifiedName.TrySetCurrentNamespaceManager(fullyQualifiedDocumentName))
            
                QualifiedName.SetCurrentNamespaceManager(fullyQualifiedDocumentName, namespaceManager);

            ProtocolDescription description = protocolDescriptionElement.Deserialize<ProtocolDescription>();

            if (description != null)
            {
                description.NamespaceManager = namespaceManager;

                description.SetFixed(true);
            }

            return description;
        }

        public static ProtocolDescription Load(Stream stream, Formats format = Formats.JSON)
        {
            StreamReader sr = new StreamReader(stream);

            DescriptionReference descriptionReference;

            switch (format)
            {
                case Formats.JSON:

                    descriptionReference = sr.ReadToEnd().DeserializeJson<DescriptionReference>();

                    break;

                default:

                    throw new NotImplementedException(format.ToString());
            }

            return descriptionReference.Description;
        }

        public static void Patch(String cachePath, String documentUrl, Stream descriptionStream, Formats format = Formats.JSON)
        {
            Uri descriptionUri = new Uri(documentUrl);

            ProtocolDescription description = LoadFromCache(cachePath, documentUrl, out string targetNamespace);

            if (description == null)

                throw new InvalidOperationException($"{descriptionUri} does not exist on this server.");

            ProtocolDescription patchDescription;

            using (MemoryStream stream = new MemoryStream())
            {
                descriptionStream.CopyTo(stream);

                stream.Position = 0;

                switch (format)
                {
                    case Formats.JSON:

                        if (descriptionUri.TryAppendSuffixToResourceName("protocol.patch.json", out Uri patchDescriptionUri))

                            patchDescriptionUri.TryCacheResource(new Uri(DescriptionConstants.DESCRIPTIONS_CACHE_PATH), stream, Encoding.UTF8, true);

                        else

                            throw new InvalidOperationException($"{descriptionUri} is not a cacheable Uri.");

                        patchDescription = ProtocolDescription.Load(stream);

                        break;

                    default:

                        throw new NotSupportedException(format.ToString());
                }
            }

            description.Patch(patchDescription);

            // ToDo: Load and save patched description using groupUri ...?

            description.Save(DescriptionConstants.DESCRIPTIONS_CACHE_PATH);
        }

        private void Patch(ProtocolDescription patchDescription)
        {
            if (!this.IsQualified)

                throw new InvalidOperationException("Cannot target unqualified resource for patching.");

            if (!patchDescription.IsQualified)

                throw new ArgumentException("An unqualified resource cannot be a patching source.", nameof(patchDescription.QualifiedName));

            if (this.QualifiedName != patchDescription.QualifiedName)

                throw new ArgumentException("Patch resource does not map to target.", nameof(patchDescription.QualifiedName));

            if (patchDescription.AnnotationsSpecified)

                this.Annotations.Patch(patchDescription.Annotations);

            if (patchDescription.resourceIds.Length > 0)

                this.resourceIds = patchDescription.resourceIds;

            foreach (Connector patchConnector in patchDescription.connectors)
            {
                if (!patchConnector.IsQualified)

                    throw new ArgumentException("An unqualified resource member cannot be applied as part of a patch.");

                if (this.TryGetConnector(patchConnector.QualifiedName, out Connector connector))

                    connector.Patch(patchConnector);

                else

                    this.AddConnector(patchConnector);
            }
        }

        private struct DescriptionReference
        {
            [JsonProperty("description")]
            public ProtocolDescription Description;
        }
    }
}
