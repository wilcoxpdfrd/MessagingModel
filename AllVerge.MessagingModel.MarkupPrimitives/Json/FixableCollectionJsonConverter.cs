using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace AllVerge.MessagingModel.MarkupPrimitives.Json
{
    using AllVerge.SystemPrimitives.Reflection;

    using Newtonsoft.Json;

    public class CollectionJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (typeof(Collection<T>).IsAssignableFrom(objectType));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<T> items = new List<T>();

            if (reader.TokenType != JsonToken.StartArray)

                throw reader.CreateException("Unexpected token type; expected '{0}', found '{1}'.", JsonToken.StartArray.ToString(), reader.TokenType);

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    items.Add(serializer.Deserialize<T>(reader));

                    if (reader.TokenType == JsonToken.EndObject)

                        continue;
                }

                break;
            }

            if (reader.TokenType != JsonToken.EndArray)

                throw reader.CreateException("Unexpected token type; expected '{0}', found '{1}'.", JsonToken.EndArray.ToString(), reader.TokenType);

            IEnumerable<ConstructorInfo> jsonCstrInfos = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(c => c.GetCustomAttributes(typeof(JsonConstructorAttribute), false).Count() > 0);

            ConstructorInfo jsonCstrInfo = jsonCstrInfos.FirstOrDefault(c =>
            {
                return c.GetParameters().Any(p => p.ParameterType.IsAssignableTo<IEnumerable<T>>());
            });

            if (jsonCstrInfo != null)
            {
                return jsonCstrInfo.Invoke(jsonCstrInfo.GetParameters().Select(p => p.ParameterType.IsAssignableTo<IEnumerable<T>>() ? items : p.HasDefaultValue ? p.DefaultValue : p.ParameterType.GetDefaultValue()).ToArray());
            }
            else
            {
                jsonCstrInfo = jsonCstrInfos.FirstOrDefault();

                Collection<T> collection;

                if (jsonCstrInfo != null)

                    collection = (Collection<T>)jsonCstrInfo.Invoke(jsonCstrInfo.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : p.ParameterType.GetDefaultValue()).ToArray());

                else

                    collection = (Collection<T>)Activator.CreateInstance(objectType);

                foreach (T item in items)

                    collection.Add(item);

                return collection;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Collection<T> collection = (Collection<T>)value;

            writer.WriteStartArray();

            foreach (T item in collection)
            {
                serializer.Serialize(writer, item);
            }

            writer.WriteEndArray();
        }
    }
}
