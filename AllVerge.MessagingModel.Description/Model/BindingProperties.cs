using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;

namespace AllVerge.MessagingModel.Description.Model
{
    using AllVerge.DataModel.Primitives;
    using AllVerge.MessagingModel.MarkupPrimitives.Json;
    using AllVerge.SystemPrimitives;
    using AllVerge.SystemPrimitives.Collections;
    using Newtonsoft.Json;

    [JsonConverter(typeof(CollectionJsonConverter<BindingProperty>))]
    public class BindingProperties : FixableCollection<BindingProperty>
    {
        public readonly static BindingProperties Empty = new BindingProperties(Fixable.AlwaysFixed);

        protected BindingProperties(Fixable fixable) : 
            base(fixable, BindingProperty.GetCollectionItemComparer())
        {
        }

        public BindingProperties() : 
            base(BindingProperty.GetCollectionItemComparer())
        {
        }

        internal BindingProperties(params BindingProperty[] bindingProperties) :
            base(bindingProperties, BindingProperty.GetCollectionItemComparer())
        {
        }

        [JsonConstructor]
        internal BindingProperties(IEnumerable<BindingProperty> bindingProperty) :
            base(Fixable.FixOnDeserialized, bindingProperty, BindingProperty.GetCollectionItemComparer())
        {
        }

        public IEnumerable<QualifiedName> GetDistinctBindingPropertyNames()
        {
            return this.Select(b => b.QualifiedName).Distinct();
        }

        public Boolean HasProperty(QualifiedName propertyName, params BindingAttribute[] bindingAttributes)
        {
            return this.Any(p => p.QualifiedName == propertyName && bindingAttributes.All(a => p.Attributes.Any(aa => aa.Name == a.Name && aa.Value == a.Value)));
        }

        public BindingProperty Put(BindingProperty property)
        {
            this.ThrowIfNotCollectionWriteable();

            //if (property.IsFixed)

            //    throw new ArgumentException(nameof(property) + " is readonly.");

            BindingProperty existingProperty;

            if (_TryGetProperty(out existingProperty, property.QualifiedName))
            {
                foreach (BindingAttribute bindingAttribute in property.Attributes)
                {
                    if (!existingProperty.GetAttributes().Any(a => a.Name == bindingAttribute.Name && a.Value == bindingAttribute.Value))

                        existingProperty.GetAttributes().Add(bindingAttribute);
                }

                foreach (BindingProperty bindingProperty in property.Properties)
                {
                    if (!existingProperty.GetProperties().Any(p => p == bindingProperty))

                        existingProperty.GetProperties().Put(bindingProperty);
                }

                property = existingProperty;
            }
            else
            {
                this.Add(property);
            }

            return property;
        }

        public BindingProperty Put(QualifiedName propertyName, string attributeName, string attributeValue)
        {
            this.ThrowIfNotCollectionWriteable();

            BindingProperty property;

            if (_TryGetProperty(out property, propertyName))
            {
                if (!property.GetAttributes().Any(a => a.Name == attributeName && a.Value == attributeValue))

                    property.GetAttributes().Add(BindingAttribute.CreateMutable(attributeName, attributeValue));
            }
            else
            {
                property = 
                    BindingProperty.CreateImutableOnRead(
                        propertyName, 
                        BindingProperties.Empty, 
                        BindingAttributes.CreateMutable(attributeName, attributeValue));

                this.Add(property);
            }

            return property;
        }

        public BindingProperty Put(QualifiedName propertyName, params BindingAttribute[] attributes)
        {
            this.ThrowIfNotCollectionWriteable();

            BindingProperty property;

            if (_TryGetProperty(out property, propertyName))
            {
                attributes = attributes.Where(a => !property.GetAttributes().Any(a1 => a1.Name == a.Name && a1.Value == a.Value)).ToArray();

                property.GetAttributes().AddRange(attributes);
            }
            else
            {
                property = 
                    BindingProperty.CreateImutableOnRead(
                        propertyName, 
                        BindingProperties.Empty, 
                        BindingAttributes.CreateMutable(attributes));

                this.Add(property);
            }

            return property;
        }

        public BindingProperty Put(QualifiedName propertyName, string attributeName, string[] attributeValues)
        {
            this.ThrowIfNotCollectionWriteable();

            BindingProperty property;

            if (_TryGetProperty(out property, propertyName))
            {
                BindingAttribute attribute;

                if (property.Attributes.TryGetItem(attributeName, out attribute))

                    attributeValues = attributeValues.Where(a => a != attribute.Value).ToArray();

                property.GetAttributes().Add(attributeName, attributeValues);
            }
            else
            {
                property = 
                    BindingProperty.CreateImutableOnRead(
                        propertyName, 
                        BindingProperties.Empty, 
                        BindingAttributes.CreateMutable(attributeName, attributeValues));

                this.Add(property);
            }

            return property;
        }

        public new BindingProperty Add(BindingProperty property)
        {
            this.ThrowIfNotCollectionWriteable();

            base.Add(property);

            return property;
        }

        private bool _TryGetProperty(out BindingProperty property, QualifiedName propertyName)
        {
            property = this.GetEnumerable().FirstOrDefault(p => p.QualifiedName == propertyName);

            return property != null;
        }

        public bool TryGetProperty(out BindingProperty property, String propertyLocalName, params BindingAttribute[] bindingAttributes)
        {
            BindingProperty[] filteredProperties;

            if (TryGetProperties(out filteredProperties, propertyLocalName, bindingAttributes) && filteredProperties.Length == 1)

                property = filteredProperties[0];

            else

                property = null;

            return property != null;
        }

        public bool TryGetProperties(out BindingProperty[] properties, String propertyLocalName, params BindingAttribute[] bindingAttributes)
        {
            IEnumerable<BindingProperty> filteredProperties = this.Where(p => p.QualifiedName.LocalName == propertyLocalName && bindingAttributes.All(a => p.Attributes.Any(aa => aa.Name == a.Name && aa.Value == a.Value)));

            if (filteredProperties.Count() > 0)

                properties = filteredProperties.ToArray();

            else

                properties = null;

            return properties != null;
        }

        public bool TryGetProperty(out BindingProperty property, QualifiedName bindingPropertyQualifiedName, params BindingAttribute[] bindingAttributes)
        {
            BindingProperty[] filteredProperties;

            if (TryGetProperties(out filteredProperties, bindingPropertyQualifiedName, bindingAttributes) && filteredProperties.Length == 1)

                property = filteredProperties[0];

            else

                property = null;

            return property != null;
        }

        public bool TryGetProperties(out BindingProperty[] properties, QualifiedName bindingPropertyQualifiedName, params BindingAttribute[] bindingAttributes)
        {
            IEnumerable<BindingProperty> filteredProperties = this.Where(p => p.QualifiedName == bindingPropertyQualifiedName && bindingAttributes.All(a => p.Attributes.Any(aa => aa.Name == a.Name && aa.Value == a.Value)));

            if (filteredProperties.Count() > 0)

                properties = filteredProperties.ToArray();

            else

                properties = null;

            return properties != null;
        }

        internal void Patch(BindingProperties patchBindingProperties, BindingTargets bindingTarget)
        {
            // ToDo: Implement BindingSpecification and here for when a BindingProperty has specified BindingProperties ...

            IEnumerable<BindingSpecification> bindingSpecifications = 
                BindingSpecification.GetBindings(bindingTarget);

            foreach (BindingProperty patchBindingProperty in patchBindingProperties)
            {
                bool flag = false;

                foreach (BindingSpecification bindingSpecification in bindingSpecifications)
                { 
                    if (bindingSpecification.Matches(patchBindingProperty, out BindingAttribute keyBindingAttribute, out IReadOnlyDictionary<String, BindingAttribute[]> bindingAttributeMap))
                    {
                        BindingProperty targetBindingProperty = this.FirstOrDefault(p => p.QualifiedName == patchBindingProperty.QualifiedName);

                        if (targetBindingProperty == null)

                            this.Add(patchBindingProperty.Clone());

                        else
                        {
                            if (keyBindingAttribute != null)
                            {
                                targetBindingProperty.Attributes.PutItem(keyBindingAttribute);
                            }

                            if (bindingAttributeMap.Count > 0)
                            {
                                using (targetBindingProperty.Attributes.GetNotFixedCriticalRegion())
                                {
                                    foreach (KeyValuePair<String, BindingAttribute[]> keyValuePair in bindingAttributeMap)
                                    {
                                        targetBindingProperty.Attributes.PutItems(keyValuePair.Key, keyValuePair.Value);
                                    }
                                }
                            }
                        }

                        flag = true;

                        break;
                    }
                }

                if (!flag)

                    throw new InvalidOperationException($"No {nameof(BindingSpecification)} found for {patchBindingProperty.QualifiedName} {nameof(BindingProperty)}.");
            }
        }

        public bool TryGetAttribute(QualifiedName propertyName, string attributeName, out BindingAttribute attribute)
        {
            BindingProperty property = this.FirstOrDefault(p => p.QualifiedName == propertyName);

            if (property != null)
            {
                attribute = property.Attributes.FirstOrDefault(a => a.Name == attributeName);

                return attribute != null;
            }

            attribute = null;

            return false;
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext)
        {
            this.Fixed.OnDeserialized(streamingContext);
        }

        public BindingProperties Clone(Func<string, string> tokenExpansionFuction = null)
        {
            BindingProperties properties = new BindingProperties();

            foreach (BindingProperty property in this)

                properties.Put(
                    property.Clone(tokenExpansionFuction));

            return properties;
        }

        public override int GetHashCode()
        {
            return this.GetItemsHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is BindingProperties)
            {
                BindingProperties other = (BindingProperties)obj;

                return this.ItemsEqual(other, BindingProperty.GetCollectionItemComparer());
            }

            return base.Equals(obj);
        }

        public static BindingProperties Create(Fixable fixable)
        {
            return new BindingProperties(fixable);
        }

        public static BindingProperties CreateImmutableOnRead()
        {
            return new BindingProperties();
        }

        public static BindingProperties CreateImmutableOnRead(params BindingProperty[] bindingProperties)
        {
            BindingProperties properties = CreateImmutableOnRead();

            foreach (BindingProperty bindingProperty in bindingProperties)

                properties.Add(bindingProperty);

            return properties;
        }
    }
}
