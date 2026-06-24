using System;
using System.Collections.Generic;
using System.Linq;

namespace AllVerge.MessagingModel.Markup.Yaml
{
    /// <summary>
    /// Provides object serialization methods.
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Serializes <paramref name="objectToSerialize"/> to a Json String.
        /// </summary>
        /// <param name="objectToSerialize">The object to serialize.</param>
        /// <returns>The document element of the serialized object.</returns>
        public static String SerializeAsYaml(this Object objectToSerialize)
        {
            if (objectToSerialize == null)

                return null;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Deserializes the <paramref name="serialized"/> Json String to  <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the deserialization target.</typeparam>
        /// <param name="serialized">The document element of the serialized object.</param>
        /// <returns>The deserialized object.</returns>
        public static T DeserializeYaml<T>(this String serialized)
        {
            if (serialized == null)

                return default(T);

            throw new NotImplementedException();
        }
    }
}
