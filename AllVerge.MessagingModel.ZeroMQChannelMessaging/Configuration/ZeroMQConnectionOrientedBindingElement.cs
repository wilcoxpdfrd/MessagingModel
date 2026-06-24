using AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels;
using AllVerge.SystemPrimitives.Net;
using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration
{
    public class ZeroMQConnectionOrientedBindingElement : StandardBindingElement
    {
        private static readonly Type baseBindingElementType = typeof(ZeroMQConnectionOrientedBindingBase);
        private Type bindingElementType;
        private ConfigurationPropertyCollection properties;

        public ZeroMQConnectionOrientedBindingElement(string configurationName)
            : base(configurationName)
        {
        }

        public ZeroMQConnectionOrientedBindingElement()
            : this(null)
        {
            this.bindingElementType = baseBindingElementType;
        }

        protected override Type BindingElementType
        {
            get { return this.bindingElementType; }
        }

        internal void SetBindingElementType(Type bindingElementType)
        {
            if (bindingElementType == null)

                throw new ArgumentNullException(nameof(bindingElementType));

            if (baseBindingElementType.IsAssignableFrom(bindingElementType) && 
                baseBindingElementType != bindingElementType)

                this.bindingElementType = bindingElementType;
            
            else
            
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument(
                    PublicSR.Format(
                        PublicSR.ConfigInvalidTypeForBinding,
                        baseBindingElementType.AssemblyQualifiedName,
                        bindingElementType.AssemblyQualifiedName
                    )
                );
        }

        [ConfigurationProperty(ConfigurationStrings.Transport, DefaultValue = ZeroMQTransportDefaults.ZeroMQTransportProtocol)]
        public ZeroMQTransportProtocols Protocol
        {
            get { return (ZeroMQTransportProtocols)base[ConfigurationStrings.Transport]; }
            set { base[ConfigurationStrings.Transport] = value; }
        }

        [ConfigurationProperty(ConfigurationStrings.TransferMode, DefaultValue = ZeroMQTransportDefaults.TransferMode)]
        public TransferMode TransferMode
        {
            get { return (TransferMode)base[ConfigurationStrings.TransferMode]; }
            set { base[ConfigurationStrings.TransferMode] = value; }
        }


        [ConfigurationProperty(ConfigurationStrings.ManualAddressing, DefaultValue = TransportDefaults.ManualAddressing)]
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

        [ConfigurationProperty(ConfigurationStrings.MessageEncoding, DefaultValue = ZeroMQConnectionOrientedBindingDefaults.MessageEncoding)]
        [ServiceModelEnumValidator(typeof(ZeroMQMessageEncodingHelper))]
        public ZeroMQMessageEncoding MessageEncoding
        {
            get { return (ZeroMQMessageEncoding)base[ConfigurationStrings.MessageEncoding]; }
            set { base[ConfigurationStrings.MessageEncoding] = value; }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (properties == null)
                {
                    ConfigurationPropertyCollection configurationPropertyCollection = base.Properties;
                    configurationPropertyCollection.Add(new ConfigurationProperty(ConfigurationStrings.Transport, typeof(ZeroMQTransportProtocols), ZeroMQTransportDefaults.ZeroMQTransportProtocol, null, null, ConfigurationPropertyOptions.None));
                    configurationPropertyCollection.Add(new ConfigurationProperty(ConfigurationStrings.ManualAddressing, typeof(bool), TransportDefaults.ManualAddressing, null, null, ConfigurationPropertyOptions.None));
                    configurationPropertyCollection.Add(new ConfigurationProperty(ConfigurationStrings.MaxBufferPoolSize, typeof(long), TransportDefaults.MaxBufferPoolSize, null, new LongValidator(0L, long.MaxValue, rangeIsExclusive: false), ConfigurationPropertyOptions.None));
                    configurationPropertyCollection.Add(new ConfigurationProperty(ConfigurationStrings.MaxReceivedMessageSize, typeof(long), TransportDefaults.MaxReceivedMessageSize, null, new LongValidator(1L, long.MaxValue, rangeIsExclusive: false), ConfigurationPropertyOptions.None));
                    properties = configurationPropertyCollection;
                }
                return properties;
            }
        }

        public override void InitializeFrom(Binding binding)
        {
            base.InitializeFrom(binding);
            ZeroMQConnectionOrientedBindingBase zeroMQBinding = (ZeroMQConnectionOrientedBindingBase)binding;
            switch (zeroMQBinding.Scheme)
            {
                case TransportProtocolSchemes.ZEROMQ_TCP_DELIMITED:
                    this.Protocol = ZeroMQTransportProtocols.TCP;
                    break;
                case TransportProtocolSchemes.ZEROMQ_IPC_DELIMITED:
                    //this.Protocol = ZeroMQTransportProtocols.IPC;
                    //break;
                default:
                    throw new ArgumentException(
                        PublicSR.Format(
                            PublicSR.BindingProtocolMappingNotDefined, 
                            zeroMQBinding.Scheme));
            }
            this.TransferMode = zeroMQBinding.TransferMode;
            this.ManualAddressing = zeroMQBinding.ManualAddressing;
            this.MaxBufferPoolSize = zeroMQBinding.MaxBufferPoolSize;
            this.MaxReceivedMessageSize = zeroMQBinding.MaxReceivedMessageSize;
        }

        protected override void OnApplyConfiguration(Binding binding)
        {
            ZeroMQConnectionOrientedBindingBase zeroMQBinding = (ZeroMQConnectionOrientedBindingBase)binding;
            switch (Protocol)
            {
                case ZeroMQTransportProtocols.TCP:
                    zeroMQBinding.Transport = new ZeroMQTcpConnectionOrientedTransportBindingElement() { TransferMode = this.TransferMode };
                    break;
                case ZeroMQTransportProtocols.IPC:
                    // zeroMQBinding.Transport = new ZeroMQIpcConnectionOrientedTransportBindingElement() { TransferMode = this.TransferMode };
                    // break;
                default:
                    throw new ArgumentException(
                        PublicSR.Format(
                            PublicSR.BindingProtocolMappingNotDefined,
                            zeroMQBinding.Scheme));

            }

            zeroMQBinding.ManualAddressing = this.ManualAddressing;
            zeroMQBinding.MaxBufferPoolSize = this.MaxBufferPoolSize;
            zeroMQBinding.MaxReceivedMessageSize = this.MaxReceivedMessageSize;

            zeroMQBinding.EnsureEncoding(this.MessageEncoding);
        }

        public static ZeroMQConnectionOrientedBindingElement CreateTcpConnectionOrientedBindingElement(ZeroMQMessageEncoding messageEncoding)
        {
            //if (messageEncoding == ZeroMQMessageEncoding.Text)
            //    return new ZeroMQConnectionOrientedBindingElement() { Protocol = ZeroMQTransportProtocols.TCP, TransferMode = TransferMode.Streamed, MessageEncoding = messageEncoding };
            return new ZeroMQConnectionOrientedBindingElement() { Protocol = ZeroMQTransportProtocols.TCP, MessageEncoding = messageEncoding };
        }

        //public static ZeroMQConnectionOrientedBindingElement CreateIpcConnectionOrientedBindingElement(ZeroMQMessageEncoding messageEncoding)
        //{
        //    if (messageEncoding == ZeroMQMessageEncoding.Text)
        //        return new ZeroMQConnectionOrientedBindingElement() { Protocol = ZeroMQTransportProtocols.IPC, TransferMode = TransferMode.Streamed, MessageEncoding = messageEncoding };
        //    return new ZeroMQConnectionOrientedBindingElement() { Protocol = ZeroMQTransportProtocols.IPC, MessageEncoding = messageEncoding };
        //}
    }

    public static class ZeroMQConnectionOrientedBindingElementExtensions
    {
        //public static ZeroMQConnectionOrientedTextMessageBinding GetTextMessageBinding(this ZeroMQConnectionOrientedBindingElement bindingElement)
        //{
        //    return new ZeroMQConnectionOrientedTextMessageBinding(bindingElement);
        //}

        //public static ZeroMQConnectionOrientedSoap11MessageBinding GetSoap11MessageBinding(this ZeroMQConnectionOrientedBindingElement bindingElement)
        //{
        //    return new ZeroMQConnectionOrientedSoap11MessageBinding(bindingElement);
        //}

        public static ZeroMQConnectionOrientedSoap12MessageBinding GetSoap12MessageBinding(this ZeroMQConnectionOrientedBindingElement bindingElement)
        {
            return new ZeroMQConnectionOrientedSoap12MessageBinding(bindingElement);
        }
    }
}
