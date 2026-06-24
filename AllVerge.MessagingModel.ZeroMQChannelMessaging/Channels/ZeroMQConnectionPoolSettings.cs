using System;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public sealed class ZeroMQConnectionPoolSettings
    {
        private string _groupName;
        private TimeSpan _idleTimeout;
        private TimeSpan _leaseTimeout;
        private int _maxOutboundConnectionsPerEndpoint;

        internal ZeroMQConnectionPoolSettings()
        {
            _groupName = ConnectionOrientedTransportDefaults.ConnectionPoolGroupName;
            _idleTimeout = ConnectionOrientedTransportDefaults.IdleTimeout;
            _leaseTimeout = TcpTransportDefaults.ConnectionLeaseTimeout;
            _maxOutboundConnectionsPerEndpoint = ConnectionOrientedTransportDefaults.MaxOutboundConnectionsPerEndpoint;
        }

        internal ZeroMQConnectionPoolSettings(ZeroMQConnectionPoolSettings other)
        {
            _groupName = other._groupName;
            _idleTimeout = other._idleTimeout;
            _leaseTimeout = other._leaseTimeout;
            _maxOutboundConnectionsPerEndpoint = other._maxOutboundConnectionsPerEndpoint;
        }


        public string GroupName
        {
            get { return _groupName; }
            set
            {
                _groupName = value ?? throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(value));
            }
        }

        public TimeSpan IdleTimeout
        {
            get { return _idleTimeout; }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value,
                        PublicSR.SFxTimeoutOutOfRange0));
                }

                if (TimeoutHelper.IsTooLarge(value))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value,
                        PublicSR.SFxTimeoutOutOfRangeTooBig));
                }

                _idleTimeout = value;
            }
        }

        public TimeSpan LeaseTimeout
        {
            get { return _leaseTimeout; }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value,
                        PublicSR.SFxTimeoutOutOfRange0));
                }

                if (TimeoutHelper.IsTooLarge(value))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value,
                        PublicSR.SFxTimeoutOutOfRangeTooBig));
                }

                _leaseTimeout = value;
            }
        }

        public int MaxOutboundConnectionsPerEndpoint
        {
            get { return _maxOutboundConnectionsPerEndpoint; }
            set
            {
                if (value < 0)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(value), value,
                        PublicSR.ValueMustBeNonNegative));
                }

                _maxOutboundConnectionsPerEndpoint = value;
            }
        }

        internal ZeroMQConnectionPoolSettings Clone()
        {
            return new ZeroMQConnectionPoolSettings(this);
        }
    }

}