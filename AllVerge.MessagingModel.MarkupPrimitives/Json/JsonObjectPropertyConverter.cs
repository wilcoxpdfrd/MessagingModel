using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace AllVerge.MessagingModel.MarkupPrimitives.Json
{
    using AllVerge.SystemPrimitives.Reflection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal class JsonObjectPropertyConverter
        : JsonConverter
    {
        private static readonly Object[] streamingContextArgs = new object[] { new StreamingContext(StreamingContextStates.All) };

        public override bool CanConvert(Type objectType)
        {
            if (typeof(ICollection).IsAssignableFrom(objectType) || typeof(IDictionary).IsAssignableFrom(objectType))

                return false;

            return objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttributes<JsonObjectPropertyAttribute>().Count() > 0).Count() > 0;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type type = value.GetType();

            MethodInfo onSerializingMethodInfo = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.GetCustomAttribute<OnSerializingAttribute>() != null).FirstOrDefault();

            if (onSerializingMethodInfo != null)

                onSerializingMethodInfo.Invoke(value, streamingContextArgs);

            JObject jo = new JObject();

            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (ShouldSerializeProperty(prop, value))
                {
                    //prop.GetCustomAttribute<JsonConverterAttribute>() ?
                    //prop.GetCustomAttribute<JsonExtensionDataAttribute>() ?
                    //prop.GetCustomAttribute<JsonPropertyAttribute>() ?
                    //prop.GetCustomAttribute<JsonRequiredAttribute>() ?

                    //int indexParameters = prop.GetIndexParameters().Length;

                    //if (indexParameters > 1)

                    //    throw new NotImplementedException($"Indexer with {indexParameters} parameters is not currently supported.");

                    //else if (indexParameters > 0)
                    //{
                    //    JArray ja = new JArray();

                    //    if (value is IDictionary)
                    //    {
                    //        foreach (Object key in (value as IDictionary).Keys)

                    //            ja.Add(JToken.FromObject(prop.GetValue(value, new object[] { key }), serializer));
                    //    }
                    //    else if (value is ICollection)
                    //    {
                    //        List<Object> propValues = new List<object>();

                    //        for (int index = 0; index < (value as ICollection).Count; index++)

                    //            ja.Add(JToken.FromObject(prop.GetValue(value, new object[] { index }), serializer));
                    //    }
                    //    else

                    //        throw new NotImplementedException($"Indexer on {value.GetType().Name} is not currently supported.");

                    //    ja.WriteTo(writer);
                    //}
                    //else
                    //{

                    object propVal = prop.GetValue(value, null);

                    if (propVal != null)
                    {
                        JsonObjectPropertyAttribute jsonPropertyObjectAttribute =
                            prop.GetCustomAttributes<JsonObjectPropertyAttribute>().FirstOrDefault(a => a.Type == propVal.GetType());

                        JsonPropertyAttribute jsonPropertyAttribute = prop.GetCustomAttribute<JsonPropertyAttribute>();

                        String propertyName;

                        if (jsonPropertyObjectAttribute != null && !String.IsNullOrEmpty(jsonPropertyObjectAttribute.PropertyName))

                            propertyName = jsonPropertyObjectAttribute.PropertyName;

                        else if (jsonPropertyAttribute != null && !String.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))

                            propertyName = jsonPropertyAttribute.PropertyName;

                        else

                            propertyName = prop.Name;

                        jo.Add(propertyName, JToken.FromObject(propVal, serializer));
                    }

                    //}
                }
            }

            if (onSerializingMethodInfo != null)
            {
                MethodInfo onSerializedMethodInfo = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.GetCustomAttribute<OnSerializedAttribute>() != null).FirstOrDefault();

                if (onSerializedMethodInfo != null)

                    onSerializedMethodInfo.Invoke(value, streamingContextArgs);
            }

            if (jo.Count > 0)

                jo.WriteTo(writer);
        }

        private bool ShouldSerializeProperty(PropertyInfo prop, object declaringValue)
        {
            if (prop.CanWrite && prop.CanRead && prop.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            {
                MethodInfo shouldSerializeMethodInfo = prop.DeclaringType.GetMethod("ShouldSerialize" + prop.Name, Type.EmptyTypes);

                if (shouldSerializeMethodInfo == null || shouldSerializeMethodInfo.ReturnType != typeof(bool))

                    return true;

                else

                    return (bool)shouldSerializeMethodInfo.Invoke(declaringValue, Type.EmptyTypes);
            }

            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            if (existingValue == null)

                existingValue = objectType.ActivatePreferablyUsingJsonConstructor();

            foreach (PropertyInfo prop in objectType.GetProperties())
            {
                if (prop.CanWrite && prop.CanRead && prop.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                {
                    //prop.GetCustomAttribute<JsonConverterAttribute>() ?
                    //prop.GetCustomAttribute<JsonExtensionDataAttribute>() ?
                    //prop.GetCustomAttribute<JsonPropertyAttribute>() ?
                    //prop.GetCustomAttribute<JsonRequiredAttribute>() ?

                    JsonPropertyAttribute jsonPropertyAttribute =
                        prop.GetCustomAttribute<JsonPropertyAttribute>();

                    IEnumerable<JsonObjectPropertyAttribute> jsonPropertyObjectAttributes =
                        prop.GetCustomAttributes<JsonObjectPropertyAttribute>();

                    String propertyName;
                    Type targetType;

                    JsonObjectPropertyAttribute jsonPropertyObjectAttribute;

                    if (jsonPropertyObjectAttributes.Count() > 0)
                    {
                        jsonPropertyObjectAttribute =
                            jsonPropertyObjectAttributes.FirstOrDefault(a => jObject.Property(a.PropertyName) != null);

                        if (jsonPropertyObjectAttribute != null)
                        {
                            propertyName = jsonPropertyObjectAttribute.PropertyName;

                            targetType = jsonPropertyObjectAttribute.Type;
                        }
                        else
                        {
                            propertyName = null;

                            targetType = null;
                        }
                    }
                    else
                    {
                        if (jsonPropertyAttribute != null && !String.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))

                            propertyName = jsonPropertyAttribute.PropertyName;

                        else

                            propertyName = prop.Name;

                        targetType = prop.PropertyType;
                    }

                    if (targetType != null)
                    {
                        object target;
                        bool targetSet = true;

                        if (targetType.IsValueType || targetType == typeof(String))

                            target = null;

                        else
                        {
                            ConstructorInfo cstrInfo = targetType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(c => c.GetCustomAttribute<JsonConstructorAttribute>() != null);

                            if (cstrInfo != null)

                                target = cstrInfo.Invoke(cstrInfo.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : p.ParameterType.GetDefaultValue() ).ToArray());

                            else

                                target = Activator.CreateInstance(targetType);
                        }

                        JToken propertyToken = jObject[propertyName];

                        if (propertyToken != null)
                        {
                            if (target == null)

                                target = propertyToken.ToObject(targetType);

                            else
                            {
                                switch (propertyToken.Type)
                                {
                                    case JTokenType.Object:
                                    case JTokenType.Array:

                                        JsonConverterAttribute targetTypeConverterAttribute =
                                            targetType.GetCustomAttribute<JsonConverterAttribute>();

                                        if (targetTypeConverterAttribute == null)
                                        {
                                            serializer.Populate(propertyToken.CreateReader(), target);
                                        }
                                        else
                                        {
                                            JsonConverter targetTypeConverter =
                                                (JsonConverter)Activator.CreateInstance(targetTypeConverterAttribute.ConverterType, targetTypeConverterAttribute.ConverterParameters);

                                            target = targetTypeConverter.ReadJson(propertyToken.CreateReader(), targetType, null, serializer);
                                        }

                                        break;

                                    default:

                                        PropertyInfo targetPropertyInfo = targetType.GetProperties().FirstOrDefault(p => p.GetCustomAttributes().Any(a => a is JsonTextPropertyAttribute));

                                        if (targetPropertyInfo != null)
                                        {
                                            targetPropertyInfo.SetValue(target, propertyToken.Value<String>());
                                        }

                                        break;
                                }
                            }
                        }
                        else if (jsonPropertyAttribute != null && jsonPropertyAttribute.DefaultValueHandling == DefaultValueHandling.Populate)
                        {
                            DefaultValueAttribute defaultValueAttribute =
                                prop.GetCustomAttribute<DefaultValueAttribute>();

                            if (defaultValueAttribute != null)

                                target = defaultValueAttribute.Value;

                            else

                                targetSet = false;
                        }
                        else

                            targetSet = false;

                        if (targetSet)

                            prop.SetValue(existingValue, target);
                    }
                }
            }

            return existingValue;
        }
    }
}
