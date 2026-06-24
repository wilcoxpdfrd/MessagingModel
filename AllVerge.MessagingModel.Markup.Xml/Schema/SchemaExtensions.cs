using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using AllVerge.SystemPrimitives.Net;

namespace AllVerge.MessagingModel.Markup.Xml.Schema
{
    public static class SchemaExtensions
    {
        private static Dictionary<String, List<SchemaDoc>> documentationCache = new Dictionary<string, List<SchemaDoc>>();
        private static Dictionary<String, XmlSchemas> schemasCache = new Dictionary<string, XmlSchemas>();

        public static Dictionary<string, XmlSchemas> SchemasCache
        {
            get
            {
                return schemasCache;
            }
        }

        public static void PutDocumentationCache(this IEnumerable<SchemaDoc> documentation, String contextNamespace)
        {
            if (!documentationCache.ContainsKey(contextNamespace))

                documentationCache.Add(contextNamespace, new List<SchemaDoc>());

            documentationCache[contextNamespace].AddRange(documentation);
        }

        public static void SetSchemasCache(this IEnumerable<XmlSchema> schemas, XmlNamespaceManager xmlNamespaceManager, String schemasBaseLocation, String contextNamespace)
        {
            schemas.Aggregate(new XmlSchemas(), (ss, s) => { ss.Add(s); return ss; }).SetSchemasCache(xmlNamespaceManager, schemasBaseLocation, contextNamespace);
        }

        public static void SetSchemasCache(this XmlSchemas schemas, XmlNamespaceManager xmlNamespaceManager, String schemasBaseLocation, String contextNamespace)
        {
            if (!schemasCache.ContainsKey(contextNamespace))
            {
                schemasCache.Add(contextNamespace, schemas);

                List<ValidationEventArgs> validationResults = new List<ValidationEventArgs>();

                if (schemas.Count == 0)
                {
                    String xsdPrefix = xmlNamespaceManager.LookupPrefix(XmlSchema.Namespace);

                    XmlSchema schema;

                    ValidationEventHandler schemaReaderValidator = delegate (Object sender, ValidationEventArgs e) { validationResults.Add(e); };

                    if (xsdPrefix == null)
                        schema = XmlSchema.Read(
                            new StringReader(
                                String.Format("<schema xmlns=\"{0}\" targetNamespace=\"{1}\" />", XmlSchema.Namespace, contextNamespace)), schemaReaderValidator);
                    else
                        schema = XmlSchema.Read(
                            new StringReader(
                                String.Format("<{0}:schema xmlns:{0}=\"{1}\" targetNamespace=\"{2}\" />", xsdPrefix, XmlSchema.Namespace, contextNamespace)), schemaReaderValidator);

                    if (validationResults.Count > 0)

                        throw validationResults.First<ValidationEventArgs>().Exception;

                    schemas.Add(schema);
                }
                else
                {
                    int i = 0;

                    while (i < schemas.Count)
                    {
                        int j = schemas[i].Includes.OfType<XmlSchemaImport>().Select(import => new SchemaImport(import.Namespace, import.SchemaLocation, schemasBaseLocation)).PutSchemasCache(xmlNamespaceManager, contextNamespace);

                        j += schemas[i].Includes.OfType<XmlSchemaInclude>().Select(include => new SchemaInclude(schemasBaseLocation, include.SchemaLocation, include.UnhandledAttributes, include.Annotation.GetSchemaDocs())).PutSchemasCache(xmlNamespaceManager, contextNamespace);

                        i += j;

                        i++;
                    }
                }
            }
        }

        public static int PutSchemasCache(this IEnumerable<SchemaImport> schemaImports, XmlNamespaceManager xmlNamespaceManager, String contextNamespace)
        {
            int imported = 0;

            if (schemaImports != null)
            {
                List<ValidationEventArgs> validationResults = new List<ValidationEventArgs>();

                ValidationEventHandler schemaReaderValidator = delegate (Object sender, ValidationEventArgs e) { validationResults.Add(e); };

                foreach (SchemaImport schemaImport in schemaImports)
                {
                    validationResults.Clear();

                    XmlSchema schema;

                    if (schemaImport.HasLocation)
                    {
                        Stream importStream;
                        String importMediaType;
                        String importMediaTypeVariant;
                        Encoding importEncoding;

                        schemaImport.AbsoluteUri.DownloadResourceAndGetResourceMediaType(
                            schemaImport.ImportsCachePathUri,
                            out importStream,
                            out importMediaType,
                            out importMediaTypeVariant,
                            out importEncoding);

                        using (importStream)
                        {
                            schema = XmlSchema.Read(importStream, schemaReaderValidator);
                        }
                    }
                    else
                    {
                        schema = new XmlSchema();

                        schema.TargetNamespace = schemaImport.Namespace;
                    }

                    if (validationResults.Count > 0)

                        throw validationResults.First<ValidationEventArgs>().Exception;

                    imported += schema.PutSchemasCache(xmlNamespaceManager, schemaImport.BaseLocation, contextNamespace);
                }
            }

            return imported;
        }

        public static int PutSchemasCache(this IEnumerable<SchemaInclude> schemaIncludes, XmlNamespaceManager xmlNamespaceManager, String contextNamespace)
        {
            int included = 0;

            if (schemaIncludes != null)
            {
                List<ValidationEventArgs> validationResults = new List<ValidationEventArgs>();

                ValidationEventHandler schemaReaderValidator = delegate (Object sender, ValidationEventArgs e) { validationResults.Add(e); };

                foreach (SchemaInclude schemaInclude in schemaIncludes.Where(i => i.AbsoluteUri != null))
                {
                    validationResults.Clear();

                    Stream includeStream;
                    String includeMediaType;
                    String includeMediaTypeVariant;
                    Encoding includeEncoding;

                    schemaInclude.AbsoluteUri.DownloadResourceAndGetResourceMediaType(
                        schemaInclude.ImportsCachePathUri,
                        out includeStream,
                        out includeMediaType,
                        out includeMediaTypeVariant,
                        out includeEncoding);

                    XmlSchema schema;

                    using (includeStream)
                    {
                        schema = XmlSchema.Read(includeStream, schemaReaderValidator);
                    }

                    if (validationResults.Count > 0)

                        throw validationResults.First<ValidationEventArgs>().Exception;

                    included += schema.PutSchemasCache(xmlNamespaceManager, schemaInclude.AbsoluteUri.AbsoluteUri, contextNamespace);
                }
            }

            return included;
        }

        public static int PutSchemasCache(this IEnumerable<XmlElement> schemaInlineElements, XmlNamespaceManager xmlNamespaceManager, String schemaBaseLocation, String contextNamespace)
        {
            List<ValidationEventArgs> validationResults = new List<ValidationEventArgs>();

            ValidationEventHandler schemaReaderValidator = delegate (Object sender, ValidationEventArgs e) { validationResults.Add(e); };

            int inlined = 0;

            if (schemaInlineElements != null)
            {
                foreach (XmlElement inlineSchemaElement in schemaInlineElements)
                {
                    if (inlineSchemaElement.LocalName == "schema" && inlineSchemaElement.NamespaceURI == XmlSchema.Namespace)
                    {
                        validationResults.Clear();

                        XmlSchema schema;

                        using (XmlReader xmlReader = XmlReader.Create(new StringReader(inlineSchemaElement.OuterXml)))
                        {
                            schema = XmlSchema.Read(xmlReader, schemaReaderValidator);
                        }

                        if (validationResults.Count > 0)

                            throw validationResults.First<ValidationEventArgs>().Exception;

                        inlined += schema.PutSchemasCache(xmlNamespaceManager, schemaBaseLocation, contextNamespace);
                    }
                    else

                        return 0;
                }
            }

            return inlined;
        }

        public static int PutSchemasCache(this StringReader schemaStringReader, XmlNamespaceManager xmlNamespaceManager, String schemaBaseLocation, String contextNamespace, out String schemaTargetNamespace)
        {
            XmlSchema schema = null;

            using (XmlReader schemaReader = XmlReader.Create(schemaStringReader))
            {
                if (schemaReader != null)
                {
                    if (schemaReader.ReadState == ReadState.Initial)

                        schemaStringReader.Read();

                    while (schemaReader.NodeType != XmlNodeType.Element && schemaReader.Read())
                    {
                    }

                    if (schemaReader.LocalName == "schema" && schemaReader.NamespaceURI == XmlSchema.Namespace)
                    {
                        List<ValidationEventArgs> validationResults = new List<ValidationEventArgs>();

                        ValidationEventHandler schemaReaderValidator = delegate (Object sender, ValidationEventArgs e) { validationResults.Add(e); };

                        schema = XmlSchema.Read(schemaStringReader, schemaReaderValidator);

                        if (validationResults.Count > 0)

                            throw validationResults.First<ValidationEventArgs>().Exception;
                    }
                    else
                    {
                        schemaTargetNamespace = null;

                        return 0;
                    }
                }
            }

            schemaTargetNamespace = schema.TargetNamespace;

            return schema.PutSchemasCache(xmlNamespaceManager, schemaBaseLocation, contextNamespace);
        }

        public static int PutSchemasCache(this StreamReader schemaStreamReader, XmlNamespaceManager xmlNamespaceManager, String schemaBaseLocation, String contextNamespace, out String schemaTargetNamespace)
        {
            XmlSchema schema = null;

            using (XmlReader schemaReader = XmlReader.Create(schemaStreamReader))
            {
                if (schemaReader != null)
                {
                    if (schemaReader.ReadState == ReadState.Initial)

                        schemaStreamReader.Read();

                    while (schemaReader.NodeType != XmlNodeType.Element && schemaReader.Read())
                    {
                    }

                    if (schemaReader.LocalName == "schema" && schemaReader.NamespaceURI == XmlSchema.Namespace)
                    {
                        List<ValidationEventArgs> validationResults = new List<ValidationEventArgs>();

                        ValidationEventHandler schemaReaderValidator = delegate (Object sender, ValidationEventArgs e) { validationResults.Add(e); };

                        schema = XmlSchema.Read(schemaReader, schemaReaderValidator);

                        if (validationResults.Count > 0)

                            throw validationResults.First<ValidationEventArgs>().Exception;
                    }
                    else
                    {
                        schemaTargetNamespace = null;

                        return 0;
                    }
                }
            }

            if (String.IsNullOrWhiteSpace(schema.TargetNamespace))

                schema.TargetNamespace = schemaBaseLocation;

            schemaTargetNamespace = schema.TargetNamespace;

            return schema.PutSchemasCache(xmlNamespaceManager, schemaBaseLocation, contextNamespace);
        }

        private static int PutSchemasCache(this XmlSchema schema, XmlNamespaceManager xmlNamespaceManager, String schemaBaseLocation, String contextNamespace)
        {
            XmlSchemas schemas;

            if (!schemasCache.ContainsKey(contextNamespace))

                new XmlSchemas().SetSchemasCache(xmlNamespaceManager, schemaBaseLocation, contextNamespace);

            schemas = schemasCache[contextNamespace];

            int schemasTotal = 0;

            if (schema != null)
            {
                schemas.Insert(0, schema);

                schemasTotal++;

                schemasTotal += schema.Includes.OfType<XmlSchemaImport>().Select(import => new SchemaImport(import.Namespace, import.SchemaLocation, schemaBaseLocation)).PutSchemasCache(xmlNamespaceManager, contextNamespace);

                schemasTotal += schema.Includes.OfType<XmlSchemaInclude>().Select(include => new SchemaInclude(schemaBaseLocation, include.SchemaLocation, include.UnhandledAttributes, include.Annotation.GetSchemaDocs())).PutSchemasCache(xmlNamespaceManager, contextNamespace);
            }

            return schemasTotal;
        }

        public static int PutSchemasCache(this XmlSchemaElement schemaElement, XmlNamespaceManager xmlNamespaceManager, String schemaBaseLocation, String contextNamespace)
        {
            XmlSchemas schemas;

            if (!schemasCache.ContainsKey(contextNamespace))

                new XmlSchemas().SetSchemasCache(xmlNamespaceManager, schemaBaseLocation, contextNamespace);

            schemas = schemasCache[contextNamespace];

            int added = 0;

            if (schemaElement != null)
            {
                XmlSchema schema;

                if (schemaElement.SchemaType == null)

                    schema = schemas[schemaElement.SchemaTypeName.Namespace];

                else

                    schema = schemas.FirstOrDefault(s => s.Items.Contains(schemaElement.SchemaType));

                if (schema != null)
                {
                    if (!schema.Items.OfType<XmlSchemaElement>().Any(e => e.Name == schemaElement.Name))
                    {
                        schema.Items.Add(schemaElement);

                        added++;
                    }
                }
            }

            return added;
        }

        public static XmlSchemaElement GetElement(this XmlQualifiedName elementName, String contextNamespace)
        {
            XmlSchemas schemas = schemasCache[contextNamespace];

            if (schemas != null)
            {
                XmlSchema schema = schemas[elementName.Namespace];

                if (schema == null)

                    throw new InvalidOperationException(String.Format("Schema for namespace '{0}' not found.", elementName.Namespace));

                else
                {
                    XmlSchemaObject schemaObject = schema.Elements[elementName];

                    if (schemaObject != null)
                    {
                        XmlSchemaElement schemaElement = (XmlSchemaElement)schemaObject;

                        if (schemaElement != null)
                        {
                            return schemaElement;
                        }
                    }
                }
            }

            return null;
        }

        public static XmlSchemaElement Clone(this XmlSchemaElement schemaElement, XmlDocument document)
        {
            if (schemaElement == null)

                return null;

            return new XmlSchemaElement()
            {
                SchemaType = schemaElement.SchemaType,
                RefName = schemaElement.RefName,
                Name = schemaElement.Name,
                IsNillable = schemaElement.IsNillable,
                //IsAbstract = schemaElement.IsAbstract,
                Form = schemaElement.Form,
                SchemaTypeName = schemaElement.SchemaTypeName,
                FixedValue = schemaElement.FixedValue,
                Final = schemaElement.Final,
                DefaultValue = schemaElement.DefaultValue,
                SubstitutionGroup = schemaElement.SubstitutionGroup,
                MaxOccurs = schemaElement.MaxOccurs,
                MaxOccursString = schemaElement.MaxOccursString,
                MinOccurs = schemaElement.MinOccurs,
                MinOccursString = schemaElement.MinOccursString,
                Annotation = schemaElement.Annotation.Clone(document),
                Id = schemaElement.Id,
                UnhandledAttributes = schemaElement.UnhandledAttributes.CloneAttributes(document),
                LineNumber = schemaElement.LineNumber,
                LinePosition = schemaElement.LinePosition,
                Namespaces = schemaElement.Namespaces.Clone(),
                Parent = schemaElement.Parent,
                SourceUri = schemaElement.SourceUri,
            };
        }

        public static XmlSchemaAnnotation Clone(this XmlSchemaAnnotation schemaAnnotation, XmlDocument document)
        {
            if (schemaAnnotation == null)

                return null;

            XmlSchemaAnnotation clone = new XmlSchemaAnnotation()
            {
                Id = schemaAnnotation.Id,
                UnhandledAttributes = schemaAnnotation.UnhandledAttributes.CloneAttributes(document),
                LineNumber = schemaAnnotation.LineNumber,
                LinePosition = schemaAnnotation.LinePosition,
                Namespaces = schemaAnnotation.Namespaces.Clone(),
                Parent = schemaAnnotation.Parent,
                SourceUri = schemaAnnotation.SourceUri,
            };

            schemaAnnotation.Items.Cast<XmlSchemaObject>().Aggregate(clone.Items, (i, o) =>
            {
                if (o is XmlSchemaAppInfo)

                    i.Add((o as XmlSchemaAppInfo).Clone(document));

                if (o is XmlSchemaDocumentation)

                    i.Add((o as XmlSchemaDocumentation).Clone(document));

                return i;
            });

            return clone;
        }

        public static XmlSchemaAppInfo Clone(this XmlSchemaAppInfo schemaAnnotation, XmlDocument document)
        {
            return new XmlSchemaAppInfo()
            {
                Markup = schemaAnnotation.Markup.CloneNodes(document),
                Source = schemaAnnotation.Source,
                LineNumber = schemaAnnotation.LineNumber,
                LinePosition = schemaAnnotation.LinePosition,
                Namespaces = schemaAnnotation.Namespaces.Clone(),
                Parent = schemaAnnotation.Parent,
                SourceUri = schemaAnnotation.SourceUri,
            };
        }

        public static XmlSchemaDocumentation Clone(this XmlSchemaDocumentation schemaDocumenation, XmlDocument document)
        {
            return new XmlSchemaDocumentation()
            {
                Language = schemaDocumenation.Language,
                Markup = schemaDocumenation.Markup.CloneNodes(document),
                Source = schemaDocumenation.Source,
                LineNumber = schemaDocumenation.LineNumber,
                LinePosition = schemaDocumenation.LinePosition,
                Namespaces = schemaDocumenation.Namespaces,
                Parent = schemaDocumenation.Parent,
                SourceUri = schemaDocumenation.SourceUri,
            };
        }

        public static XmlSerializerNamespaces Clone(this XmlSerializerNamespaces serializerNamespaces)
        {
            return new XmlSerializerNamespaces(serializerNamespaces.ToArray());
        }

        public static XmlSchemaType GetElementType(this XmlQualifiedName elementName, String contextNamespace)
        {
            XmlSchemaType schemaType = null;

            XmlSchemaElement schemaElement = elementName.GetElement(contextNamespace);

            if (schemaElement != null)
            {
                schemaType = schemaElement.ElementSchemaType;

                if (schemaType == null)

                    schemaType = schemaElement.SchemaType;

                if (schemaType == null)

                    schemaType = schemaElement.SchemaTypeName.GetSchemaType(contextNamespace);
            }

            return schemaType;
        }

        public static XmlSchemaType GetSchemaType(this XmlQualifiedName typeName, String contextNamespace)
        {
            XmlSchemaType schemaType = null;

            if (typeName.Namespace == XmlSchema.Namespace)
            {
                schemaType = XmlSchemaType.GetBuiltInSimpleType(typeName);

                if (schemaType == null)

                    schemaType = XmlSchemaType.GetBuiltInComplexType(typeName);
            }
            else
            {
                XmlSchemas schemas = schemasCache[contextNamespace];

                if (schemas != null)
                {
                    XmlSchema schema = schemas[typeName.Namespace];

                    if (schema != null)
                    {
                        XmlSchemaObject schemaObject = schema.SchemaTypes[typeName];

                        if (schemaObject != null)

                            schemaType = (XmlSchemaType)schemaObject;
                    }
                }
            }

            return schemaType;
        }

        public static XmlQualifiedName GetBuiltInTypeName(this XmlQualifiedName name, String contextNamespace)
        {
            XmlSchemaType schemaType = GetSchemaType(name, contextNamespace);

            while (schemaType != null && schemaType.QualifiedName.Namespace != XmlSchema.Namespace)
            {
                schemaType = schemaType.BaseXmlSchemaType;
            }

            if (schemaType == null)

                return null;

            return schemaType.QualifiedName;
        }

        public static bool TryAddItem(this XmlSchema schema, XmlSchemaObject xmlSchemaObject)
        {
            if (schema != null && xmlSchemaObject != null)

                return schema.Items.Add(xmlSchemaObject) >= 0;

            return false;
        }

        public static bool TryGetElement(this XmlSchema schema, XmlQualifiedName elementName, out XmlSchemaElement schemaElement)
        {
            if (schema != null && elementName != null && schema.TargetNamespace == elementName.Namespace)

                schemaElement = schema.Items.OfType<XmlSchemaElement>().FirstOrDefault(o => o.Name == elementName.Name);

            else

                schemaElement = null;

            return schemaElement != null;
        }

        public static bool TryGetSimpleType(this XmlSchema schema, XmlQualifiedName partTypeName, out XmlSchemaSimpleType schemaSimpleType)
        {
            if (schema != null && partTypeName != null && schema.TargetNamespace == partTypeName.Namespace)

                schemaSimpleType = schema.Items.OfType<XmlSchemaSimpleType>().FirstOrDefault(o => o.Name == partTypeName.Name);

            else

                schemaSimpleType = null;

            return schemaSimpleType != null;
        }

        public static bool TryGetComplexType(this XmlSchema schema, XmlQualifiedName partTypeName, out XmlSchemaComplexType schemaComplexType)
        {
            if (schema != null && partTypeName != null && schema.TargetNamespace == partTypeName.Namespace)

                schemaComplexType = schema.Items.OfType<XmlSchemaComplexType>().FirstOrDefault(o => o.Name == partTypeName.Name);

            else

                schemaComplexType = null;

            return schemaComplexType != null;
        }

        public static IEnumerable<SchemaDoc> GetSchemaDocs(this XmlSchemaAnnotation schemaAnnotation)
        {
            IEnumerable<XmlSchemaObject> schemaObjects = schemaAnnotation.Items.Cast<XmlSchemaObject>();

            List<SchemaDoc> schemaDocs = new List<SchemaDoc>();

            // ToDo: ...

            foreach (XmlSchemaDocumentation docItem in schemaObjects.OfType<XmlSchemaDocumentation>())
            {
            }

            foreach (XmlSchemaAppInfo appInfo in schemaObjects.OfType<XmlSchemaAppInfo>())
            {
            }

            return schemaDocs;
        }
    }
}
