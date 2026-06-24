using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Policy;
using System.Text;

namespace AllVerge.MessagingModel.MarkupPrimitives
{
    /// <summary>
    /// Provides a helper method that can be used to register a contract attribute type recognized by the 
    /// <see cref="DataContractSerializer"/> or <see cref="DataContractJsonSerializer"/>.
    /// </summary>
    /// <remarks>
    /// When serialization or deserializion extension methods in the <see cref="Xml.XmlSerialization"/> or <see cref="Json.JsonSerialization"/> namespaces
    /// are used to serialize to or deserialize from Xml or Json a contract type decorated with a contract attribute recognized by the 
    /// <see cref="DataContractSerializer"/> or <see cref="DataContractJsonSerializer"/> serializers, those serializers will be used.
    /// Otherwise the <see cref="System.Xml.Serialization.XmlSerializer"/> or <see cref="Newtonsoft.Json.JsonSerializer"/> serializers will be used.
    /// Contract attributes that might need to be registered include System.ServiceModel.MessageContractAttribute.
    /// Note that <see cref="DataContractAttribute"/> is registered by the system.
    /// </remarks>
    public class DataContractSerializationHelper
    {
        static ResourceManager resourceManager = new ResourceManager("FxResources.System.Private.DataContractSerialization.SR", typeof(DataContractSerializer).Assembly);

        static DataContractSerializationHelper()
        {
            DataContractSerializationHelper.RegisterContractAttributeType(typeof(DataContractAttribute));
        }

        static List<Type> contractAttrTypes = new List<Type>();

        internal static bool HasRegisteredContractAttribute(Type contractType)
        {
            return contractAttrTypes.Any(contractAttrType => contractType.GetCustomAttributes(contractAttrType, false).Length > 0);
        }

        /// <summary>
        /// Allows registering contract attributes that are recognized by the <see cref="DataContractSerializer"/>.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="attributeType"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void RegisterContractAttributeType(Type attributeType)
        {
            if (!typeof(Attribute).IsAssignableFrom(attributeType))

                throw new ArgumentException($"Argument must derive from {nameof(Attribute)}", nameof(attributeType));

            contractAttrTypes.Add(attributeType);
        }

        /// <summary>
        ///   The value of the DataContractSerialization string resource localized for the caller's current UI culture, or null if name cannot be found.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <returns></returns>
        public static string GetResourceString(string name)
        {
            return resourceManager.GetString(name);
        }

        /// <summary>
        ///  Returns the value of the DataContractSerialization string resource localized for the specified culture, or null if name cannot be found.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="cultureInfo">The culture object for which a resource file name is constructed.</param>
        /// <returns></returns>
        public static string GetResourceString(string name, CultureInfo cultureInfo)
        {
            return resourceManager.GetString(name, cultureInfo);
        }
    }
}
