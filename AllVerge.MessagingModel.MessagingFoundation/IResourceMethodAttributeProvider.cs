using System.ServiceModel;

using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

namespace AllVerge.MessagingModel
{
    internal interface IResourceMethodAttributeProvider : IAttributeProvider<ResourceActionAttribute>
    {
        ResourceActionAttribute GetResourceMethodAttribute();
    }
}
