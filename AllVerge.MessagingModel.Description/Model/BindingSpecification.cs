using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace AllVerge.MessagingModel.Description.Model
{
    using AllVerge.DataModel.Primitives;

    public class BindingSpecification
    {
        public BindingSpecification(QualifiedName bindingQualifiedName, string keyBindingAttributeName, params string[] bindingAttributeNames)
        {
            this.QualifiedName = bindingQualifiedName;
            this.KeyAttributeName = keyBindingAttributeName;
            this.AttributeNames = bindingAttributeNames;
        }

        public QualifiedName QualifiedName { get; private set; }
        public String KeyAttributeName { get; private set; }
        public String[] AttributeNames { get; private set; }

        public static IEnumerable<BindingSpecification> GetBindings(BindingTargets bindingTarget)
        {
            List<BindingSpecification> bindingSpecifications = new List<BindingSpecification>();

            switch (bindingTarget)
            {
                case BindingTargets.Connection:
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.HTTP_BINDING_PROPERTY_NAME, BindingConstants.BINDING_TRANSPORT_ATTRIBUTE_NAME)
                    );
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.HTTP_BINDING_ADDRESS_PROPERTY_NAME, BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME)
                    );
                    break;
                case BindingTargets.Interaction:
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.HTTP_BINDING_PROPERTY_NAME, BindingConstants.BINDING_VERB_ATTRIBUTE_NAME)
                    );
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.HTTP_BINDING_OPERATION_PROPERTY_NAME, BindingConstants.BINDING_LOCATION_ATTRIBUTE_NAME)
                    );
                    break;
                case BindingTargets.Message:
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.MIME_CONTENT_BINDING_PROPERTY_NAME, BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME, BindingConstants.MIME_CONTENT_BINDING_TYPE_ATTRIBUTE_NAME)
                    );
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.URLENCODED_PROPERTY_LOCAL_NAME, BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME)
                    );
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.URLREPLACEMENT_PROPERTY_LOCAL_NAME, BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME)
                    );
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.HTTP_BINDING_HEADER_PROPERTY_NAME, BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME)
                    );
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.HTTP_BINDING_STATUS_CODE_PROPERTY_NAME, BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME)
                    );
                    bindingSpecifications.Add(
                        new BindingSpecification(BindingConstants.HTTP_BINDING_STATUS_CODE_PROPERTY_NAME, null, BindingConstants.MESSAGE_BINDING_PART_ATTRIBUTE_NAME)
                    );
                    break;
            }

            return bindingSpecifications;
        }

        internal bool Matches(BindingProperty patchBindingProperty, out BindingAttribute keyBindingAttribute, out IReadOnlyDictionary<String, BindingAttribute[]> bindingAttributeMap)
        {
            if (!patchBindingProperty.AttributesSpecified)
            {
                throw new ArgumentException("Parameter has no attributes!", nameof(patchBindingProperty));
            }

            if (this.QualifiedName == patchBindingProperty.QualifiedName)
            {
                if (this.KeyAttributeName == null)

                    keyBindingAttribute = null;

                else if (!patchBindingProperty.Attributes.TryGetItem(this.KeyAttributeName, out keyBindingAttribute))
                {
                    bindingAttributeMap = null;

                    return false;
                }

                Dictionary<String, BindingAttribute[]> bindingAttributeList = new Dictionary<string, BindingAttribute[]>();

                foreach (String attributeName in this.AttributeNames)
                {
                    bindingAttributeList.Add(attributeName, patchBindingProperty.Attributes.GetItems(attributeName));
                }

                bindingAttributeMap = new ReadOnlyDictionary<String, BindingAttribute[]>(bindingAttributeList);

                return true;
            }

            keyBindingAttribute = null;
            bindingAttributeMap = null;

            return false;
        }
    }
}
