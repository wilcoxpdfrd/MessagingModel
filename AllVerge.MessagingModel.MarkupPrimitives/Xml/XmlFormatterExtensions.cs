using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Resolvers;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MarkupPrimitives.Xml
{
    /// <summary>
    /// Provides formatting functions for use with Xml resources.
    /// </summary>
    public static class XmlFormatterExtensions
    {
        /// <summary>
        /// FAILED_REASON_NOT_XML
        /// </summary>
        public const string FAILED_REASON_NOT_XML = "Input does not appear to contain Xml.";
        /// <summary>
        /// FAILED_REASON_XML_DECLARATION_OR_ELEMENT_NOT_FOUND
        /// </summary>
        public const string FAILED_REASON_XML_DECLARATION_OR_ELEMENT_NOT_FOUND = "Input does not start with xml declaration, document element with system attribute specifying XHTML, or element.";

        /// <summary>
        /// Determines whether the stream contains valid Xml.
        /// </summary>
        /// <param name="stream">A <see cref="System.IO.Stream"/> to check.</param>
        /// <param name="exception">An exception associated with a false return value.  Otherwise null.</param>
        /// <returns>A <see cref="System.Boolean"/> indicating the result of the check.</returns>
        public static bool IsXml(this Stream stream, out Exception exception)
        {
            XmlReader reader = null;
            exception = null;

            try
            {
                reader = XmlReader.Create(stream, new XmlReaderSettings()
                {
                    IgnoreWhitespace = true, 
                    DtdProcessing = DtdProcessing.Parse, 
                    XmlResolver = new XmlPreloadedResolver(), 
                    ConformanceLevel = ConformanceLevel.Auto
                });

                if (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.XmlDeclaration:
                            break;
                        case XmlNodeType.DocumentType:
                            if (!reader.MoveToAttribute("SYSTEM") && 
                                (!reader.Value.Equals("http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", StringComparison.InvariantCultureIgnoreCase) ||
                                !reader.Value.Equals("http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd", StringComparison.InvariantCultureIgnoreCase) ||
                                !reader.Value.Equals("http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd", StringComparison.InvariantCultureIgnoreCase)))
                                exception = new XmlException(FAILED_REASON_XML_DECLARATION_OR_ELEMENT_NOT_FOUND);
                            break;
                        case XmlNodeType.Element:
                            exception = new XmlException(FAILED_REASON_XML_DECLARATION_OR_ELEMENT_NOT_FOUND);
                            break;
                        default:
                            exception = new XmlException(FAILED_REASON_NOT_XML);
                            break;
                    }
                }
                else
                    exception = new XmlException(FAILED_REASON_NOT_XML);
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            stream.Position = 0;

            return exception == null;
        }

        /// <summary>
        /// Determines whether the string contains valid Xml.
        /// </summary>
        /// <param name="string">A <see cref="System.String"/> to check.</param>
        /// <param name="exception">An exception associated with a false return value.  Otherwise null.</param>
        /// <returns>A <see cref="System.Boolean"/> indicating the result of the check.</returns>
        public static bool IsXml(this string @string, out Exception exception)
        {
            StringReader sr = null;
            XmlReader reader = null;
            exception = null;

            try
            {
                sr = new StringReader(@string);

                reader = XmlReader.Create(sr, new XmlReaderSettings()
                {
                    IgnoreWhitespace = true,
                    DtdProcessing = DtdProcessing.Parse,
                    XmlResolver = new XmlPreloadedResolver(),
                    ConformanceLevel = ConformanceLevel.Auto
                });

                if (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.XmlDeclaration:
                            break;
                        case XmlNodeType.DocumentType:
                            if (!reader.MoveToAttribute("SYSTEM") &&
                                (!reader.Value.Equals("http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", StringComparison.InvariantCultureIgnoreCase) ||
                                !reader.Value.Equals("http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd", StringComparison.InvariantCultureIgnoreCase) ||
                                !reader.Value.Equals("http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd", StringComparison.InvariantCultureIgnoreCase)))
                                exception = new XmlException(FAILED_REASON_XML_DECLARATION_OR_ELEMENT_NOT_FOUND);
                            break;
                        case XmlNodeType.Element:
                            exception = new XmlException(FAILED_REASON_XML_DECLARATION_OR_ELEMENT_NOT_FOUND);
                            break;
                        default:
                            exception = new XmlException(FAILED_REASON_NOT_XML);
                            break;
                    }
                }
                else
                    exception = new XmlException(FAILED_REASON_NOT_XML);
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                if (sr != null)
                    sr.Close();

                if (reader != null)
                    reader.Close();
            }

            return exception == null;
        }

        /// <summary>
        /// Formats the <paramref name="stream"/> with the <paramref name="encoding"/> as Xml.
        /// </summary>
        /// <param name="stream">A <see cref="System.IO.Stream"/> to format as Xml.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <returns>A <see cref="System.Xml.XmlElement"/> containing the encoded Xml.</returns>
        public static XmlElement FormatXml(this Stream stream, Encoding encoding)
        {
            XmlDocument responseDocument = new XmlDocument();

            string formatted = FormatXmlString(stream, encoding);

            responseDocument.LoadXml(formatted);

            return responseDocument.DocumentElement;
        }

        /// <summary>
        /// Formats the <paramref name="stream"/> with the <paramref name="encoding"/> as an Xml string.
        /// </summary>
        /// <param name="stream">A <see cref="System.IO.Stream"/> to format as Xml.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <returns>A <see cref="System.Xml.XmlElement"/> containing the encoded Xml.</returns>
        public static string FormatXmlString(this Stream stream, Encoding encoding)
        {
            return
                string.Format(
                        "<?xml version=\"1.0\" encoding=\"{0}\"?><string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\"><![CDATA[{1}]]></string>",
                        encoding.EncodingName,
                        new StreamReader(stream, encoding).ReadToEnd());
        }

        /// <summary>
        /// Formats the <paramref name="xml"/> as Xml.
        /// </summary>
        /// <param name="xml">A <see cref="System.String"/> to format as Xml.</param>
        /// <returns>A <see cref="System.Xml.XmlElement"/> containing the Xml.</returns>
        public static XmlElement FormatXml(this string xml)
        {
            XmlDocument document = new XmlDocument();

            string formatted = FormatXmlString(xml);

            document.LoadXml(formatted);

            return document.DocumentElement;
        }

        /// <summary>
        /// Converts the string representation of an Xml Qualifed Name to an equivalent <see cref="XmlQualifiedName"/> object.
        /// </summary>
        /// <param name="xmlQualifiedName"></param>
        /// <returns></returns>
        public static XmlQualifiedName ParseXmlQualifiedName(this string xmlQualifiedName)
        {
            if (xmlQualifiedName == null)

                throw new ArgumentNullException("xmlQualifiedName");

            string[] segments = xmlQualifiedName.Split(':');

            if (segments.Length > 0)

                return new XmlQualifiedName(segments[segments.Length - 1], String.Join(":", segments.Take(segments.Length - 1)));

            return new XmlQualifiedName();
        }

        /// <summary>
        /// Formats the <paramref name="_string"/> as an Xml string.
        /// </summary>
        /// <param name="_string">A <see cref="System.String"/> to format as Xml.</param>
        /// <returns>A <see cref="System.Xml.XmlElement"/> containing the Xml.</returns>
        public static string FormatXmlString(this string _string)
        {
            return
                string.Format("<?xml version=\"1.0\"?><string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/\"><![CDATA[{0}]]></string>", _string);
        }

        /// <summary>
        /// Loads the <paramref name="stream"/> and returns <paramref name="element"/> or the <paramref name="exception"/>. 
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> to load.</param>
        /// <param name="element">The <see cref="System.Xml.XmlElement"/> to populate with the contents of the stream.</param>
        /// <param name="exception">An exception associated with a false return value.  Otherwise null.</param>
        /// <returns></returns>
        public static bool ReadXml(this Stream stream, out XmlElement element, out Exception exception)
        {
            XmlReader reader = null;
            element = null;
            exception = null;

            try
            {
                reader = XmlReader.Create(stream, new XmlReaderSettings()
                {
                    IgnoreWhitespace = true,
                    DtdProcessing = DtdProcessing.Parse,
                    XmlResolver = new XmlPreloadedResolver(),
                    ConformanceLevel = ConformanceLevel.Auto
                });

                XmlDocument responseDocument = new XmlDocument();

                responseDocument.Load(reader);

                if (responseDocument.DocumentElement == null)

                    throw new NullReferenceException($"NullReferenceException: {nameof(responseDocument.DocumentElement)}");

                element = responseDocument.DocumentElement;
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            stream.Position = 0;
            
            return (exception == null);
        }

        /// <summary>
        /// Loads the <paramref name="xml"/> and returns <paramref name="element"/> or the <paramref name="exception"/>. 
        /// </summary>
        /// <param name="xml">The <see cref="System.String"/> to load.</param>
        /// <param name="element">The <see cref="System.Xml.XmlElement"/> to populate with the contents of the stream.</param>
        /// <param name="exception">An exception associated with a false return value.  Otherwise null.</param>
        /// <returns></returns>
        public static bool ReadXml(this string xml, out XmlElement element, out Exception exception)
        {
            XmlReader reader = null;
            element = null;
            exception = null;

            try
            {
                reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings()
                {
                    IgnoreWhitespace = true,
                    DtdProcessing = DtdProcessing.Parse,
                    XmlResolver = new XmlPreloadedResolver(),
                    ConformanceLevel = ConformanceLevel.Auto
                });

                XmlDocument responseDocument = new XmlDocument();

                responseDocument.Load(reader);

                element = responseDocument.DocumentElement;
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            return (exception == null);
        }

        /// <summary>
        /// Writes the <paramref name="element"/> to the <paramref name="fs"/> and returns true or the <paramref name="exception"/>. 
        /// </summary>
        /// <param name="fs">The <see cref="FileStream"/> to write to.</param>
        /// <param name="element">The <see cref="System.Xml.XmlElement"/> to write to the stream.</param>
        /// <param name="exception">An exception associated with a false return value.  Otherwise null.</param>
        /// <returns></returns>
        public static bool WriteXml(this FileStream fs, XmlElement element, out Exception exception)
        {
            exception = null;

            using (XmlTextWriter w = new XmlTextWriter(fs, Encoding.UTF8))
            {
                w.Formatting = Formatting.Indented;

                element.WriteTo(w);

                w.Flush();
            }

            return true;
        }

        /// <summary>
        /// Writes the <paramref name="element"/> to a <see cref="System.IO.Stream"/> with the encoding. 
        /// </summary>
        /// <param name="element">The <see cref="System.Xml.XmlElement"/> to write.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <returns>The <see cref="System.IO.Stream"/>.</returns>
        public static Stream ToStream(this XmlElement element, Encoding encoding)
        {
            MemoryStream buffer = new MemoryStream();
            MemoryStream toStream = new MemoryStream();

            using (XmlTextWriter w = new XmlTextWriter(buffer, encoding))
            {
                element.WriteTo(w);

                w.Flush();

                buffer.Position = 0;

                buffer.CopyTo(toStream);
            }
            
            toStream.Position = 0;
            
            return toStream;
        }

        /// <summary>
        /// Checks if <paramref name="reader"/> is positioned on a node named <paramref name="elementName"/>, 
        /// and, if so, and if the node is the element empty, reads the next node or, if the node is the end element, reads the end element.
        /// Otherwise, throws an <see cref="XmlException"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="elementName"></param>
        public static void ReadEmptyOrEndElement(this XmlReader reader, String elementName)
        {
            if (reader.Name == elementName)
            {
                if (reader.NodeType == XmlNodeType.EndElement)

                    reader.ReadEndElement();

                else if (reader.NodeType == XmlNodeType.Element && reader.IsEmptyElement)

                    reader.Read();

                else

                    throw reader.CreateUnexpectedNodeTypeOrNameException(XmlNodeType.EndElement, elementName);
            }
            else if (reader.NodeType != XmlNodeType.None)

                throw reader.CreateUnexpectedNodeTypeOrNameException(XmlNodeType.EndElement, elementName);
        }

        /// <summary>
        /// Gets the sub-tree reader of this node positioned on the first node.  Be sure to close the returned reader!
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static XmlReader GetSubTreeReader(this XmlReader reader)
        {
            var r = reader.ReadSubtree();

            if (r.NodeType == XmlNodeType.None)

                r.Read();

            return r;
        }

        /// <summary>
        /// Reads the OuterXml of the sub-tree reader of this node.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static String ReadSubTreeOuterXml(this XmlReader reader)
        {
            using (var r = reader.ReadSubtree())
            {
                if (r.NodeType == XmlNodeType.None)

                    r.Read();

                return r.ReadOuterXml();
            }
        }

        /// <summary>
        /// Writes the current node from the <paramref name="reader"/> to the <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The <see cref="System.Xml.XmlWriter"/> with which to write the node.</param>
        /// <param name="reader">The <see cref="System.Xml.XmlReader"/> containing the node to write.</param>
        public static void WriteShallowNode(this XmlWriter writer, XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    writer.WriteAttributes(reader, true);
                    if (reader.IsEmptyElement)
                    {
                        writer.WriteEndElement();
                    }
                    break;
                case XmlNodeType.Text:
                    writer.WriteString(reader.Value);
                    break;
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    writer.WriteWhitespace(reader.Value);
                    break;
                case XmlNodeType.CDATA:
                    writer.WriteCData(reader.Value);
                    break;
                case XmlNodeType.EntityReference:
                    writer.WriteEntityRef(reader.Name);
                    break;
                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.ProcessingInstruction:
                    writer.WriteProcessingInstruction(reader.Name, reader.Value);
                    break;
                case XmlNodeType.DocumentType:
                    writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"), reader.Value);
                    break;
                case XmlNodeType.Comment:
                    writer.WriteComment(reader.Value);
                    break;
                case XmlNodeType.EndElement:
                    writer.WriteFullEndElement();
                    break;
            }
        }

        /// <summary>
        /// Writes an element with the specified local name and value.
        /// </summary>
        /// <param name="writer">The <see cref="System.Xml.XmlWriter"/> instance.</param>
        /// <param name="valueFormat">An expression with which to format the parameters.</param>
        /// <param name="localName">The local name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <exception cref="System.ArgumentException">See <see cref="System.Xml.XmlWriter.WriteElementString(string, string)"/>.</exception>
        /// <exception cref="System.FormatException">See <see cref="System.String.Format(string, object[])"/></exception>
        /// <exception cref="System.Text.EncoderFallbackException">See <see cref="System.Xml.XmlWriter.WriteElementString(string, string)"/>.</exception>
        public static void WriteFormattedElementString(this XmlWriter writer, string valueFormat, string localName, object value)
        {
            string elementValue;

            if (valueFormat != null)
                elementValue = string.Format(valueFormat, localName, value);
            else
                elementValue = value.ToString();

            writer.WriteElementString(localName, elementValue);
        }

        /// <summary>
        /// Writes an element with the specified local name, namespace URI, and value.
        /// </summary>
        /// <param name="writer">The <see cref="System.Xml.XmlWriter"/> instance.</param>
        /// <param name="valueFormat">An expression with which to format the parameters.</param>
        /// <param name="localName">The local name of the element.</param>
        /// <param name="ns">The namespace URI of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <exception cref="System.ArgumentException">See <see cref="System.Xml.XmlWriter.WriteElementString(string, string, string)"/>.</exception>
        /// <exception cref="System.FormatException">See <see cref="System.String.Format(string, object[])"/></exception>
        /// <exception cref="System.Text.EncoderFallbackException">See <see cref="System.Xml.XmlWriter.WriteElementString(string, string, string)"/>.</exception>
        public static void WriteFormattedElementString(this XmlWriter writer, string valueFormat, string localName, string ns, string value)
        {
            string elementValue; 
            
            if (valueFormat != null)
                elementValue = string.Format(valueFormat, localName, ns, value);
            else
                elementValue = value.ToString();

            writer.WriteElementString(localName, ns, elementValue);
        }

        /// <summary>
        /// Writes an element with the specified prefix, local name, namespace URI, and value.
        /// </summary>
        /// <param name="writer">The <see cref="System.Xml.XmlWriter"/> instance.</param>
        /// <param name="valueFormat">An expression with which to format the parameters.</param>
        /// <param name="prefix">The prefix of the element.</param>
        /// <param name="localName">The local name of the element.</param>
        /// <param name="ns">The namespace URI of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <exception cref="System.ArgumentException">See <see cref="System.Xml.XmlWriter.WriteElementString(string, string, string, string)"/>.</exception>
        /// <exception cref="System.FormatException">See <see cref="System.String.Format(string, object[])"/></exception>
        /// <exception cref="System.Text.EncoderFallbackException">See <see cref="System.Xml.XmlWriter.WriteElementString(string, string, string, string)"/>.</exception>
        public static void WriteFormattedElementString(this XmlWriter writer, string valueFormat, string prefix, string localName, string ns, string value)
        {
            string elementValue;

            if (valueFormat != null)
                elementValue = string.Format(valueFormat, prefix, localName, ns, value);
            else
                elementValue = value.ToString();

            writer.WriteElementString(prefix, localName, ns, elementValue);
        }

        /// <summary>
        /// Merges an element with an updated version.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="update"></param>
        /// <param name="identifierMap"></param>
        public static void Merge(this XmlElement element, XmlElement update, Dictionary<String, String> identifierMap)
        {
            if (update != null && element.Name == update.Name)
            {
                foreach (XmlAttribute attr in element.Attributes)
                {
                    XmlAttribute attrUpdate = (XmlAttribute)update.Attributes.GetNamedItem(attr.Name);

                    if (attrUpdate != null && attrUpdate.Value != attr.Value)

                        attr.Value = attrUpdate.Value;
                }

                if (element.HasChildNodes)
                {
                    foreach (XmlNode child in element.ChildNodes)
                    {
                        if (child is XmlElement)
                        {
                            string xpath = child.Name;

                            String identifier = null;

                            if (identifierMap.ContainsKey(element.Name + "/" + child.Name))

                                identifier = identifierMap[element.Name + "/" + child.Name];

                            else if (identifierMap.ContainsKey(child.Name))

                                identifier = identifierMap[child.Name];

                            if (identifier != null)
                            {
                                if (identifier.StartsWith("@"))
                                {
                                    XmlAttribute nameNode = (XmlAttribute)child.Attributes.GetNamedItem(identifier.Substring(1));

                                    if (nameNode != null)

                                        xpath += "["+ identifier +"='"+ nameNode.Value + "']";
                                }
                                else
                                {
                                    if (identifier == "text()")

                                        xpath += "[" + identifier + "='" + child.SelectSingleNode("text()").Value + "']";

                                    else if (identifier == "index()")

                                        throw new NotImplementedException("index()");

                                    else
                                    {
                                        XmlElement nameNode = (XmlElement)child.SelectSingleNode(identifier);

                                        if (nameNode != null)

                                            xpath += "[" + identifier + "='" + nameNode.SelectSingleNode("text()").Value + "']";
                                    }
                                }
                            }

                            Merge((XmlElement)child, (XmlElement)update.SelectSingleNode(xpath), identifierMap);
                        }
                        else if (child is XmlText)
                        {
                            XmlText updateText = (XmlText)update.SelectSingleNode("text()");

                            if (updateText != null)

                                (child as XmlText).Value = updateText.Value;
                        }
                        else if (child is XmlCDataSection)
                        {
                            XmlText updateText = (XmlText)update.SelectSingleNode("text()");

                            if (updateText != null)

                                (child as XmlCDataSection).Value = updateText.Value;
                        }
                    }
                }
                else if (update.HasChildNodes)
                {
                    foreach (XmlNode child in update.ChildNodes)
                    {
                        element.AppendChild(element.AppendChild(element.OwnerDocument.ImportNode(child.CloneNode(true), true)));
                    }
                }
            }
        }

        public static bool TryGetXmlEnumAttributeNameFromEnum(object enumValue, out String enumAttributeName)
        {
            if (enumValue == null)

                throw new ArgumentNullException(nameof(enumValue));

            Type objectType = enumValue.GetType();

            if (!objectType.IsEnum)

                throw new ArgumentException("Parameter is not a System.Enum type.", nameof(enumValue));

            Enum @enum = (Enum)(Object)enumValue;

            enumAttributeName = null;

            MemberInfo enumInfo = objectType.GetMember(@enum.ToString()).FirstOrDefault();

            return TryGetXmlEnumAttributeNameFromEnumInfo(ref enumAttributeName, enumInfo);
        }

        public static bool TryGetXmlEnumAttributeNameFromEnum<E>(this E @enum, out String enumAttributeName) where E : struct, IConvertible
        {
            Type enumType = typeof(E);

            if (!enumType.IsEnum)

                throw new ArgumentException("Generic parameter is not a System.Enum type.", nameof(E));

            enumAttributeName = null;

            MemberInfo enumValueInfo = enumType.GetMember(@enum.ToString()).FirstOrDefault();

            return TryGetXmlEnumAttributeNameFromEnumInfo(ref enumAttributeName, enumValueInfo);
        }

        private static bool TryGetXmlEnumAttributeNameFromEnumInfo(ref string enumAttributeName, MemberInfo enumValueInfo)
        {
            XmlEnumAttribute enumAttribute = enumValueInfo.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault();

            if (enumAttribute != null)

                enumAttributeName = enumAttribute.Name;

            return enumAttributeName != null;
        }

        public static bool TryGetEnumFromXmlEnumAttributeName(this Type enumType, object enumAttributeName, out Object @enum)
        {
            if (enumType == null)

                throw new ArgumentNullException(nameof(enumType));

            if (!enumType.IsEnum)

                throw new ArgumentException("Parameter is not a System.Enum type.", nameof(enumType));

            if (enumAttributeName == null)

                throw new ArgumentNullException(nameof(enumAttributeName));

            IEnumerable<Object> enumValues = enumType.GetEnumValues().Cast<Object>();

            bool found = false;

            @enum = enumValues.FirstOrDefault(enumValue =>
            {
                MemberInfo enumValueInfo = enumType.GetMember(enumValue.ToString()).FirstOrDefault();

                if (enumValueInfo != null)
                {
                    XmlEnumAttribute enumAttribute = enumValueInfo.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault();

                    if (enumAttribute != null && enumAttribute.Name == enumAttributeName.ToString())
                    {
                        return found = true;
                    }
                }

                return false;
            });

            return found;
        }

        public static bool TryGetEnumFromXmlEnumAttributeName<E>(this object enumAttributeName, out E @enum) where E : struct, IConvertible
        {
            Type enumType = typeof(E);

            if (!enumType.IsEnum)

                throw new ArgumentException("Generic parameter is not a System.Enum type.", nameof(E));

            E[] enumValues = (E[])enumType.GetEnumValues();

            bool found = false;

            @enum = enumValues.FirstOrDefault(enumValue =>
            {
                MemberInfo enumValueInfo = enumType.GetMember(enumValue.ToString()).FirstOrDefault();

                if (enumValueInfo != null)
                {
                    XmlEnumAttribute enumAttribute = enumValueInfo.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault();

                    if (enumAttribute != null && enumAttribute.Name == enumAttributeName.ToString())
                    {
                        found = true;
                    }
                }

                return found;
            });

            return found;
        }
    }
}
