using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public static class MarkupAttributeExtensions
    {
        public static bool TryGetAttributeByName(this MarkupNode dataAttribute, String attributeName, out MarkupAttribute attribute)
        {
            attribute = null;

            if (dataAttribute != null)
            {
                if (dataAttribute.Attributes.Contains(attributeName))

                    attribute = dataAttribute.Attributes[attributeName];
            }

            return attribute != null;
        }

        public static bool TryGetTextAsBoolean(this MarkupAttribute dataAttribute, out bool textAsBoolean)
        {
            string text;

            if (TryGetText(dataAttribute, out text) && bool.TryParse(text, out textAsBoolean))

                return true;

            textAsBoolean = false;

            return false;
        }

        public static bool TryGetTextAsInt(this MarkupAttribute dataAttribute, out int textAsInt)
        {
            string text;

            if (TryGetText(dataAttribute, out text) && int.TryParse(text, out textAsInt))

                return true;

            textAsInt = 0;

            return false;
        }

        public static bool TryGetTextAsDecimal(this MarkupAttribute dataAttribute, out decimal textAsDecimal)
        {
            string text;

            if (TryGetText(dataAttribute, out text) && decimal.TryParse(text, out textAsDecimal))

                return true;

            textAsDecimal = 0;

            return false;
        }

        public static bool TryGetAttributeTextAsUri(this MarkupAttribute dataAttribute, out Uri textAsUri)
        {
            string text;

            if (TryGetText(dataAttribute, out text) && Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out textAsUri))

                return true;

            textAsUri = null;

            return false;
        }

        public static bool TryGetText(this MarkupAttribute dataAttribute, out string text)
        {
            if (dataAttribute == null || String.IsNullOrWhiteSpace(dataAttribute.Value))
            {
                text = null;

                return false;
            }

            text = dataAttribute.Value;

            return true;
        }
    }
}
