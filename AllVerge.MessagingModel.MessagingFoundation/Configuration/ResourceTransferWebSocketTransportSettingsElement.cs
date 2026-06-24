// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace AllVerge.MessagingModel.MessagingFoundation.Configuration
{
    using System.Configuration;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Configuration;

    /// <summary>
    /// Element for <see cref="ResourceTransferBindingElement.WebSocketTransportSettings"/>.
    /// </summary>
    public sealed partial class ResourceTransferWebSocketTransportSettingsElement : WebSocketTransportSettingsElement
    {
        [ConfigurationProperty(ConfigurationStrings.TransportUsage, DefaultValue = NetHttpBindingDefaults.TransportUsage)]
        [ServiceModelEnumValidator(typeof(WebSocketTransportUsageHelper))]
        public override WebSocketTransportUsage TransportUsage
        {
            get { return base.TransportUsage; }
            set { base.TransportUsage = value; }
        }

        [ConfigurationProperty(ConfigurationStrings.SubProtocol, DefaultValue = WebSocketTransportSettings.SoapSubProtocol)]
        [StringValidator(MinLength = 0)]
        public override string SubProtocol
        {
            get { return base.SubProtocol; }
            set { base.SubProtocol = value; }
        }
    }
}
