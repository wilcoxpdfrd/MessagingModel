using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MessagingFoundation.Description
{
    public class XmlAttributeOverridesSerializerOperationBehavior : DataContractSerializerOperationBehavior
    {
        private XmlAttributeOverrides overrides;

        public XmlAttributeOverridesSerializerOperationBehavior(OperationDescription operation) : 
            this(operation, (XmlAttributeOverridesSerializerFormatAttribute)null)
        {
        }

        public XmlAttributeOverridesSerializerOperationBehavior(OperationDescription operation, XmlAttributeOverrides overrides) :
            this(operation, (DataContractFormatAttribute)null, overrides)
        {
        }

        public XmlAttributeOverridesSerializerOperationBehavior(OperationDescription operation, XmlAttributeOverridesSerializerFormatAttribute xmlMessageSerializerFormatAttribute) : 
            this(operation, xmlMessageSerializerFormatAttribute?.DataContractFormatAttribute, xmlMessageSerializerFormatAttribute?.XmlAttributeOverrides)
        {
        }

        private XmlAttributeOverridesSerializerOperationBehavior(OperationDescription operation, DataContractFormatAttribute dataContractFormatAttribute, XmlAttributeOverrides overrides) :
            base(operation, dataContractFormatAttribute)
        {
            this.overrides = overrides;
        }

        public override XmlObjectSerializer CreateSerializer(Type type, string rootName, string rootNamespace, IList<Type> knownTypes)
        {
            XmlDictionary xmlDictionary = new XmlDictionary(2);

            return this.CreateSerializer(type, xmlDictionary.Add(rootName), xmlDictionary.Add(rootNamespace), knownTypes);
        }

        public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString rootName, XmlDictionaryString rootNamespace, IList<Type> knownTypes)
        {
            return new XmlAttributeOverridesSerializer(type, rootName, rootNamespace, this.overrides, GetNotNullKnownTypes(knownTypes));
        }

        private static Type[] GetNotNullKnownTypes(IList<Type> knownTypes)
        {
            if (knownTypes == null)

                return Array.Empty<Type>();

            else

                return knownTypes.ToArray();
        }

        public static XmlAttributeOverridesSerializerOperationBehavior ApplyTo(OperationDescription operation)
        {
            DataContractSerializerOperationBehavior dataContractSerializerOperationBehavior = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();

            if (dataContractSerializerOperationBehavior != null)
            {
                XmlAttributeOverridesSerializerOperationBehavior xmlMessageSerializerOperationBehavior = new XmlAttributeOverridesSerializerOperationBehavior(operation, dataContractSerializerOperationBehavior.DataContractFormatAttribute, null);

                operation.Behaviors.Remove(dataContractSerializerOperationBehavior);
                operation.Behaviors.Add(xmlMessageSerializerOperationBehavior);

                return xmlMessageSerializerOperationBehavior;
            }

            return null;
        }

        public static XmlAttributeOverridesSerializerOperationBehavior ApplyTo(OperationDescription operation, XmlAttributeOverrides overrides)
        {
            DataContractSerializerOperationBehavior dataContractSerializerOperationBehavior = operation.Behaviors.Find<DataContractSerializerOperationBehavior>();

            if (dataContractSerializerOperationBehavior != null)
            {
                XmlAttributeOverridesSerializerOperationBehavior xmlMessageSerializerOperationBehavior = new XmlAttributeOverridesSerializerOperationBehavior(operation, dataContractSerializerOperationBehavior.DataContractFormatAttribute, overrides);

                operation.Behaviors.Remove(dataContractSerializerOperationBehavior);
                operation.Behaviors.Add(xmlMessageSerializerOperationBehavior);

                return xmlMessageSerializerOperationBehavior;
            }

            return null;
        }
    }
}
