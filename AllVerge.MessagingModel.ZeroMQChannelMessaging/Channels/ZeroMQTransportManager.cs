using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQTransportManager : ConnectionOrientedTransportManager<ZeroMQConnectionOrientedChannelListener>
    {
        private ZeroMQTransportManagerRegistration registration;
        private IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>> protocolContextAccessor;
        bool closed;
        ConnectionDemuxer connectionDemuxer;
        IConnectionListener connectionListener;

        internal ZeroMQTransportManager(ZeroMQTransportManagerRegistration registration, ZeroMQConnectionOrientedChannelListener channelListener) : 
            base(channelListener, OnOpen, OnAbort, OnClose)
        {
            this.registration = registration;

            this.protocolContextAccessor = channelListener.GetProperty<IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>>>();
        }

        protected virtual bool IsCompatible(ZeroMQConnectionOrientedChannelListener channelListener)
        {
            return IsCompatible(channelListener);
        }

        static void OnOpen(TransportManager transportManager)
        {
            ZeroMQTransportManager _this = transportManager as ZeroMQTransportManager;

            _this.connectionListener = new ZeroMQConnectionListener(_this.protocolContextAccessor, _this.registration.ListenUri, _this.registration.MaxOutputDelay, _this.registration.ConnectionBufferSize);

            if (DiagnosticUtility.ShouldUseActivity)
            {
                _this.connectionListener = new TracingConnectionListener(_this.connectionListener, _this.registration.ListenUri.ToString(), false);
            }

            _this.connectionDemuxer = 
                new ConnectionDemuxer(
                    _this.connectionListener,
                    _this.registration.MaxPendingAccepts,
                    _this.registration.MaxPendingConnections,
                    _this.registration.ChannelInitializationTimeout,
                    _this.registration.IdleTimeout,
                    _this.registration.MaxPooledConnections,
                    _this.registration.ExposeConnectionProperty,
                    _this.TransportSettingsCallback,
                    _this.SingletonPreambleDemuxCallback,
                    _this.ServerSessionPreambleDemuxCallback,
                    _this.ErrorCallback);

            bool startedDemuxing = false;

            try
            {
                _this.connectionDemuxer.StartDemuxing();
                startedDemuxing = true;
            }
            finally
            {
                if (!startedDemuxing)
                {
                    _this.connectionDemuxer.Dispose();
                }
            }
        }

        static void OnAbort(TransportManager transportManager)
        {
            ZeroMQTransportManager.OnClose(transportManager, TimeSpan.Zero);
        }

        static void OnClose(TransportManager transportManager, TimeSpan timeout)
        {
            ZeroMQTransportManager _this = transportManager as ZeroMQTransportManager;

            lock (_this.Lock)
            {
                if (_this.closed)
                {
                    return;
                }

                _this.closed = true;
            }

            if (_this.connectionDemuxer != null)
            {
                _this.connectionDemuxer.Dispose();
            }

            if (_this.connectionListener != null)
            {
                _this.connectionListener.Dispose();
            }

            _this.registration.OnClose(_this);
        }
    }
}