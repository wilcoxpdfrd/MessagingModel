using AllVerge.SystemPrimitives.Collections;
using AllVerge.SystemPrimitives.Net.Mime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection.Metadata;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    /// <summary>
    /// Extends <see cref="ContentType"/>, exposing <see cref="NormalizedMediaType"/>
    /// and additional detail about the <see cref="MessageVersion"/>.
    /// </summary>
    public class MediaContentType : ContentType
    {
        public const String PARAMETER_KEY_CHARSET = "charset";
        public const String PARAMETER_KEY_SOAP_ACTION = "action";

        public MediaContentType() : base() { }

        /// <summary>
        /// MediaContentType cstr
        /// </summary>
        /// <param name="contentType">A media-type; charset; parameter list, e.g. application/soap+xml; charset=utf-8</param>
        /// <param name="parameters">
        /// Array of additional parameter (Key, Value, Override) tuples.  
        /// Pass Override true to use Value for the parameter if the same key and a different Value is also supplied in the contentType parameter list.
        /// </param>
        public MediaContentType(string contentType, params (String Key, String Value, bool Override)[] parameters) :
            base(contentType)
        {
            foreach ((String Key, String Value, bool Override) parameter in parameters)
            {
                TryPutParameter(new NameValueHeaderValue(parameter.Key, parameter.Value), parameter.Override);
            }

            if (base.CharSet == null)

                base.CharSet = Encoding.UTF8.WebName;

            OnMediaTypeChanged();
        }

        /// <summary>
        /// MediaContentType cstr
        /// </summary>
        /// <param name="mediaType">A media-type, e.g. application/soap+xml</param>
        /// <param name="charSet">A Charset, e.g. utf-8</param>
        /// <param name="parameters"></param>
        public MediaContentType(string mediaType, string charSet, ICollection<NameValueHeaderValue> parameters)
            : base()
        {
            base.MediaType = mediaType;
            base.CharSet = charSet;
            foreach (NameValueHeaderValue parameter in parameters)
                TryPutParameter(parameter, false);

            OnMediaTypeChanged();
        }

        /// <summary>
        /// MediaContentType cstr
        /// </summary>
        /// <param name="contentType">
        /// A <see cref="MediaTypeHeaderValue"/> value.
        /// </param>
        public MediaContentType(MediaTypeHeaderValue contentType)
        {
            base.MediaType = contentType.MediaType;
            base.CharSet = contentType.CharSet;
            foreach (NameValueHeaderValue parameter in contentType.Parameters)
                TryPutParameter(parameter, false);

            OnMediaTypeChanged();
        }

        public new string MediaType { get => base.MediaType; set { base.MediaType = value; OnMediaTypeChanged(); } }
        public string NormalizedMediaType;
        public MessageEncodingFormat TransferFormat;
        public bool IsFormTransferFormat
        {
            get
            {
                switch (TransferFormat)
                {
                    case MessageEncodingFormat.FormMultipartData:
                    case MessageEncodingFormat.FormUrlEncoded:
                        return true;
                }
                return false;
            }
        }
        public MessageVersion MessageVersion;

        private void TryPutParameter(NameValueHeaderValue parameter, bool @override)
        {
            if (this.Parameters.ContainsKey(parameter.Name))
            {
                if (this.Parameters[parameter.Name] != parameter.Value && @override)

                    this.Parameters[parameter.Name] = parameter.Value;
            }
            else

                this.Parameters[parameter.Name] = parameter.Value;
        }

        private void OnMediaTypeChanged()
        {
            string mediaType;

            if (MediaTypes.TryGetNormalizedResourceMediaType(this.MediaType, out this.NormalizedMediaType))

                mediaType = this.NormalizedMediaType;

            else

                mediaType = this.MediaType;

            GetMessageSpecification(mediaType, this.Parameters, out this.TransferFormat, out this.MessageVersion);
        }

        public String ToMediaTypePlusCharSet()
        {
            String charset = Parameters[PARAMETER_KEY_CHARSET];
            if (charset == null)
                return MediaType;
            String[] parts = new string[4];
            parts[0] = MediaType;
            parts[1] = "; ";
            parts[2] = $"{PARAMETER_KEY_CHARSET}=";
            parts[3] = charset;
            return String.Join(String.Empty, parts);
        }

        public String ToMediaTypePlusParameters()
        {
            return this.ToString();
        }

        internal static void GetMessageSpecification(string mediaType, StringDictionary parameters, out MessageEncodingFormat transferFormat, out MessageVersion messageVersion)
        {
            switch (mediaType)
            {
                case MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE:
                case MediaTypeConstants.APPLICATION_SCHEMA_PLUS_JSON_MEDIA_TYPE:
                    transferFormat = MessageEncodingFormat.Json;
                    messageVersion = MessageVersion.None;
                    break;
                case MediaTypeConstants.APPLICATION_XML_MEDIA_TYPE:
                case MediaTypeConstants.TEXT_XML_MEDIA_TYPE:
                    if (parameters.ContainsKey(PARAMETER_KEY_SOAP_ACTION))
                    {
                        transferFormat = MessageEncodingFormat.Soap11WSAddressingAugust2004;
                        messageVersion = MessageVersion.Soap11WSAddressingAugust2004;
                    }
                    else
                    {
                        transferFormat = MessageEncodingFormat.Xml;
                        messageVersion = MessageVersion.None;
                    }
                    break;
                case MediaTypeConstants.APPLICATION_SCHEMA_PLUS_XML_MEDIA_TYPE:
                    transferFormat = MessageEncodingFormat.Xml;
                    messageVersion = MessageVersion.None;
                    break;
                case MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE:
                    transferFormat = MessageEncodingFormat.Text;
                    messageVersion = MessageVersion.None;
                    break;
                case MediaTypeConstants.APPLICATION_XHTML_PLUS_XML_MEDIA_TYPE:
                    transferFormat = MessageEncodingFormat.Html;
                    messageVersion = MessageVersion.None;
                    break;
                case MediaTypeConstants.APPLICATION_FORM_URLENCODED:
                    transferFormat = MessageEncodingFormat.FormUrlEncoded;
                    messageVersion = MessageVersion.None;
                    break;
                case MediaTypeConstants.MULTIPART_FORMDATA:
                    transferFormat = MessageEncodingFormat.FormMultipartData;
                    messageVersion = MessageVersion.None;
                    break;
                case MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE:
                    if (parameters.ContainsKey(PARAMETER_KEY_SOAP_ACTION))
                    {
                        transferFormat = MessageEncodingFormat.Soap12WSAddressing10;
                        messageVersion = MessageVersion.Soap12WSAddressing10;
                    }
                    else
                    {
                        transferFormat = MessageEncodingFormat.Soap12;
                        messageVersion = MessageVersion.Soap12;
                    }
                    break;
                case MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_MEDIA_TYPE:
                default:
                    transferFormat = MessageEncodingFormat.Binary;
                    messageVersion = MessageVersion.Soap12;
                    break;
                case MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_PLUS_GZIP_MEDIA_TYPE:
                    transferFormat = MessageEncodingFormat.BinaryPlusGzip;
                    messageVersion = MessageVersion.Soap12;
                    break;
                case MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_PLUS_DEFLATE_MEDIA_TYPE:
                    transferFormat = MessageEncodingFormat.BinaryPlusDeflate;
                    messageVersion = MessageVersion.Soap12;
                    break;
                case MediaTypeConstants.APPLICATION_OCTET_STREAM_MEDIA_TYPE:
                    transferFormat = MessageEncodingFormat.Raw;
                    messageVersion = MessageVersion.None;
                    break;
            }
        }

        // We must parse out any action parameter because ContentType will barf parsing an action value of type xsd:anyUri that is not enclosed in escaped quotes (\").
        // see https://www.w3.org/TR/soap12-part2/#ActionFeature
        private static string ParseActionParameters(string contentType, out (string Key, string Value, bool Override) actionParameter)
        {
            if (String.IsNullOrWhiteSpace(contentType))
            {
                actionParameter = (null, null, false);

                return contentType;
            }

            String[] components = contentType.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            if (components.Length == 1)
            {
                actionParameter = (null, null, false);

                return contentType;
            }

            String[] parameters = components.Skip(1).ToArray();

            int actionIndex = parameters.FindIndex(p => p.TrimStart(' ').StartsWith(PARAMETER_KEY_SOAP_ACTION));

            if (actionIndex >= 0)
            {
                string[] actionComponents = parameters[actionIndex].Split('=');

                if (actionComponents.Length > 1)

                    actionParameter = (PARAMETER_KEY_SOAP_ACTION, actionComponents[1], true);

                else

                    actionParameter = (PARAMETER_KEY_SOAP_ACTION, null, false);
            }
            else

                actionParameter = (null, null, false);

            if (actionIndex >= 0)
            {
                return String.Join("; ", components[0].ToEnumerable().Concat(parameters.Skip(actionIndex).ToArray()));
            }

            return contentType;
        }

        public static implicit operator MediaTypeHeaderValue(MediaContentType messageContentType)
        {
            MediaTypeHeaderValue mediaTypeHeaderValue = new MediaTypeHeaderValue(messageContentType.MediaType) { CharSet = messageContentType.CharSet };

            messageContentType.Parameters.Cast<DictionaryEntry>().Where(p => p.Key.ToString() != PARAMETER_KEY_CHARSET).Aggregate(mediaTypeHeaderValue.Parameters, (p, h) => { p.Add(new NameValueHeaderValue(h.Key.ToString(), h.Value.ToString())); return p; });

            return mediaTypeHeaderValue;
        }

        public static implicit operator MediaContentType(MediaTypeHeaderValue mediaTypeHeaderValue)
        {
            return new MediaContentType(mediaTypeHeaderValue.MediaType, mediaTypeHeaderValue.CharSet, mediaTypeHeaderValue.Parameters);
        }

        public static implicit operator String(MediaContentType messageContentType)
        {
            if (messageContentType == null)

                return null;

            return messageContentType.ToMediaTypePlusParameters();
        }

        public static implicit operator MediaContentType(String contentType)
        {
            if (contentType == null)

                return null;

            return new MediaContentType(contentType);
        }
    }

    public static class MessageContentTypeExtensions
    {
        //TODO Create static MediaContentType for each transferFormat ... Get each using an extension method ...
        public static MediaContentType CreateMessageContentType(this MessageEncodingFormat transferFormat, out MessageVersion messageVersion)
        {
            switch (transferFormat)
            {
                case MessageEncodingFormat.Binary:
                case MessageEncodingFormat.Default:
                    messageVersion = MessageVersion.Soap12;
                    return new MediaContentType(MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_MEDIA_TYPE);
                case MessageEncodingFormat.BinaryPlusGzip:
                    messageVersion = MessageVersion.Soap12;
                    return new MediaContentType(MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_PLUS_GZIP_MEDIA_TYPE);
                case MessageEncodingFormat.BinaryPlusDeflate:
                    messageVersion = MessageVersion.Soap12;
                    return new MediaContentType(MediaTypeConstants.APPLICATION_SOAP_PLUS_BINARY_PLUS_DEFLATE_MEDIA_TYPE);
                case MessageEncodingFormat.Html:
                    messageVersion = MessageVersion.None;
                    return new MediaContentType(MediaTypeConstants.TEXT_HTML_MEDIA_TYPE);
                case MessageEncodingFormat.Json:
                    messageVersion = MessageVersion.None;
                    return new MediaContentType(MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE);
                case MessageEncodingFormat.Text:
                    messageVersion = MessageVersion.None;
                    return new MediaContentType(MediaTypeConstants.TEXT_PLAIN_MEDIA_TYPE);
                case MessageEncodingFormat.Raw:
                    messageVersion = MessageVersion.None;
                    return new MediaContentType(MediaTypeConstants.APPLICATION_OCTET_STREAM_MEDIA_TYPE);
                case MessageEncodingFormat.Soap11WSAddressing10:
                    messageVersion = MessageVersion.Soap11WSAddressing10;
                    return new MediaContentType(MediaTypeConstants.TEXT_XML_MEDIA_TYPE, (MediaContentType.PARAMETER_KEY_SOAP_ACTION, null, false));
                case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                    messageVersion = MessageVersion.Soap11WSAddressingAugust2004;
                    return new MediaContentType(MediaTypeConstants.TEXT_XML_MEDIA_TYPE, (MediaContentType.PARAMETER_KEY_SOAP_ACTION, null, false));
                case MessageEncodingFormat.Soap11:
                    messageVersion = MessageVersion.Soap11;
                    return new MediaContentType(MediaTypeConstants.TEXT_XML_MEDIA_TYPE);
                case MessageEncodingFormat.Soap12WSAddressing10:
                    messageVersion = MessageVersion.Soap12WSAddressing10;
                    return new MediaContentType(MediaTypeConstants.TEXT_XML_MEDIA_TYPE, (MediaContentType.PARAMETER_KEY_SOAP_ACTION, null, false));
                case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                    messageVersion = MessageVersion.Soap11WSAddressingAugust2004;
                    return new MediaContentType(MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE, (MediaContentType.PARAMETER_KEY_SOAP_ACTION, null, false));
                case MessageEncodingFormat.Soap12:
                    messageVersion = MessageVersion.Soap12;
                    return new MediaContentType(MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE);
                case MessageEncodingFormat.Xml:
                    messageVersion = MessageVersion.None;
                    return new MediaContentType(MediaTypeConstants.APPLICATION_XML_MEDIA_TYPE);
                default:
                    messageVersion = MessageVersion.None;
                    return new MediaContentType(MediaTypeConstants.ANY_MEDIA_TYPE);
            }
        }

        public static MediaContentType CreateMessageContentType(this MessageVersion messageVersion)
        {
            if (messageVersion == MessageVersion.Soap12WSAddressing10 ||
                messageVersion == MessageVersion.Default ||
                messageVersion == MessageVersion.Soap12WSAddressingAugust2004)
                return new MediaContentType(MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE, (MediaContentType.PARAMETER_KEY_SOAP_ACTION, null, false));
            if (messageVersion == MessageVersion.Soap12)
                return new MediaContentType(MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE);
            if (messageVersion == MessageVersion.Soap11WSAddressing10 ||
                messageVersion == MessageVersion.Soap11WSAddressingAugust2004)
                return new MediaContentType(MediaTypeConstants.TEXT_XML_MEDIA_TYPE, (MediaContentType.PARAMETER_KEY_SOAP_ACTION, null, false));
            if (messageVersion == MessageVersion.Soap11)
                return new MediaContentType(MediaTypeConstants.TEXT_XML_MEDIA_TYPE);
            return new MediaContentType(MediaTypeConstants.ANY_MEDIA_TYPE);
        }
    }
}
