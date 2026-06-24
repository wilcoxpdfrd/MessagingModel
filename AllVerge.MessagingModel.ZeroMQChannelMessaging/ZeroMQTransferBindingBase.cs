using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;

using AllVerge.Core.Resource;

using AllVerge.Core.ServiceModel.ZeroMQ.Channels;
using AllVerge.Core.ServiceModel.ZeroMQ.Configuration;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    /// <summary>
    /// Request Binding for ZeroMQ. 
    /// </summary>
    public abstract class ZeroMQTransferBindingBase : Binding
    {
        // private BindingElements
        private ZeroMQTransferTransportBindingElementBase transport;
        private SecurityBindingElement security;
        private TextMessageEncodingBindingElement encoding;
        //private SecurityBindingElement messageSecurity;

        protected ZeroMQTransferBindingBase()
        {
            Initialize();
        }

        protected ZeroMQTransferBindingBase(string configurationName) : this()
        {
            ApplyConfiguration(configurationName);
        }

        protected ZeroMQTransferBindingBase(ZeroMQTransferBindingElement configurationElement) : this()
        {
            ApplyConfiguration(configurationElement);
        }

        public override string Scheme { get => transport.Scheme; }

        internal ZeroMQTransferTransportBindingElementBase Transport { get => transport; set => transport = value; }

        internal TextMessageEncodingBindingElement Encoding { get => encoding; set => encoding = value; }

        public Encoding TextEncoding
        {
            get
            {
                return encoding.WriteEncoding;
            }

            set
            {
                encoding.WriteEncoding = value;
            }
        }

        [DefaultValue(TransportDefaults.ManualAddressing)]
        public bool ManualAddressing { get => transport.ManualAddressing; set => transport.ManualAddressing = value; }

        [DefaultValue(TransportDefaults.MaxBufferPoolSize)]
        public long MaxBufferPoolSize { get => transport.MaxBufferPoolSize; set => transport.MaxBufferPoolSize = value; }

        [DefaultValue(TransportDefaults.MaxReceivedMessageSize)]
        public long MaxReceivedMessageSize { get => transport.MaxReceivedMessageSize; set => transport.MaxReceivedMessageSize = value; }

        public EnvelopeVersion EnvelopeVersion => GetEnvelopeVersion();

        internal abstract EnvelopeVersion GetEnvelopeVersion();

        /// <summary>
        /// Create the set of binding elements that make up this binding. 
        /// NOTE: order of binding elements is important.
        /// </summary>
        /// <returns></returns>
        public override BindingElementCollection CreateBindingElements()
        {
            BindingElementCollection bindingElements = new BindingElementCollection();

            //ToDo: message security binding element ...

            bindingElements.Add(encoding);

            //ToDo: transport security binding element ...?

            if (security != null)

                bindingElements.Add(security);

            bindingElements.Add(transport);

            return bindingElements.Clone();
        }

        private void ApplyConfiguration(string configurationName)
        {
            ZeroMQTransferBindingCollectionElement requestBindingsection = ZeroMQTransferBindingCollectionElement.GetBindingCollectionElement();
            ZeroMQTransferBindingElement requestBindingElement = requestBindingsection.Bindings[configurationName];
            if (requestBindingElement == null)
            {
                throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
                    "There is no binding named {0} at {1}.", configurationName, requestBindingsection.BindingName));
            }
            else
            {
                this.ApplyConfiguration(requestBindingElement);
            }
        }

        private void ApplyConfiguration(ZeroMQTransferBindingElement configurationElement)
        {
            configurationElement.SetBindingElementType(this.GetType());

            configurationElement.ApplyConfiguration(this);
        }

        void Initialize()
        {
            // Note: transport can be overriden in ApplyConfiguration

            transport = new ZeroMQTcpTransferTransportBindingElement();
            security = null;
            encoding = new TextMessageEncodingBindingElement();

            OnInitialize();
        }

        void Initialize(ZeroMQTransportProtocols protocol)
        {
            // Note transport can be overriden in ApplyConfiguration

            switch (protocol)
            {
                case ZeroMQTransportProtocols.TCP:
                    this.transport = new ZeroMQTcpTransferTransportBindingElement();
                    break;
                case ZeroMQTransportProtocols.IPC:
                    this.transport = new ZeroMQIpcTransferTransportBindingElement();
                    break;
            }

            OnInitialize();
        }

        /// <summary>
        /// Override to set <see cref="MessageVersion"/> of <see cref="Encoding"/>.
        /// </summary>
        protected abstract void OnInitialize();

        void InitializeFrom(ZeroMQConnectionOrientedTransportBindingElementBase zeroMQTransportBindingElement, TextMessageEncodingBindingElement textMessageEncodingBindingElement)
        {
            switch (zeroMQTransportBindingElement.Scheme)
            {
                case ResourceProtocolSchemes.ZEROMQ_TCP_DELIMITED:
                    this.transport = new ZeroMQTcpTransferTransportBindingElement();
                    break;
                case ResourceProtocolSchemes.ZEROMQ_IPC_DELIMITED:
                    this.transport = new ZeroMQIpcTransferTransportBindingElement();
                    break;
            }
            this.transport.MaxBufferPoolSize = zeroMQTransportBindingElement.MaxReceivedMessageSize;
            this.transport.MaxReceivedMessageSize = zeroMQTransportBindingElement.MaxReceivedMessageSize;
            this.transport.ManualAddressing = zeroMQTransportBindingElement.ManualAddressing;

            ((TextMessageEncodingBindingElement)this.encoding).WriteEncoding = textMessageEncodingBindingElement.WriteEncoding;
            
            textMessageEncodingBindingElement.ReaderQuotas.CopyTo(((TextMessageEncodingBindingElement)this.encoding).ReaderQuotas);
        }
    }
}
