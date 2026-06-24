using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public static class MarkupNodeExtensions
    {
        public static bool TryRemoveChildNodeByName(this MarkupNode node, String childNodeName, out MarkupNode childNode)
        {
            if (node.TryGetChildNodeByName(childNodeName, out childNode))

                return node.RemoveChildNode(childNode);

            return false;
        }

        public static bool TryRemoveChildNode(this MarkupNode node, MarkupNode childNode)
        {
            if (node == null)

                return false;

            return node.RemoveChildNode(childNode);
        }

        public static bool TryMoveNode(this MarkupNode node, MarkupNode parentNode)
        {
            if (node == null)

                return false;

            return node.ParentNode.RemoveChildNode(node) && node.SetParentNode(parentNode);
        }

        public static bool TryAddChildName(this MarkupNode node, String childName)
        {
            if (node != null)

                return node.AddChildName(childName);

            return false;
        }

        public static int TryAddAttributes(this MarkupNode node, IEnumerable<MarkupAttribute> attributes)
        {
            if (node != null)

                return node.AddAttributes(attributes);

            return 0;
        }

        public static bool TryClearText(this MarkupNode node)
        {
            if (node != null)
            {
                return node.ClearText();
            }

            return false;
        }

        public static bool TryAddText(this MarkupNode node, string text)
        {
            if (node != null)
            {
                return node.AddText(text);
            }

            return false;
        }

        public static bool TryGetTextArray(this MarkupNode node, out string[] textArray)
        {
            if (node != null && node.NodeType == "array" && node.Children.Count() > 0 && node.Children.All(c => c.Name == "item"))

                textArray = node.Children.Select(c => c.Text).ToArray();

            else

                textArray = null;

            return textArray != null;
        }

        public static bool TryGetText(this MarkupNode node, out string text)
        {
            if (node == null || String.IsNullOrWhiteSpace(node.Text))
            {
                text = null;

                return false;
            }

            text = node.Text;

            return true;
        }

        public static bool TryGetTextAsBoolean(this MarkupNode node, out bool textAsBoolean)
        {
            string text;

            if (TryGetText(node, out text) && bool.TryParse(text, out textAsBoolean))

                return true;

            textAsBoolean = false;

            return false;
        }

        public static bool TryGetTextAsInt(this MarkupNode node, out int textAsInt)
        {
            string text;

            if (TryGetText(node, out text) && int.TryParse(text, out textAsInt))

                return true;

            textAsInt = 0;

            return false;
        }

        public static bool TryGetTextAsDecimal(this MarkupNode node, out decimal textAsDecimal)
        {
            string text;

            if (TryGetText(node, out text) && decimal.TryParse(text, out textAsDecimal))

                return true;

            textAsDecimal = 0;

            return false;
        }

        public static bool TryGetTextAsUri(this MarkupNode node, out Uri textAsUri)
        {
            string text;

            if (TryGetText(node, out text) && Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out textAsUri))

                return true;

            textAsUri = null;

            return false;
        }

        public static bool TryGetChildNodeTextByName(this MarkupNode node, string childNodeName, out string nodeText)
        {
            MarkupNode childNode;

            if (node != null && node.TryGetChildNodeByName(childNodeName, out childNode) && childNode.TryGetText(out nodeText))

                return true;

            nodeText = null;

            return false;
        }

        public static bool TryGetChildNodeTextByNameAsBoolean(this MarkupNode node, string childNodeName, out bool textAsBoolean)
        {
            string text;

            MarkupNode childNode;

            if (node != null && node.TryGetChildNodeByName(childNodeName, out childNode) && childNode.TryGetText(out text) && bool.TryParse(text, out textAsBoolean))

                return true;

            textAsBoolean = false;

            return false;
        }

        public static bool TryGetChildNodeTextByNameAsInt(this MarkupNode node, string childNodeName, out int textAsInt)
        {
            string text;

            MarkupNode childNode;

            if (node != null && node.TryGetChildNodeByName(childNodeName, out childNode) && childNode.TryGetText(out text) && int.TryParse(text, out textAsInt))

                return true;

            textAsInt = 0;

            return false;
        }

        public static bool TryGetChildNodeTextByNameAsDecimal(this MarkupNode node, string childNodeName, out decimal textAsDecimal)
        {
            string text;

            MarkupNode childNode;

            if (node != null && node.TryGetChildNodeByName(childNodeName, out childNode) && childNode.TryGetText(out text) && decimal.TryParse(text, out textAsDecimal))

                return true;

            textAsDecimal = 0;

            return false;
        }

        public static bool TryGetChildNodeTextByNameAsUri(this MarkupNode node, string childNodeName, out Uri textAsUri)
        {
            string text;

            MarkupNode childNode;

            if (node != null && node.TryGetChildNodeByName(childNodeName, out childNode) && childNode.TryGetText(out text) && Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out textAsUri))
            {
                if (!textAsUri.IsWellFormedOriginalString())

                    throw new UriFormatException("Node text is not a well formed Uri.");

                return true;
            }

            textAsUri = null;

            return false;
        }

        public static bool TryGetAttributeTextByNameAsUri(this MarkupNode node, string attributeName, out Uri textAsUri)
        {
            if (node != null && node.TryGetAttributeByName(attributeName, out MarkupAttribute dataAttribute) && dataAttribute.TryGetText(out string text) && Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out textAsUri))
            {
                if (!textAsUri.IsWellFormedOriginalString())

                    throw new UriFormatException("Node text is not a well formed Uri.");

                return true;
            }

            textAsUri = null;

            return false;
        }

        public static bool TryGetChildNodeTextByNameAsArray(this MarkupNode node, string childNodeName, out String[] textArray)
        {
            MarkupNode childNode;

            if (node != null && node.TryGetChildNodeByName(childNodeName, out childNode) && childNode.TryGetTextArray(out textArray))
            {
                return true;
            }

            textArray = null;

            return false;
        }

        public static bool TryGetChildNodeByNamePattern(this MarkupNode node, String childNamePattern, out MarkupNode childNode)
        {
            childNode = node.Children.FirstOrDefault(c => Regex.IsMatch(c.Name, childNamePattern));

            return childNode != null;
        }

        public static bool HasChildNodeByName(this MarkupNode node, string childName)
        {
            if (node == null)

                return false;

            else

                return node.Children.Any(c => c.Name == childName);
        }

        public static bool TryGetChildNodeByName(this MarkupNode node, string childName, out MarkupNode childNode)
        {
            if (node == null)

                childNode = null;

            else

                childNode = node.Children.FirstOrDefault(c => c.Name == childName);

            return childNode != null;
        }

        public static bool TryGetChildNodesByName(this MarkupNode node, string childName, out MarkupNode[] childNodes)
        {
            if (node == null)

                childNodes = Enumerable.Empty<MarkupNode>().ToArray();

            else

                childNodes = node.Children.Where(c => c.Name == childName).ToArray();

            return childNodes.Count() > 0;
        }

        public static bool TryGetChildNodesByNamePattern(this MarkupNode node, String childNamePattern, out MarkupNode[] childNodes)
        {
            if (node == null)

                childNodes = Enumerable.Empty<MarkupNode>().ToArray();

            else

                childNodes = node.Children.Where(c => Regex.IsMatch(c.Name, childNamePattern)).ToArray();

            return childNodes.Count() > 0;
        }

        public static bool TryFindFirstChildNode(this MarkupNode node, Func<MarkupNode, bool> search, out MarkupNode childNode)
        {
            if (node == null)

                childNode = null;

            else

                childNode = node.Children.FirstOrDefault(c => search.Invoke(c));

            return childNode != null;
        }

        public static bool TryFindChildNodes(this MarkupNode node, Func<MarkupNode, bool> search, out MarkupNode[] childNodes)
        {
            if (node == null)

                childNodes = Enumerable.Empty<MarkupNode>().ToArray();

            else

                childNodes = node.Children.Where(c => search.Invoke(c)).ToArray();

            return childNodes.Count() > 0;
        }

        public static bool ContainsAnyChildName(this MarkupNode node, params string[] childNames)
        {
            if (node == null)

                return false;

            return node.Children.Any(c => childNames.Any(n => n == c.Name));
        }

        public static IEnumerable<MarkupNode> Clone(this IEnumerable<MarkupNode> nodes, MarkupNode parentNode)
        {
            return nodes.Select(n => n.Clone(parentNode));
        }
    }
}
