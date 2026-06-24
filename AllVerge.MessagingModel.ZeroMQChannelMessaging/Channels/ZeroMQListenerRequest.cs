using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

using AllVerge.Core.ServiceModel.Channels;
using AllVerge.Core.ServiceModel.Transfer;

using Microsoft.Extensions.Primitives;
using NetMQ;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public class ZeroMQListenerRequest
    {
        private ZeroMQRequestListenerContext listenerContext;

        private long contentLength64;
        private MediaContentType contentType;
        private StringValues contentEncoding;
        private StringValues contentLanguage;
        private Stream inputStream;
        private bool closed = false;

        public ZeroMQListenerRequest(RoutingKey routingKey, string requestLine)
        {
            this.RoutingKey = routingKey;

            if (requestLine != null)
            {
                string[] requestLineElements = requestLine.Split(' ');

                if (requestLineElements.Length == 2)
                {
                    this.Url = new Uri(requestLineElements[1]);
                    this.Method = requestLineElements[0];
                }
                else
                    throw new ArgumentException("Invalid parameter format.", nameof(requestLine));
            }
        }

        public RoutingKey RoutingKey { get; }
        public Uri Url { get; }
        public EndpointAddress LocalEndPoint { get; }
        public EndpointAddress RemoteEndPoint { get; }
        public String Method { get; }
        public IDictionary<string, StringValues> Headers { get; internal set; }
        public Guid RequestTraceIdentifier { get; internal set; }

        public long ContentLength64
        {
            get
            {
                if (Headers != null && long.TryParse(Headers[TransferMessageHeaderNames.ContentLength], out long contentLength))

                    this.contentLength64 = contentLength;

                 return this.contentLength64;
            }
        }

        public MediaContentType ContentType
        {
            get
            {
                if (Headers != null && Headers.TryGetContentType(out MediaContentType contentType))

                    this.contentType = contentType;

                return this.contentType;
            }
        }

        public StringValues ContentEncoding
        {
            get
            {
                if (Headers != null && Headers.TryGetValue(TransferMessageHeaderNames.ContentEncoding, out StringValues contentEncoding))

                    this.contentEncoding = contentEncoding;

                return this.contentEncoding;
            }
        }

        public StringValues ContentLanguage
        {
            get
            {
                if (Headers != null && Headers.TryGetValue(TransferMessageHeaderNames.ContentLanguage, out StringValues contentLangugage))

                    this.contentLanguage = contentLangugage;

                return this.contentLanguage;
            }
        }

        public MessageVersion Version
        {
            get
            {
                if (ContentType.Parameters.ContainsKey(MediaContentType.PARAMETER_KEY_SOAP_11_SOAPACTION))

                    return ContentType.MessageVersion;

                return null;
            }
        }

        public string SoapAction
        {
            get
            {
                if (ContentType.Parameters.ContainsKey(MediaContentType.PARAMETER_KEY_SOAP_11_SOAPACTION))

                    return ContentType.Parameters[MediaContentType.PARAMETER_KEY_SOAP_11_SOAPACTION];

                return null;
            }
        }
        public Stream InputStream
        {
            get
            {
                CheckDisposed();

                return this.inputStream;
            } 
            
            internal set => this.inputStream = value;
        }

        internal ZeroMQListenerRequest SetContext(ZeroMQRequestListenerContext listenerContext)
        {
            this.listenerContext = listenerContext;

            return this;
        }

        internal void CheckDisposed()
        {
            if (this.closed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }


        internal void Close()
        {
            if (!this.closed)
            {
                if (InputStream != null)

                    InputStream.Close();

                this.closed = true;
            }
        }
    }
}