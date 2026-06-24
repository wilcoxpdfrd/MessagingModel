using System;
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
    using AllVerge.DataModel.Primitives;
    using AllVerge.MessagingModel.MarkupPrimitives.Xml;
    using AllVerge.SystemPrimitives;
    using AllVerge.SystemPrimitives.Collections;
    using Newtonsoft.Json;

    [XmlRoot(ELEMENT_NAME)]
    public class BindingProperty : IFixable, IXmlSerializable
    {
        private static BindingPropertyCollectionItemComparer comparer;

        internal const String ELEMENT_NAME = "Binding";
        internal const String ATTRIBUTE_NAME = "name";

        static readonly XmlSerializer propertySerializer = new XmlSerializer(typeof(BindingProperty));
        static readonly XmlSerializer attributeSerializer = new XmlSerializer(typeof(BindingAttribute));

        private Fixed @fixed;
        private String typeName;
        private QualifiedName name;
        private BindingProperties bindingProperties;
        private BindingAttributes bindingAttributes;

        [JsonConstructor]
        protected BindingProperty(Fixable fixable) 
        {
            this.@fixed = Fixed.Create(fixable, this.OnSetFixed);
            this.typeName = this.GetType().Name;

            this.SetLocalFields(null, null, null);
        }

        protected BindingProperty(Fixable fixable, QualifiedName name, BindingProperties bindingProperties) :
            this(fixable)
        {
            SetLocalFields(name, bindingProperties, null);
        }

        protected BindingProperty(Fixable fixable, QualifiedName name, BindingAttributes bindingAttributes) :
            this(fixable)
        {
            SetLocalFields(name, null, bindingAttributes);
        }

        protected BindingProperty(Fixable fixable, QualifiedName name, BindingProperties bindingProperties, BindingAttributes bindingAttributes) :
            this(fixable)
        {
            SetLocalFields(name, bindingProperties, bindingAttributes);
        }

        public BindingProperty()
            : this(Fixable.FixOnRead)
        {
        }

        public BindingProperty(QualifiedName name, string attrName, string attrValue)
            : this(Fixable.FixOnRead)
        {
            this.SetLocalFields(name, null, new BindingAttributes(attrName, attrValue));
        }

        public BindingProperty(QualifiedName name, string attrName, string[] attrValues)
            : this(Fixable.FixOnRead)
        {
            this.SetLocalFields(name, null, new BindingAttributes(attrName, attrValues));
        }

        public BindingProperty(QualifiedName name, params BindingAttribute[] bindingAttributes)
            : this(Fixable.FixOnRead)
        {
            this.SetLocalFields(name, null, new BindingAttributes(bindingAttributes));
        }

        public BindingProperty(QualifiedName name, BindingProperties bindingProperties, params BindingAttribute[] bindingAttributes)
            : this(Fixable.FixOnRead)
        {
            this.SetLocalFields(name, bindingProperties, new BindingAttributes(bindingAttributes));
        }

        private void SetLocalFields(QualifiedName name, BindingProperties bindingProperties, BindingAttributes bindingAttributes)
        {
            if (name == null)

                this.name = QualifiedName.Create(this.@fixed.ConstructorParameter);

            else

                this.name = name;

            if (bindingProperties == null)

                this.bindingProperties = BindingProperties.Create(this.@fixed.ConstructorParameter);

            else

                this.bindingProperties = bindingProperties;

            if (bindingAttributes == null)

                this.bindingAttributes = BindingAttributes.Create(this.@fixed.ConstructorParameter);

            else

                this.bindingAttributes = bindingAttributes;
        }

        IFixed IFixable.Fixed
        {
            get => this.Fixed;
        }

        protected Fixed Fixed
        {
            get
            {
                return this.@fixed;
            }
        }

        void IFixable.SetFixed(bool? isFixed)
        {
            this.SetFixed(isFixed);
        }

        protected void SetFixed(bool? @fixed)
        {
            this.@fixed.SetFixed(@fixed);
        }

        private void OnSetFixed(bool? @fixed)
        {
            ((IFixable)this.QualifiedName).SetFixed(@fixed);

            ((IFixable)this.Attributes).SetFixed(@fixed);

            ((IFixable)this.Properties).SetFixed(@fixed);
        }

        [XmlAttribute("name")]
        [JsonProperty("@name")]
        public String QualifiedNameToken
        {
            get
            {
                this.@fixed.OnRead();

                return this.name.Name;
            }
            set
            {
                this.@fixed.ThrowIfNotWriteable(this.typeName, nameof(QualifiedNameToken));

                this.name.Name = value;
            }
        }

        public QualifiedName QualifiedName
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Internal <see cref="Properties"/> accessor (bypasses Fixable checks).
        /// </summary>
        internal BindingProperties GetProperties() => this.bindingProperties;

        [JsonProperty("properties")]
        public BindingProperties Properties
        {
            get
            {
                this.@fixed.OnRead();

                return this.bindingProperties;
            }

            set
            {
                this.@fixed.ThrowIfNotWriteable(this.typeName, nameof(Properties));

                this.bindingProperties = value;
            }
        }

        public bool PropertiesSpecified
        {
            get
            {
                return this.bindingProperties.Count > 0;
            }
        }

        /// <summary>
        /// Used for Json serialization.
        /// </summary>
        /// <returns></returns>
        public bool ShouldSerializeProperties()
        {
            return this.PropertiesSpecified;
        }

        /// <summary>
        /// Internal <see cref="Attributes"/> accessor (bypasses Fixable checks).
        /// </summary>
        internal BindingAttributes GetAttributes() => this.bindingAttributes;

        [JsonProperty("attributes")]
        public BindingAttributes Attributes
        {
            get
            {
                this.@fixed.OnRead();

                return this.bindingAttributes;
            }
            set
            {
                this.@fixed.ThrowIfNotWriteable(this.typeName, nameof(Attributes));

                this.bindingAttributes = value;
            }
        }

        public bool AttributesSpecified
        {
            get
            {
                return this.bindingAttributes.Count > 0;
            }
        }

        /// <summary>
        /// Used for Json serialization.
        /// </summary>
        /// <returns></returns>
        public bool ShouldSerializeAttributes()
        {
            return this.AttributesSpecified;
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            this.Fixed.OnDeserialized(streamingContext);
        }

        public BindingProperty Clone(Func<string, string> tokenExpansionFuction = null)
        {
            return new BindingProperty(
                this.Fixed.ConstructorParameter,
                this.QualifiedName.Clone(), 
                this.Properties.Clone(tokenExpansionFuction), 
                this.Attributes.Clone(tokenExpansionFuction));
        }

        public BindingProperty Clone(params String[] excludeAttributeNames)
        {
            return new BindingProperty(
                this.Fixed.ConstructorParameter,
                this.QualifiedName.Clone(), 
                this.Properties.Clone(), 
                this.Attributes.Clone(excludeAttributeNames));
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.MoveToAttribute(ATTRIBUTE_NAME))

                this.name = reader.Value;

            reader.MoveToElement();

            Boolean isEmpty = reader.IsEmptyElement;

            // Read single node attributes (as xml attributes)

            this.bindingAttributes.ReadXml(reader);

            if (!isEmpty)
            {
                reader.Read();

                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == BindingAttribute.ELEMENT_NAME)
                    {
                        using (XmlReader r = reader.ReadSubtree())
                        {
                            this.bindingAttributes.Add((BindingAttribute)attributeSerializer.Deserialize(r));
                        }

                        reader.ReadEmptyOrEndElement(BindingAttribute.ELEMENT_NAME);
                    }
                    else  if (reader.Name == ELEMENT_NAME)
                    {
                        using (XmlReader r = reader.ReadSubtree())
                        {
                            this.bindingProperties.Add((BindingProperty)propertySerializer.Deserialize(r));
                        }

                        reader.ReadEmptyOrEndElement(ELEMENT_NAME);
                    }
                }
            }

            reader.Read();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(ATTRIBUTE_NAME, this.QualifiedName);

            // Write unique name attributes (as xml attributes)

            this.Attributes.WriteXml(writer);

            // Write duplicate name attributes

            IEnumerable<BindingAttribute> nonUniqueBindingAttributes =
                this.Attributes.GroupBy(a => a.Name, (name, attrs) => new { Name = name, Attrs = attrs }).Where(a => a.Attrs.Count() > 1).SelectMany(a => a.Attrs);

            foreach (BindingAttribute bindingAttribute in nonUniqueBindingAttributes)

                attributeSerializer.Serialize(writer, bindingAttribute, XmlSerialization.EmptyNSMap);
            
            // Write properties 

            foreach (BindingProperty property in this.Properties)

                propertySerializer.Serialize(writer, property);
        }

        public override int GetHashCode()
        {
            int hashCode = 17;

            hashCode = hashCode * 23 + this.name.GetHashCode();

            if (this.AttributesSpecified)

                hashCode = hashCode * 23 + this.Attributes.GetHashCode();

            if (this.PropertiesSpecified)

                hashCode = hashCode * 23 + this.Properties.GetHashCode();

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is BindingProperty)
            {
                BindingProperty other = (BindingProperty)obj;

                if (other.QualifiedName == this.QualifiedName)
                {
                    if (other.AttributesSpecified && other.Attributes.Count == this.Attributes.Count)
                    {
                        foreach (BindingAttribute otherAttribute in other.Attributes)
                        {
                            bool otherExistsInThis = this.Attributes.Any(a => a.Name == otherAttribute.Name && a.Value == otherAttribute.Value);

                            if (!otherExistsInThis)

                                return false;
                        }
                    }

                    if (other.PropertiesSpecified && other.Properties.Count == this.Properties.Count)
                    {
                        foreach (BindingProperty otherProperty in other.Properties)
                        {
                            bool otherExistsInThis = this.Properties.Any(p => p == otherProperty);

                            if (!otherExistsInThis)

                                return false;
                        }
                    }

                    return true;
                }

                return false;
            }

            return base.Equals(obj);
        }

        public static bool operator ==(BindingProperty right, BindingProperty left)
        {
            if ((Object)right == null)

                return (Object)left == null;

            else if ((Object)left == null)

                return false;

            return left.Equals(right);
        }

        public static bool operator !=(BindingProperty right, BindingProperty left)
        {
            if ((Object)right == null)

                return (Object)left != null;

            else if ((Object)left == null)

                return true;

            return !left.Equals(right);
        }

        public static BindingProperty Create(Fixable fixable)
        {
            return new BindingProperty(fixable);
        }

        public static BindingProperty CreateMutable()
        {
            return new BindingProperty(Fixable.NeverFixed);
        }

        public static BindingProperty CreateImutableOnRead()
        {
            return new BindingProperty(Fixable.FixOnRead);
        }

        public static BindingProperty CreateImutableOnRead(QualifiedName name, BindingProperties bindingProperties)
        {
            return new BindingProperty(Fixable.FixOnRead, name, bindingProperties);
        }

        public static BindingProperty CreateImutableOnRead(QualifiedName name, BindingAttributes bindingAttributes)
        {
            return new BindingProperty(Fixable.FixOnRead, name, bindingAttributes);
        }

        public static BindingProperty CreateImutableOnRead(QualifiedName name, BindingProperties bindingProperties, BindingAttributes bindingAttributes)
        {
            return new BindingProperty(Fixable.FixOnRead, name, bindingProperties, bindingAttributes);
        }

        public static BindingProperty CreateImmutable(QualifiedName name, BindingProperties bindingProperties, BindingAttributes bindingAttributes)
        {
            return new BindingProperty(Fixable.NeverFixed, name, bindingProperties, bindingAttributes);
        }

        public static ICollectionItemComparer<BindingProperty> GetCollectionItemComparer()
        {
            if (comparer == null)

                comparer = new BindingPropertyCollectionItemComparer();

            return comparer;
        }

        private class BindingPropertyCollectionItemComparer : ICollectionItemComparer<BindingProperty>
        {
            public int Compare(BindingProperty x, BindingProperty y)
            {
                throw new NotImplementedException();
            }

            public bool Equals(BindingProperty left, BindingProperty right)
            {
                return left.Equals(right);
            }

            public int GetHashCode(BindingProperty obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
