using AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels;
using AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration;
using AllVerge.SystemPrimitives.Net;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    /// <summary>
    /// Binding for ZeroMQ. 
    /// </summary>
    public abstract class ZeroMQConnectionOrientedBindingBase : Binding
    {
        private OptionalReliableSession reliableSession;
        // private BindingElements
        private ZeroMQConnectionOrientedTransportBindingElementBase transport;
        private MessageEncodingBindingElement encoding;
        private TransactionFlowBindingElement context;
        private ReliableSessionBindingElement session;
        private ZeroMQSecurity security = new ZeroMQSecurity();

        protected ZeroMQConnectionOrientedBindingBase() { Initialize(); }
        protected ZeroMQConnectionOrientedBindingBase(SecurityMode securityMode)
            : this()
        {
            this.security.Mode = securityMode;
        }

        protected ZeroMQConnectionOrientedBindingBase(SecurityMode securityMode, bool reliableSessionEnabled)
            : this(securityMode)
        {
            this.ReliableSession.Enabled = reliableSessionEnabled;
        }

        protected ZeroMQConnectionOrientedBindingBase(string configurationName)
            : this()
        {
            ApplyConfiguration(configurationName);
        }

        protected ZeroMQConnectionOrientedBindingBase(ZeroMQConnectionOrientedBindingElement element)
            : this()
        {
            ApplyConfiguration(element);
        }
        
        ZeroMQConnectionOrientedBindingBase(ZeroMQConnectionOrientedTransportBindingElementBase transport, MessageEncodingBindingElement encoding, TransactionFlowBindingElement context, ReliableSessionBindingElement session, ZeroMQSecurity security)
            : this()
        {
            this.security = security;
            this.ReliableSession.Enabled = session != null;
            InitializeFrom(transport, encoding, context, session);
        }

        internal ZeroMQConnectionOrientedTransportBindingElementBase Transport { get => transport; set => transport = value; }

        protected MessageEncodingBindingElement Encoding { get => encoding; }

        [DefaultValue(ZeroMQTransportDefaults.TransactionsEnabled)]
        public bool TransactionFlow
        {
            get { return context.Transactions; }
            set { context.Transactions = value; }
        }

        public TransactionProtocol TransactionProtocol
        {
            get { return this.context.TransactionProtocol; }
            set { this.context.TransactionProtocol = value; }
        }

        [DefaultValue(ConnectionOrientedTransportDefaults.TransferMode)]
        public TransferMode TransferMode
        {
            get { return this.transport.TransferMode; }
            set { this.transport.TransferMode = value; }
        }

        [DefaultValue(ConnectionOrientedTransportDefaults.HostNameComparisonMode)]
        public HostNameComparisonMode HostNameComparisonMode
        {
            get { return transport.HostNameComparisonMode; }
            set { transport.HostNameComparisonMode = value; }
        }

        [DefaultValue(TransportDefaults.ManualAddressing)]
        public bool ManualAddressing
        {
            get => transport.ManualAddressing; set => transport.ManualAddressing = value;
        }

        [DefaultValue(TransportDefaults.MaxBufferPoolSize)]
        public long MaxBufferPoolSize
        {
            get { return transport.MaxBufferPoolSize; }
            set
            {
                transport.MaxBufferPoolSize = value;
            }
        }

        [DefaultValue(TransportDefaults.MaxBufferSize)]
        public int MaxBufferSize
        {
            get { return transport.MaxBufferSize; }
            set { transport.MaxBufferSize = value; }
        }

        public int MaxConnections
        {
            get { return transport.MaxPendingConnections; }
            set
            {
                transport.MaxPendingConnections = value;
                transport.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = value;
            }
        }

        internal bool IsMaxConnectionsSet
        {
            get { return transport.IsMaxPendingConnectionsSet; }
        }

        //public int ListenBacklog
        //{
        //    get { return transport.ListenBacklog; }
        //    set { transport.ListenBacklog = value; }
        //}

        //internal bool IsListenBacklogSet
        //{
        //    get { return transport.IsListenBacklogSet; }
        //}

        [DefaultValue(TransportDefaults.MaxReceivedMessageSize)]
        public long MaxReceivedMessageSize
        {
            get { return transport.MaxReceivedMessageSize; }
            set { transport.MaxReceivedMessageSize = value; }
        }

        //[DefaultValue(TcpTransportDefaults.PortSharingEnabled)]
        //public bool PortSharingEnabled
        //{
        //    get { return transport.PortSharingEnabled; }
        //    set { transport.PortSharingEnabled = value; }
        //}

        public XmlDictionaryReaderQuotas ReaderQuotas
        {
            get { return (encoding as TextMessageEncodingBindingElement)?.ReaderQuotas; }
            set
            {
                if (value == null)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                TextMessageEncodingBindingElement t = encoding as TextMessageEncodingBindingElement;
                if (t != null)
                {
                    value.CopyTo(t.ReaderQuotas);
                    return;
                }
                BinaryMessageEncodingBindingElement b = encoding as BinaryMessageEncodingBindingElement;
                if (b != null)
                {
                    value.CopyTo(b.ReaderQuotas);
                    return;
                }
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("value", "encoding is not expected type.");
            }
        }

        //bool IBindingRuntimePreferences.ReceiveSynchronously
        //{
        //    get { return false; }
        //}

        public OptionalReliableSession ReliableSession
        {
            get
            {
                return reliableSession;
            }
            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("value"));
                }
                this.reliableSession.CopySettings(value);
            }
        }

        public override string Scheme { get => transport.Scheme; }

        public EnvelopeVersion EnvelopeVersion => GetEnvelopeVersion();

        internal abstract EnvelopeVersion GetEnvelopeVersion();

        public ZeroMQSecurity Security
        {
            get { return security; }
            set
            {
                if (value == null)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                security = value;
            }
        }

        static TransactionFlowBindingElement GetDefaultTransactionFlowBindingElement()
        {
            return TransactionFlowBindingElement.Create(ZeroMQTransportDefaults.TransactionsEnabled);
        }

        void Initialize()
        {
            transport = new ZeroMQTcpConnectionOrientedTransportBindingElement();
            encoding = new BinaryMessageEncodingBindingElement();
            context = GetDefaultTransactionFlowBindingElement();
            session = new ReliableSessionBindingElement();
            this.reliableSession = new OptionalReliableSession(session);
            
            OnInitialize();
        }

        /// <summary>
        /// Override to set <see cref="MessageVersion"/> of <see cref="Encoding"/>.
        /// </summary>
        protected abstract void OnInitialize();

        void InitializeFrom(ZeroMQConnectionOrientedTransportBindingElementBase transportBindingElement, MessageEncodingBindingElement messageEncodingBindingElement, TransactionFlowBindingElement context, ReliableSessionBindingElement session)
        {
            Fx.Assert(transport != null, "Invalid (null) transport value.");
            Fx.Assert(encoding != null, "Invalid (null) encoding value.");
            Fx.Assert(context != null, "Invalid (null) context value.");
            Fx.Assert(security != null, "Invalid (null) security value.");

            // transport

            switch (transportBindingElement.Scheme)
            {
                case TransportProtocolSchemes.ZEROMQ_TCP_DELIMITED:
                    this.transport = new ZeroMQTcpConnectionOrientedTransportBindingElement();
                    break;
                case TransportProtocolSchemes.ZEROMQ_IPC_DELIMITED:
                    //this.transport = new ZeroMQIpcConnectionOrientedTransportBindingElement();
                    throw new NotSupportedException(Scheme);
                    break;
            }

            this.transport.TransferMode = transportBindingElement.TransferMode;

            if (transportBindingElement.TransferMode == TransferMode.Buffered)
            {
                if (!(messageEncodingBindingElement is BinaryMessageEncodingBindingElement))

                    throw new Exception();
            }

            this.transport.HostNameComparisonMode = transportBindingElement.HostNameComparisonMode;
            //this.MaxBufferPoolSize = transportBindingElement.MaxBufferPoolSize;
            //this.MaxBufferSize = transportBindingElement.MaxBufferSize;
            if (transportBindingElement.IsMaxPendingConnectionsSet)
            {
                //this.transport.m.MaxConnections = transportBindingElement.MaxPendingConnections;
                this.transport.MaxPendingConnections = transportBindingElement.MaxPendingConnections;
            }
            //if (transportBindingElement.IsListenBacklogSet)
            //{
            //    this.ListenBacklog = transportBindingElement.ListenBacklog;
            //}
            this.transport.MaxReceivedMessageSize = transportBindingElement.MaxReceivedMessageSize;
            //this.PortSharingEnabled = transportBindingElement.PortSharingEnabled;
            this.transport.TransferMode = transportBindingElement.TransferMode;
            this.transport.MaxBufferPoolSize = transportBindingElement.MaxReceivedMessageSize;
            this.transport.MaxReceivedMessageSize = transportBindingElement.MaxReceivedMessageSize;
            this.transport.ManualAddressing = transportBindingElement.ManualAddressing;

            // encoding
            UpdateOrSetEncoding(messageEncodingBindingElement);

            // context
            this.TransactionFlow = context.Transactions;
            this.TransactionProtocol = context.TransactionProtocol;

            //session
            if (session != null)
            {
                // only set properties that have standard binding manifestations
                this.session.InactivityTimeout = session.InactivityTimeout;
                this.session.Ordered = session.Ordered;
            }
        }

        internal void EnsureEncoding(ZeroMQMessageEncoding messageEncoding)
        {
            switch(messageEncoding)
            {
                case ZeroMQMessageEncoding.Binary:
                    UpdateOrSetEncoding(new BinaryMessageEncodingBindingElement());
                    break;
                //case ZeroMQMessageEncoding.Text:
                //    UpdateOrSetEncoding(new TextMessageEncodingBindingElement());
                //    break;
            }
        }

        private void UpdateOrSetEncoding(MessageEncodingBindingElement messageEncodingBindingElement)
        {
            BinaryMessageEncodingBindingElement b = messageEncodingBindingElement as BinaryMessageEncodingBindingElement;

            if (b == null)
            {
                TextMessageEncodingBindingElement t = messageEncodingBindingElement as TextMessageEncodingBindingElement;

                if (t == null)

                    this.encoding = messageEncodingBindingElement;

                else
                {
                    TextMessageEncodingBindingElement t1 = this.encoding as TextMessageEncodingBindingElement;

                    if (t1 == null)

                        this.encoding = messageEncodingBindingElement;

                    else
                    {
                        t1.WriteEncoding = t.WriteEncoding;
                        t.ReaderQuotas.CopyTo(t1.ReaderQuotas);
                    }
                }
            }
            else
            {
                BinaryMessageEncodingBindingElement b1 = this.encoding as BinaryMessageEncodingBindingElement;

                if (b1 == null)

                    this.encoding = messageEncodingBindingElement;

                else

                    b.ReaderQuotas.CopyTo(b1.ReaderQuotas);
            }
        }

        bool IsBindingElementsMatch(ZeroMQConnectionOrientedTransportBindingElementBase transport, TextMessageEncodingBindingElement encoding, TransactionFlowBindingElement context, ReliableSessionBindingElement session)
        {
            if (!this.transport.IsMatch(transport))
                return false;
            if (!MessageEncodingBindingElement.IsMatch(this.encoding, encoding))
                return false;
            if (!TransactionFlowBindingElement.IsMatch(this.context, context))
                return false;
            if (reliableSession.Enabled)
            {
                if (!ReliableSessionBindingElement.IsMatch(this.session, session))
                    return false;
            }
            else if (session != null)
                return false;

            return true;
        }

        void ApplyConfiguration(string configurationName)
        {
            ZeroMQConnectionOrientedBindingCollectionElement section = ZeroMQConnectionOrientedBindingCollectionElement.GetBindingCollectionElement();
            ZeroMQConnectionOrientedBindingElement element = section.Bindings[configurationName];
            if (element == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ConfigurationErrorsException(
                    PublicSR.Format(PublicSR.ConfigInvalidBindingConfigurationName,
                                 configurationName,
                                 ZeroMQConfigurationStrings.ZeroMQBindingCollectionElementName)));
            }
            else
            {
                ApplyConfiguration(element);
            }
        }

        private void ApplyConfiguration(ZeroMQConnectionOrientedBindingElement element)
        {
            element.SetBindingElementType(this.GetType());

            element.ApplyConfiguration(this);
        }

        // In the Win8 profile, some settings for the binding security are not supported.
        void CheckSettings()
        {
            //if (!UnsafeNativeMethods.IsTailoredApplication.Value)
            //{
            //    return;
            //}

            ZeroMQSecurity security = this.Security;
            if (security == null)
            {
                return;
            }

            SecurityMode mode = security.Mode;
            if (mode == SecurityMode.None)
            {
                return;
            }
            else if (mode == SecurityMode.Message)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(PublicSR.Format(PublicSR.UnsupportedSecuritySetting, "Mode", mode)));
            }

            // Message.ClientCredentialType = Certificate, IssuedToken or Windows are not supported.
            if (mode == SecurityMode.TransportWithMessageCredential)
            {
                MessageSecurityOverTcp message = security.Message;
                if (message != null)
                {
                    MessageCredentialType mct = message.ClientCredentialType;
                    if ((mct == MessageCredentialType.Certificate) || (mct == MessageCredentialType.IssuedToken) || (mct == MessageCredentialType.Windows))
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(PublicSR.Format(PublicSR.UnsupportedSecuritySetting, "Message.ClientCredentialType", mct)));
                    }
                }
            }

            // Transport.ClientCredentialType = Certificate is not supported.
            Fx.Assert((mode == SecurityMode.Transport) || (mode == SecurityMode.TransportWithMessageCredential), "Unexpected SecurityMode value: " + mode);
            TcpTransportSecurity transport = security.Transport;
            if ((transport != null) && (transport.ClientCredentialType == TcpClientCredentialType.Certificate))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(PublicSR.Format(PublicSR.UnsupportedSecuritySetting, "Transport.ClientCredentialType", transport.ClientCredentialType)));
            }
        }

        public override BindingElementCollection CreateBindingElements()
        {
            this.CheckSettings();

            // Recommended stack order:
            // TransactionFlow, ReliableSession, Security, CompositeDuplex, OneWay, StreamSecurity, MessageEncoding, Transport.

            // return collection of BindingElements
            BindingElementCollection bindingElements = new BindingElementCollection();
            // order of BindingElements is important

            // duplex session (transaction flow/reliable session) only supported with 
            // Buffered TransferMode on ConnectionOrientedTransport, and BinaryMessageEncoding ...
            if (transport.TransferMode == TransferMode.Buffered)
            {
                if (encoding is TextMessageEncodingBindingElement)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(PublicSR.Format(PublicSR.UnsupportedSecuritySetting, transport.TransferMode.ToString(), encoding.GetType().FullName)));
                // add context
                bindingElements.Add(context);
                // add session
                if (reliableSession.Enabled)
                    bindingElements.Add(session);
            }
            else
            {
                {
                    if (encoding is BinaryMessageEncodingBindingElement)
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(PublicSR.Format(PublicSR.UnsupportedSecuritySetting, transport.TransferMode.ToString(), encoding.GetType().FullName)));
                    // add context
                    bindingElements.Add(context);
                    // add session
                    if (reliableSession.Enabled)
                        bindingElements.Add(session);
                }
            }
            // add security (*optional)
#if ignore
            SecurityBindingElement wsSecurity = CreateMessageSecurity();
            if (wsSecurity != null)
                bindingElements.Add(wsSecurity);
            // add encoding
#endif
            bindingElements.Add(encoding);
#if ignore
            // add transport security
            BindingElement transportSecurity = CreateTransportSecurity();
            if (transportSecurity != null)
            {
                bindingElements.Add(transportSecurity);
            }
            transport.ExtendedProtectionPolicy = security.Transport.ExtendedProtectionPolicy;
#endif
            // add transport (tcp)
            bindingElements.Add(transport);

            return bindingElements.Clone();
        }

        internal static bool TryCreate(BindingElementCollection elements, out Binding binding)
        {
            binding = null;
            if (elements.Count > 6)
                return false;

            // collect all binding elements
            ZeroMQConnectionOrientedTransportBindingElementBase transport = null;
            TextMessageEncodingBindingElement encoding = null;
            TransactionFlowBindingElement context = null;
            ReliableSessionBindingElement session = null;
            SecurityBindingElement wsSecurity = null;
            BindingElement transportSecurity = null;

            foreach (BindingElement element in elements)
            {
                if (element is SecurityBindingElement)
                    wsSecurity = element as SecurityBindingElement;
                else if (element is TransportBindingElement)
                    transport = element as ZeroMQConnectionOrientedTransportBindingElementBase;
                else if (element is MessageEncodingBindingElement)
                    encoding = element as TextMessageEncodingBindingElement;
                else if (element is TransactionFlowBindingElement)
                    context = element as TransactionFlowBindingElement;
                else if (element is ReliableSessionBindingElement)
                    session = element as ReliableSessionBindingElement;
                else
                {
                    if (transportSecurity != null)
                        return false;
                    transportSecurity = element;
                }
            }

            if (transport == null)
                return false;
            if (encoding == null)
                return false;
            if (context == null)
                context = GetDefaultTransactionFlowBindingElement();

            TcpTransportSecurity tcpTransportSecurity = new TcpTransportSecurity();
            
            UnifiedSecurityMode mode = GetModeFromTransportSecurity(transportSecurity);

            ZeroMQSecurity security;
            if (!TryCreateSecurity(wsSecurity, mode, session != null, transportSecurity, tcpTransportSecurity, out security))
                return false;

            if (!SetTransportSecurity(transportSecurity, security.Mode, tcpTransportSecurity))
                return false;
            ZeroMQConnectionOrientedBindingBase zeroMQBinding = CreateBinding(transport, encoding, context, session, security);
            if (!zeroMQBinding.IsBindingElementsMatch(transport, encoding, context, session))
                return false;

            binding = zeroMQBinding;
            return true;
        }

        private static ZeroMQConnectionOrientedBindingBase CreateBinding(ZeroMQConnectionOrientedTransportBindingElementBase transport, TextMessageEncodingBindingElement encoding, TransactionFlowBindingElement context, ReliableSessionBindingElement session, ZeroMQSecurity security)
        {
            return null;
        }

        BindingElement CreateTransportSecurity()
        {
            return this.security.CreateTransportSecurity();
        }

        static UnifiedSecurityMode GetModeFromTransportSecurity(BindingElement transport)
        {
            return ZeroMQSecurity.GetModeFromTransportSecurity(transport);
        }

        static bool SetTransportSecurity(BindingElement transport, SecurityMode mode, TcpTransportSecurity transportSecurity)
        {
            return ZeroMQSecurity.SetTransportSecurity(transport, mode, transportSecurity);
        }

        SecurityBindingElement CreateMessageSecurity()
        {
            if (this.security.Mode == SecurityMode.Message || this.security.Mode == SecurityMode.TransportWithMessageCredential)
            {
                return this.security.CreateMessageSecurity(this.ReliableSession.Enabled);
            }
            else
            {
                return null;
            }
        }

        static bool TryCreateSecurity(SecurityBindingElement sbe, UnifiedSecurityMode mode, bool isReliableSession, BindingElement transportSecurity, TcpTransportSecurity tcpTransportSecurity, out ZeroMQSecurity security)
        {
            if (sbe != null)
                mode &= UnifiedSecurityMode.Message | UnifiedSecurityMode.TransportWithMessageCredential;
            else
                mode &= ~(UnifiedSecurityMode.Message | UnifiedSecurityMode.TransportWithMessageCredential);

            SecurityMode securityMode = SecurityModeHelper.ToSecurityMode(mode);
            Fx.Assert(SecurityModeHelper.IsDefined(securityMode), string.Format("Invalid SecurityMode value: {0}.", securityMode.ToString()));

            if (ZeroMQSecurity.TryCreate(sbe, securityMode, isReliableSession, transportSecurity, tcpTransportSecurity, out security))
                return true;

            return false;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeReaderQuotas()
        {
            return (!EncoderDefaults.IsDefaultReaderQuotas(this.ReaderQuotas));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeSecurity()
        {
            return this.security.InternalShouldSerialize();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeTransactionProtocol()
        {
            return (TransactionProtocol != ZeroMQTransportDefaults.TransactionProtocol);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeReliableSession()
        {
            return (this.ReliableSession.Ordered != ZeroMQReliableSessionDefaults.Ordered
                || this.ReliableSession.InactivityTimeout != ZeroMQReliableSessionDefaults.InactivityTimeout
                || this.ReliableSession.Enabled != ZeroMQReliableSessionDefaults.Enabled);
        }

        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public bool ShouldSerializeListenBacklog()
        //{
        //    return transport.ShouldSerializeListenBacklog();
        //}

        //[EditorBrowsable(EditorBrowsableState.Never)]
        //public bool ShouldSerializeMaxConnections()
        //{
        //    return transport.ShouldSerializeListenBacklog();
        //}
        
        //private const string NetMQBindingSectionName = "system.serviceModel/bindings/NetMQBinding";

        //// private BindingElements
        //private ZeroMQTransportBindingElementBase transport;
        //private TextMessageEncodingBindingElement encoding;
        ////private SecurityBindingElement messageSecurity;

        //protected ZeroMQBindingBase()
        //{
        //    Initialize();
        //}

        //protected ZeroMQBindingBase(string configurationName) : this()
        //{
        //    ApplyConfiguration(configurationName);
        //}

        //protected ZeroMQBindingBase(ZeroMQBindingElement configurationElement) : this()
        //{
        //    ApplyConfiguration(configurationElement);
        //}

        //internal ZeroMQTransportBindingElementBase Transport { get => transport; set => transport = value; }

        //protected TextMessageEncodingBindingElement Encoding { get => encoding; }

        //public override string Scheme { get => transport.Scheme; }

        //[DefaultValue(TransportDefaults.ManualAddressing)]
        //public bool ManualAddressing { get => transport.ManualAddressing; set => transport.ManualAddressing = value; }

        //[DefaultValue(TransportDefaults.MaxBufferPoolSize)]
        //public long MaxBufferPoolSize { get => transport.MaxBufferPoolSize; set => transport.MaxBufferPoolSize = value; }

        //[DefaultValue(TransportDefaults.MaxReceivedMessageSize)]
        //public long MaxReceivedMessageSize { get => transport.MaxReceivedMessageSize; set => transport.MaxReceivedMessageSize = value; }

        //public EnvelopeVersion EnvelopeVersion => GetEnvelopeVersion();

        //internal abstract EnvelopeVersion GetEnvelopeVersion();

        ///// <summary>
        ///// Create the set of binding elements that make up this binding. 
        ///// NOTE: order of binding elements is important.
        ///// </summary>
        ///// <returns></returns>
        //public override BindingElementCollection CreateBindingElements()
        //{
        //    BindingElementCollection bindingElements = new BindingElementCollection();

        //    //ToDo: message security binding element ...
        //    bindingElements.Add(encoding);
        //    //ToDo: transport security binding element ...?
        //    bindingElements.Add(transport);

        //    return bindingElements.Clone();
        //}

        //private void ApplyConfiguration(string configurationName)
        //{
        //    ZeroMQBindingCollectionElement section = (ZeroMQBindingCollectionElement)ConfigurationManager.GetSection(NetMQBindingSectionName);
        //    ZeroMQBindingElement element = section.Bindings[configurationName];
        //    if (element == null)
        //    {
        //        throw new ConfigurationErrorsException(string.Format(CultureInfo.CurrentCulture,
        //            "There is no binding named {0} at {1}.", configurationName, section.BindingName));
        //    }
        //    else
        //    {
        //        this.ApplyConfiguration(element);
        //    }
        //}

        //private void ApplyConfiguration(ZeroMQBindingElement configurationElement)
        //{
        //    configurationElement.SetBindingElementType(this.GetType());

        //    configurationElement.ApplyConfiguration(this);
        //}

        //void Initialize()
        //{
        //    // Note: transport can be overriden in ApplyConfiguration

        //    transport = new ZeroMQTcpTransportBindingElement();
        //    encoding = new TextMessageEncodingBindingElement();

        //    OnInitialize();
        //}

        //void Initialize(ZeroMQTransportProtocols protocol)
        //{
        //    // Note transport can be overriden in ApplyConfiguration

        //    switch (protocol)
        //    {
        //        case ZeroMQTransportProtocols.TCP:
        //            this.transport = new ZeroMQTcpTransportBindingElement();
        //            break;
        //        case ZeroMQTransportProtocols.IPC:
        //            this.transport = new ZeroMQIpcTransportBindingElement();
        //            break;
        //    }

        //    OnInitialize();
        //}

        ///// <summary>
        ///// Override to set <see cref="MessageVersion"/> of <see cref="Encoding"/>.
        ///// </summary>
        //protected abstract void OnInitialize();

        //void InitializeFrom(ZeroMQTransportBindingElementBase zeroMQTransportBindingElement, TextMessageEncodingBindingElement textMessageEncodingBindingElement)
        //{
        //    switch (zeroMQTransportBindingElement.Scheme)
        //    {
        //        case ResourceProtocolSchemes.ZEROMQ_TCP_DELIMITED:
        //            this.transport = new ZeroMQTcpTransportBindingElement();
        //            break;
        //        case ResourceProtocolSchemes.ZEROMQ_IPC_DELIMITED:
        //            this.transport = new ZeroMQIpcTransportBindingElement();
        //            break;
        //    }
        //    this.transport.MaxBufferPoolSize = zeroMQTransportBindingElement.MaxReceivedMessageSize;
        //    this.transport.MaxReceivedMessageSize = zeroMQTransportBindingElement.MaxReceivedMessageSize;
        //    this.transport.ManualAddressing = zeroMQTransportBindingElement.ManualAddressing;

        //    ((TextMessageEncodingBindingElement)this.encoding).WriteEncoding = textMessageEncodingBindingElement.WriteEncoding;

        //    textMessageEncodingBindingElement.ReaderQuotas.CopyTo(((TextMessageEncodingBindingElement)this.encoding).ReaderQuotas);
        //}
    }
}
