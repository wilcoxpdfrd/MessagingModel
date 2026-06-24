namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Diagnostics;
    using System.Runtime;
    using System.ServiceModel.Diagnostics.Application;
    using System.Runtime.Diagnostics;
    using Microsoft.Extensions.Hosting;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.ServiceModel.Channels;
    using System;

    public interface ITransportManager<TItem> where TItem : class
    {
        void Register(Uri listenUri, TItem item);
        void Unregister(Uri listenUri);
    }

    public class TransportManager<TItem> : ITransportManager<TItem> where TItem : class
    {
        volatile UriPrefixTable<TItem> addressTables;
        readonly HostNameComparisonMode hostNameComparisonMode;

        public TransportManager()
            : this(HostNameComparisonMode.Exact)
        {
        }

        public TransportManager(HostNameComparisonMode hostNameComparisonMode)
        {
            this.hostNameComparisonMode = hostNameComparisonMode;
            this.addressTables = new UriPrefixTable<TItem>(true);
        }

        public HostNameComparisonMode HostNameComparisonMode
        {
            get
            {
                return this.hostNameComparisonMode;
            }
        }

        Regex rex = new Regex(@"/^(?:[a-z][a-z|0-9\+\.])*(?:\/\/$/)([\*|\+|.*]):(?:\d*)\/(?:.*)$", RegexOptions.IgnoreCase);

        public bool TryLookupUri(
            Uri requestUri,
            HostNameComparisonMode hostNameComparisonMode,
            out TItem item)
        {
            return this.addressTables.TryLookupUri(requestUri, this.HostNameComparisonMode, out item);
        }

        void ITransportManager<TItem>.Register(Uri listenUri, TItem item)
        {
            this.Register(listenUri, item);
        }

        private void Register(Uri listenUri, TItem item)
        {
            addressTables.RegisterUri(listenUri, this.HostNameComparisonMode, item);
        }

        void ITransportManager<TItem>.Unregister(Uri listenUri)
        {
            this.Unregister(listenUri);
        }

        private void Unregister(Uri listenUri)
        {
            addressTables.UnregisterUri(listenUri, this.HostNameComparisonMode);
        }
    }
}
