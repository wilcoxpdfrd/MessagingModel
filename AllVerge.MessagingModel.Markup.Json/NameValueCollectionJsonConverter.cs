using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace AllVerge.MessagingModel.Markup.Json
{
    public class NameValueCollectionJsonConverter : JsonConverter<NameValueCollection>
    {
        public override NameValueCollection ReadJson(JsonReader reader, Type objectType, NameValueCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(Dictionary<String, Object>))
            {
                Dictionary<String, Object> dictionary = serializer.Deserialize<Dictionary<String, Object>>(reader);

                return dictionary.Aggregate(
                    new NameValueCollection(), (nvc, kvp) =>
                    {
                        if (kvp.Value is string[])
                            (kvp.Value as string[]).Aggregate(nvc, (nvc1, value) => { nvc1.Add(kvp.Key, value); return nvc1; });
                        else
                            nvc.Add(kvp.Key, kvp.Value as string); 
                        return nvc;
                    });
            }
            else
                throw new NotSupportedException($"Deserializing {objectType.FullName} is not supported by the {nameof(NameValueCollectionJsonConverter)}.");
        }

        public override void WriteJson(JsonWriter writer, NameValueCollection value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.AllKeys.ToDictionary<string, string, object>(k => k, k => { string[] values = value.GetValues(k); if (values.Length == 1) return values[0]; return values; }));
        }
    }
}
