using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.MarkupPrimitives
{
    using AllVerge.SystemPrimitives.Net.Mime;

    /// <summary>
    /// Semi-structured data formatting extensions.  
    /// </summary>
    public static class FormatExtensions
    {
        /// <summary>
        /// Attempts to get the <see cref="Formats"/> correcsponding to the <paramref name="mediaType"/>.
        /// </summary>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        public static Formats? GetFormat(String mediaType)
        {
            if (MediaTypes.TryGetNormalizedResourceMediaType(mediaType, out String normalizedMediaType))
            {
                switch (normalizedMediaType)
                {
                    case MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE:
                        return Formats.JSON;
                    case MediaTypeConstants.APPLICATION_XML_MEDIA_TYPE:
                        return Formats.XML;
                    case MediaTypeConstants.APPLICATION_YAML_MEDIA_TYPE:
                    case MediaTypeConstants.APPLICATION_RAML_PLUS_YAML_MEDIA_TYPE:
                        return Formats.YAML;
                }
            }

            return null;
        }

        /// <summary>
        /// Converts an Xml graph to JSON.
        /// </summary>
        /// <param name="element">The <see cref="System.Xml.XmlElement"/> to be transformed using JSON.</param>
        /// <param name="showNodeName">Controls whether to render the root node.</param>
        /// <param name="arrayNames">A list of element names that should be rendered as an array.</param>
        /// <returns></returns>
        public static string ToJSON(this XmlElement element, bool showNodeName, params string[] arrayNames)
        {
            return element.XmlToJSON(showNodeName, arrayNames);
        }

        /// <summary>
        /// Converts an Xml graph to a JSON stream.
        /// </summary>
        /// <param name="element">The <see cref="System.Xml.XmlElement"/> to be transformed using JSON.</param>
        /// <param name="showNodeName">Controls whether to render the root node.</param>
        /// <param name="arrayNames">A list of element names that should be rendered as an array.</param>
        /// <returns></returns>
        public static Stream ToJSONStream(this XmlElement element, bool showNodeName, params string[] arrayNames)
        {
            return new MemoryStream(UTF8Encoding.UTF8.GetBytes(element.XmlToJSON(showNodeName, arrayNames).ToCharArray()));
        }

        /// <summary>
        /// Converts a JSON stream to an Xml graph.
        /// </summary>
        /// <param name="stream">The JSON stream.</param>
        /// <returns></returns>
        public static XmlElement FromJSONStream(this Stream stream)
        {
            return stream.JSONToXml();
        }

        /// <summary>
        /// Converts an Xml graph to Yaml.
        /// </summary>
        /// <param name="element">The <see cref="System.Xml.XmlElement"/> to be transformed using JSON.</param>
        /// <param name="showNodeName">Controls whether to render the root node.</param>
        /// <param name="arrayNames">A list of element names that should be rendered as an array.</param>
        /// <returns></returns>
        public static string ToYAML(this XmlElement element, bool showNodeName, params string[] arrayNames)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts an Xml graph to a YAML stream.
        /// </summary>
        /// <param name="element">The <see cref="System.Xml.XmlElement"/> to be transformed using JSON.</param>
        /// <param name="showNodeName">Controls whether to render the root node.</param>
        /// <param name="arrayNames">A list of element names that should be rendered as an array.</param>
        /// <returns></returns>
        public static Stream ToYAMLStream(this XmlElement element, bool showNodeName, params string[] arrayNames)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts a YAML stream to an Xml graph.
        /// </summary>
        /// <param name="stream">The YAML stream.</param>
        /// <returns></returns>
        public static XmlElement FromYAMLStream(this Stream stream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts an Xml Graph to Json.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="showNodeName"></param>
        /// <param name="arrayNames"></param>
        /// <seealso cref="!:http://www.phdcc.com/xml2json.htm"/>
        /// <returns></returns>
        private static string XmlToJSON(this XmlElement element, bool showNodeName, params string[] arrayNames)
        {
            StringBuilder sbJSON = new StringBuilder();
            sbJSON.Append("{ ");
            XmlToJSONNode(sbJSON, element, showNodeName, arrayNames);
            sbJSON.Append("}");
            return sbJSON.ToString();
        }

        //  XmlToJSONnode:  Output an XmlElement, possibly as part of a higher array
        private static void XmlToJSONNode(StringBuilder sbJSON, XmlElement node, bool showNodeName, params string[] arrayNames)
        {
            bool childAdded = false;
            if (showNodeName)
                sbJSON.Append("\"" + SafeJSON(node.Name) + "\": ");
            sbJSON.Append("{");
            // Build a sorted list of key-value pairs
            //  where   key is case-sensitive nodeName
            //          value is an ArrayList of string or XmlElement
            //  so that we know whether the nodeName is an array or not.
            SortedList childNodeNames = new SortedList();

            //  Add in all node attributes
            if (node.Attributes != null)
                foreach (XmlAttribute attr in node.Attributes)
                {
                    switch (attr.NamespaceURI)
                    {
                        case "http://www.w3.org/2000/xmlns/":
                        //break;
                        default:
                            StoreChildNode(childNodeNames, "@" + attr.Name, attr.InnerText);
                            break;
                    }
                }

            //  Add in all nodes
            foreach (XmlNode cnode in node.ChildNodes)
            {
                childAdded = true;
                if (cnode.ChildNodes[0] is XmlCDataSection)
                    StoreChildNode(childNodeNames, cnode.Name, cnode.ChildNodes[0].InnerText);
                else if (cnode is XmlText)
                    StoreChildNode(childNodeNames, "value", cnode.InnerText);
                else if (cnode is XmlElement)
                    StoreChildNode(childNodeNames, cnode.Name, cnode);
            }

            // Now output all stored info
            foreach (string childname in childNodeNames.Keys)
            {
                childAdded = true;
                String arrayName;
                ArrayList alChild = (ArrayList)childNodeNames[childname];
                if (IsArray(node.LocalName, childname, alChild, arrayNames))
                {
                    sbJSON.Append(" \"" + SafeJSON(childname) + "\": [ ");
                    foreach (object Child in alChild)
                        OutputNode(childname, Child, sbJSON, false, arrayNames);
                    sbJSON.Remove(sbJSON.Length - 2, 2);
                    sbJSON.Append(" ], ");
                }
                else if (IsEmptyChildArray(childname, alChild, arrayNames, out arrayName))
                {
                    sbJSON.Append(" \"" + SafeJSON(childname) + "\": {");
                    sbJSON.Append(" \"" + SafeJSON(arrayName) + "\": [");
                    sbJSON.Append("] }, ");
                }
                else
                    OutputNode(childname, alChild[0], sbJSON, true, arrayNames);
            }
            sbJSON.Remove(sbJSON.Length - 2, 2);
            if (childAdded)
            {
                sbJSON.Append(" }");
            }
            else
            {
                sbJSON.Append(" null");
            }
        }

        private static bool IsArray(string parentName, string childname, ArrayList alChild, string[] arrayNames)
        {
            if (arrayNames.Length > 0)

                return arrayNames.Contains(childname) || arrayNames.Contains(parentName + "/" + childname);

            else

                return alChild.Count > 1;
        }

        private static bool IsEmptyChildArray(string childname, ArrayList alChild, string[] arrayNames, out String arrayName)
        {
            arrayName = null;

            if (arrayNames.Length > 0)
            {
                String arrayNamePath = arrayNames.FirstOrDefault(n => n.StartsWith(childname + '/') && alChild.Count == 1 && alChild[0] == null);

                if (arrayNamePath != null)

                    arrayName = arrayNamePath.Split('/')[1];
            }

            return arrayName != null;
        }

        //  StoreChildNode: Store data associated with each nodeName
        //                  so that we know whether the nodeName is an array or not.
        private static void StoreChildNode(SortedList childNodeNames, string nodeName, object nodeValue)
        {
            // Pre-process contraction of XmlElement-s
            if (nodeValue is XmlElement)
            {
                // Convert  <aa></aa> into "aa":null
                //          <aa>xx</aa> into "aa":"xx"
                XmlNode cnode = (XmlNode)nodeValue;
                if (cnode.Attributes.Count == 0)
                {
                    XmlNodeList children = cnode.ChildNodes;
                    if (children.Count == 0)
                        nodeValue = null;
                    else if (children.Count == 1 && (children[0] is XmlText))
                        nodeValue = ((XmlText)(children[0])).InnerText;
                }
            }
            // Add nodeValue to ArrayList associated with each nodeName
            // If nodeName doesn't exist then add it
            object oValuesAL = childNodeNames[nodeName];
            ArrayList ValuesAL;
            if (oValuesAL == null)
            {
                ValuesAL = new ArrayList();
                childNodeNames[nodeName] = ValuesAL;
            }
            else
                ValuesAL = (ArrayList)oValuesAL;
            ValuesAL.Add(nodeValue);
        }

        private static void OutputNode(string childname, object alChild, StringBuilder sbJSON, bool showNodeName, string[] arrayNames)
        {
            if (alChild == null)
            {
                if (showNodeName)
                    sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
                sbJSON.Append("null");
            }
            else if (alChild is string)
            {
                if (showNodeName)
                    sbJSON.Append("\"" + SafeJSON(childname) + "\": ");
                string sChild = (string)alChild;
                sChild = sChild.Trim();
                Double temp;
                if (Double.TryParse(sChild, out temp))
                    sbJSON.Append(SafeJSON(sChild));
                else
                    sbJSON.Append("\"" + SafeJSON(sChild) + "\"");
            }
            else
                XmlToJSONNode(sbJSON, (XmlElement)alChild, showNodeName, arrayNames);
            sbJSON.Append(", ");
        }

        // Make a string safe for JSON
        private static string SafeJSON(string name)
        {
            StringBuilder sb = new StringBuilder(name.Length);
            foreach (char @char in name)
            {
                if (Char.IsControl(@char) || @char == '\'')
                {
                    int charInt = (int)@char;
                    sb.Append(@"\u" + charInt.ToString("x4"));
                    continue;
                }
                else if (@char == '\"' || @char == '\\' || @char == '/')
                {
                    sb.Append('\\');
                }
                sb.Append(@char);
            }
            return sb.ToString();
        }

        private static XmlElement JSONToXml(this Stream stream)
        {
            XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(stream, Encoding.UTF8, XmlDictionaryReaderQuotas.Max, null);

            if (reader.ReadState != ReadState.Interactive && !reader.Read())
            {
                return null;
            }

            XmlDocument doc = new XmlDocument();

            JSONToXmlNode(reader, doc);

            return doc.DocumentElement;
        }

        private static void JSONToXmlNode(XmlDictionaryReader reader, XmlDocument doc)
        {
            XmlNode root;
            while ((root = ReadJsonNode(reader, doc, false)) != null)
            {
                doc.AppendChild(root);
                if (!reader.Read())
                {
                    return;
                }
            }
        }

        private static XmlNode ReadJsonNode(XmlReader reader, XmlDocument doc, bool preserveWhitespace)
        {
            List<XmlAttribute> prefixedNodes = new List<XmlAttribute>();
            Stack<XmlNode> arrayNodes = new Stack<XmlNode>();
            XmlNode currentArrayNode = null;
            NodeContext currentContext = null;
            while (true)
            {
                XmlNode childNode = null;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool isEmptyElement = reader.IsEmptyElement;
                        XmlElement xmlElement = doc.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                        xmlElement.IsEmpty = isEmptyElement;
                        if (reader.MoveToFirstAttribute())
                        {
                            XmlAttributeCollection attributes = xmlElement.Attributes;
                            do
                            {
                                XmlAttribute node = LoadAttributeNode(reader, doc);
                                if (node.Name == "type")
                                {
                                    if (node.Value == "array")
                                    {
                                        if (currentArrayNode != null)
                                            arrayNodes.Push(currentArrayNode);
                                        currentArrayNode = xmlElement;
                                    }
                                }
                                else
                                    attributes.Append(node);
                            }
                            while (reader.MoveToNextAttribute());
                            reader.MoveToElement();
                        }
                        if (!isEmptyElement)
                        {
                            XmlNode referenceNode = xmlElement;
                            XmlNode currentNode = referenceNode;
                            if (currentContext != null)
                            {
                                if (xmlElement.Name == "item" && currentArrayNode != null)
                                {
                                    currentNode = currentArrayNode.ParentNode.AppendChild(currentArrayNode.Clone());
                                }
                                else if (xmlElement.NamespaceURI == "item" && xmlElement.LocalName == "item" && currentArrayNode != null)
                                {
                                    currentNode = currentArrayNode.ParentNode.AppendChild(currentArrayNode.Clone());
                                }
                                else if (xmlElement.HasAttribute("xmlns:" + xmlElement.Prefix))
                                {
                                    string attrValue = xmlElement.GetAttribute("xmlns:" + xmlElement.Prefix);
                                    if (xmlElement.HasAttribute(attrValue))
                                    {
                                        string targetAttr = xmlElement.GetAttribute(attrValue);
                                        if (targetAttr.StartsWith("@"))
                                            targetAttr = targetAttr.Substring(1);
                                        currentNode = currentContext.AppendChild(doc.CreateAttribute(targetAttr));
                                        prefixedNodes.Add((XmlAttribute)currentNode);
                                    }
                                    else
                                        currentContext.AppendChild(xmlElement);
                                }
                                else
                                    currentContext.AppendChild(xmlElement);
                            }
                            currentContext = new NodeContext(currentContext, currentNode, referenceNode);
                        }
                        else
                        {
                            childNode = xmlElement;
                        }
                        break;
                    case XmlNodeType.Attribute:
                        childNode = LoadAttributeNode(reader, doc);
                        break;
                    case XmlNodeType.Text:
                        childNode = doc.CreateTextNode(reader.Value);
                        break;
                    case XmlNodeType.CDATA:
                        childNode = doc.CreateCDataSection(reader.Value);
                        break;
                    case XmlNodeType.EntityReference:
                        childNode = ResolveEntityReference(reader, doc, false);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        childNode = doc.CreateProcessingInstruction(reader.Name, reader.Value);
                        break;
                    case XmlNodeType.Comment:
                        childNode = doc.CreateComment(reader.Value);
                        break;
                    case XmlNodeType.Whitespace:
                        if (preserveWhitespace)
                            childNode = doc.CreateWhitespace(reader.Value);
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        childNode = doc.CreateSignificantWhitespace(reader.Value);
                        break;
                    case XmlNodeType.EndElement:
                        if (currentContext != null)
                        {
                            if (currentArrayNode != null && reader.Name == currentArrayNode.ParentNode.Name)
                            {
                                currentArrayNode.ParentNode.RemoveChild(currentArrayNode);
                                if (arrayNodes.Count > 0)
                                    currentArrayNode = arrayNodes.Pop();
                                else
                                    currentArrayNode = null;
                            }
                            if (currentContext.IsReferenceName(reader.Name) && !currentContext.IsParentName("root"))
                            {
                                currentContext = currentContext.Parent;
                            }
                        }
                        break;
                    case XmlNodeType.EndEntity:
                        break;
                    case XmlNodeType.DocumentType:
                        throw new ArgumentException("UnexpectedNodeType", reader.NodeType.ToString());
                    case XmlNodeType.XmlDeclaration:
                        throw new ArgumentException("UnexpectedNodeType", reader.NodeType.ToString());
                }
                if (currentContext != null && childNode != null)
                {
                    currentContext.AppendChild(childNode);
                }
                if (!reader.Read())
                {
                    break;
                }
                if (currentContext == null)
                    break;
            }
            if (currentContext != null)
            {
                while (!currentContext.IsParentName("root"))
                {
                    currentContext = currentContext.Parent;
                }
            }
            XmlNode rootNode = currentContext.GetNodeAsOrphan();
            FixUpPrefixNodes(prefixedNodes, rootNode);
            return rootNode;
        }

        private static XmlNode ResolveEntityReference(XmlReader reader, XmlDocument doc, bool direct)
        {
            throw new NotImplementedException();
        }

        private static XmlAttribute LoadAttributeNode(XmlReader reader, XmlDocument doc)
        {
            if (reader.IsDefault)
            {
                return LoadDefaultAttribute(reader, doc);
            }
            XmlAttribute xmlAttribute = doc.CreateAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
            while (reader.ReadAttributeValue())
            {
                XmlNodeType nodeType = reader.NodeType;
                XmlNode xmlNode;
                if (nodeType != XmlNodeType.Text)
                {
                    if (nodeType != XmlNodeType.EntityReference)
                    {
                        throw new ArgumentException("UnexpectedNodeType", reader.NodeType.ToString());
                    }
                    xmlNode = ResolveEntityReference(reader, doc, false);
                }
                else
                {
                    xmlNode = doc.CreateTextNode(reader.Value);
                }
                xmlAttribute.AppendChild(xmlNode);
            }
            return xmlAttribute;
        }

        private static XmlAttribute LoadDefaultAttribute(XmlReader reader, XmlDocument doc)
        {
            throw new NotImplementedException("LoadDefaultAttribute");
        }

        private static void FixUpPrefixNodes(List<XmlAttribute> prefixedNodes, XmlNode rootNode)
        {
            Dictionary<String, String> nsMap =
                rootNode.Attributes.Cast<XmlAttribute>().Where(a => a.Prefix == "xmlns").Aggregate(new Dictionary<String, String>(), (a, b) => { a.Add(b.LocalName, b.Value); return a; });

            foreach (XmlAttribute a1 in prefixedNodes)
            {
                if (nsMap.ContainsKey(a1.Prefix))
                {
                    String ns = nsMap[a1.Prefix];
                    XmlAttribute a2 = a1.OwnerElement.Attributes.Append(a1.OwnerDocument.CreateAttribute(a1.Prefix, a1.LocalName, ns));
                    a1.OwnerElement.Attributes.Remove(a1);
                    a2.Value = a1.Value;
                }
            }
        }

        private class NodeContext
        {
            NodeContext parent;
            XmlNode currentNode;
            XmlNode referenceNode;

            public NodeContext(NodeContext parent, XmlNode currentNode, XmlNode referenceNode)
            {
                this.parent = parent;
                this.currentNode = currentNode;
                this.referenceNode = referenceNode;
            }

            internal NodeContext Parent
            {
                get
                {
                    return parent;
                }
            }

            internal XmlNode GetNodeAsOrphan()
            {
                if (this.currentNode.ParentNode == null)

                    return this.currentNode;

                else

                    return this.currentNode.ParentNode.RemoveChild(this.currentNode);
            }

            public XmlNode AppendChild(XmlNode childNode)
            {
                return this.currentNode.AppendChild(childNode);
            }

            public XmlNode AppendChild(XmlAttribute childNode)
            {
                return this.currentNode.Attributes.Append(childNode);
            }

            internal bool IsReferenceName(string name)
            {
                return this.referenceNode.Name == name;
            }

            internal bool IsParentName(string name)
            {
                return this.currentNode.ParentNode != null && this.currentNode.ParentNode.Name == name;
            }
        }
    }
}
