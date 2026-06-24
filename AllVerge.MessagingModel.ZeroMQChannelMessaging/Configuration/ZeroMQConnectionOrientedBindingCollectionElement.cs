using System.ServiceModel.Configuration;
using System.ServiceModel.Channels;
using System;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration
{
    /// <summary>
    /// Binding Section for ZeroMQ. Implements configuration for ZeroMQTransportBinding.
    /// </summary>
    public class ZeroMQConnectionOrientedBindingCollectionElement : StandardBindingCollectionElement<ZeroMQConnectionOrientedBindingBase, ZeroMQConnectionOrientedBindingElement>
    {
        internal static ZeroMQConnectionOrientedBindingCollectionElement GetBindingCollectionElement()
        {
            return (ZeroMQConnectionOrientedBindingCollectionElement)ConfigurationHelpers.GetBindingCollectionElement(ZeroMQConfigurationStrings.ZeroMQBindingCollectionElementName);
        }
    }
}
