using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AllVerge.MessagingModel.Markup.Json
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class JsonLinqExtensions
    {
        /// <summary>
        /// Sets or adds <paramref name="valueToken"/> to <paramref name="token"/>, 
        /// depending respectively on whether <paramref name="token"/> is of type <see cref="JProperty"/> or <see cref="JObject"/>.
        /// </summary>
        /// <param name="token">A token of type <see cref="JProperty"/> or <see cref="JObject"/>.</param>
        /// <param name="valueToken">The value token.</param>
        public static void SetOrAddToken(this JToken token, JToken valueToken)
        {
            if (token is JProperty)
                (token as JProperty).Value = valueToken;
            if (token is JObject)
                (token as JObject).Add(valueToken);
        }

        /// <summary>
        /// Converts <paramref name="element"/> to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static Stream ToStream(this JToken element)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(element.ToString()));
        }

        public static JToken ToJToken(this StreamReader json)
        {
            return JToken.Parse(json.ReadToEnd());
        }

        /// <summary>
        /// Converts <paramref name="element"/> to a <see cref="JToken"/> (converting from an Xml based linq to Json based linq element).
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static JToken ToJToken(this XElement element)
        {
            using (XmlReader xmlReader = element.CreateReader())
            {
                XmlDocument xmlDoc = new XmlDocument();

                xmlDoc.Load(xmlReader);
                
                return JToken.Parse(JsonConvert.SerializeXmlNode(xmlDoc.DocumentElement));
            }
        }

        public static void WriteRootToken(this XmlWriter xmlWriter, JToken jToken)
        {
            foreach (JToken childToken in jToken.Children())
            {
                WriteToken(xmlWriter, childToken);
            }
        }

        /// <summary>
        /// Writes the <paramref name="jToken"/> using <paramref name="xmlWriter"/>.  The latter must be an implementation of <see cref="XmlDictionaryWriter"/> that writes Json.
        /// </summary>
        /// <param name="xmlWriter"></param>
        /// <param name="jToken"></param>
        public static void WriteToken(this XmlWriter xmlWriter, JToken jToken)
        {
            switch (jToken.Type)
            {
                case JTokenType.Property:

                    JProperty jProperty = (JProperty)jToken;

                    xmlWriter.WriteStartElement(jProperty.Name);

                    WriteToken(xmlWriter, jProperty.Value);

                    xmlWriter.WriteEndElement();

                    break;

                case JTokenType.Array:

                    xmlWriter.WriteAttributeString("type", "array");

                    foreach (JToken childToken in jToken.Children())
                    {
                        xmlWriter.WriteStartElement("item");

                        WriteToken(xmlWriter, childToken);

                        xmlWriter.WriteEndElement(); // item
                    }

                    break;

                case JTokenType.Object:

                    xmlWriter.WriteAttributeString("type", "object");

                    foreach (JToken childToken in jToken.Children())
                    {
                        WriteToken(xmlWriter, childToken);
                    }

                    break;

                case JTokenType.Integer:
                case JTokenType.Float:

                    xmlWriter.WriteAttributeString("type", "number");

                    xmlWriter.WriteString((jToken as JValue).Value.ToString());

                    break;

                case JTokenType.Boolean:

                    xmlWriter.WriteAttributeString("type", "boolean");

                    // https://stackoverflow.com/questions/491334/why-does-boolean-tostring-output-true-and-not-true

                    xmlWriter.WriteString((jToken as JValue).Value.ToString().ToLower());

                    break;

                case JTokenType.String:

                    xmlWriter.WriteAttributeString("type", "string");

                    xmlWriter.WriteString((jToken as JValue).Value.ToString());

                    break;

                case JTokenType.Bytes:

                    xmlWriter.WriteAttributeString("type", "string");

                    xmlWriter.WriteString((jToken as JValue).Value.ToString());

                    break;

                case JTokenType.Uri:

                    xmlWriter.WriteAttributeString("type", "string");

                    xmlWriter.WriteString((jToken as JValue).Value.ToString());

                    break;

                case JTokenType.Date:

                    xmlWriter.WriteAttributeString("type", "string");

                    xmlWriter.WriteString((jToken as JValue).Value.ToString());

                    break;

                case JTokenType.TimeSpan:

                    xmlWriter.WriteAttributeString("type", "string");

                    xmlWriter.WriteString((jToken as JValue).Value.ToString());

                    break;

                case JTokenType.Guid:

                    xmlWriter.WriteAttributeString("type", "string");

                    xmlWriter.WriteString((jToken as JValue).Value.ToString());

                    break;

                case JTokenType.Null:

                    xmlWriter.WriteAttributeString("type", "string");

                    xmlWriter.WriteString(null);

                    break;

                default:

                    break;
            }
        }
    }
}
