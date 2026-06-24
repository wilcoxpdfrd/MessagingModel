using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MarkupPrimitives.Json
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class JsonFormatterExtensions
    {
        /// <summary>
        ///  Reads the current JSON token and it's content as raw JSON.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static String ReadRaw(this JsonReader reader)
        {
            return JObject.Load(reader).ToString();
        }

        public static void ReadOrThrow(this JsonReader reader)
        {
            if (!reader.Read())

                throw reader.CreateException(null, "Unexpected on read: no more tokens.");
        }

        /// <summary>
        /// Creates an <see cref="JsonReaderException"/> from the <paramref name="messageFormat"/> and <paramref name="args"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="messageFormat"></param>
        /// <returns></returns>
        public static JsonReaderException CreateException(this JsonReader reader, String messageFormat, params Object[] args)
        {
            return reader.CreateException(null, messageFormat, args);
        }
        
        /// <summary>
        /// Creates an <see cref="JsonReaderException"/> from the <paramref name="innerException"/>, <paramref name="messageFormat"/> and and <paramref name="args"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="innerException"></param>
        /// <param name="messageFormat"></param>
        /// <returns></returns>
        public static JsonReaderException CreateException(this JsonReader reader, Exception innerException, String messageFormat, params Object[] args)
        {
            IJsonLineInfo lineInfo = (IJsonLineInfo)reader;

            if (args.Length > 0)

                messageFormat = String.Format(messageFormat, args);

            return new JsonReaderException(messageFormat, reader.Path, lineInfo.LineNumber, lineInfo.LinePosition, innerException);
        }
    }
}
