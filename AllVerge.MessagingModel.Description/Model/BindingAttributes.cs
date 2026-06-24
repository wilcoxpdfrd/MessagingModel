using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.Description.Model
{
    using AllVerge.MessagingModel.MarkupPrimitives.Json;
    using AllVerge.SystemPrimitives;
    using AllVerge.SystemPrimitives.Collections;
    using Newtonsoft.Json;

    [JsonConverter(typeof(CollectionJsonConverter<BindingAttribute>))]
    public class BindingAttributes : FixableCollection<BindingAttribute>, IXmlSerializable
    {
        public readonly static BindingAttributes Empty = new BindingAttributes(Fixable.AlwaysFixed);

        protected BindingAttributes(Fixable fixable) :
            base(fixable, BindingAttribute.GetCollectionItemComparer())
        {
        }

        protected BindingAttributes(Fixable fixable, IEnumerable<BindingAttribute> bindingAttributes) :
            base(fixable, bindingAttributes, BindingAttribute.GetCollectionItemComparer())
        {
        }

        protected BindingAttributes(Fixable fixable, params BindingAttribute[] bindingAttributes) :
            base(fixable, bindingAttributes, BindingAttribute.GetCollectionItemComparer())
        {
        }

        public BindingAttributes() : 
            base(BindingAttribute.GetCollectionItemComparer())
        {
        }

        internal BindingAttributes(string attrName, string attrValue) :
            base(Select(attrName, attrValue), BindingAttribute.GetCollectionItemComparer())
        {
        }

        internal BindingAttributes(string attrName, string[] attrValues) :
            base(Select(attrName, attrValues), BindingAttribute.GetCollectionItemComparer())
        {
        }

        internal BindingAttributes(params BindingAttribute[] bindingAttributes) :
            base(bindingAttributes, BindingAttribute.GetCollectionItemComparer())
        {
        }

        [JsonConstructor]
        internal BindingAttributes(IEnumerable<BindingAttribute> bindingAttributes) :
            base(Fixable.FixOnDeserialized, bindingAttributes, BindingAttribute.GetCollectionItemComparer())
        {
        }

        public BindingAttributes Add(string name, string value)
        {
            this.Add(new BindingAttribute(name, value));

            return this;
        }

        public BindingAttributes Add(string name, string[] values)
        {
            foreach (string value in values.Take(values.Length - 1))

                this.Add(new BindingAttribute(name, value));

            this.Add(new BindingAttribute(name, values.Last()));

            return this;
        }

        public bool TryGetItem(String attributeName, out BindingAttribute attribute)
        {
            attribute = this.FirstOrDefault(a => a.Name == attributeName);

            return attribute != null;
        }

        public BindingAttribute[] GetItems(String attributeName)
        {
            return this.Where(a => a.Name == attributeName).ToArray();
        }

        public bool TryGetItems(String attributeName, out BindingAttribute[] bindingAttributes)
        {
            bindingAttributes = this.GetItems(attributeName);

            return bindingAttributes.Length > 0;
        }

        internal void PutItems(string attributeName, BindingAttribute[] bindingAttributes)
        {
            int[] indices = this.FindIndices(a => a.Name == attributeName);

            IEnumerator indicesEnumerator = indices.GetEnumerator();

            int l = bindingAttributes.Length;
            int m = Math.Max(indices.Length, l);

            int index;

            if (indicesEnumerator.MoveNext())

                index = (int)indicesEnumerator.Current;

            else

                index = -1;

            int i = 0;
            int inserted = 0;

            for (int j = 0; j < m; j++)
            {
                if (j == index)
                {
                    this[j] = bindingAttributes[i].Clone();

                    i++;

                    if (indicesEnumerator.MoveNext())

                        index = (int)indicesEnumerator.Current;
                }
                else if (j > index)
                {
                    if (l > i)
                    {
                        inserted++;

                        this.Insert(index + inserted, bindingAttributes[i].Clone());

                        i++;
                    }
                    else
                    {
                        this.RemoveAt(j);
                    }
                }
            }
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            this.Fixed.OnDeserialized(streamingContext);
        }

        public BindingAttributes Clone(params String[] excludeAttributeNames)
        {
            BindingAttributes attributes = new BindingAttributes();

            foreach (BindingAttribute attribute in this.Where(a => !excludeAttributeNames.Any(f => f == a.Name)))

                attributes.Add(attribute.Clone());

            return attributes;
        }

        public BindingAttributes Clone(Func<String, String> tokenExpansionFuction = null)
        {
            BindingAttributes attributes = new BindingAttributes();

            foreach (BindingAttribute attribute in this)

                attributes.Add(attribute.Clone(tokenExpansionFuction));

            return attributes;
        }

        public override int GetHashCode()
        {
            return this.GetItemsHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BindingAttributes)
            {
                BindingAttributes other = (BindingAttributes)obj;

                return this.ItemsEqual(other, BindingAttribute.GetCollectionItemComparer());
            }

            return base.Equals(obj);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    if (reader.Name != BindingProperty.ATTRIBUTE_NAME)

                        this.Add(reader.Name, reader.Value);
                }
                while (reader.MoveToNextAttribute());

                reader.MoveToElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            IEnumerable<BindingAttribute> uniqueBindingAttributes = 
                this.GroupBy(a => a.Name, (name, attrs) => new { Name = name, Attrs = attrs }).Where(a => a.Attrs.Count() == 1).Select(a => a.Attrs.First());

            foreach (BindingAttribute bindingAttribute in uniqueBindingAttributes)
            {
                writer.WriteAttributeString(bindingAttribute.Name, bindingAttribute.Value);
            }
        }

        private static IEnumerable<BindingAttribute> Select(string attrName, params string[] attrValues)
        {
            return attrValues.Select(attrValue => new BindingAttribute(attrName, attrValue));
        }

        public static BindingAttributes Create(Fixable fixable) { return new BindingAttributes(fixable); }

        public static BindingAttributes CreateImmutableOnRead() { return new BindingAttributes(); }

        public static BindingAttributes CreateMutable() { return new BindingAttributes(Fixable.NeverFixed); }

        public static BindingAttributes CreateMutable(string attributeName, string attributeValue) { return new BindingAttributes(Fixable.NeverFixed, BindingAttribute.CreateImmutable(attributeName, attributeValue)); }

        public static BindingAttributes CreateMutable(string attributeName, string[] attributeValues) { return new BindingAttributes(Fixable.NeverFixed, BindingAttributes.Select(attributeName, attributeValues)); }

        public static BindingAttributes CreateMutable(IEnumerable<BindingAttribute> bindingAttributes) { return new BindingAttributes(Fixable.NeverFixed, bindingAttributes); }

        public static BindingAttributes CreateImmutableOnRead(string attributeName, string attributeValue) { return new BindingAttributes(attributeName, attributeValue); }

        public static BindingAttributes CreateImmutableOnRead(string attributeName, string[] attributeValues) { return new BindingAttributes(attributeName, attributeValues); }

        public static BindingAttributes CreateImmutableOnRead(IEnumerable<BindingAttribute> bindingAttributes) { return new BindingAttributes(Fixable.FixOnRead, bindingAttributes); }

        public static BindingAttributes CreateImmutable(IEnumerable<BindingAttribute> bindingAttributes) { return new BindingAttributes(Fixable.NeverFixed, bindingAttributes); }
    }
}
