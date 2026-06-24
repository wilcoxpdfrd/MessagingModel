using System.ServiceModel.Configuration;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration
{
    /// <summary>
    /// Binding Section for ZeroMQ. Implements configuration for ZeroMQTransferBinding.
    /// </summary>
    public class ZeroMQTransferBindingCollectionElement : StandardBindingCollectionElement<ZeroMQTransferBindingBase, ZeroMQTransferBindingElement>
    {
        internal static ZeroMQTransferBindingCollectionElement GetBindingCollectionElement()
        {
            return (ZeroMQTransferBindingCollectionElement)ConfigurationHelpers.GetBindingCollectionElement(ZeroMQConfigurationStrings.ZeroMQTransferBindingSectionName);
        }

    }
}
