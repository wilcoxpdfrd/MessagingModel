using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;

namespace AllVerge.MessagingModel.MarkupPrimitives.Xml
{
    using AllVerge.MessagingModel.MarkupPrimitives;
    using AllVerge.MessagingModel.MarkupPrimitives.Xml.Serialization;

    using AllVerge.SystemPrimitives.Reflection;
    using AllVerge.SystemPrimitives.Collections;
    using Newtonsoft.Json.Serialization;
    using AllVerge.SystemPrimitives.Diagnostics;

    /// <summary>
    /// Provides object serialization methods.
    /// </summary>
    public static class XmlSerialization
    {
        static Type TypeOfString = typeof(string);
        static Type TypeOfIXmlSerializable = typeof(IXmlSerializable);

        private static ConcurrentDictionary<int, XmlSerializer> xmlSerializerMap = new ConcurrentDictionary<int, XmlSerializer>();

        public static readonly XmlSerializerNamespaces EmptyNSMap = new XmlSerializerNamespaces().AddEmptyNsToMap();

        private static XmlWriterSettings OmitXmlDeclarationSettings = new XmlWriterSettings() { OmitXmlDeclaration = true };

        /// <summary>
        /// Adds an empty namespace to the map.
        /// </summary>
        /// <param name="namespaceMaps"></param>
        /// <returns>The map with the empty namespace added.</returns>
        /// <seealso cref="https://stackoverflow.com/questions/258960/how-to-serialize-an-object-to-xml-without-getting-xmlns"/>
        public static XmlSerializerNamespaces AddEmptyNsToMap(this XmlSerializerNamespaces namespaceMaps)
        {
            namespaceMaps.Add("", "");

            return namespaceMaps;
        }

        public static Func<T, XmlAttributeOverrides> AddXmlRootAttributeFunc<T>(Func<T, String> rootNameFunc)
        {
            return t =>
            {
                XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();

                XmlAttributes xmlAttributes = new XmlAttributes();

                xmlAttributes.XmlRoot = new XmlRootAttribute(rootNameFunc.Invoke(t));

                xmlAttributeOverrides.Add(typeof(T), xmlAttributes);

                return xmlAttributeOverrides;
            };
        }

        public static Func<String, XmlAttributeOverrides> AddXmlRootAndAttributeAttributesFunc<T>(String attributeName, Func<String, Object> attributeValueFunc)
        {
            return s =>
            {
                XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();

                XmlAttributes xmlAttributes = new XmlAttributes();

                xmlAttributes.XmlRoot = new XmlRootAttribute(s);

                xmlAttributeOverrides.Add(typeof(T), xmlAttributes);

                xmlAttributes = new XmlAttributes();

                xmlAttributes.XmlAttribute = new XmlAttributeAttribute(attributeName);

                xmlAttributes.XmlDefaultValue = attributeValueFunc(s);

                xmlAttributeOverrides.Add(typeof(T), attributeName, xmlAttributes);

                return xmlAttributeOverrides;
            };
        }

        /// <summary>
        /// Returns an <see cref="TryGetXmlAttributeOverridesProvider"/> associated with the <paramref name="type"/> 
        /// via an attached <see cref="XmlAttributeOverridesProviderAttribute"/> and returns true, or false 
        /// if there is no attached accessor (see remarks).
        /// </summary>
        /// <param name="type"></param>
        /// <param name="xmlAttributeOverridesProvider"></param>
        /// <returns></returns>
        /// <remarks>
        /// Any modifications to the provider returned via this method will persist statically; 
        /// each subsequent call to this method will return the statically modified provider.
        /// </remarks>
        public static bool TryGetXmlAttributeOverridesProvider(this Type type, out XmlAttributeOverridesProvider xmlAttributeOverridesProvider)
        {
            if (type.TryGetXmlAttributeOverridesProviderObject(out Object obj))

                xmlAttributeOverridesProvider = (XmlAttributeOverridesProvider)obj;

            else

                xmlAttributeOverridesProvider = null;

            return xmlAttributeOverridesProvider != null;
        }

        /// <summary>
        /// Returns an <see cref="XmlAttributeOverrides"/> associated with the <paramref name="type"/> via 
        /// an attached <see cref="XmlAttributeOverridesProviderAttribute"/> and returns true, or false 
        /// if there is no attached accessor (see remarks).
        /// </summary>
        /// <param name="type"></param>
        /// <param name="xmlAttributeOverrides"></param>
        /// <returns></returns>
        /// <remarks>This method returns an object that is a clone of the object returned by 
        /// <see cref="TryGetXmlAttributeOverridesProvider(Type, out XmlAttributeOverridesProvider)"/>; 
        /// each call to this method will return an object that reflects all static changes to the provider, 
        /// but the returned object can be modified without changing the underlying provider.</remarks>
        public static bool TryGetXmlAttributeOverrides(this Type type, out XmlAttributeOverrides xmlAttributeOverrides)
        {
            if (type.TryGetXmlAttributeOverridesProviderObject(out Object obj))

                xmlAttributeOverrides = (XmlAttributeOverrides)(XmlAttributeOverridesProvider)obj;

            else

                xmlAttributeOverrides = null;

            return xmlAttributeOverrides != null;
        }

        private static bool TryGetXmlAttributeOverridesProviderObject(this Type type, out object obj)
        {
            XmlAttributeOverridesProviderAttribute xmlAttributeOverridesAccessorAttribute = type.GetCustomAttribute<XmlAttributeOverridesProviderAttribute>();

            if (xmlAttributeOverridesAccessorAttribute != null)
            {
                string methodName = xmlAttributeOverridesAccessorAttribute.MethodName;

                // ToDo: change error messages to reflect XmlAttributeOverridesAccessor ...

                if (methodName == null)
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeNoData"), type.FullName)));
                }

                if (methodName.Length == 0)
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeEmptyString"), type.FullName)));
                }

                MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeUnknownMethod"), method, type.FullName)));
                }

                if (!typeof(XmlAttributeOverridesProvider).IsAssignableFrom(method.ReturnType))
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeReturnType"), type.FullName, method)));
                }

                obj = method.Invoke(null, Array.Empty<object>());

                if (obj == null)
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeMethodNull"), type.FullName)));
                }
            }
            else

                obj = null;

            return obj != null;
        }

        /// <summary>
        /// Serializes <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="knownTypes">A list of extra types that <paramref name="graph"/> is dependent on.</param>
        /// <returns>The document element of the serialized object.</returns>
        public static XmlElement Serialize(this Object graph, params Type[] knownTypes)
        {
            if (graph == null)

                return null;

            Type graphType = graph.GetType();

            if (DataContractSerializationHelper.HasRegisteredContractAttribute(graphType))
            {
                DataContractSerializer serializer;

                if (knownTypes.Length > 0)
                    serializer = new DataContractSerializer(graphType, knownTypes);
                else
                    serializer = new DataContractSerializer(graphType);

                using (StringWriter sw = new StringWriter())
                {
                    using (XmlWriter w = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                    {
                        serializer.WriteObject(w, graph);

                        w.Flush();

                        return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                    }
                }
            }
            else if (graphType.GetConstructor(Type.EmptyTypes) != null)
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlWriter writer = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                    {
                        GetXmlSerializer(graphType, null, knownTypes).Serialize(writer, graph);

                        return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                    }
                }
            }

            if (graphType.IsArray)
            {
                return SerializeArray(graph, graphType, (XmlSerializerNamespaces)null, null, knownTypes);
            }

            if (graph is XmlDocument)
                return (graph as XmlDocument).DocumentElement;

            XmlElement serialized = graph.TryCast<XmlElement>();

            if (serialized != null)
                return serialized;

            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter w = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                {
                    w.WriteStartElement(graphType.Name, graphType.Namespace.Replace('.', '/'));

                    w.WriteString(graph.ToString());

                    w.WriteEndElement();

                    w.Flush();

                    return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                }
            }
        }

        /// <summary>
        /// Serializes <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="overrides"><see cref="XmlAttributeOverrides"/> with which to parameterize xml serialization of <paramref name="graph"/>.</param>
        /// <returns>The document element of the serialized object.</returns>
        public static XmlElement Serialize(this Object graph, XmlAttributeOverrides overrides)
        {
            if (graph == null)

                return null;

            if (graph is XmlDocument)

                return (graph as XmlDocument).DocumentElement;

            XmlElement serialized = graph.TryCast<XmlElement>();

            if (serialized != null)

                return serialized;

            Type graphType = graph.GetType();

            if (graphType.IsArray)
            {
                return SerializeArray(graph, graphType, (XmlSerializerNamespaces)null, overrides);
            }

            if (graphType.GetConstructor(Type.EmptyTypes) != null)
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlWriter w = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                    {
                        GetXmlSerializer(graphType, overrides).Serialize(w, graph);

                        return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                    }
                }
            }

            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter w = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                {
                    w.WriteStartElement(graphType.Name, graphType.Namespace.Replace('.', '/'));

                    w.WriteString(graph.ToString());

                    w.WriteEndElement();

                    w.Flush();

                    return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                }
            }
        }

        /// <summary>
        /// Serializes <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="namespaceMaps"><see cref="XmlSerializerNamespaces"/> with which to produce namespace prefixes during serialization of <paramref name="graph"/>.</param>
        /// <returns>The document element of the serialized object.</returns>
        public static XmlElement Serialize(this Object graph, XmlSerializerNamespaces namespaceMaps)
        {
            if (graph == null)

                return null;

            if (graph is XmlDocument)

                return (graph as XmlDocument).DocumentElement;

            XmlElement serialized = graph.TryCast<XmlElement>();

            if (serialized != null)

                return serialized;

            Type graphType = graph.GetType();

            if (graphType.IsArray)
            {
                return SerializeArray(graph, graphType, namespaceMaps, null);
            }

            if (graphType.GetConstructor(Type.EmptyTypes) != null)
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlWriter w = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                    {
                        GetXmlSerializer(graphType, null).Serialize(w, graph, namespaceMaps);

                        return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                    }
                }
            }

            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter w = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                {
                    w.WriteStartElement(graphType.Name, graphType.Namespace.Replace('.', '/'));

                    w.WriteString(graph.ToString());

                    w.WriteEndElement();

                    w.Flush();

                    return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                }
            }
        }

        /// <summary>
        /// Serializes <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The object to serialize.</param>
        /// <param name="overrides"><see cref="XmlAttributeOverrides"/> with which to parameterize xml serialization of <paramref name="graph"/>.</param>
        /// <param name="namespaceMaps"><see cref="XmlSerializerNamespaces"/> with which to produce namespace prefixes during serialization of <paramref name="graph"/>.</param>
        /// <returns>The document element of the serialized object.</returns>
        public static XmlElement Serialize(this Object graph, XmlAttributeOverrides overrides, XmlSerializerNamespaces namespaceMaps)
        {
            if (graph == null)

                return null;

            if (graph is XmlDocument)

                return (graph as XmlDocument).DocumentElement;

            XmlElement serialized = graph.TryCast<XmlElement>();

            if (serialized != null)

                return serialized;

            Type graphType = graph.GetType();

            if (graphType.IsArray)
            {
                return SerializeArray(graph, graphType, namespaceMaps, overrides);
            }

            if (graphType.GetConstructor(Type.EmptyTypes) != null)
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlWriter w = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                    {
                        GetXmlSerializer(graphType, overrides).Serialize(w, graph, namespaceMaps);

                        return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                    }
                }
            }

            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter w = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                {
                    w.WriteStartElement(graphType.Name, graphType.Namespace.Replace('.', '/'));

                    w.WriteString(graph.ToString());

                    w.WriteEndElement();

                    w.Flush();

                    return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                }
            }
        }

        public static void Serialize(this Object graph, XmlWriter writer, XmlAttributeOverrides overrides, params Type[] knownTypes)
        {
            Type graphType = graph.GetType();

            if (graphType.IsArray)
            {
                GetArrayXmlRootAttribute(graphType, overrides, out XmlRootAttribute arrayXmlRootAttribute, out Type arrayItemType);

                SerializeArray(graph, arrayItemType, writer, arrayXmlRootAttribute, overrides, knownTypes);
            }
            else if (graphType.GetConstructor(Type.EmptyTypes) != null)

                GetXmlSerializer(graphType, overrides, knownTypes).Serialize(writer, graph);

            else
            {
                if (overrides.TryGetXmlRootAttribute(graphType, out XmlRootAttribute xmlRootAttribute))
                {
                    writer.WriteStartElement(xmlRootAttribute.ElementName, xmlRootAttribute.Namespace);

                    writer.WriteString(graph.ToString());

                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteStartElement(graphType.Name, graphType.Namespace.Replace('.', '/'));

                    writer.WriteString(graph.ToString());

                    writer.WriteEndElement();
                }
            }
        }

        private static XmlElement SerializeArray(object array, Type graphType, XmlSerializerNamespaces namespaceMaps, XmlAttributeOverrides overrides, params Type[] knownTypes)
        {
            GetArrayXmlRootAttribute(graphType, overrides, out XmlRootAttribute arrayXmlRootAttribute, out Type arrayItemType);

            using (StringWriter sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, OmitXmlDeclarationSettings))
                {
                    SerializeArray(array, arrayItemType, writer, arrayXmlRootAttribute, overrides);

                    writer.Flush();

                    return (XmlElement)new XmlDocument().ReadNode(XElement.Parse(sw.ToString()).CreateReader());
                }
            }
        }

        private static void GetArrayXmlRootAttribute(Type graphType, XmlAttributeOverrides overrides, out XmlRootAttribute xmlArrayRootAttribute, out Type arrayItemType)
        {
            if (overrides != null)

                xmlArrayRootAttribute = overrides[graphType]?.XmlRoot;

            else

                xmlArrayRootAttribute = null;

            arrayItemType = graphType.GetElementType();

            if (xmlArrayRootAttribute == null)

                xmlArrayRootAttribute = new XmlRootAttribute($"ArrayOf{arrayItemType.Name}");
        }

        private static void SerializeArray(object array, Type arrayItemType, XmlWriter writer, XmlRootAttribute arrayXmlRootAttribute, XmlAttributeOverrides overrides, params Type[] knownTypes)
        {
            if (!String.IsNullOrWhiteSpace(arrayXmlRootAttribute.Namespace))
            
                writer.WriteStartElement(arrayXmlRootAttribute.ElementName, arrayXmlRootAttribute.Namespace);

            else

                writer.WriteStartElement(arrayXmlRootAttribute.ElementName);

            foreach (Object arrayItem in (Array)array)

                arrayItem.Serialize(writer, overrides, knownTypes);

            writer.WriteEndElement();
        }

        /// <summary>
        /// Deserializes <paramref name="serialized"/> to  <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="serialized">The document element of the serialized object.</param>
        /// <param name="knownTypes">A list of extra types the target is dependent on.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(this XmlElement serialized, params Type[] knownTypes)
        {
            if (serialized == null)

                return default(T);

            Type serializedType = typeof(T);

            using (XmlReader reader = serialized.CreateNavigator().ReadSubtree())
            {
                return (T)reader.Deserialize(serializedType, null, knownTypes);
            }
        }

        /// <summary>
        /// Deserializes <paramref name="serialized"/> to  <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="reader">The <see cref="XmlReader"/> containing the graph of the serialized object.</param>
        /// <param name="overrides"><see cref="XmlAttributeOverrides"/> with which to parameterize xml de-serialization of <paramref name="serialized"/>.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(this XmlElement serialized, XmlAttributeOverrides overrides)
        {
            if (serialized == null)

                return default(T);

            Type serializedType = typeof(T);

            using (XmlReader reader = serialized.CreateNavigator().ReadSubtree())
            {
                return (T)reader.Deserialize(serializedType, overrides, Type.EmptyTypes);
            }
        }

        /// <summary>
        /// Deserializes <paramref name="serialized"/> to  <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="reader">The <see cref="XmlReader"/> containing the graph of the serialized object.</param>
        /// <param name="overrides"><see cref="XmlAttributeOverrides"/> with which to parameterize xml de-serialization of <paramref name="serialized"/>.</param>
        /// <param name="knownTypes">A list of extra types the target is dependent on.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(this XmlElement serialized, XmlAttributeOverrides overrides, params Type[] knownTypes)
        {
            if (serialized == null)

                return default(T);

            Type serializedType = typeof(T);

            using (XmlReader reader = serialized.CreateNavigator().ReadSubtree())
            {
                return (T)reader.Deserialize(serializedType, overrides, knownTypes);
            }
        }
        
        /// <summary>
        /// Deserializes an instance of <typeparamref name="T"/> from the Xml read by the <paramref name="reader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="reader">Reader initialized to the document element of the serialized object.</param>
        /// <param name="knownTypes">A list of extra types the target is dependent on.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(this XmlReader reader, params Type[] knownTypes)
        {
            if (reader == null)

                return default(T);

            Type serializedType = typeof(T);

            return (T)reader.Deserialize(serializedType, null, knownTypes);
        }

        /// <summary>
        /// Deserializes an instance of <typeparamref name="T"/> from the Xml read by the <paramref name="reader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="reader">The <see cref="XmlReader"/> containing the graph of the serialized object.</param>
        /// <param name="overrides"><see cref="XmlAttributeOverrides"/> with which to parameterize xml de-serialization of <paramref name="reader"/>.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(this XmlReader reader, XmlAttributeOverrides overrides)
        {
            Type serializedType = typeof(T);

            return (T)reader.Deserialize(serializedType, overrides, Type.EmptyTypes);
        }

        /// <summary>
        /// Deserializes an instance of <typeparamref name="T"/> from the Xml read by the <paramref name="reader"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="reader">The <see cref="XmlReader"/> containing the graph of the serialized object.</param>
        /// <param name="overrides"><see cref="XmlAttributeOverrides"/> with which to parameterize xml de-serialization of <paramref name="reader"/>.</param>
        /// <param name="knownTypes">A list of extra types the target is dependent on.</param>
        /// <returns>The deserialized object.</returns>
        public static T Deserialize<T>(this XmlReader reader, XmlAttributeOverrides overrides, params Type[] knownTypes)
        {
            Type serializedType = typeof(T);

            return (T)reader.Deserialize(serializedType, overrides, knownTypes);
        }

        /// <summary>
        /// Deserializes an instance of <paramref name="serializedType"/>, using the <paramref name="reader"/>.
        /// </summary>
        /// <param name="serializedType">The type of the serialized object.</param>
        /// <param name="knownTypes">A list of extra types the <paramref name="serializedType"/> is dependent on.</param>
        /// <returns>The deserialized object.</returns>
        public static Object Deserialize(this XmlReader reader, Type serializedType, params Type[] knownTypes)
        {
            return reader.Deserialize(serializedType, null, knownTypes);
        }

        /// <summary>
        /// Deserializes an instance of <paramref name="serializedType"/>, using the <paramref name="reader"/>.
        /// </summary>
        /// <param name="serializedType">The type of the serialized object.</param>
        /// <param name="overrides"><see cref="XmlAttributeOverrides"/> with which to parameterize xml de-serialization of <paramref name="reader"/>.</param>
        /// <param name="knownTypes">A list of extra types the <paramref name="serializedType"/> is dependent on.</param>
        /// <returns>The deserialized object.</returns>
        public static Object Deserialize(this XmlReader reader, Type serializedType, XmlAttributeOverrides overrides, params Type[] knownTypes)
        {
            if (reader == null)

                return null;

            if (serializedType.IsArray)
            {
                return DeserializeArray(reader, serializedType, overrides, knownTypes);
            }

            if (DataContractSerializationHelper.HasRegisteredContractAttribute(serializedType))
            {
                DataContractSerializer serializer;

                if (knownTypes.Length > 0)
                    serializer = new DataContractSerializer(serializedType, knownTypes);
                else
                    serializer = new DataContractSerializer(serializedType);

                return serializer.ReadObject(reader);
            }

            if (serializedType.IsValueType || serializedType == TypeOfString)
            {
                return reader.ReadElementContentAs(serializedType, new XmlNamespaceManager(reader.NameTable));
            }
            
            if (TypeOfIXmlSerializable.IsAssignableFrom(serializedType) && (overrides != null || knownTypes.Length > 0))
            {
                using (XmlReader knownTypesReader = new KnownTypesXmlReader(reader, overrides, knownTypes))
                {
                    return GetXmlSerializer(serializedType, overrides, knownTypes).Deserialize(knownTypesReader);
                }
            }

            return GetXmlSerializer(serializedType, overrides, knownTypes).Deserialize(reader);
        }

        private static Object DeserializeArray(XmlReader reader, Type serializedType, XmlAttributeOverrides overrides, params Type[] knownTypes)
        {
            Type arrayItemType = serializedType.GetElementType();

            String arrayElementName = $"ArrayOf{arrayItemType.Name}";

            List<Object> items = new List<Object>();

            if (reader.NodeType == XmlNodeType.None)

                reader.Read();

            bool flag = false;

            if (reader.IsStartElement(arrayElementName))

                flag = reader.Read();

            XmlAttributes arrayItemAttributes = overrides[arrayItemType];

            String arrayItemName;

            if (arrayItemAttributes != null && arrayItemAttributes.XmlRoot != null)

                arrayItemName = arrayItemAttributes.XmlRoot.ElementName;

            else

                arrayItemName = arrayItemType.Name;

            while (reader.IsStartElement(arrayItemName))

                items.Add(reader.Deserialize(arrayItemType, overrides, knownTypes));

            if (flag)

                reader.ReadEndElement();

            return (Object)items.ToArrayOfElementType(arrayItemType);
        }

        private static XmlSerializer GetXmlSerializer(Type serializedType, XmlAttributeOverrides overrides, params Type[] knownTypes)
        {
            XmlSerializer serializer;

            if (serializedType == null)

                throw new ArgumentNullException(nameof(serializedType));

            if (serializedType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new ArgumentException($"{serializedType.FullName} does not have a parameterless constructor.", nameof(serializedType));
            }

            if (serializedType.TryGetXmlAttributeOverrides(out XmlAttributeOverrides serializedTypeOverrides))
            {
                if (overrides == null)

                    overrides = new XmlAttributeOverridesProvider(serializedTypeOverrides);

                else

                    overrides = new XmlAttributeOverridesProvider(overrides, serializedTypeOverrides);
            }

            // Calculate hashcode for serializedType + external parameters

            int serializerHashCode = 17;

            if (overrides != null)

                serializerHashCode = serializerHashCode * 23 + overrides.GetExHashCode();

            knownTypes = serializedType.GetAllKnownTypes(knownTypes, overrides?.GetTypeAttributes(serializedType));

            serializerHashCode = knownTypes.OrderBy(t => t.FullName).Aggregate(serializerHashCode * serializedType.FullName.GetHashCode(), (hash, s) => hash * 23 + s.GetHashCode());

            if (!xmlSerializerMap.ContainsKey(serializerHashCode))
            {
                if (knownTypes.Length > 0)
                {
                    if (overrides != null)

                        serializer = new XmlSerializer(serializedType, overrides, knownTypes, null, null);

                    else

                        serializer = new XmlSerializer(serializedType, knownTypes);
                }
                else
                {
                    if (overrides != null)

                        serializer = new XmlSerializer(serializedType, overrides);

                    else

                        serializer = new XmlSerializer(serializedType);
                }

                xmlSerializerMap.TryAdd(serializerHashCode, serializer);
            }
            else

                serializer = xmlSerializerMap[serializerHashCode];

            return serializer;
        }

        private static Type[] GetAllKnownTypes(this Type type, Type[] knownTypes, Dictionary<string, XmlAttributes> typeAttributes)
        {
            List<Type> knownTypeList = new List<Type>(knownTypes);

            IEnumerable<KnownTypeAttribute> knownTypeAttributes = type.GetCustomAttributes(typeof(KnownTypeAttribute), false).OfType<KnownTypeAttribute>();

            foreach (KnownTypeAttribute knownTypeAttribute in knownTypeAttributes)
            {
                if (knownTypeAttribute.Type != null)
                {
                    knownTypeList.Add(knownTypeAttribute.Type);

                    continue;
                }

                string methodName = knownTypeAttribute.MethodName;

                if (methodName == null)
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeNoData"), type.FullName)));
                }

                if (methodName.Length == 0)
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeEmptyString"), type.FullName)));
                }

                MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeUnknownMethod"), method, type.FullName)));
                }

                if (!typeof(IEnumerable<Type>).IsAssignableFrom(method.ReturnType))
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeReturnType"), type.FullName, method)));
                }

                object obj = method.Invoke(null, Array.Empty<object>());

                if (obj == null)
                {
                    throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeMethodNull"), type.FullName)));
                }

                foreach (Type item in (IEnumerable<Type>)obj)
                {
                    if (item == null)
                    {
                        throw DiagnosticHelper.ExceptionHelper.ThrowHelperError(new InvalidDataContractException(String.Format(DataContractSerializationHelper.GetResourceString("KnownTypeAttributeValidMethodTypes"), type.FullName)));
                    }

                    knownTypeList.Add(item);
                }
            }

            if (typeAttributes != null)
            {
                foreach (String member in typeAttributes.Keys)
                {
                    XmlAttributes attributes = typeAttributes[member];

                    if (attributes.XmlArrayItems != null)
                    {
                        knownTypeList.AddRange(
                            attributes.XmlArrayItems.OfType<XmlArrayItemAttribute>().Select(i => i.Type).Where(t => !t.IsPrimitive && t != TypeOfString));
                    }

                    if (attributes.XmlElements != null)
                    {
                        knownTypeList.AddRange(
                            attributes.XmlElements.OfType<XmlElementAttribute>().Select(i => i.Type).Where(t => !t.IsPrimitive && t != TypeOfString));
                    }

                    if (attributes.XmlAttribute != null)
                    {
                        Type attributeType = attributes.XmlAttribute.Type;

                        if (!attributeType.IsPrimitive && attributeType != TypeOfString)

                            knownTypeList.Add(attributes.XmlAttribute.Type);
                    }
                }
            }

            return knownTypeList.ToArray();
        }
    }
}
