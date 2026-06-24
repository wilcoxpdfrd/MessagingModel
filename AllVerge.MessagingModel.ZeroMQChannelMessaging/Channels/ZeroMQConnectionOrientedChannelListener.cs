using System;
using System.Collections.Generic;
using System.Runtime;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.MessagingModel.MessagingApplication;
    using System.Net;
    using BindingContext = System.ServiceModel.Channels.BindingContext;

    abstract class ZeroMQConnectionOrientedChannelListener : ConnectionOrientedTransportChannelListener
    {
        protected class ZeroMQTransportReplyChannelAcceptor : ConnectionOrientedTransportReplyChannelAcceptor
        {
            public ZeroMQTransportReplyChannelAcceptor(ZeroMQConnectionOrientedChannelListener listener)
                : base(listener)
            {
            }
        }

        static UriPrefixTable<ITransportManagerRegistration> transportManagerTable =
            new UriPrefixTable<ITransportManagerRegistration>(true);
        
        private IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>> protocolContextAccessor;

        protected ZeroMQConnectionOrientedChannelListener(ZeroMQConnectionOrientedTransportBindingElementBase bindingElement, BindingContext context)
            : base(bindingElement, context)
        {
            IServiceProvider serviceProvider = context.BindingParameters.Find<IServiceProvider>();

            this.protocolContextAccessor = serviceProvider.GetService<IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>>>();
        }

        public override T GetProperty<T>()
        {
            if (typeof(T) == typeof(IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>>))
            {
                return (T)(object)(this.protocolContextAccessor);
            }

            return base.GetProperty<T>();
        }

        internal static UriPrefixTable<ITransportManagerRegistration> StaticTransportManagerTable
        {
            get
            {
                return transportManagerTable;
            }
        }

        internal bool ExposeConnectionProperty => this.IsConnectionPropertyExposed();

        protected override UriPrefixTable<ITransportManagerRegistration> GetTransportManagerTable()
        {
            return transportManagerTable;
        }

        protected override ITransportManagerRegistration CreateTransportManagerRegistration(Uri listenUri)
        {
            return new ZeroMQTransportManagerRegistration(listenUri, this);
        }
    }

    abstract class ZeroMQConnectionOrientedChannelListener<TChannel, TChannelAcceptor>
        : ZeroMQConnectionOrientedChannelListener, IChannelListener<TChannel>
        where TChannel : class, IChannel
        where TChannelAcceptor : ChannelAcceptor<TChannel>
    {
        protected ZeroMQConnectionOrientedChannelListener(ZeroMQConnectionOrientedTransportBindingElementBase bindingElement, BindingContext context)
            : base(bindingElement, context)
        {
        }

        protected abstract TChannelAcceptor ChannelAcceptor { get; }

        
        protected override void OnOpen(TimeSpan timeout)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            base.OnOpen(timeoutHelper.RemainingTime());
            ChannelAcceptor.Open(timeoutHelper.RemainingTime());
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            return Task.Factory.FromAsync(base.OnBeginOpen, (t) => { base.OnEndOpen(t); Task.Factory.FromAsync(ChannelAcceptor.BeginOpen, ChannelAcceptor.EndOpen, timeoutHelper.RemainingTime(), t.AsyncState); }, timeoutHelper.RemainingTime(), state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            result.ToApmEnd();
        }

        protected override void OnClose(TimeSpan timeout)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            ChannelAcceptor.Close(timeoutHelper.RemainingTime());
            base.OnClose(timeoutHelper.RemainingTime());
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            return Task.Factory.FromAsync(base.OnBeginClose, (t) => { base.OnEndClose(t); Task.Factory.FromAsync(ChannelAcceptor.BeginClose, ChannelAcceptor.EndClose, timeoutHelper.RemainingTime(), t.AsyncState); }, timeoutHelper.RemainingTime(), state);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            result.ToApmEnd();
        }

        protected override void OnAbort()
        {
            this.ChannelAcceptor.Abort();
            base.OnAbort();
        }

        public TChannel AcceptChannel()
        {
            return this.AcceptChannel(this.DefaultReceiveTimeout);
        }

        public IAsyncResult BeginAcceptChannel(AsyncCallback callback, object state)
        {
            return this.BeginAcceptChannel(this.DefaultReceiveTimeout, callback, state);
        }

        public TChannel AcceptChannel(TimeSpan timeout)
        {
            (this as CommunicationObject).ThrowIfNotOpened();
            return ChannelAcceptor.AcceptChannel(timeout);
        }

        public IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            (this as CommunicationObject).ThrowIfNotOpened();
            return ChannelAcceptor.BeginAcceptChannel(timeout, callback, state);
        }

        public TChannel EndAcceptChannel(IAsyncResult result)
        {
            (this as CommunicationObject).ThrowPending();
            return ChannelAcceptor.EndAcceptChannel(result);
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            return ChannelAcceptor.WaitForChannel(timeout);
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return ChannelAcceptor.BeginWaitForChannel(timeout, callback, state);
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            return ChannelAcceptor.EndWaitForChannel(result);
        }
    }
}
