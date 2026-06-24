using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    /// <summary>
    /// Stores and retrieves the message encoding format of incoming and outgoing messages for the <see cref="CompositeMessageEncodingFormatEncoderFactory"/>.
    /// </summary>
    public sealed class MessageEncodingFormatProperty : IMessageProperty
    {
        /// <summary>Returns the name of the property.</summary>
        public const string Name = nameof(MessageEncodingFormatProperty);

        private MessageEncodingFormat resourceTransferMessageFormat;

        private static MessageEncodingFormatProperty formUrlEncodedProperty;
        private static MessageEncodingFormatProperty formMultipartData;
        private static MessageEncodingFormatProperty jsonProperty;
        private static MessageEncodingFormatProperty soap11WSAddressing10Property;
        private static MessageEncodingFormatProperty soap11WSAddressingAugust2004Property;
        private static MessageEncodingFormatProperty soap11Property;
        private static MessageEncodingFormatProperty soap12WSAddressing10Property;
        private static MessageEncodingFormatProperty soap12WSAddressingAugust2004Property;
        private static MessageEncodingFormatProperty soap12Property;
        private static MessageEncodingFormatProperty binaryProperty;
        private static MessageEncodingFormatProperty binaryPlusGzipProperty;
        private static MessageEncodingFormatProperty binaryPlusDeflateProperty;
        private static MessageEncodingFormatProperty xmlProperty;
        private static MessageEncodingFormatProperty textProperty;
        private static MessageEncodingFormatProperty htmlProperty;
        private static MessageEncodingFormatProperty rawProperty;
        // private static ResourceTransferFormatMessageProperty rawEncodedProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceTransferFormatMessageProperty" /> class with a specified format.
        /// </summary>
        /// <param name="resourceTransferMessageFormat">
        /// The <see cref="Channels.ResourceTransferFormat" /> of the message body.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// The <paramref name="resourceTransferMessageFormat"/> cannot be set to the <see cref="ResourceTransferFormat.Default" /> value in the constructor.
        /// </exception>
        public MessageEncodingFormatProperty(MessageEncodingFormat resourceTransferMessageFormat)
        {
            if (resourceTransferMessageFormat == MessageEncodingFormat.Default)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(PublicSR.DefaultContentFormatNotAllowedInProperty));
            }
            this.resourceTransferMessageFormat = resourceTransferMessageFormat;
        }

        /// <summary>
        /// Gets the format used for the message body.
        /// </summary>
        /// <returns>
        /// The <see cref="Channels.MessageEncodingFormat" /> value that specifies the resource transfer format used for the message body.
        /// </returns>
        public MessageEncodingFormat Format
        {
            get { return this.resourceTransferMessageFormat; }
        }

        internal static MessageEncodingFormatProperty FormMultipartDataProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.formMultipartData == null)
                {
                    MessageEncodingFormatProperty.formMultipartData = new MessageEncodingFormatProperty(MessageEncodingFormat.FormMultipartData);
                }
                return MessageEncodingFormatProperty.formMultipartData;
            }
        }

        internal static MessageEncodingFormatProperty FormUrlEncodedProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.formUrlEncodedProperty == null)
                {
                    MessageEncodingFormatProperty.formUrlEncodedProperty = new MessageEncodingFormatProperty(MessageEncodingFormat.FormUrlEncoded);
                }
                return MessageEncodingFormatProperty.formUrlEncodedProperty;
            }
        }

        internal static MessageEncodingFormatProperty JsonProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.jsonProperty == null)
                {
                    MessageEncodingFormatProperty.jsonProperty = new MessageEncodingFormatProperty(MessageEncodingFormat.Json);
                }
                return MessageEncodingFormatProperty.jsonProperty;
            }
        }

        internal static MessageEncodingFormatProperty Soap11WSAddressing10Property
        {
            get
            {
                if (MessageEncodingFormatProperty.soap11WSAddressing10Property == null)
                {
                    MessageEncodingFormatProperty.soap11WSAddressing10Property = new MessageEncodingFormatProperty(MessageEncodingFormat.Soap11WSAddressing10);
                }
                return MessageEncodingFormatProperty.soap11WSAddressing10Property;
            }
        }

        internal static MessageEncodingFormatProperty Soap11WSAddressingAugust2004Property
        {
            get
            {
                if (MessageEncodingFormatProperty.soap11WSAddressingAugust2004Property == null)
                {
                    MessageEncodingFormatProperty.soap11WSAddressingAugust2004Property = new MessageEncodingFormatProperty(MessageEncodingFormat.Soap11WSAddressingAugust2004);
                }
                return MessageEncodingFormatProperty.soap11WSAddressingAugust2004Property;
            }
        }

        internal static MessageEncodingFormatProperty Soap11Property
        {
            get
            {
                if (MessageEncodingFormatProperty.soap11Property == null)
                {
                    MessageEncodingFormatProperty.soap11Property = new MessageEncodingFormatProperty(MessageEncodingFormat.Soap11);
                }
                return MessageEncodingFormatProperty.soap11Property;
            }
        }

        internal static MessageEncodingFormatProperty Soap12WSAddressing10Property
        {
            get
            {
                if (MessageEncodingFormatProperty.soap12WSAddressing10Property == null)
                {
                    MessageEncodingFormatProperty.soap12WSAddressing10Property = new MessageEncodingFormatProperty(MessageEncodingFormat.Soap12WSAddressing10);
                }
                return MessageEncodingFormatProperty.soap12WSAddressing10Property;
            }
        }

        internal static MessageEncodingFormatProperty Soap12WSAddressingAugust2004Property
        {
            get
            {
                if (MessageEncodingFormatProperty.soap12WSAddressingAugust2004Property == null)
                {
                    MessageEncodingFormatProperty.soap12WSAddressingAugust2004Property = new MessageEncodingFormatProperty(MessageEncodingFormat.Soap12WSAddressingAugust2004);
                }
                return MessageEncodingFormatProperty.soap12WSAddressingAugust2004Property;
            }
        }

        internal static MessageEncodingFormatProperty Soap12Property
        {
            get
            {
                if (MessageEncodingFormatProperty.soap12Property == null)
                {
                    MessageEncodingFormatProperty.soap12Property = new MessageEncodingFormatProperty(MessageEncodingFormat.Soap12);
                }
                return MessageEncodingFormatProperty.soap12Property;
            }
        }

        public static MessageEncodingFormatProperty BinaryProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.binaryProperty == null)
                {
                    MessageEncodingFormatProperty.binaryProperty = new MessageEncodingFormatProperty(MessageEncodingFormat.Binary);
                }
                return MessageEncodingFormatProperty.binaryProperty;
            }
        }

        public static MessageEncodingFormatProperty BinaryPlusGzipProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.binaryPlusGzipProperty == null)
                {
                    MessageEncodingFormatProperty.binaryPlusGzipProperty = new MessageEncodingFormatProperty(MessageEncodingFormat.BinaryPlusGzip);
                }
                return MessageEncodingFormatProperty.binaryPlusGzipProperty;
            }
        }

        public static MessageEncodingFormatProperty BinaryPlusDeflateProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.binaryPlusDeflateProperty == null)
                {
                    MessageEncodingFormatProperty.binaryPlusDeflateProperty = new MessageEncodingFormatProperty(MessageEncodingFormat.BinaryPlusDeflate);
                }
                return MessageEncodingFormatProperty.binaryPlusDeflateProperty;
            }
        }


        internal static MessageEncodingFormatProperty XmlProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.xmlProperty == null)
                {
                    MessageEncodingFormatProperty.xmlProperty = new MessageEncodingFormatProperty(MessageEncodingFormat.Xml);
                }
                return MessageEncodingFormatProperty.xmlProperty;
            }
        }

        internal static MessageEncodingFormatProperty TextProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.textProperty == null)
                {
                    MessageEncodingFormatProperty.textProperty = new MessageEncodingFormatProperty(MessageEncodingFormat.Text);
                }
                return MessageEncodingFormatProperty.textProperty;
            }
        }

        internal static MessageEncodingFormatProperty HtmlProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.htmlProperty == null)
                {
                    MessageEncodingFormatProperty.htmlProperty = new MessageEncodingFormatProperty(MessageEncodingFormat.Html);
                }
                return MessageEncodingFormatProperty.htmlProperty;
            }
        }

        internal static MessageEncodingFormatProperty RawProperty
        {
            get
            {
                if (MessageEncodingFormatProperty.rawProperty == null)
                {
                    MessageEncodingFormatProperty.rawProperty = new MessageEncodingFormatProperty(MessageEncodingFormat.Raw);
                }
                return MessageEncodingFormatProperty.rawProperty;
            }
        }

        /// <summary>
        /// Returns a copy of the current property.
        /// </summary>
        /// <returns>
        /// An instance of the <see cref="IMessageProperty" /> interface that is a copy of the current <see cref="MessageEncodingFormatProperty" />, replacing the <see cref="TransferEncoding"/> with the supplied <paramref name="transferEncoding"/>.
        /// </returns>
        public IMessageProperty CreateCopy()
        {
            return new MessageEncodingFormatProperty(this.Format);
        }

        /// <summary>Returns the name of the property and the encoding format used when constructed.</summary>
        /// <returns>Returns "TransferMessageEncodingMessageProperty: MessageEncoding={0}", where {0} is MessageEncoding.ToString(), which specifies the encoding format used.</returns>
        public override string ToString()
        {
            // ToDo: Change SR2.WebBodyFormatPropertyToString to pass name of property, Add TransferEncoding to return ...

            //return SR2.Format(SR2.WebBodyFormatPropertyToString, this.MessageEncoding.ToString());

            return AMMMFR.Format(AMMMFR.TransferMessageEncodingMessagePropertyToString, this.Format.ToString());
        }
    }
}
