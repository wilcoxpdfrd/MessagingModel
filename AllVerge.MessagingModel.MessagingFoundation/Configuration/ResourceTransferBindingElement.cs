using System;
using System.ComponentModel;
using System.Configuration;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace AllVerge.MessagingModel.MessagingFoundation.Configuration
{
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    public static class ResourceTransferBindingElementExtensions
    {
        public static ResourceTransferBinding GetBinding(this ResourceTransferBindingElement transferBindingElement)
        {
            ResourceTransferBinding transferBinding = new ResourceTransferBinding();

            transferBindingElement.ApplyConfiguration(transferBinding);

            return transferBinding;
        }
    }

    /// <summary>
    /// TransferBindingElement for TransferBinding
    /// </summary>
    public class ResourceTransferBindingElement : HttpBindingBaseElement
    {
        public ResourceTransferBindingElement(string name)
            : base(name)
        {
        }

        public ResourceTransferBindingElement()
            : this(null)
        {
        }

        [ConfigurationProperty(ConfigurationStrings.Security)]
        public BasicHttpSecurityElement Security
        {
            get { return (BasicHttpSecurityElement)base[ConfigurationStrings.Security]; }
        }

        protected override Type BindingElementType
        {
            get { return typeof(ResourceTransferBinding); }
        }

        [ConfigurationProperty(ConfigurationStrings.MessageEncoding, DefaultValue = MessageEncodingFormat.Default)]
        [ServiceModelEnumValidator(typeof(MessageEncodingFormatHelper))]
        public MessageEncodingFormat Format
        {
            get { return (MessageEncodingFormat)base[ConfigurationStrings.MessageEncoding]; }
            set { base[ConfigurationStrings.MessageEncoding] = value; }
        }

        [ConfigurationProperty(ConfigurationStrings.MaxPendingAccepts, DefaultValue = HttpTransportDefaults.DefaultMaxPendingAccepts)]
        public int TransportMaxPendingAccepts
        {
            get { return (int)base[ConfigurationStrings.MaxPendingAccepts]; }
            set { base[ConfigurationStrings.MaxPendingAccepts] = value; }
        }

        [ConfigurationProperty(ConfigurationStrings.WebSocketSettingsSectionName)]
        public ResourceTransferWebSocketTransportSettingsElement WebSocketTransportSettings
        {
            get { return (ResourceTransferWebSocketTransportSettingsElement)base[ConfigurationStrings.WebSocketSettingsSectionName]; }
            set { base[ConfigurationStrings.WebSocketSettingsSectionName] = value; }
        }

        public override void InitializeFrom(Binding binding)
        {
            base.InitializeFrom(binding);

            ResourceTransferBinding transferBinding = (ResourceTransferBinding)binding;
            
            this.Security.InitializeFrom(transferBinding.BasicHttpSecurity);
        }

        protected override void OnApplyConfiguration(Binding binding)
        {
            base.OnApplyConfiguration(binding);

            ResourceTransferBinding transferBinding = (ResourceTransferBinding)binding;
            
            transferBinding.TransferMessageEncodingBindingElement.Format = this.Format;

            this.WebSocketTransportSettings.ApplyConfiguration(transferBinding.WebSocketSettings);
            this.Security.ApplyConfiguration(transferBinding.BasicHttpSecurity);

            HttpTransportBindingElement httpTransportBindingElement = transferBinding.GetTransport() as HttpTransportBindingElement;

            switch (this.Format)
            {
                case MessageEncodingFormat.Soap11:
                    transferBinding.TransferMessageEncodingBindingElement.MessageVersion = MessageVersion.Soap11;
                    break;
                case MessageEncodingFormat.Soap11WSAddressingAugust2004:
                    transferBinding.TransferMessageEncodingBindingElement.MessageVersion = MessageVersion.Soap11WSAddressingAugust2004;
                    break;
                case MessageEncodingFormat.Soap11WSAddressing10:
                    transferBinding.TransferMessageEncodingBindingElement.MessageVersion = MessageVersion.Soap11WSAddressing10;
                    break;
                case MessageEncodingFormat.Soap12:
                case MessageEncodingFormat.Binary:
                case MessageEncodingFormat.BinaryPlusGzip:
                case MessageEncodingFormat.BinaryPlusDeflate:
                case MessageEncodingFormat.Default:
                    transferBinding.TransferMessageEncodingBindingElement.MessageVersion = MessageVersion.Soap12;
                    break;
                case MessageEncodingFormat.Soap12WSAddressingAugust2004:
                    transferBinding.TransferMessageEncodingBindingElement.MessageVersion = MessageVersion.Soap12WSAddressingAugust2004;
                    break;
                case MessageEncodingFormat.Soap12WSAddressing10:
                    transferBinding.TransferMessageEncodingBindingElement.MessageVersion = MessageVersion.Soap12WSAddressingAugust2004;
                    break;
                case MessageEncodingFormat.Text:
                case MessageEncodingFormat.Html:
                case MessageEncodingFormat.Json:
                case MessageEncodingFormat.Xml:
                case MessageEncodingFormat.Raw:
                    httpTransportBindingElement.ManualAddressing = true;
                    transferBinding.TransferMessageEncodingBindingElement.MessageVersion = MessageVersion.None;
                    break;
            }

            httpTransportBindingElement.MaxPendingAccepts = this.TransportMaxPendingAccepts;
        }
    }
}
