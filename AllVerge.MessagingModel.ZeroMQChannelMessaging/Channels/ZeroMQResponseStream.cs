namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.Core.ServiceModel.Transfer;
    using System;
	using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal class ZeroMQResponseStream : Stream
	{
		private ZeroMQRequestListenerContext listenerContext;

        private bool opaqueMode;
        private long? remainingDataSize = null;
        private bool closed;

        internal ZeroMQResponseStream(ZeroMQRequestListenerContext listenerContext)
		{
			this.listenerContext = listenerContext;
		}

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override bool CanRead => false;

		internal bool Closed => closed;

		internal ZeroMQRequestListenerContext InternalRequestContext => listenerContext;

		public override long Length
		{
			get
			{
				throw new NotSupportedException(SR.net_noseek);
			}
		}

		public override long Position
		{
			get
			{
				throw new NotSupportedException(SR.net_noseek);
			}
			set
			{
				throw new NotSupportedException(SR.net_noseek);
			}
		}

        internal void SetClosedFlag()
        {
            closed = true;
        }

        public override void Flush()
		{
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException(SR.net_noseek);
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException(SR.net_noseek);
		}

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException(SR.net_writeonlystream);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			throw new InvalidOperationException(SR.net_writeonlystream);
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			throw new InvalidOperationException(SR.net_writeonlystream);
		}

        public override void Write(byte[] buffer, int offset, int size)
        {
            //	if (Logging.On)
            //	{
            //		Logging.Enter(Logging.HttpListener, this, "Write", "");
            //	}
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            bool sentHeaders = listenerContext.Response.SentHeaders;
            uint dataSize = (uint)size;
            if (remainingDataSize == null)
            {
                remainingDataSize = this.listenerContext.Request.Method == "HEAD" ? 0 : this.listenerContext.Response.ContentLength64;
            }
            ResponseSentCode responseSentCode = ResponseSentCode.Pending;
            if (size == 0)
            {
                responseSentCode = listenerContext.Response.SendHeaders();
            }
            else
            {
                if (listenerContext.Response.SendChunked)
                {
                    throw new NotImplementedException($"Chunked encoding is not implemented for ZeroMQ channels at this time.");
                }

                if (sentHeaders)

                    responseSentCode = ResponseSentCode.Success;

                else
                
                    responseSentCode = listenerContext.Response.SendHeaders();

                if (responseSentCode == ResponseSentCode.Success)

                    responseSentCode = listenerContext.Response.SendBody(buffer, offset, size);

                if (listenerContext.Listener.IgnoreWriteExceptions)
                {
                    responseSentCode = ResponseSentCode.Success;
                }
            }
            if (responseSentCode.IsNotSentCode())
            {
                Exception ex = new ResponseException(responseSentCode);
                closed = true;
                listenerContext.Abort();
                throw ex;
            }
            UpdateAfterWrite(dataSize);
        }

        private void UpdateAfterWrite(uint dataSize)
        {
            if (!opaqueMode)
            {
                if (remainingDataSize > 0)
                {
                    remainingDataSize -= dataSize;
                }
                if (remainingDataSize == 0L)
                {
                    closed = true;
                }
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return base.BeginWrite(buffer, offset, count, callback, state);
        }
        //[HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        //public unsafe override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        //{
        //	if (buffer == null)
        //	{
        //		throw new ArgumentNullException("buffer");
        //	}
        //	if (offset < 0 || offset > buffer.Length)
        //	{
        //		throw new ArgumentOutOfRangeException("offset");
        //	}
        //	if (size < 0 || size > buffer.Length - offset)
        //	{
        //		throw new ArgumentOutOfRangeException("size");
        //	}
        //	UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS hTTP_FLAGS = ComputeLeftToWrite();
        //	if (m_Closed || (size == 0 && m_LeftToWrite != 0L))
        //	{
        //		if (Logging.On)
        //		{
        //			Logging.Exit(Logging.HttpListener, this, "BeginWrite", "");
        //		}
        //		ZeroMQResponseStreamAsyncResult httpResponseStreamAsyncResult = new ZeroMQResponseStreamAsyncResult(this, state, callback);
        //		httpResponseStreamAsyncResult.InvokeCallback(0u);
        //		return httpResponseStreamAsyncResult;
        //	}
        //	if (m_LeftToWrite >= 0 && size > m_LeftToWrite)
        //	{
        //		throw new ProtocolViolationException(SR.GetString("net_entitytoobig"));
        //	}
        //	uint numBytes = 0u;
        //	hTTP_FLAGS = (UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS)((int)hTTP_FLAGS | ((m_LeftToWrite != size) ? 2 : 0));
        //	bool sentHeaders = listenerContext.Response.SentHeaders;
        //	ZeroMQResponseStreamAsyncResult httpResponseStreamAsyncResult2 = new ZeroMQResponseStreamAsyncResult(this, state, callback, buffer, offset, size, listenerContext.Response.BoundaryType == BoundaryType.Chunked, sentHeaders);
        //	UpdateAfterWrite((uint)((listenerContext.Response.BoundaryType != BoundaryType.Chunked) ? size : 0));
        //	uint num;
        //	try
        //	{
        //		if (!sentHeaders)
        //		{
        //			num = listenerContext.Response.SendHeaders(null, httpResponseStreamAsyncResult2, hTTP_FLAGS, isWebSocketHandshake: false);
        //		}
        //		else
        //		{
        //			listenerContext.EnsureBoundHandle();
        //			num = UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(listenerContext.RequestQueueHandle, listenerContext.RequestId, (uint)hTTP_FLAGS, httpResponseStreamAsyncResult2.dataChunkCount, httpResponseStreamAsyncResult2.pDataChunks, &numBytes, SafeLocalFree.Zero, 0u, httpResponseStreamAsyncResult2.m_pOverlapped, null);
        //		}
        //	}
        //	catch (Exception e)
        //	{
        //		if (Logging.On)
        //		{
        //			Logging.Exception(Logging.HttpListener, this, "BeginWrite", e);
        //		}
        //		httpResponseStreamAsyncResult2.InternalCleanup();
        //		m_Closed = true;
        //		listenerContext.Abort();
        //		throw;
        //	}
        //	if (num != 0 && num != 997)
        //	{
        //		httpResponseStreamAsyncResult2.InternalCleanup();
        //		if (!listenerContext.Listener.IgnoreWriteExceptions || !sentHeaders)
        //		{
        //			Exception ex = new HttpListenerException((int)num);
        //			if (Logging.On)
        //			{
        //				Logging.Exception(Logging.HttpListener, this, "BeginWrite", ex);
        //			}
        //			m_Closed = true;
        //			listenerContext.Abort();
        //			throw ex;
        //		}
        //	}
        //	if (num == 0 && HttpListener.SkipIOCPCallbackOnSuccess)
        //	{
        //		httpResponseStreamAsyncResult2.IOCompleted(num, numBytes);
        //	}
        //	if ((hTTP_FLAGS & UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_SEND_RESPONSE_FLAG_MORE_DATA) == UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.NONE)
        //	{
        //		m_LastWrite = httpResponseStreamAsyncResult2;
        //	}
        //	if (Logging.On)
        //	{
        //		Logging.Exit(Logging.HttpListener, this, "BeginWrite", "");
        //	}
        //	return httpResponseStreamAsyncResult2;
        //}

        public override void EndWrite(IAsyncResult asyncResult)
        {
            base.EndWrite(asyncResult);
        }
        //public override void EndWrite(IAsyncResult asyncResult)
        //{
        //	if (Logging.On)
        //	{
        //		Logging.Enter(Logging.HttpListener, this, "EndWrite", "");
        //	}
        //	if (asyncResult == null)
        //	{
        //		throw new ArgumentNullException("asyncResult");
        //	}
        //	ZeroMQResponseStreamAsyncResult httpResponseStreamAsyncResult = asyncResult as ZeroMQResponseStreamAsyncResult;
        //	if (httpResponseStreamAsyncResult == null || httpResponseStreamAsyncResult.AsyncObject != this)
        //	{
        //		throw new ArgumentException(SR.GetString("net_io_invalidasyncresult"), "asyncResult");
        //	}
        //	if (httpResponseStreamAsyncResult.EndCalled)
        //	{
        //		throw new InvalidOperationException(SR.GetString("net_io_invalidendcall", "EndWrite"));
        //	}
        //	httpResponseStreamAsyncResult.EndCalled = true;
        //	object obj = httpResponseStreamAsyncResult.InternalWaitForCompletion();
        //	Exception ex = obj as Exception;
        //	if (ex != null)
        //	{
        //		if (Logging.On)
        //		{
        //			Logging.Exception(Logging.HttpListener, this, "EndWrite", ex);
        //		}
        //		m_Closed = true;
        //		listenerContext.Abort();
        //		throw ex;
        //	}
        //	if (Logging.On)
        //	{
        //		Logging.Exit(Logging.HttpListener, this, "EndWrite", "");
        //	}
        //}

        //private void UpdateAfterWrite(uint dataWritten)
        //{
        //	if (!m_InOpaqueMode)
        //	{
        //		if (m_LeftToWrite > 0)
        //		{
        //			m_LeftToWrite -= dataWritten;
        //		}
        //		if (m_LeftToWrite == 0L)
        //		{
        //			m_Closed = true;
        //		}
        //	}
        //}

        protected override void Dispose(bool disposing)
        {
            //	if (Logging.On)
            //	{
            //		Logging.Enter(Logging.HttpListener, this, "Close", "");
            //	}
            //	try
            //	{
            //		if (disposing)
            //		{
            //			if (m_Closed)
            //			{
            //				if (Logging.On)
            //				{
            //					Logging.Exit(Logging.HttpListener, this, "Close", "");
            //				}
            //				return;
            //			}
            //			m_Closed = true;
            //			UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS hTTP_FLAGS = ComputeLeftToWrite();
            //			if (m_LeftToWrite > 0 && !m_InOpaqueMode)
            //			{
            //				throw new InvalidOperationException(SR.GetString("net_io_notenoughbyteswritten"));
            //			}
            //			bool sentHeaders = listenerContext.Response.SentHeaders;
            //			if (sentHeaders && m_LeftToWrite == 0L)
            //			{
            //				if (Logging.On)
            //				{
            //					Logging.Exit(Logging.HttpListener, this, "Close", "");
            //				}
            //				return;
            //			}
            //			uint num = 0u;
            //			if ((listenerContext.Response.BoundaryType == BoundaryType.Chunked || listenerContext.Response.BoundaryType == BoundaryType.None) && string.Compare(listenerContext.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase) != 0)
            //			{
            //				if (listenerContext.Response.BoundaryType == BoundaryType.None)
            //				{
            //					hTTP_FLAGS |= UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY;
            //				}
            //				try
            //				{
            //					byte[] chunkTerminator = NclConstants.ChunkTerminator;
            //					fixed (IntPtr* pBuffer = chunkTerminator)
            //					{
            //						UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK* ptr = null;
            //						if (listenerContext.Response.BoundaryType == BoundaryType.Chunked)
            //						{
            //							UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK hTTP_DATA_CHUNK = default(UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK);
            //							hTTP_DATA_CHUNK.DataChunkType = UnsafeNclNativeMethods.HttpApi.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;
            //							hTTP_DATA_CHUNK.pBuffer = (byte*)pBuffer;
            //							hTTP_DATA_CHUNK.BufferLength = (uint)NclConstants.ChunkTerminator.Length;
            //							ptr = &hTTP_DATA_CHUNK;
            //						}
            //						if (!sentHeaders)
            //						{
            //							num = listenerContext.Response.SendHeaders(ptr, null, hTTP_FLAGS, isWebSocketHandshake: false);
            //						}
            //						else
            //						{
            //							num = UnsafeNclNativeMethods.HttpApi.HttpSendResponseEntityBody(listenerContext.RequestQueueHandle, listenerContext.RequestId, (uint)hTTP_FLAGS, (ushort)((ptr != null) ? 1 : 0), ptr, null, SafeLocalFree.Zero, 0u, null, null);
            //							if (listenerContext.Listener.IgnoreWriteExceptions)
            //							{
            //								num = 0u;
            //							}
            //						}
            //					}
            //				}
            //				finally
            //				{
            //				}
            //			}
            //			else if (!sentHeaders)
            //			{
            //				num = listenerContext.Response.SendHeaders(null, null, hTTP_FLAGS, isWebSocketHandshake: false);
            //			}
            //			if (num != 0 && num != 38)
            //			{
            //				Exception ex = new HttpListenerException((int)num);
            //				if (Logging.On)
            //				{
            //					Logging.Exception(Logging.HttpListener, this, "Close", ex);
            //				}
            //				listenerContext.Abort();
            //				throw ex;
            //			}
            //			m_LeftToWrite = 0L;
            //		}
            //	}
            //	finally
            //	{
            //		base.Dispose(disposing);
            //	}
            //	if (Logging.On)
            //	{
            //		Logging.Exit(Logging.HttpListener, this, "Dispose", "");
            //	}
        }

        //internal void SwitchToOpaqueMode()
        //{
        //	m_InOpaqueMode = true;
        //	m_LeftToWrite = long.MaxValue;
        //}

        //internal unsafe void CancelLastWrite(CriticalHandle requestQueueHandle)
        //{
        //	ZeroMQResponseStreamAsyncResult lastWrite = m_LastWrite;
        //	if (lastWrite != null && !lastWrite.IsCompleted)
        //	{
        //		UnsafeNclNativeMethods.CancelIoEx(requestQueueHandle, lastWrite.m_pOverlapped);
        //	}
        //}
    }
}