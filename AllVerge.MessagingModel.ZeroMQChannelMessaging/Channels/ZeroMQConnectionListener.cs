using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using System.ServiceModel;
using System.ServiceModel.Channels;
using CBP = System.ServiceModel.Channels.ConnectionBufferPool;

using System.Runtime;
using System.IO;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.MessagingModel.MessagingApplication;
    using NetMQ;
    using NetMQ.Sockets;

    internal class ZeroMQConnectionListener : IConnectionListener
    {
        private IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>> protocolContextAccessor;
        private Uri listenUri;
        private string bindUri;
        private TimeSpan maxOutputDelay;
        private IConnectionBufferPool connectionBufferPool;

        private CancellationTokenSource cancellationTokenSource;
        private RouterSocket routerSocket;
        private BufferBlock<RoutingKey> acceptQueue;
        private ConcurrentDictionary<RoutingKey, ZeroMQListenerConnection> routingMap;
        private bool disposedValue;
        
        public ZeroMQConnectionListener(IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>> protocolContextAccessor, Uri listenUri, TimeSpan maxOutputDelay, int connectionBufferSize)
        {
            this.protocolContextAccessor = protocolContextAccessor;
            this.listenUri = listenUri;
            this.bindUri = ZeroMQProtocolSchemesHelper.NormalizeServerAddress(listenUri.AbsoluteUri, true);
            this.maxOutputDelay = maxOutputDelay;
            this.connectionBufferPool = CBP.Create(connectionBufferSize);
        }

        internal IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>> ProtocolContextAccessor => this.protocolContextAccessor;
        internal Uri ListenUri => this.listenUri;
        internal IConnectionBufferPool ConnectionBufferPool => this.connectionBufferPool;
        internal RouterSocket RouterSocket => this.routerSocket;

        public CancellationToken CancellationToken
        {
            get
            {
                if (this.cancellationTokenSource == null)

                    throw new InvalidOperationException($"{nameof(ZeroMQConnectionListener)} is not ready.  Listen has not been called.");

                return this.cancellationTokenSource.Token;
            }
        }

        internal void StopListening(RoutingKey routingKey)
        {
            this.routingMap.TryRemove(routingKey, out _);
        }

        public void Listen()
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            this.routingMap = new ConcurrentDictionary<RoutingKey, ZeroMQListenerConnection>();
            this.acceptQueue = new BufferBlock<RoutingKey>();

            StartAcceptingConnections();
        }

        private void StartAcceptingConnections()
        {
            ZeroMQRuntime.Start(this.cancellationTokenSource.Token);

            ZeroMQRuntime.Run(async () =>
            {
                this.routerSocket = new RouterSocket();

                this.routerSocket.Bind(this.bindUri);

                while (!this.CancellationToken.IsCancellationRequested)
                {
                    (RoutingKey RoutingKey, bool More) routingKeyResult;

                    try
                    {
                        routingKeyResult = await this.routerSocket.ReceiveRoutingKeyAsync(this.CancellationToken);

                        await AcceptAsync(routingKeyResult, this);
                    }
                    catch (InvalidOperationException)
                    {
                        // No ZeroMQ runtime, bail ...

                        throw;
                    }
                    catch (ArgumentException e)
                    {
                        throw;
                    }
                    catch (TaskCanceledException e)
                    {
                        DiagnosticUtility.ExceptionUtility.TraceHandledException(e, TraceEventType.Error);

                        routerSocket.SignalError();
                    }
                    catch (AggregateException e)
                    {
                        foreach (Exception ex in e.InnerExceptions)
                        {
                            DiagnosticUtility.ExceptionUtility.TraceHandledException(ex, TraceEventType.Error);
                        }

                        routerSocket.SignalError();
                    }
                }
            });
        }

        private static Task AcceptAsync((RoutingKey RoutingKey, bool More) routingKeyResult, ZeroMQConnectionListener zeroMQConnectionListener)
        {
            TaskCompletionSource<int> readCompletionSource = new TaskCompletionSource<int>();

            ZeroMQRuntime.Run(async () =>
            {
                bool acceptingConnection = false;

                if (!zeroMQConnectionListener.routingMap.TryGetValue(routingKeyResult.RoutingKey, out ZeroMQListenerConnection listenerConnection))
                {
                    listenerConnection = new ZeroMQListenerConnection(routingKeyResult.RoutingKey, zeroMQConnectionListener);

                    acceptingConnection = zeroMQConnectionListener.routingMap.TryAdd(routingKeyResult.RoutingKey, listenerConnection);
                }
                
                ByteBuffer buffer = new ByteBuffer(listenerConnection.AsyncReadBufferSize);

                bool more = routingKeyResult.More;
                int bytesRead = 0;

                while (more)
                {
                    (byte[] Result, bool More) nextResult = default;

                    try
                    {
                        nextResult = await zeroMQConnectionListener.RouterSocket.ReceiveFrameBytesAsync(zeroMQConnectionListener.cancellationTokenSource.Token);
                    }
                    catch (InvalidOperationException e)
                    {
                        // No ZeroMQ runtime, bail and infrastructure will abort this connection ...
                        readCompletionSource.SetException(e);
                    }
                    catch (TaskCanceledException e)
                    {
                        DiagnosticUtility.ExceptionUtility.TraceHandledException(e, TraceEventType.Error);

                        zeroMQConnectionListener.RouterSocket.SignalError();

                        readCompletionSource.TrySetCanceled();
                    }
                    catch (AggregateException e)
                    {
                        bool operationCancelled = false;

                        foreach (Exception ex in e.InnerExceptions)
                        {
                            if (ex is OperationCanceledException)

                                operationCancelled = true;

                            DiagnosticUtility.ExceptionUtility.TraceHandledException(ex, TraceEventType.Error);
                        }

                        zeroMQConnectionListener.RouterSocket.SignalError();

                        if (operationCancelled)

                            readCompletionSource.TrySetCanceled();
                    }

                    if (readCompletionSource.Task.IsCompleted)

                        more = false;

                    else
                    {
                        BufferedStream stream = buffer.OpenSection();

                        stream.Write(nextResult.Result, 0, nextResult.Result.Length);

                        buffer.CloseSection();

                        bytesRead += nextResult.Result.Length;

                        more = nextResult.More;
                    }
                }

                buffer.Close();

                if (!readCompletionSource.Task.IsCompleted)
                {
                    listenerConnection.Messages.Add((bytesRead, buffer));

                    if (acceptingConnection)
                    
                        zeroMQConnectionListener.acceptQueue.Post(routingKeyResult.RoutingKey);

                    readCompletionSource.SetResult(bytesRead);
                }
            });

            return readCompletionSource.Task;
        }

        public IAsyncResult BeginAccept(AsyncCallback callback, object state)
        {
            return this.acceptQueue.ReceiveAsync(this.CancellationToken).ToApm(callback, state);
        }

        public IConnection EndAccept(IAsyncResult result)
        {
            RoutingKey acceptedKey = result.ToApmEnd<RoutingKey>();

            if (this.routingMap.TryGetValue(acceptedKey, out ZeroMQListenerConnection listenerConnection))

                return listenerConnection;

            return null;
        }

        public void Abort()
        {
            if (!this.cancellationTokenSource.IsCancellationRequested)
            {
                this.cancellationTokenSource.Cancel();

                if (this.routerSocket != null)
                {
                    this.routerSocket.Disconnect(this.bindUri);

                    this.routerSocket.Close();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    this.Abort();

                    this.cancellationTokenSource.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ZeroMQConnectionListener()
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
}