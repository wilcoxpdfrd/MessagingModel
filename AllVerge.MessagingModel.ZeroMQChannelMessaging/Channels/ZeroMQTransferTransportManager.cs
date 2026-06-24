using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.Diagnostics;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Diagnostics;
    using System.ServiceModel.Diagnostics.Application;

    internal abstract class ZeroMQTransferTransportManager : TransportManager, ITransportManagerRegistration
    {
        volatile Dictionary<string, UriPrefixTable<ZeroMQTransferTransportChannelListenerBase>> addressTables;
        readonly HostNameComparisonMode hostNameComparisonMode;
        readonly Uri listenUri;
        readonly string realm;

        private ZeroMQTransferTransportManager()
        {
            this.addressTables = new Dictionary<string, UriPrefixTable<ZeroMQTransferTransportChannelListenerBase>>();
        }

        internal ZeroMQTransferTransportManager(Uri listenUri, HostNameComparisonMode hostNameComparisonMode)
            : this()
        {
            this.hostNameComparisonMode = hostNameComparisonMode;
            this.listenUri = listenUri;
        }

        internal ZeroMQTransferTransportManager(Uri listenUri, HostNameComparisonMode hostNameComparisonMode, string realm)
            : this(listenUri, hostNameComparisonMode)
        {
            this.realm = realm;
        }

        internal string Realm
        {
            get
            {
                return this.realm;
            }
        }

        public HostNameComparisonMode HostNameComparisonMode
        {
            get
            {
                return this.hostNameComparisonMode;
            }
        }

        internal bool IsHosted { get; set; }

        internal override string Scheme => this.listenUri.Scheme;

        internal virtual UriPrefixTable<ITransportManagerRegistration> TransportManagerTable
        {
            get
            {
                return ZeroMQTransferTransportChannelListenerBase.StaticTransportManagerTable;
            }
        }

        public Uri ListenUri
        {
            get
            {
                return this.listenUri;
            }
        }

        protected void Fault(Exception exception)
        {
            lock (ThisLock)
            {
                foreach (KeyValuePair<string, UriPrefixTable<ZeroMQTransferTransportChannelListenerBase>> pair in this.addressTables)
                {
                    this.Fault(pair.Value, exception);
                }
            }
        }

        internal virtual bool IsCompatible(ZeroMQTransferTransportChannelListenerBase listener)
        {
            return (
                (this.hostNameComparisonMode == listener.HostNameComparisonMode) &&
                (this.realm == listener.Realm)
                );
        }

        internal override void OnClose(TimeSpan timeout)
        {
            Cleanup();
        }

        internal override void OnAbort()
        {
            Cleanup();
            base.OnAbort();
        }

        void Cleanup()
        {
            this.TransportManagerTable.UnregisterUri(this.ListenUri, this.HostNameComparisonMode);
        }

        protected void StartReceiveBytesActivity(ServiceModelActivity activity, Uri requestUri)
        {
            Fx.Assert(DiagnosticUtility.ShouldUseActivity, "should only call this if we're using SM Activities");
            ServiceModelActivity.Start(activity, SSR.Format(SSR.ActivityReceiveBytes, requestUri.ToString()), ActivityType.ReceiveBytes);
        }

        protected void TraceMessageReceived(EventTraceActivity eventTraceActivity, Uri listenUri)
        {
            if (TD.HttpMessageReceiveStartIsEnabled())
            {
                TD.HttpMessageReceiveStart(eventTraceActivity);
            }
        }

        protected bool TryLookupUri(Uri requestUri, string requestMethod,
            HostNameComparisonMode hostNameComparisonMode, out ZeroMQTransferTransportChannelListenerBase listener)
        {
            listener = null;

            if (requestMethod == null)
            {
                requestMethod = string.Empty;
            }

            UriPrefixTable<ZeroMQTransferTransportChannelListenerBase> addressTable;
            Dictionary<string, UriPrefixTable<ZeroMQTransferTransportChannelListenerBase>> localAddressTables = addressTables;

            // check for a method match if necessary
            ZeroMQTransferTransportChannelListenerBase methodListener = null;
            if (requestMethod.Length > 0)
            {
                if (localAddressTables.TryGetValue(requestMethod, out addressTable))
                {
                    if (addressTable.TryLookupUri(requestUri, hostNameComparisonMode, out methodListener)
                        && string.Compare(requestUri.AbsolutePath, methodListener.Uri.AbsolutePath, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        methodListener = null;
                    }
                }
            }
            // and also check the wildcard bucket 
            if (localAddressTables.TryGetValue(string.Empty, out addressTable)
                && addressTable.TryLookupUri(requestUri, hostNameComparisonMode, out listener))
            {
                if (methodListener != null && methodListener.Uri.AbsoluteUri.Length >= listener.Uri.AbsoluteUri.Length)
                {
                    listener = methodListener;
                }
            }
            else
            {
                listener = methodListener;
            }

            return (listener != null);
        }

        internal override void Register(TransportChannelListener channelListener)
        {
            string method = ((ZeroMQTransferTransportChannelListenerBase)channelListener).Method;

            UriPrefixTable<ZeroMQTransferTransportChannelListenerBase> addressTable;
            if (!addressTables.TryGetValue(method, out addressTable))
            {
                lock (ThisLock)
                {
                    if (!addressTables.TryGetValue(method, out addressTable))
                    {
                        Dictionary<string, UriPrefixTable<ZeroMQTransferTransportChannelListenerBase>> newAddressTables =
                            new Dictionary<string, UriPrefixTable<ZeroMQTransferTransportChannelListenerBase>>(addressTables);

                        addressTable = new UriPrefixTable<ZeroMQTransferTransportChannelListenerBase>();
                        newAddressTables[method] = addressTable;

                        addressTables = newAddressTables;
                    }
                }
            }

            addressTable.RegisterUri(channelListener.Uri,
                channelListener.InheritBaseAddressSettings ? hostNameComparisonMode : channelListener.HostNameComparisonModeInternal,
                (ZeroMQTransferTransportChannelListenerBase)channelListener);
        }

        IList<TransportManager> ITransportManagerRegistration.Select(TransportChannelListener channelListener)
        {
            IList<TransportManager> result = null;
            if (this.IsCompatible((ZeroMQTransferTransportChannelListenerBase)channelListener))
            {
                result = new List<TransportManager>();
                result.Add(this);
            }
            return result;
        }

        internal override void Unregister(TransportChannelListener channelListener)
        {
            UriPrefixTable<ZeroMQTransferTransportChannelListenerBase> addressTable;
            if (!addressTables.TryGetValue(((ZeroMQTransferTransportChannelListenerBase)channelListener).Method, out addressTable))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SSR.Format(
                     SSR.ListenerFactoryNotRegistered, channelListener.Uri)));
            }

            HostNameComparisonMode registeredMode = channelListener.InheritBaseAddressSettings ? hostNameComparisonMode : channelListener.HostNameComparisonModeInternal;

            EnsureRegistered(addressTable, (ZeroMQTransferTransportChannelListenerBase)channelListener, registeredMode);
            addressTable.UnregisterUri(channelListener.Uri, registeredMode);
        }

        protected class ActivityHolder : IDisposable
        {
            internal ZeroMQRequestContext context;
            internal ServiceModelActivity activity;

            public ActivityHolder(ServiceModelActivity activity, ZeroMQRequestContext requestContext)
            {
                Fx.Assert(requestContext != null, "requestContext cannot be null.");
                this.activity = activity;
                this.context = requestContext;
            }

            public void Dispose()
            {
                if (this.activity != null)
                {
                    this.activity.Dispose();
                }
            }
        }
    }
}