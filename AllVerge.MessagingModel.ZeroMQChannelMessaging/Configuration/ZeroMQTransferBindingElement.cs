using AllVerge.Core.ServiceModel.Channels;
using AllVerge.Core.ServiceModel.Transfer.Configuration;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Runtime;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration
{
    public static class ZeroMQTransferBindingElementExtensions
    {
        public static ZeroMQTransferBindingBase GetBinding(this ZeroMQTransferBindingElement transferBindingElement, TransferMessageEncodings transferMessageEncoding)
        {
            ZeroMQTransferBindingBase transferBinding;

            switch (transferMessageEncoding)
            {
                default:
                case TransferMessageEncodings.Default:
                case TransferMessageEncodings.Binary:
                case TransferMessageEncodings.BinaryPlusGzip:
                case TransferMessageEncodings.BinaryPlusDeflate:
                case TransferMessageEncodings.Soap12:
                case TransferMessageEncodings.Soap12WSAddressingAugust2004:
                case TransferMessageEncodings.Soap12WSAddressing10:
                    transferBinding = new ZeroMQTransferSoap12MessageBinding(transferBindingElement);
                    break;
                case TransferMessageEncodings.Soap11:
                case TransferMessageEncodings.Soap11WSAddressingAugust2004:
                case TransferMessageEncodings.Soap11WSAddressing10:
                    transferBinding = new ZeroMTransferQSoap11MessageBinding(transferBindingElement);
                    break;
                case TransferMessageEncodings.Json:
                    transferBinding = new ZeroMQTransferTextMessageBinding(transferBindingElement);
                    break;
                case TransferMessageEncodings.Xml:
                    transferBinding = new ZeroMQTransferTextMessageBinding(transferBindingElement);
                    break;
                case TransferMessageEncodings.Html:
                    transferBinding = new ZeroMQTransferTextMessageBinding(transferBindingElement);
                    break;
                case TransferMessageEncodings.FormUrlEncoded:
                    transferBinding = new ZeroMQTransferTextMessageBinding(transferBindingElement);
                    break;
                case TransferMessageEncodings.FormMultipartData:
                    transferBinding = new ZeroMQTransferTextMessageBinding(transferBindingElement);
                    break;
                /// <summary>The "Raw" (byte stream) format.</summary>
                case TransferMessageEncodings.Raw:
                    transferBinding = null;
                    break;
            }

            return transferBinding;
        }
    }


    public class ZeroMQTransferBindingElement : StandardBindingElement
    {
        private Type bindingElementType;

        internal void SetBindingElementType(Type type)
        {
            if (!typeof(ZeroMQTransferBindingBase).IsAssignableFrom(type))

                throw new ArgumentException($"Parameter must derive from {nameof(ZeroMQTransferBindingBase)}", nameof(type));

            this.bindingElementType = type;
        }

        protected override Type BindingElementType => this.bindingElementType;

        [ConfigurationProperty(ConfigurationStrings.Transport, DefaultValue = ZeroMQTransferTransportDefaults.ZeroMQTransportProtocol)]
        public ZeroMQTransportProtocols Protocol
        {
            get { return (ZeroMQTransportProtocols)base[ConfigurationStrings.Transport]; }
            set { base[ConfigurationStrings.Transport] = value; }
        }

        [ConfigurationProperty(ConfigurationStrings.ManualAddressing, DefaultValue = ZeroMQTransferTransportDefaults.ManualAddressing)]
        public bool ManualAddressing
        {
            get { return (bool)base[ConfigurationStrings.ManualAddressing]; }
            set { base[ConfigurationStrings.ManualAddressing] = value; }
        }

        [ConfigurationProperty(ConfigurationStrings.MaxBufferPoolSize, DefaultValue = TransportDefaults.MaxBufferPoolSize)]
        [LongValidator(MinValue = 0)]
        public long MaxBufferPoolSize
        {
            get { return (long)base[ConfigurationStrings.MaxBufferPoolSize]; }
            set { base[ConfigurationStrings.MaxBufferPoolSize] = value; }
        }

        [ConfigurationProperty(ConfigurationStrings.MaxReceivedMessageSize, DefaultValue = TransportDefaults.MaxReceivedMessageSize)]
        [LongValidator(MinValue = 1)]
        public long MaxReceivedMessageSize
        {
            get { return (long)base[ConfigurationStrings.MaxReceivedMessageSize]; }
            set { base[ConfigurationStrings.MaxReceivedMessageSize] = value; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(FxCop.Category.Configuration, "Configuration104",
                    Justification = "This attribute comes from previous releases.")]
        [ConfigurationProperty(ConfigurationStrings.TextEncoding, DefaultValue = TextEncoderDefaults.EncodingString)]
        [TypeConverter(typeof(EncodingConverter))]
        public Encoding TextEncoding
        {
            get { return (Encoding)base[ConfigurationStrings.TextEncoding]; }
            set { base[ConfigurationStrings.TextEncoding] = value; }
        }

        [ConfigurationProperty("messageHandlerFactory", DefaultValue = null)]
        [TransferMessagingHandlerFactoryValidator]
        public TransferMessagingHandlerFactoryElement MessageHandlerFactory
        {
            get
            {
                return (TransferMessagingHandlerFactoryElement)this["messageHandlerFactory"];
            }
            set
            {
                this["messageHandlerFactory"] = (object)value;
            }
        }

        protected override void OnApplyConfiguration(Binding binding)
        {
            ZeroMQTransferBindingBase transferBinding = (ZeroMQTransferBindingBase)binding;

            switch (Protocol)
            {
                case ZeroMQTransportProtocols.TCP:
                default:
                    transferBinding.Transport = new ZeroMQTcpTransferTransportBindingElement();
                    break;
                case ZeroMQTransportProtocols.IPC:
                    transferBinding.Transport = new ZeroMQIpcTransferTransportBindingElement();
                    break;
            }

            transferBinding.ManualAddressing = this.ManualAddressing;
            transferBinding.MaxBufferPoolSize = this.MaxBufferPoolSize;
            transferBinding.MaxReceivedMessageSize = this.MaxReceivedMessageSize;

            transferBinding.TextEncoding = this.TextEncoding;

            if (this.MessageHandlerFactory != null)

                transferBinding.Transport.MessageHandlerFactory = ZeroMQTransferMessagingHandlerFactory.CreateFromConfigurationElement(this.MessageHandlerFactory);

            //this.Security.ApplyConfiguration(requestBinding.Security);
        }

        public static ZeroMQTransferBindingElement CreateTcpTransferBindingElement()
        {
            return new ZeroMQTransferBindingElement() { Protocol = ZeroMQTransportProtocols.TCP };
        }

        public static ZeroMQTransferBindingElement CreateIpcTransferBindingElement()
        {
            return new ZeroMQTransferBindingElement() { Protocol = ZeroMQTransportProtocols.IPC };
        }

    }
}