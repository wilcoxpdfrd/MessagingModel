using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MarkupPrimitives.Json
{
    using AllVerge.MessagingModel.MarkupPrimitives.Xml;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class JsonStringEnumConverter : StringEnumConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStringEnumConverter" /> class.
        /// </summary>
        public JsonStringEnumConverter() : base()
        {
        }

        /// <summary>
		/// Gets or sets a value indicating whether the written enum text should use the <see cref="XmlEnumAttribute"/> name (if present).
		/// The default value is <c>false</c> (also, see remarks).
        /// </summary>
        /// <remarks>
        /// If set, this value takes precedence over <see cref="CamelCaseText"/> and <see cref="AllowIntegerValues"/>, 
        /// and the latter settings will be ignored.  However, the latter values will be respected if there are no
        /// <see cref="XmlEnumAttribute"/> present.
        /// </remarks>
        public bool XmlEnumAttributeNameText { get; set; }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (XmlEnumAttributeNameText)
            {
                if (value == null)
                {
                    writer.WriteNull();
                }
                else
                {
                    String enumName;

                    if (XmlFormatterExtensions.TryGetXmlEnumAttributeNameFromEnum(value, out enumName))

                        writer.WriteValue(enumName);

                    else

                        base.WriteJson(writer, value, serializer);
                }
            }
            else

                base.WriteJson(writer, value, serializer);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String && XmlEnumAttributeNameText)
            {
                if (XmlFormatterExtensions.TryGetEnumFromXmlEnumAttributeName(objectType, reader.Value, out Object @enum))

                    return @enum;

                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            else

                return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
