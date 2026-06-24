using AllVerge.Core.ServiceModel.Channels;
using AllVerge.Core.ServiceModel.Transfer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public class ZeroMQListenerResponse : IDisposable
    {
        private enum ResponseState
        {
            Created,
            SentHeaders,
            Closed
        }

        private BoundaryType boundaryType;
        private ResponseState responseState;

        private ZeroMQResponseStream responseStream;
        private ZeroMQRequestListenerContext listenerContext;
        private bool disposedValue;

        internal ZeroMQListenerResponse()
        {
            //m_NativeResponse = default(UnsafeNclNativeMethods.HttpApi.HTTP_RESPONSE);
            //m_WebHeaders = new WebHeaderCollection(WebHeaderCollectionType.HttpListenerResponse);
            ContentType = new MediaContentType();
            boundaryType = BoundaryType.None;
            //m_NativeResponse.StatusCode = 200;
            //m_NativeResponse.Version.MajorVersion = 1;
            //m_NativeResponse.Version.MinorVersion = 1;
            KeepAlive = true;
            responseState = ResponseState.Created;
        }

        internal ZeroMQListenerResponse(ZeroMQRequestListenerContext listenerContext) : 
            this()
        {
            this.listenerContext = listenerContext;
        }

        public bool KeepAlive { get; internal set; }

        public bool IsFault { get; internal set; }

        public bool SendChunked { get; set; }

        public string MIMEVersion { get; internal set; }

        public IEnumerable<string> ContentEncoding { get; internal set; }

        public long ContentLength64 { get; internal set; }

        public MediaContentType ContentType { get; internal set; }

        public string Action { get; internal set; }

        public Uri To { get; internal set; }

        public UniqueId RelatesTo { get; internal set; }

        public bool SentHeaders => responseState >= ResponseState.SentHeaders;

        public Stream OutputStream
        {
            get
            {
                CheckDisposed();
                EnsureResponseStream();
                return responseStream;
            }
        }

        private void CheckDisposed()
        {
            if (responseState >= ResponseState.Closed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private void EnsureResponseStream()
        {
            if (responseStream == null)
            {
                responseStream = new ZeroMQResponseStream(listenerContext);
            }
        }

        internal void SendFault(FaultCode faultCode)
        {
            throw new NotImplementedException();
        }

        internal void Abort()
        {
            if (responseState >= ResponseState.Closed)
            {
                return;
            }

            responseState = ResponseState.Closed;
            listenerContext.Abort();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ZeroMQListenerException" />
        internal void Close()
        {
            // Should only throw ZeroMQListenerException ..

            this.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    EnsureResponseStream();
                    responseStream.Close();
                    responseState = ResponseState.Closed;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        internal ResponseSentCode SendHeaders()
        {
            if (listenerContext.Response.ContentLength64 > 0)
                this.listenerContext.Listener.Write(
                    this.listenerContext.Request.RoutingKey, Encoding.ASCII.GetBytes($"{TransferMessageHeaderNames.ContentType}:{listenerContext.Response.ContentType.ToMediaTypePlusParameters()}"));
            
            this.listenerContext.Listener.Write(
                this.listenerContext.Request.RoutingKey, Encoding.ASCII.GetBytes($"{TransferMessageHeaderNames.ContentLength}:{listenerContext.Response.ContentLength64.ToString()}"));

            if (listenerContext.Response.ContentEncoding != null)
            {
                string contentEncoding = String.Join(",", listenerContext.Response.ContentEncoding.ToArray());

                if (contentEncoding.Length > 0 )

                    this.listenerContext.Listener.Write(
                        this.listenerContext.Request.RoutingKey, Encoding.ASCII.GetBytes($"{TransferMessageHeaderNames.ContentEncoding}:{contentEncoding}"));
            }

            this.responseState = ResponseState.SentHeaders;

            return ResponseSentCode.Success;
        }

        internal ResponseSentCode SendBody(byte[] buffer, int offset, int size)
        {
            this.listenerContext.Listener.Write(
                this.listenerContext.Request.RoutingKey, Array.Empty<byte>(), 0, 0, true, TimeSpan.MaxValue);

            this.listenerContext.Listener.Write(
                this.listenerContext.Request.RoutingKey, buffer, offset, size, true, TimeSpan.MaxValue);

            this.listenerContext.Listener.Write(
                this.listenerContext.Request.RoutingKey);

            return ResponseSentCode.Success;
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ZeroMQListenerResponse()
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