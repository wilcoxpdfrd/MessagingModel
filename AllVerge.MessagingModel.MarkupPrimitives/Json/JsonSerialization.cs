using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.MarkupPrimitives.Json
{
    using AllVerge.SystemPrimitives.Reflection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    
    using System.Runtime.Serialization.Json;

    /// <summary>
    /// Provides object serialization methods.
    /// </summary>
    public static class JsonSerialization
    {
        class WritablePropertiesOnlyResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);

                return props.Where(p => p.Writable).ToList();
            }
        }

        private static XmlWriterSettings OmitXmlDeclarationSettings = new XmlWriterSettings() { OmitXmlDeclaration = true };

        private static JsonSerializerSettings InitializeSettings(JsonConverter[] jsonConverters)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();

            foreach (JsonConverter jsonConverter in jsonConverters)

                settings.Converters.Add(jsonConverter);

            return settings;
        }

        private static void EnsureStandardSettings(ref JsonSerializerSettings settings)
        {
            if (settings == null)

                settings = new JsonSerializerSettings();

            if (settings.ContractResolver == null)

                settings.ContractResolver = new WritablePropertiesOnlyResolver();

            settings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;

            settings.Converters.Add(new JsonObjectPropertyConverter());
            settings.Converters.Add(new JsonTextPropertyConverter());
            settings.Converters.Add(new JsonStringEnumConverter() { XmlEnumAttributeNameText = true });
        }

        private static void ValidateMethodCannotBeUsedForTypeDecoratedWithContractAttribute(Type graphType, JsonConverter[] jsonConverters)
        {
            if (DataContractSerializationHelper.HasRegisteredContractAttribute(graphType) && jsonConverters.Length > 0)

                throw new ArgumentException(nameof(jsonConverters), $"{nameof(graphType)} is decorated with a Contract Attribute, which is not expected when providing an array of {nameof(JsonConverter)} ({nameof(jsonConverters)}).  Considering providing an array of {nameof(Type)} instead.");
        }

        private static void ValidateMethodCannotBeUsedForTypeNotDecoratedWithContractAttribute(Type graphType, Type[] knownTypes)
        {
            if (!DataContractSerializationHelper.HasRegisteredContractAttribute(graphType) && knownTypes.Length > 0)

                throw new ArgumentException(nameof(knownTypes), $"{nameof(graphType)} is not decorated with a Contract Attribute, which is expected when providing an array of {nameof(Type)} ({nameof(knownTypes)}).  Considering providing an array of {nameof(JsonConverter)} instead.");
        }

        /// <summary>
        /// Activates the type using the first contructor found decorated with <see cref="JsonConstructorAttribute"/>.  
        /// If no such contructor is found, activates the type using the parameterless contructor.
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public static Object ActivatePreferablyUsingJsonConstructor(this Type objectType)
        {
            IEnumerable<ConstructorInfo> jsonCstrInfos = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(c => c.GetCustomAttributes(typeof(JsonConstructorAttribute), false).Count() > 0);

            ConstructorInfo jsonCstrInfo = jsonCstrInfos.FirstOrDefault();

            if (jsonCstrInfo != null)

                return jsonCstrInfo.Invoke(jsonCstrInfo.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : p.ParameterType.GetDefaultValue()).ToArray());

            return Activator.CreateInstance(objectType);
        }

        /// <summary>
        /// Serializes <paramref name="graph"/> to a Json String.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <returns>The graph serialized to a string.</returns>
        public static String SerializeAsJsonString<T>(this T graph)
        {
            if (graph == null)

                return null;

            Type graphType = typeof(T);

            if (DataContractSerializationHelper.HasRegisteredContractAttribute(graphType))

                return graph.SerializeAsJsonString<T>(Array.Empty<Type>());

            else

                return graph.SerializeAsJsonString<T>(Array.Empty<JsonConverter>());
        }

        /// <summary>
        /// Serializes <paramref name="graph"/> to a Json String.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <returns>The graph serialized to a string.</returns>
        public static String SerializeAsJsonString<T>(this T graph, JsonConverter[] jsonConverters)
        {
            if (graph == null)

                return null;

            Type graphType = typeof(T);

            ValidateMethodCannotBeUsedForTypeDecoratedWithContractAttribute(graphType, jsonConverters);

            JsonSerializerSettings settings = InitializeSettings(jsonConverters);

            return graph.SerializeAsJsonString(settings);
        }

        /// <summary>
        /// Serializes <paramref name="graph"/> to a Json String.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="settings"></param>
        /// <param name="jsonConverters"></param>
        /// <returns>The graph serialized to a string.</returns>
        public static String SerializeAsJsonString<T>(this T graph, JsonSerializerSettings settings)
        {
            if (graph == null)

                return null;

            Type graphType = typeof(T);

            EnsureStandardSettings(ref settings);

            return JsonConvert.SerializeObject(graph, Formatting.Indented, settings);
        }

        /// <summary>
        /// Serializes <paramref name="graph"/> to a Json String.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="settings"></param>
        /// <param name="jsonConverters"></param>
        /// <returns>The graph serialized to a string.</returns>
        public static String SerializeAsJsonString<T>(this T graph, Type[] knownTypes)
        {
            if (graph == null)

                return null;

            Type graphType = typeof(T);

            ValidateMethodCannotBeUsedForTypeNotDecoratedWithContractAttribute(graphType, knownTypes);

            DataContractJsonSerializer serializer;

            if (knownTypes.Length > 0)
                serializer = new DataContractJsonSerializer(graphType, knownTypes);
            else
                serializer = new DataContractJsonSerializer(graphType);

            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter w = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                {
                    serializer.WriteObject(w, graph);

                    w.Flush();
                }

                sw.Flush();

                return sw.ToString();
            }
        }

        /// <summary>
        /// Serializes the <paramref name="graph"/> to a Json Stream.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <returns>The document element of the serialized object.</returns>
        public static Stream SerializeAsJsonStream<T>(this T graph)
        {
            Type graphType = typeof(T);

            if (DataContractSerializationHelper.HasRegisteredContractAttribute(graphType))

                return graph.SerializeAsJsonStream<T>(Array.Empty<Type>());

            else

                return graph.SerializeAsJsonStream<T>(Array.Empty<JsonConverter>());
        }

        /// <summary>
        /// Serializes the <paramref name="graph"/> to a Json Stream.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <returns>The document element of the serialized object.</returns>
        public static Stream SerializeAsJsonStream<T>(this T graph, JsonConverter[] jsonConverters)
        {
            MemoryStream ms = new MemoryStream();

            using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
            {
                SerializeAsJson(graph, sw, jsonConverters);

                sw.Flush();

                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }
        }

        /// <summary>
        /// Serializes the <paramref name="graph"/> to a Json Stream.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <returns>The document element of the serialized object.</returns>
        public static Stream SerializeAsJsonStream<T>(this T graph, Type[] knownTypes)
        {
            MemoryStream ms = new MemoryStream();

            using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
            {
                SerializeAsJson(graph, sw, knownTypes);

                sw.Flush();

                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }
        }

        /// <summary>
        /// Serializes an instance of <typeparamref name="T"/> from <paramref name="graph"/> to a <see cref="JToken"/>.
        /// </summary>
        /// <typeparam name="T">The type of the serialization source.</typeparam>
        /// <param name="graph">the serialization source</param>
        /// <param name="jsonConverters">A set of <see cref="JsonConverter"/>.</param>
        /// <returns>The JToken at the root of the serialization graph.</returns>
        public static JToken SerializeAsJson<T>(this T graph, JsonConverter[] jsonConverters)
        {
            ValidateMethodCannotBeUsedForTypeDecoratedWithContractAttribute(typeof(T), jsonConverters);

            if (jsonConverters.Length > 0)
            {
                JsonSerializer serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings { Converters = jsonConverters });

                return JToken.FromObject(graph, serializer);
            }

            return JToken.FromObject(graph);
        }

        /// <summary>
        /// Serializes the <paramref name="graph"/> to Json using a StreamWriter.  
        /// This method does not flush the writer (<paramref name="sw"/>)!
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="sw"></param>
        public static void SerializeAsJson<T>(this T graph, StreamWriter sw)
        {
            Type graphType = typeof(T);

            if (DataContractSerializationHelper.HasRegisteredContractAttribute(graphType))

                graph.SerializeAsJson<T>(sw, Array.Empty<Type>());

            else

                graph.SerializeAsJson<T>(sw, Array.Empty<JsonConverter>());
        }

        /// <summary>
        /// Serializes the <paramref name="graph"/> to Json using a StreamWriter.  
        /// This method does not flush the writer (<paramref name="sw"/>)!
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="sw"></param>
        public static void SerializeAsJson<T>(this T graph, StreamWriter sw, JsonConverter[] jsonConverters)
        {
            sw.Write(graph.SerializeAsJsonString(jsonConverters).ToCharArray());
        }

        /// <summary>
        /// Serializes the <paramref name="graph"/> to Json using a StreamWriter.  
        /// This method does not flush the writer (<paramref name="sw"/>)!
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="sw"></param>
        public static void SerializeAsJson<T>(this T graph, StreamWriter sw, Type[] knownTypes)
        {
            sw.Write(graph.SerializeAsJsonString(knownTypes).ToCharArray());
        }

        /// <summary>
        /// Deserializes the <paramref name="serialized"/> Json String to an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="serialized">The document element of the serialized object.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeJson<T>(this String serialized)
        {
            if (serialized == null)

                return default(T);

            Type serializedType = typeof(T);

            if (DataContractSerializationHelper.HasRegisteredContractAttribute(serializedType))

                return serialized.DeserializeJson<T>(Array.Empty<Type>());

            else

                return serialized.DeserializeJson<T>(Array.Empty<JsonConverter>());
        }

        /// <summary>
        /// Deserializes an instance of <typeparamref name="T"/> from the Json read by the <paramref name="reader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="reader">Reader initialzed with the serialized object.</param>
        /// <param name="jsonConverters">A set of <see cref="JsonConverter"/>.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeJson<T>(this XmlReader reader, params JsonConverter[] jsonConverters)
        {
            if (reader == null || (!(reader is IXmlJsonReaderInitializer)))

                return default(T);

            String json = reader.ReadOuterXml();

            return json.DeserializeJson<T>(jsonConverters);
        }
        
        /// <summary>
        /// Deserializes the <paramref name="serialized"/> Json String to an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="serialized">The document element of the serialized object.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeJson<T>(this String serialized, params JsonConverter[] jsonConverters)
        {
            Type serializedType = typeof(T);

            ValidateMethodCannotBeUsedForTypeDecoratedWithContractAttribute(serializedType, jsonConverters);

            JsonSerializerSettings settings = InitializeSettings(jsonConverters);

            return DeserializeJson<T>(serialized, settings);
        }

        private static T DeserializeJson<T>(string serialized, JsonSerializerSettings settings)
        {
            if (serialized == null)

                return default(T);

            EnsureStandardSettings(ref settings);

            return JsonConvert.DeserializeObject<T>(serialized, settings);
        }

        /// <summary>
        /// Deserializes the <paramref name="serialized"/> Json String to an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="serialized">The document element of the serialized object.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeJson<T>(this String serialized, Type[] knownTypes)
        {
            if (serialized == null)

                return default(T);

            Type serializedType = typeof(T);

            ValidateMethodCannotBeUsedForTypeNotDecoratedWithContractAttribute(serializedType, knownTypes);

            DataContractJsonSerializer serializer;

            if (knownTypes.Length > 0)
                serializer = new DataContractJsonSerializer(serializedType, knownTypes);
            else
                serializer = new DataContractJsonSerializer(serializedType);

            return (T)serializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(serialized)));
        }
    }
}
