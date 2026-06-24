using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.MarkupPrimitives.Xml.Serialization
{
    public class XmlAttributeOverridesProvider
    {
        private readonly Dictionary<Type, Dictionary<string, XmlAttributes>> _types = new Dictionary<Type, Dictionary<string, XmlAttributes>>();

        public XmlAttributeOverridesProvider(params XmlAttributeOverrides[] attributeOverrides)
        {
            foreach (XmlAttributeOverrides overrides in attributeOverrides)
            {
                foreach (KeyValuePair<Type, Dictionary<string, XmlAttributes>> typeAttributes in overrides.GetTypesAttributes())
                {
                    if (!_types.ContainsKey(typeAttributes.Key))
                    
                        _types.Add(typeAttributes.Key, typeAttributes.Value.Aggregate(new Dictionary<string, XmlAttributes>(), (d, v) => { d.Add(v.Key, v.Value.Clone()); return d; }));

                    else
                    {
                        foreach (KeyValuePair<String, XmlAttributes> attributes in typeAttributes.Value)
                        {
                            if (!_types[typeAttributes.Key].ContainsKey(attributes.Key))

                                _types[typeAttributes.Key].Add(attributes.Key, attributes.Value);

                            else

                                _types[typeAttributes.Key][attributes.Key].Merge(attributes.Value);
                        }
                    }
                }
            }
        }

        public XmlAttributes this[Type type, string member]
        {
            get
            {
                if (!_types.TryGetValue(type, out Dictionary<string, XmlAttributes> value) || !value.TryGetValue(member, out XmlAttributes value2))
                {
                    return null;
                }
                return value2;
            }
        }

        public void Add(Type type, string member, XmlAttributes attributes)
        {
            if (!_types.TryGetValue(type, out Dictionary<string, XmlAttributes> value))
            {
                value = new Dictionary<string, XmlAttributes>();
                _types.Add(type, value);
            }
            else if (value.ContainsKey(member))
            {
                throw new InvalidOperationException($"{type.FullName}.{member} already has attributes");
            }
            value.Add(member, attributes);
        }

        public void Add(Type type, XmlAttributes attributes)
        {
            Add(type, string.Empty, attributes);
        }

        public bool TryAddItemsXmlElementAttribute(Type itemsType, String itemsElementName, Type itemType, String itemElementName)
        {
            string ns;

            if (itemType.TryGetXmlRootAttribute(out XmlRootAttribute rootAttribute))

                ns = rootAttribute.Namespace;

            else

                ns = null;

            XmlAttributes attrs = this[itemsType, itemsElementName];

            if (attrs == null)
            {
                //attrs = new XmlAttributes(itemsType.GetProperty(itemsElementName));
                attrs = new XmlAttributes();

                this.Add(itemsType, itemsElementName, attrs);
            }

            XmlElementAttribute itemAttribute = new XmlElementAttribute(itemElementName, itemType) { Namespace = ns };

            if (!attrs.XmlElements.Contains(itemAttribute)) // Contains does a field level comparison ...
            {
                attrs.XmlElements.Add(itemAttribute);

                return true;
            }

            return false;
        }

        public static implicit operator XmlAttributeOverrides(XmlAttributeOverridesProvider provider)
        {
            XmlAttributeOverrides overrides = new XmlAttributeOverrides();

            foreach (KeyValuePair<Type, Dictionary<string, XmlAttributes>> type in provider._types)
            {
                foreach (KeyValuePair<string, XmlAttributes> attributeMap in type.Value)
                {
                    overrides.Add(type.Key, attributeMap.Key, attributeMap.Value.Clone());
                }
            }

            return overrides;
        }
    }
}
