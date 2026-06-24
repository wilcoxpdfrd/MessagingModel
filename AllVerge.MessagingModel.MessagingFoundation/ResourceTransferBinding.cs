using System;
using System.ComponentModel;
using System.Runtime;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    using System.ServiceModel;
    using System.ServiceModel.Channels;

    public class ResourceTransferBinding : 
        HttpBindingBase, IBindingRuntimePreferences
    {
        internal static MessageEncodingFormatBindingElement DefaultBindingElement = 
            new MessageEncodingFormatBindingElement();

        BasicHttpSecurity basicHttpSecurity;

        public ResourceTransferBinding()
            : base()
        {
            Initialize();
        }

        private void Initialize()
        {
            WebSocketSettings.TransportUsage = NetHttpBindingDefaults.TransportUsage;
            basicHttpSecurity = new BasicHttpSecurity();
        }

        protected override void ConfigureMessageEncodingBindingElement()
        {
            TransferMessageEncodingBindingElement = (MessageEncodingFormatBindingElement)DefaultBindingElement.Clone();
        }

        public new XmlDictionaryReaderQuotas ReaderQuotas
        {
            get
            {
                return TransferMessageEncodingBindingElement.ReaderQuotas;
            }

            set
            {
                if (value == null)
                {
                    throw FxTrace.Exception.ArgumentNull("value");
                }

                value.CopyTo(TransferMessageEncodingBindingElement.ReaderQuotas);
                SetReaderQuotas(value);
            }
        }

        public override string Scheme
        {
            get
            {
                return GetTransport().Scheme;
            }
        }

        public new Encoding TextEncoding
        {
            get
            {
                return TransferMessageEncodingBindingElement.WriteEncoding;
            }

            set
            {
                TransferMessageEncodingBindingElement.WriteEncoding = value;
            }
        }

        public WebSocketTransportSettings WebSocketSettings
        {
            get
            {
                return InternalWebSocketSettings;
            }
        }

        bool IBindingRuntimePreferences.ReceiveSynchronously
        {
            get { return false; }
        }

        internal MessageEncodingFormatBindingElement TransferMessageEncodingBindingElement { get; private set; }

        protected override void InitializeFrom(MessageEncodingBindingElement encodingBindingElement)
        {
            if (encodingBindingElement is MessageEncodingFormatBindingElement)
            {
                MessageEncodingFormatBindingElement text = (MessageEncodingFormatBindingElement)encodingBindingElement;
                this.TextEncoding = text.WriteEncoding;
                this.ReaderQuotas = text.ReaderQuotas;
            }
            else

                base.InitializeFrom(encodingBindingElement);
        }

        public override BasicHttpSecurity BasicHttpSecurity => basicHttpSecurity;

        protected override EnvelopeVersion GetEnvelopeVersion() => TransferMessageEncodingBindingElement.MessageVersion.Envelope;

        protected override void SetReaderQuotas(XmlDictionaryReaderQuotas readerQuotas)
        {
            readerQuotas.CopyTo(TransferMessageEncodingBindingElement.ReaderQuotas);
        }

        public override BindingElementCollection CreateBindingElements()
        {
            CheckSettings();

            // return collection of BindingElements
            BindingElementCollection bindingElements = new BindingElementCollection();

            // order of BindingElements is important

            // add security (*optional)
            SecurityBindingElement messageSecurity = BasicHttpSecurity.CreateMessageSecurity();
            if (messageSecurity != null)
            {
                bindingElements.Add(messageSecurity);
            }

            bindingElements.Add(TransferMessageEncodingBindingElement);

            // add transport (http or https)
            bindingElements.Add(GetTransport());

            return bindingElements.Clone();
        }
    }
}
