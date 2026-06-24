using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.MarkupPrimitives.Xml
{
    public static class XmlReaderExtensions
    {
        /// <summary>
        /// Creates an "Element not a recognized representation of <typeparamref name="T"/> <see cref="XmlException"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static XmlException CreateElementNotRecognizedRepresentationException<T>(this XmlReader reader)
        {
            return reader.CreateException("Element {0} is not a recognized representation of {1}.", reader.Name, typeof(T));
        }

        /// <summary>
        /// Creates an "Unexpected node type encountered" <see cref="XmlException"/> from the <paramref name="args"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="expectedNodeType"></param>
        /// <returns></returns>
        public static XmlException CreateUnexpectedNodeTypeException(this XmlReader reader, XmlNodeType expectedNodeType)
        {
            return reader.CreateException("Unexpected node type; expected '{0}', found '{1}'.", expectedNodeType, reader.NodeType);
        }

        /// <summary>
        /// Creates an "Unexpected node type or name encountered" <see cref="XmlException"/> from the <paramref name="args"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="expectedNodeType"></param>
        /// <param name="expectedName"></param>
        /// <returns></returns>
        public static XmlException CreateUnexpectedNodeTypeOrNameException(this XmlReader reader, XmlNodeType expectedNodeType, String expectedName)
        {
            return reader.CreateException("Unexpected node type or name encountered; expected '{0}'/'{1}', found '{2}'/'{3}'.", expectedNodeType, expectedName, reader.NodeType, reader.Name);
        }

        /// <summary>
        /// Creates a "Missing expected element attribute" <see cref="XmlException"/> from the <paramref name="args"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="expectedAttributeName"></param>
        /// <returns></returns>
        public static XmlException CreateMissingExpectedElementAttributeException(this XmlReader reader, String expectedAttributeName)
        {
            return reader.CreateException("Missing expected element attribute '{0}'.", expectedAttributeName);
        }

        /// <summary>
        /// Creates an "Unexpected element node" <see cref="XmlException"/> from the <paramref name="args"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="expectedAttributeName"></param>
        /// <returns></returns>
        public static XmlException CreateUnexpectedElementException(this XmlReader reader, String expectedElementName)
        {
            return reader.CreateException("Unexpected element node; expected '{0}', found '{1}'.", expectedElementName, reader.Name);
        }

        /// <summary>
        /// Creates an <see cref="XmlException"/> from the <paramref name="messageFormat"/> and <paramref name="args"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="messageFormat"></param>
        /// <returns></returns>
        public static XmlException CreateException(this XmlReader reader, String messageFormat, params Object[] args)
        {
            return CreateException(reader, null, messageFormat, args);
        }

        /// <summary>
        /// Creates an <see cref="XmlException"/> from the <paramref name="innerException"/>, <paramref name="messageFormat"/> and and <paramref name="args"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="innerException"></param>
        /// <param name="messageFormat"></param>
        /// <returns></returns>
        public static XmlException CreateException(this XmlReader reader, Exception innerException, String messageFormat, params Object[] args)
        {
            if (args.Length > 0)

                messageFormat = String.Format(messageFormat, args);

            IXmlLineInfo lineInfo = reader as IXmlLineInfo;

            if (lineInfo != null)

                return new XmlException(messageFormat, innerException, lineInfo.LineNumber, lineInfo.LinePosition);

            else

                return new XmlException(messageFormat, innerException);
        }
    }
}
