using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AllVerge.MessagingModel.Description.Model
{
    using AllVerge.SystemPrimitives;
    using AllVerge.SystemPrimitives.Collections;
    using Newtonsoft.Json;

    [XmlRoot(ELEMENT_NAME)]
    public class BindingAttribute : IFixable
    {
        private static BindingAttributeCollectionItemComparer comparer;

        internal const String ELEMENT_NAME = "Attribute";

        private Fixed @fixed;
        private String typeName;
        private string name;
        private string value;

        [JsonConstructor]
        protected BindingAttribute(Fixable fixable)
        {
            this.@fixed = Fixed.Create(fixable);

            this.SetLocalFields(null, null);
        }

        protected BindingAttribute(Fixable fixable, string name, string value)
            : this(fixable)
        {
            this.SetLocalFields(name, value);
        }

        public BindingAttribute()
            : this(Fixable.FixOnRead)
        {
        }

        public BindingAttribute(string name, string value)
            : this(Fixable.FixOnRead)
        {
            this.SetLocalFields(name, value);
        }

        private void SetLocalFields(string name, string value)
        {
            this.typeName = this.GetType().Name;

            if (name == null)

                this.name = String.Empty;

            else

                this.name = name;

            if (value == null)

                this.value = String.Empty;

            else

                this.value = value;
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

        protected void SetFixed(bool? isFixed)
        {
            this.@fixed.SetFixed(isFixed);
        }

        [XmlAttribute("name")]
        [JsonProperty("@name")]
        public String Name
        {
            get
            {
                this.@fixed.OnRead();

                return this.name;
            }
            set
            {
                this.@fixed.ThrowIfNotWriteable(this.typeName, nameof(Name));

                this.name = value;
            }
        }

        [XmlText]
        [JsonProperty("value")]
        public String Value
        {
            get
            {
                this.@fixed.OnRead();

                return this.value;
            }
            set
            {
                this.@fixed.ThrowIfNotWriteable(this.typeName, nameof(Value));

                this.value = value;
            }
        }

        internal void AppendToValue(String value)
        {
            this.value += value;
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            this.Fixed.OnDeserialized(streamingContext);
        }

        public BindingAttribute Clone(Func<String, String> tokenExpansionFuction = null)
        {
            if (tokenExpansionFuction == null)

                return new BindingAttribute(this.@fixed.ConstructorParameter, this.Name, this.Value);

            else

                return new BindingAttribute(this.@fixed.ConstructorParameter, this.Name, tokenExpansionFuction(this.Value));
        }

        public override int GetHashCode()
        {
            int hashCode = 17;

            hashCode = hashCode * 23 + this.name.GetHashCode();

            hashCode = hashCode * 23 + this.value.GetHashCode();

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is BindingAttribute)
            {
                BindingAttribute other = (BindingAttribute)obj;

                if (this.name != other.name)

                    return false;

                if (this.value != other.value)

                    return false;

                return true;
            }
            return base.Equals(obj);
        }

        public static BindingAttribute Create(Fixable fixable)
        {
            return new BindingAttribute(fixable);
        }

        public static BindingAttribute CreateMutable()
        {
            return new BindingAttribute(Fixable.NeverFixed);
        }

        public static BindingAttribute CreateMutable(string name, string value)
        {
            return new BindingAttribute(Fixable.NeverFixed, name, value);
        }

        public static BindingAttribute CreateImmutable(string name, string value)
        {
            return new BindingAttribute(Fixable.AlwaysFixed, name, value);
        }

        public static ICollectionItemComparer<BindingAttribute> GetCollectionItemComparer()
        {
            if (comparer == null)

                comparer = new BindingAttributeCollectionItemComparer();

            return comparer;
        }

        private class BindingAttributeCollectionItemComparer : ICollectionItemComparer<BindingAttribute>
        {
            public int Compare(BindingAttribute x, BindingAttribute y)
            {
                return x.name.CompareTo(y.name);
            }

            public bool Equals(BindingAttribute left, BindingAttribute right)
            {
                return left.Equals(right);
            }

            public int GetHashCode(BindingAttribute obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
