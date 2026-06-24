using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using AllVerge.Core.Collections;
using AllVerge.Core.ServiceModel.Transfer;
using Microsoft.Extensions.Primitives;

using NetMQ;
using NetMQ.Sockets;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQRequestListener : IDisposable
    {
        private int state = 0; // Stopped = 0, Started = 1, Closed = 2
        private string socketAddress;
        private CancellationTokenSource cts;
        private TaskCompletionSource<VoidTaskResult> tcs;
        private BufferBlock<ZeroMQListenerRequest> requestQueue;
        private TaskCollection replyTasks;
        private ConcurrentDictionary<RoutingKey, bool> repliesReady;
        private ConcurrentDictionary<RoutingKey, List<byte[]>> replies;

        private bool ignoreWriteExceptions;
        private RouterSocket routerSocket;

        public ZeroMQRequestListener(Uri listenUri)
        {
            this.socketAddress = ZeroMQProtocolSchemesHelper.NormalizeServerAddress(listenUri.AbsoluteUri, true);
            this.cts = new CancellationTokenSource();
            this.tcs = new TaskCompletionSource<VoidTaskResult>();
            this.requestQueue = new BufferBlock<ZeroMQListenerRequest>();
            this.repliesReady = new ConcurrentDictionary<RoutingKey, bool>();
            this.replyTasks = new TaskCollection(this.cts.Token);
            this.replies = new ConcurrentDictionary<RoutingKey, List<byte[]>>();

        }

        public bool IgnoreWriteExceptions {
            get => ignoreWriteExceptions;
            internal set { ignoreWriteExceptions = value; }
        }

        internal void Start()
        {
            this.CheckDisposed();

            if (Interlocked.CompareExchange(ref this.state, 1, 0) == 0)
            {
                Task.Run(() =>
                {
                    using (NetMQRuntime runtime = new NetMQRuntime())
                    {
                        runtime.Run(ListenAsync(), replyTasks);
                    }
                });
            }
        }

        private async Task ListenAsync()
        {
            using (RouterSocket socket = new RouterSocket())
            {
                try
                {
                    socket.Bind(this.socketAddress);

                    this.routerSocket = socket;

                    while (!cts.IsCancellationRequested)
                    {
                        List<ArraySegment<byte>> data = new List<ArraySegment<byte>>();

                        TypedDictionary<String, StringValues> headers =
                            new TypedDictionary<string, StringValues>();

                        bool receivedAllHeaders = false;

                        (RoutingKey RoutingKey, bool More) result = await socket.ReceiveRoutingKeyAsync(this.cts.Token);

                        bool more = result.More;

                        (byte[] Result, bool More) nextResult;

                        String requestLine;

                        if (more)
                        {
                            nextResult = await socket.ReceiveFrameBytesAsync(this.cts.Token);

                            more = nextResult.More;

                            ArraySegment<byte> s = new ArraySegment<byte>(nextResult.Result);

                            requestLine = Encoding.ASCII.GetString(s.Array);
                        }
                        else

                            requestLine = null;

                        while (more)
                        {
                            try
                            {
                                nextResult = await socket.ReceiveFrameBytesAsync(this.cts.Token);

                                more = nextResult.More;

                                int resultSize = nextResult.Result.Length;

                                if (receivedAllHeaders)
                                {
                                    data.Add(new ArraySegment<byte>(nextResult.Result));
                                }
                                else
                                {
                                    if (resultSize > 0)
                                    {
                                        ArraySegment<byte> s = new ArraySegment<byte>(nextResult.Result);

                                        int indexOfColon = Array.IndexOf(s.Array, (byte)':');

                                        if (indexOfColon < 0)

                                            headers.Add(String.Empty, Encoding.ASCII.GetString(s.Array));

                                        else

                                            headers.Add(Encoding.ASCII.GetString(s.Slice(0, indexOfColon).ToArray()), Encoding.ASCII.GetString(s.Slice(indexOfColon + 1).ToArray()));
                                    }
                                    else

                                        receivedAllHeaders = true;
                                }
                            }
                            catch (Exception e)
                            {
                                // log ...
                            }
                        }

                        ZeroMQListenerRequest request =
                            new ZeroMQListenerRequest(result.RoutingKey, requestLine) { Headers = headers };

                        request.InputStream = new MemoryStream();

                        if (request.ContentLength64 > 0)
                        {
                            int frames = data.Count;

                            for (int i = 0; i < frames; i++)
                            {
                                ArraySegment<byte> buffer = data.ElementAt(i);

                                if (buffer.Count > 0)
                                {
                                    request.InputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
                                }
                            }
                        }

                        request.InputStream.Seek(0, SeekOrigin.Begin);

                        this.AddReplyAsync(request.RoutingKey);

                        this.requestQueue.Post(request);
                    }
                }
                finally
                {
                    this.routerSocket = null;

                    socket.Unbind(this.socketAddress);

                    this.tcs.SetResult(new VoidTaskResult());
                }
            }
        }

        private void AddReplyAsync(RoutingKey routingKey)
        {
            this.replyTasks.TryAdd(ReplyAsync(routingKey));
        }

        private async Task ReplyAsync(RoutingKey routingKey)
        {
            while (!cts.IsCancellationRequested)
            {
                if (this.repliesReady.TryRemove(routingKey, out _))
                {
                    if (this.replies.TryRemove(routingKey, out List<byte[]> buffers))
                    {
                        if (this.routerSocket != null)
                        {
                            this.routerSocket.SendMoreFrame(routingKey);

                            int i = 0;
                            int j = buffers.Count - 1;

                            foreach (byte[] buffer in buffers)
                            {
                                if (buffer.Length == 0)

                                    this.routerSocket.SendFrameEmpty(i++ < j);

                                else

                                    this.routerSocket.SendFrame(buffer, i++ < j);
                            }
                        }
                    }
                    break;
                }

                await Task.Yield();
            }
        }

        internal void Write(RoutingKey routingKey)
        {
            this.Write(routingKey, null, 0, 0, true, TimeSpan.MaxValue);
        }

        public void Write(RoutingKey routingKey, byte[] buffer)
        {
            this.Write(routingKey, buffer, 0, buffer.Length, true, TimeSpan.MaxValue);
        }

        public void Write(RoutingKey routingKey, byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout)
        {
            if (immediate)
            {
                if (buffer == null)

                    this.repliesReady.TryAdd(routingKey, true);

                else
                {
                    byte[] writeBuffer;

                    if (offset > 0 || buffer.Length > offset + size)
                    {
                        writeBuffer = new byte[size];

                        Buffer.BlockCopy(buffer, offset, writeBuffer, 0, size);
                    }
                    else

                        writeBuffer = buffer;

                    this.replies.AddOrUpdate(routingKey, new List<byte[]>(writeBuffer.ToEnumerable()), (k, l) => { l.Add(writeBuffer); return l; });
                }
            }
            else
            {
                throw NotImplemented.ByDesignWithMessage("Buffering not yet implemented.");
            }
        }

        internal void Stop()
        {
            this.CheckDisposed();

            if (Interlocked.CompareExchange(ref this.state, -1, -1) == 0)

                return;

            if (this.cts.Token.CanBeCanceled)
            {
                try
                {
                    this.cts.Cancel(false);
                }
                catch (Exception)
                {
                }

                this.tcs.Task.WaitForCompletionNoSpin();
            }

            Interlocked.CompareExchange(ref this.state, 0, this.state);
        }

        internal void Abort()
        {
            this.Stop();
        }

        internal void Close()
        {
            this.Dispose();
        }

        internal void CheckDisposed()
        {
            if (Interlocked.CompareExchange(ref this.state, -1, -1) == 2)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref this.state, -1, -1) < 2)
            {
                if (disposing)
                {
                    this.Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null

                Interlocked.CompareExchange(ref this.state, 2, this.state);
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ZeroMQRequestListener()
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

        internal IAsyncResult BeginGetContext(AsyncCallback onGetContext, EventTraceActivity eventTraceActivity)
        {
            this.CheckDisposed();
            if (Interlocked.CompareExchange(ref this.state, -1, - 1) == 0)
            {
                throw new InvalidOperationException(System.SR.Format(System.SR.net_listener_mustcall, "Start()"));
            }
            return this.requestQueue.ReceiveAsync(this.cts.Token).ToApm(onGetContext, this);
        }

        internal ZeroMQRequestListenerContext EndGetContext(IAsyncResult listenerRequestResult)
        {
            this.CheckDisposed();
            if (Interlocked.CompareExchange(ref this.state, -1, -1) == 0)
            {
                throw new InvalidOperationException(System.SR.Format(System.SR.net_listener_mustcall, "Start()"));
            }

            ZeroMQListenerRequest listenterRequest = listenerRequestResult.ToApmEnd<ZeroMQListenerRequest>();

            return new ZeroMQRequestListenerContext(this, listenterRequest);
        }
    }
}