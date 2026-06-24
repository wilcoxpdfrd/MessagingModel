using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using NetMQ;
using NetMQ.Sockets;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQConnectionInitiator : IConnectionInitiator
    {
        private IConnectionBufferPool connectionBufferPool;
        private static CancellationTokenSource lifetime;
        private bool disposedValue;

        static ZeroMQConnectionInitiator()
        {
            lifetime = new CancellationTokenSource();

            ZeroMQRuntime.Start(lifetime.Token);
        }

        public ZeroMQConnectionInitiator(int connectionBufferSize)
        {
            this.connectionBufferPool = ConnectionBufferPool.Create(connectionBufferSize);
        }

        public IConnection Connect(Uri uri, TimeSpan timeout)
        {
            return ConnectAsync(uri, timeout).GetAwaiter().GetResult();
        }

        private IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback asyncCallback, Object state)
        {
            return this.ConnectAsync(uri, timeout).ToApm<IConnection>(asyncCallback, state);
        }

        private IConnection EndConnect(IAsyncResult result)
        {
            return result.ToApmEnd<IConnection>();
        }

        public Task<IConnection> ConnectAsync(Uri uri, TimeSpan timeout)
        {
            return ZeroMQRuntime.Run(() =>
            {
                DealerSocket socket = new DealerSocket();

                socket.Options.Identity = Guid.NewGuid().ToByteArray();

                socket.Connect(ZeroMQProtocolSchemesHelper.NormalizeServerAddress(uri.AbsoluteUri));

                return (IConnection)new ZeroMQConnection(socket, this.connectionBufferPool);
            });
        }

        ~ZeroMQConnectionInitiator()
        {
            if (lifetime != null && !lifetime.IsCancellationRequested)
            {
                lifetime.Cancel();
            }

            ZeroMQRuntime.WaitUntilStoppedAsync().GetAwaiter().GetResult();
        }
    }
}