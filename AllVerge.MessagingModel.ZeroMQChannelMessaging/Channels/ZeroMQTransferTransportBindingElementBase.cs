using System;
using System.ComponentModel;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public abstract class ZeroMQTransferTransportBindingElementBase : TransportBindingElement
    {
        private HostNameComparisonMode _hostNameComparisonMode;
        private int _maxBufferSize;
        private bool _maxBufferSizeInitialized;
        private int _maxPendingAccepts;
        private string _method;
        private string _realm;
        private TransferMode _transferMode;
        private TimeSpan _requestInitializationTimeout;
        HttpAnonymousUriPrefixMatcher anonymousUriPrefixMatcher;

        protected ZeroMQTransferTransportBindingElementBase() : base()
        {
            _hostNameComparisonMode = ZeroMQTransferTransportDefaults.HostNameComparisonMode;
            _maxBufferSize = TransportDefaults.MaxBufferSize;
            _maxPendingAccepts = ZeroMQTransferTransportDefaults.DefaultMaxPendingAccepts;
            _method = string.Empty;
            _transferMode = ZeroMQTransferTransportDefaults.TransferMode;
        }

        protected ZeroMQTransferTransportBindingElementBase(ZeroMQTransferTransportBindingElementBase elementToBeCloned) : 
            base(elementToBeCloned)
        {
            _hostNameComparisonMode = elementToBeCloned._hostNameComparisonMode;
            _maxBufferSize = elementToBeCloned._maxBufferSize;
            _maxBufferSizeInitialized = elementToBeCloned._maxBufferSizeInitialized;
            _maxPendingAccepts = elementToBeCloned._maxPendingAccepts;
            _method = elementToBeCloned._method;
            _realm = elementToBeCloned._realm;
            _transferMode = elementToBeCloned._transferMode;
            if (elementToBeCloned.anonymousUriPrefixMatcher != null)
            {
                this.anonymousUriPrefixMatcher = new HttpAnonymousUriPrefixMatcher(elementToBeCloned.anonymousUriPrefixMatcher);
            }
            MessageHandlerFactory = elementToBeCloned.MessageHandlerFactory;
        }

        [DefaultValue(ZeroMQTransferTransportDefaults.HostNameComparisonMode)]
        public HostNameComparisonMode HostNameComparisonMode
        {
            get
            {
                return _hostNameComparisonMode;
            }
            set
            {
                HostNameComparisonModeHelper.Validate(value);
                _hostNameComparisonMode = value;
            }
        }

        [DefaultValue(ZeroMQTransferTransportDefaults.KeepAliveEnabled)]
        public bool KeepAliveEnabled { get; set; }


        // client
        // server
        [DefaultValue(TransportDefaults.MaxBufferSize)]
        public int MaxBufferSize
        {
            get
            {
                if (_maxBufferSizeInitialized || TransferMode != TransferMode.Buffered)
                {
                    return _maxBufferSize;
                }

                long maxReceivedMessageSize = MaxReceivedMessageSize;
                if (maxReceivedMessageSize > int.MaxValue)
                {
                    return int.MaxValue;
                }
                else
                {
                    return (int)maxReceivedMessageSize;
                }
            }
            set
            {
                if (value <= 0)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value,
                        SSR.ValueMustBePositive));
                }

                _maxBufferSizeInitialized = true;
                _maxBufferSize = value;
            }
        }

        [DefaultValue(ZeroMQTransferTransportDefaults.DefaultMaxPendingAccepts)]
        public int MaxPendingAccepts
        {
            get
            {
                return _maxPendingAccepts;
            }
            set
            {
                if (value < 0)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value,
                        SSR.ValueMustBeNonNegative));
                }

                if (value > ZeroMQTransferTransportDefaults.MaxPendingAcceptsUpperLimit)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value,
                        SSR.Format(SSR.HttpMaxPendingAcceptsTooLargeError, ZeroMQTransferTransportDefaults.MaxPendingAcceptsUpperLimit)));
                }

                _maxPendingAccepts = value;
            }
        }

        // string.Empty == wildcard
        internal string Method
        {
            get
            {
                return _method;
            }

            set
            {
                _method = value ?? throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));
            }
        }

        [DefaultValue(ZeroMQTransferTransportDefaults.Realm)]
        internal string Realm
        {
            get
            {
                return _realm;
            }

            set
            {
                _realm = value ?? throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));
            }
        }

        [DefaultValue(typeof(TimeSpan), ZeroMQTransferTransportDefaults.RequestInitializationTimeoutString)]
        public TimeSpan RequestInitializationTimeout
        {
            get
            {
                return _requestInitializationTimeout;
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value, SSR.SFxTimeoutOutOfRange0));
                }
                if (TimeoutHelper.IsTooLarge(value))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value, SSR.SFxTimeoutOutOfRangeTooBig));
                }

                _requestInitializationTimeout = value;
            }
        }

        // client
        // server
        [DefaultValue(ZeroMQTransferTransportDefaults.TransferMode)]
        public TransferMode TransferMode
        {
            get
            {
                return _transferMode;
            }
            set
            {
                TransferModeHelper.Validate(value);
                _transferMode = value;
            }
        }

        internal HttpAnonymousUriPrefixMatcher AnonymousUriPrefixMatcher
        {
            get
            {
                return this.anonymousUriPrefixMatcher;
            }
        }

        internal ZeroMQTransferMessagingHandlerFactory MessageHandlerFactory { get; set; }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(IAnonymousUriPrefixMatcher))
            {
                if (anonymousUriPrefixMatcher == null)
                {
                    anonymousUriPrefixMatcher = new HttpAnonymousUriPrefixMatcher();
                }
                return (T)(object)anonymousUriPrefixMatcher;
            }

            return base.GetProperty<T>(context);
        }

        /// <summary>Determines whether a channel listener of the specified type can be built.</summary>
        /// <returns>true if a channel listener can be built; otherwise false.</returns>
        /// <param name="context">The <see cref="BindingContext" /> for the channel.</param>
        /// <typeparam name="TChannel">The type of channel to check.</typeparam>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context" /> is null.</exception>
        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (typeof(TChannel) == typeof(IReplyChannel))
            {
                return true;
            }
            if (typeof(TChannel) == typeof(IDuplexSessionChannel))
            {
                return true;
            }
            return false;
        }

        /// <summary>Determines whether a channel factory of the specified type can be built.</summary>
        /// <returns>true if a channel factory can be built; otherwise false.</returns>
        /// <param name="context">The <see cref="BindingContext" /> for the channel.</param>
        /// <typeparam name="TChannel">The type of channel to check.</typeparam>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context" /> is null.</exception>
        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            if (typeof(TChannel) == typeof(IRequestChannel))
            {
                return true;
            }
            if (typeof(TChannel) == typeof(IDuplexSessionChannel))
            {
                return true;
            }
            return false;
        }

        /// <summary>Creates a channel factory that can be used to create a channel.</summary>
        /// <returns>A channel factory of the specified type.</returns>
        /// <param name="context">
        ///   <see cref="BindingContext" /> members that describe bindings, behaviors, contracts and other information required to create the channel factory.</param>
        /// <typeparam name="TChannel">Type of channel factory.</typeparam>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context" /> cannot be null.</exception>
        /// <exception cref="ArgumentException">An invalid argument was passed.</exception>
        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (!CanBuildChannelFactory<TChannel>(context))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("TChannel", SSR.Format(SSR.CouldnTCreateChannelForChannelType2, context.Binding.Name, typeof(TChannel)));
            }

            //if (authenticationScheme == AuthenticationSchemes.None)
            //{
            //    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("value", SR.GetString("HttpAuthSchemeCannotBeNone", authenticationScheme));
            //}

            //if (!authenticationScheme.IsSingleton())
            //{
            //    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("value", SR.GetString("HttpRequiresSingleAuthScheme", authenticationScheme));
            //}

            return (IChannelFactory<TChannel>)(object)new ZeroMQRequestChannelFactory<TChannel>(this, context);
        }
    }
}
