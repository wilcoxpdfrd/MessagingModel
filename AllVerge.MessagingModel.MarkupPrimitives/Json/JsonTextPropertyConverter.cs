using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AllVerge.MessagingModel.MarkupPrimitives.Json
{
    internal class JsonTextPropertyConverter
        : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttributes<JsonTextPropertyAttribute>().Count() > 0).Count() > 0;
        }

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            JRaw jr = new JRaw("\"" + value.ToString() + "\"");

            jr.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            JObject jObject = JObject.Load(reader);

            //if (existingValue == null)

            //    existingValue = Activator.CreateInstance(objectType);

            //foreach (PropertyInfo prop in objectType.GetProperties())
            //{
            //    if (prop.CanWrite && prop.CanRead && prop.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            //    {
            //        //prop.GetCustomAttribute<JsonConverterAttribute>() ?
            //        //prop.GetCustomAttribute<JsonExtensionDataAttribute>() ?
            //        //prop.GetCustomAttribute<JsonPropertyAttribute>() ?
            //        //prop.GetCustomAttribute<JsonRequiredAttribute>() ?

            //        JsonPropertyAttribute jsonPropertyAttribute =
            //            prop.GetCustomAttribute<JsonPropertyAttribute>();

            //        IEnumerable<JsonObjectPropertyAttribute> jsonPropertyObjectAttributes =
            //            prop.GetCustomAttributes<JsonObjectPropertyAttribute>();

            //        String propertyName;
            //        Type targetType;

            //        JsonObjectPropertyAttribute jsonPropertyObjectAttribute;

            //        if (jsonPropertyObjectAttributes.Count() > 0)
            //        {
            //            jsonPropertyObjectAttribute =
            //                jsonPropertyObjectAttributes.FirstOrDefault(a => jObject.Property(a.PropertyName) != null);

            //            if (jsonPropertyObjectAttribute != null)
            //            {
            //                propertyName = jsonPropertyObjectAttribute.PropertyName;

            //                targetType = jsonPropertyObjectAttribute.Type;
            //            }
            //            else
            //            {
            //                propertyName = null;

            //                targetType = null;
            //            }
            //        }
            //        else
            //        {
            //            if (jsonPropertyAttribute != null && !String.IsNullOrEmpty(jsonPropertyAttribute.PropertyName))

            //                propertyName = jsonPropertyAttribute.PropertyName;

            //            else

            //                propertyName = prop.Name;

            //            targetType = prop.PropertyType;
            //        }

            //        if (targetType != null)
            //        {
            //            object target;

            //            if (targetType.IsValueType || targetType == typeof(String))

            //                target = null;

            //            else

            //                target = Activator.CreateInstance(targetType);

            //            JToken propertyToken = jObject[propertyName];

            //            if (propertyToken != null)
            //            {
            //                if (target == null)

            //                    target = propertyToken.ToObject(targetType);

            //                else

            //                    serializer.Populate(propertyToken.CreateReader(), target);
            //            }
            //            else if (jsonPropertyAttribute != null && jsonPropertyAttribute.DefaultValueHandling == DefaultValueHandling.Populate)
            //            {
            //                DefaultValueAttribute defaultValueAttribute =
            //                    prop.GetCustomAttribute<DefaultValueAttribute>();

            //                if (defaultValueAttribute != null)

            //                    target = defaultValueAttribute.Value;
            //            }

            //            prop.SetValue(existingValue, target);
            //        }
            //    }
            //}

            return existingValue;
        }
    }
}
