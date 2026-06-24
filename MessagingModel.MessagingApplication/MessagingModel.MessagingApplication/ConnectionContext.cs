using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.MessagingApplication
{
    using AllVerge.SystemPrimitives.Threading;
    using AllVerge.SystemPrimitives.Threading.Tasks;
    using System.Collections;
    using System.Collections.ObjectModel;

    public class ConnectionContext :
        IDisposable
    {
        private bool disposedValue;

        public ConnectionContext()
        {
            this.Items = new Dictionary<Object, Object>();
        }

        /// <summary>
        /// Gets the connection Id for the connection associated with the endpoints.
        /// </summary>
        public string ConnectionId { get; protected set; }

        public IDictionary<object, object> Items { get; }

        /// <summary>
        /// Gets the endpoint IP address of the local endpoint.
        /// </summary>
        public IPEndPoint LocalIPEndpoint { get; protected set; }

        /// <summary>
        /// Gets the endpoint IP address of the remote endpoint.
        /// </summary>
        public IPEndPoint RemoteIPEndpoint { get; protected set; }

        private TaskEventCompletionSource upgradedChannelClosedEventCompletionSource;

        public void SetUpgradeChannelClosedTECS(TaskEventCompletionSource upgradedChannelClosedEventCompletionSource)
        {
            this.upgradedChannelClosedEventCompletionSource = upgradedChannelClosedEventCompletionSource;
        }
        
        public bool IsChannelUpgraded(out CancellationToken upgradedChannelClosedToken)
        {
            if (this.upgradedChannelClosedEventCompletionSource != null)
            {
                CancellationTokenSource cts = new CancellationTokenSource();

                this.upgradedChannelClosedEventCompletionSource.EventTask.ContinueWith(t => cts.Cancel());

                upgradedChannelClosedToken = cts.Token;

                return true;
            }

            upgradedChannelClosedToken = CancellationToken.None;

            return false;
        }

        public static void Map(
            ConnectionContext connectionContext,
            ConnectionContext values)
        {
            Map(
                connectionContext, 
                values.ConnectionId,
                values.Items,
                values.LocalIPEndpoint,
                values.RemoteIPEndpoint);
        }

        public static void Map(
            ConnectionContext connectionContext,
            string connectionId = null,
            IDictionary<object, object> items = null,
            IPEndPoint localIPEndpoint = null,
            IPEndPoint remoteIPEndpoint = null)
        {
            connectionContext.ConnectionId = connectionId;

            if (items != null)
            {
                lock (items)
                {
                    items.Aggregate(connectionContext.Items, (i, p) => { if (i.TryGetValue(p.Key, out object value)) i.Add(p.Key, value); return i; });
                }
            }
            connectionContext.LocalIPEndpoint = localIPEndpoint;
            connectionContext.RemoteIPEndpoint = remoteIPEndpoint;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.OnDispose();
                    this.Items.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        protected virtual void OnDispose()
        {
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ConnectionContext()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public static class ConnectionContextExtensions
    {
        public static void Map(
            this ConnectionContext connectionContext,
            string connectionId = null,
            IDictionary<object, object> items = null,
            IPEndPoint localIPEndpoint = null,
            IPEndPoint remoteIPEndpoint = null)
        {
            ConnectionContext.Map(connectionContext, connectionId, items, localIPEndpoint, remoteIPEndpoint);
        }
    }
}