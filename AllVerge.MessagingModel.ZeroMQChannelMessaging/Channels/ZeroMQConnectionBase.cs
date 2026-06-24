using AllVerge.MessagingModel.MessagingFoundation.Channels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal abstract class ZeroMQConnectionBase : IChannelConnection
    {
        private IConnectionBufferPool connectionBufferPool;
        private IAsyncResult asyncReadResult;
        private IAsyncResult asyncWriteResult;
        private ChannelState state;
        private bool aborted;
        private TimeSpan asyncReceiveTimeout;
        private TimeSpan asyncSendTimeout;
        private bool asyncReceivedTimedout;
        private bool asyncSendTimedout;

        protected ZeroMQConnectionBase(IConnectionBufferPool connectionBufferPool)
        {
            this.connectionBufferPool = connectionBufferPool;
            this.AsyncReadBuffer = connectionBufferPool.Take();
            this.AsyncReadBufferSize = connectionBufferPool.BufferSize;
            this.state = ChannelState.Open;
        }

        public abstract UniqueId ConnectionId { get; }
        string IChannelConnection.ConnectionId => this.ConnectionId?.ToString();
        public virtual IPEndPoint RemoteIPEndPoint { get; }
        public virtual IPEndPoint LocalIPEndPoint { get; }
        public virtual EndpointAddress RemoteAddress { get; }
        public virtual EndpointAddress LocalAddress { get; }

        public byte[] AsyncReadBuffer { get; private set; }

        public int AsyncReadBufferSize { get; }

        public TraceEventType ExceptionEventType { get; set; }

        private object ThisLock
        {
            get { return this; }
        }

        public ChannelState State => state;

        public object GetCoreTransport()
        {
            return null;
        }

        private void TryReturnReadBuffer()
        {
            if (AsyncReadBuffer != null)
            {
                connectionBufferPool.Return(AsyncReadBuffer);

                AsyncReadBuffer = null;
            }
        }

        public int Read(byte[] buffer, int offset, int size, TimeSpan timeout)
        {
            buffer.ValidateBufferBounds(offset, size);
            ThrowIfNotOpen();
            return ReadCoreAsync(buffer, offset, size, timeout).GetAwaiter().GetResult();
        }

        protected abstract Task<int> ReadCoreAsync(byte[] buffer, int offset, int size, TimeSpan timeout);

        public AsyncCompletionResult BeginRead(int offset, int size, TimeSpan timeout, Action<object> callback, object state)
        {
            AsyncReadBuffer.ValidateBufferBounds(offset, size);

            ThrowIfNotOpen();

            return BeginReadCore(offset, size, timeout, callback, state);
        }

        private AsyncCompletionResult BeginReadCore(int offset, int size, TimeSpan timeout, Action<object> callback, object state)
        {
            lock (ThisLock)
            {
                ThrowIfClosed(true);
                asyncReceiveTimeout = timeout;
                asyncReceivedTimedout = false;
            }

            this.asyncReadResult = this.ReadCoreAsync(AsyncReadBuffer, offset, size, timeout).ToApm(t => callback(t.AsyncState), state);

            if (asyncReadResult.IsCompleted)

                return AsyncCompletionResult.Completed;

            return AsyncCompletionResult.Queued;
        }

        public int EndRead()
        {
            int asyncReadSize = this.asyncReadResult.ToApmEnd<int>();

            lock (ThisLock)
            {
                if (state == ChannelState.Closed)
                {
                    TryReturnReadBuffer();
                }
            }

            return asyncReadSize;
        }

        public void Write(byte[] buffer, int offset, int size, bool disableNagle, TimeSpan timeout, BufferManager bufferManager)
        {
            try
            {
                Write(buffer, offset, size, disableNagle, timeout);
            }
            finally
            {
                bufferManager.ReturnBuffer(buffer);
            }
        }

        public void Write(byte[] buffer, int offset, int size, bool disableNagle, TimeSpan timeout)
        {
            // disabling nagle is not supported in ZeroMQ ...

            if (offset > 0)
            {
                byte[] offsetBuffer = new byte[size];

                Array.Copy(buffer, offset, offsetBuffer, 0, size);

                this.WriteCore(offsetBuffer, size, timeout);
            }
            else

                this.WriteCore(buffer, size, timeout);
        }

        protected abstract void WriteCore(byte[] writeBuffer, int size, TimeSpan timeout);

        public AsyncCompletionResult BeginWrite(byte[] buffer, int offset, int size, bool disableNagle, TimeSpan timeout, Action<object> callback, object state)
        {
            lock (ThisLock)
            {
                ThrowIfClosed(false);
                this.asyncSendTimeout = timeout;
                this.asyncSendTimedout = false;
                this.asyncWriteResult = Task.Run(() => Write(buffer, offset, size, disableNagle, timeout)).ToApm(r => callback(r.AsyncState), state);
            }

            if (asyncWriteResult.IsCompleted)

                return AsyncCompletionResult.Completed;

            return AsyncCompletionResult.Queued;
        }

        public void EndWrite()
        {
            this.asyncWriteResult.ToApmEnd();
            this.asyncWriteResult = null;
        }

        protected void ThrowIfNotOpen()
        {
            if (state == ChannelState.Closing || state == ChannelState.Closed)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    CreateObjectDisposedException());
            }
        }

        protected virtual void ThrowIfClosed(bool reading)
        {
            if (state == ChannelState.Closed)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    CreateObjectDisposedException());
            }
        }

        protected Exception CreateObjectDisposedException()
        {
            ObjectDisposedException disposedException = new ObjectDisposedException(GetType().ToString(), PublicSR.SocketConnectionDisposed);

            if (asyncReceivedTimedout)
            {
                return new TimeoutException(PublicSR.Format(PublicSR.SocketAbortedReceiveTimedOut, asyncReceiveTimeout), disposedException);
            }
            else if (asyncSendTimedout)
            {
                return new TimeoutException(PublicSR.Format(PublicSR.SocketAbortedSendTimedOut, asyncSendTimeout), disposedException);
            }
            else if (aborted)
            {
                return new CommunicationObjectAbortedException(PublicSR.SocketConnectionDisposed, disposedException);
            }
            else
            {
                return disposedException;
            }
        }

        public void Abort()
        {
            lock (ThisLock)
            {
                if (state == ChannelState.Aborting || state == ChannelState.Closed)
                {
                    return;
                }
                this.state = ChannelState.Aborting;
                AbortingCore();
                TryReturnReadBuffer();
                aborted = true;
                state = ChannelState.Closed;
            }
        }

        /// <summary>
        /// Override to implement any aborting behavior.
        /// </summary>
        protected virtual void AbortingCore()
        {
        }

        public void Close(TimeSpan timeout, bool asyncAndLinger)
        {
            Action closingCallback = this.Closing();

            ChannelState _beginState;

            lock (ThisLock)
            {
                _beginState = state;

                if (state == ChannelState.Open)
                {
                    state = ChannelState.Closing;
                }
            }

            if (closingCallback != null)

                closingCallback();

            if (_beginState != ChannelState.Open)
            {
                // already closing or closed, so just return
                return;
            }

            Action closedCallback = this.ClosedCore(timeout);

            lock (ThisLock)
            {
                // Abort could have been called on a separate thread and cleaned up 
                // our buffers/completion here
                if (state != ChannelState.Closed)
                {
                    TryReturnReadBuffer();
                }

                state = ChannelState.Closed;
            }

            if (closedCallback != null)

                closedCallback();
        }

        /// <summary>
        /// Override to return a callback to be called whenever <see cref="Close(TimeSpan, bool)"/> is called.
        /// </summary>
        /// <returns></returns>
        protected virtual Action Closing()
        {
            return null;
        }

        /// <summary>
        /// Override to implement any close behavior and return a callback to be called when closed.
        /// </summary>
        /// <returns></returns>
        protected virtual Action ClosedCore(TimeSpan timeout)
        {
            return null;
        }
    }
}
