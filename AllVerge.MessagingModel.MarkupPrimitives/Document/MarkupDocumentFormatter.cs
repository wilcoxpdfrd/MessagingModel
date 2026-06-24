using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public static class MarkupDocumentFormatter
    {
        public static MarkupNode ReadMarkup(this XmlDictionaryReader reader)
        {
            if (reader.ReadState > ReadState.Interactive)

                return null;

            MarkupNode currentNode = null;

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Comment:

                        MarkupNode commentNode = new MarkupNode(currentNode, MarkupTokens.COMMENT_NODE_NAME, MarkupTokens.COMMENT_NODE_NAME);

                        commentNode.TryAddText(reader.Value);

                        break;

                    case XmlNodeType.Element:

                        String nodeType = null;

                        MarkupAttribute[] attributes = reader.ReadAttributes(out nodeType);

                        if (nodeType == null && reader.LocalName != MarkupTokens.ARRAY_ITEM_ELEMENT_NAME)

                            throw new InvalidOperationException("Element type attribute not found.");

                        if (attributes.Length > 0)
                        {
                            String itemName;

                            if (attributes.IsElementItemAttributed(reader.LocalName, out itemName))

                                currentNode = new MarkupNode(currentNode, itemName, nodeType);

                            else

                                currentNode = new MarkupNode(currentNode, reader.LocalName, nodeType);

                            currentNode.AddAttributes(attributes);
                        }
                        else

                            currentNode = new MarkupNode(currentNode, reader.LocalName, nodeType);

                        if (reader.IsEmptyElement)
                        {
                            currentNode.SetNullText();

                            currentNode = currentNode.ParentNode;
                        }

                        break;

                    case XmlNodeType.EndElement:

                        if (currentNode.ParentNode != null)

                            currentNode = currentNode.ParentNode;

                        break;

                    case XmlNodeType.Text:

                        currentNode.TryAddText(reader.Value);

                        break;

                    default:

                        break;
                }
            }

            return currentNode;
        }

        private static MarkupAttribute[] ReadAttributes(this XmlDictionaryReader reader, out String typeAttributeValue)
        {
            typeAttributeValue = null;

            List<MarkupAttribute> elementAttributes = new List<MarkupAttribute>();

            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    MarkupAttribute attribute = ReadAttribute(reader);

                    if (attribute.Name == MarkupTokens.TYPE_ATTRIBUTE_NAME)

                        typeAttributeValue = attribute.Value;

                    else

                        elementAttributes.Add(attribute);
                }
                while (reader.MoveToNextAttribute());

                reader.MoveToElement();
            }

            return elementAttributes.ToArray();
        }

        private static MarkupAttribute ReadAttribute(this XmlDictionaryReader reader)
        {
            if (reader.NodeType != XmlNodeType.Attribute)

                throw new ArgumentException("Invalid NodeType.", "reader", new InvalidOperationException("reader not set to attribute node."));

            //reader.IsDefault? // don't think we care ...

            StringBuilder sb = new StringBuilder();

            while (reader.ReadAttributeValue())
            {
                XmlNodeType nodeType = reader.NodeType;

                if (nodeType != XmlNodeType.Text)
                {
                    if (nodeType == XmlNodeType.EntityReference)

                        throw new NotImplementedException("ResolveEntityReference");

                    throw new ArgumentException("UnexpectedNodeType", reader.NodeType.ToString());
                }
                else
                {
                    sb.Append(reader.Value);
                }
            }

            if (reader.NamespaceURI == MarkupNamespaceAttribute.XmlNameSpaceUri)

                return new MarkupNamespaceAttribute(reader.LocalName, sb.ToString());

            return new MarkupAttribute(reader.Name, sb.ToString());
        }

        private static bool IsElementItemAttributed(this IEnumerable<MarkupAttribute> elementAttributes, string name, out string itemName)
        {
            itemName = null;

            if (name == MarkupTokens.ARRAY_ITEM_ELEMENT_NAME)
            {
                MarkupNamespaceAttribute itemNamespaceAttribute = elementAttributes.OfType<MarkupNamespaceAttribute>().FirstOrDefault(a => a.Value == MarkupTokens.ARRAY_ITEM_ELEMENT_NAME);

                if (itemNamespaceAttribute != null)
                {
                    MarkupAttribute itemAttribute = elementAttributes.OfType<MarkupAttribute>().FirstOrDefault(a => a.Name == itemNamespaceAttribute.Value);

                    if (itemAttribute != null && !String.IsNullOrWhiteSpace(itemAttribute.Value))

                        itemName = itemAttribute.Value;

                    else

                        itemName = MarkupTokens.ARRAY_ITEM_ELEMENT_NAME;
                }
            }
            else if (name.Contains(":"))
            {
                MarkupNamespaceAttribute itemNamespaceAttribute = elementAttributes.OfType<MarkupNamespaceAttribute>().FirstOrDefault(a => a.Value == MarkupTokens.ARRAY_ITEM_ELEMENT_NAME);

                if (itemNamespaceAttribute != null)
                {
                    if (name == itemNamespaceAttribute.Name + ":" + MarkupTokens.ARRAY_ITEM_ELEMENT_NAME)
                    {
                        MarkupAttribute itemAttribute = elementAttributes.OfType<MarkupAttribute>().FirstOrDefault(a => a.Name == itemNamespaceAttribute.Value);

                        if (itemAttribute != null && !String.IsNullOrWhiteSpace(itemAttribute.Value))

                            itemName = itemAttribute.Value;

                        else

                            itemName = MarkupTokens.ARRAY_ITEM_ELEMENT_NAME;
                    }
                }
            }

            return itemName != null;
        }
    }
}
