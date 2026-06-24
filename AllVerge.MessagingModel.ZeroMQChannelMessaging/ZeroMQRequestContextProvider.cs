using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Runtime.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;

using AllVerge.Core.Resource;
using AllVerge.Core.ServiceModel.Channels;

using AllVerge.Core.ServiceModel.Transfer;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;
using AllVerge.Core.Threading;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    class ZeroMQRequestContextProvider : ZeroMQRequestContext, IRequestContextProvider
    {
        private ITransferTransportFactorySettings settings;
        private ZeroMQTransferMessagingContextHandlerContext zeroMQTransferMessagingContextHandlerContext;

        private IPEndPoint localEndPoint = null;
        private IPEndPoint remoteEndPoint = null;
        private int? messageContentLength = null;
        private MediaContentType messageContentType = null;
        private ICollection<string> messageContentEncoding = null;
        private ICollection<string> messageContentLanguage = null;
        private String action = null;
        private Uri requestUri = null;

        protected ZeroMQRequestContextProvider(ITransferTransportFactorySettings settings, ZeroMQTransferTransportChannelListenerBase listener, ZeroMQTransferMessagingContextHandlerContext zeroMQTransferMessagingContextHandlerContext, Message requestMessage, EventTraceActivity eventTraceActivity) :
            base(listener, requestMessage, eventTraceActivity)
        {
            this.settings = settings;
            this.zeroMQTransferMessagingContextHandlerContext = zeroMQTransferMessagingContextHandlerContext;
            this.RoutingKey = zeroMQTransferMessagingContextHandlerContext.ReceivedContext.Request.RoutingKey;
        }

        internal ITransferTransportFactorySettings Settings => settings;

        //public IPEndPoint LocalEndPoint
        //{
        //    get
        //    {
        //        if (this.localEndPoint == null)
        //        {
        //            ZeroMQMessagingContext receivedMessagingContext = this.zeroMQMessageContextHandlerContext.Received;

        //            ConnectionInfo connection = receivedMessagingContext.Connection;

        //            this.localEndPoint = new IPEndPoint(connection.LocalIpAddress, connection.LocalPort);
        //        }

        //        return this.localEndPoint;
        //    }
        //}

        //public IPEndPoint RemoteEndPoint
        //{
        //    get
        //    {
        //        if (this.remoteEndPoint == null)
        //        {
        //            ZeroMQMessagingContext receivedMessagingContext = this.zeroMQMessageContextHandlerContext.Received;

        //            ConnectionInfo connection = receivedMessagingContext.Connection;

        //            this.remoteEndPoint = new IPEndPoint(connection.RemoteIpAddress, connection.RemotePort);
        //        }

        //        return this.remoteEndPoint;
        //    }
        //}

        public override string Method => this.zeroMQTransferMessagingContextHandlerContext.ReceivedMethod;

        public override MediaContentType ContentType 
        {
            get
            {
                if (this.messageContentType == null)
                {
                    if (this.zeroMQTransferMessagingContextHandlerContext.ReceivedHeaders.TryGetContentType(out MediaContentType contentType))
                    {
                        this.messageContentType = contentType;
                    }
                }
                return this.messageContentType;
            }
        }

        public override long? ContentLength
        {
            get
            {
                if (!this.messageContentLength.HasValue)
                {
                    if (this.zeroMQTransferMessagingContextHandlerContext.ReceivedHeaders.TryGetValue(TransferMessageHeaderNames.ContentLength, out StringValues values) && values.Count > 0)

                        this.messageContentLength = int.Parse(values.First());
                }
                
                if (this.messageContentLength.HasValue)
                    
                    return this.messageContentLength.Value;

                return null;
            }
        }

        public override ICollection<string> ContentEncoding
        {
            get
            {
                if (this.messageContentEncoding == null)
                {
                    if (this.zeroMQTransferMessagingContextHandlerContext.ReceivedHeaders.TryGetValue(TransferMessageHeaderNames.ContentEncoding, out StringValues values))
                    {
                        this.messageContentEncoding = values;
                    }
                }
                return this.messageContentEncoding;
            }
        }

        public override ICollection<string> ContentLanguage
        {
            get
            {
                if (this.messageContentLanguage == null)
                {
                    if (this.zeroMQTransferMessagingContextHandlerContext.ReceivedHeaders.TryGetValue(TransferMessageHeaderNames.ContentLanguage, out StringValues values))
                    {
                        this.messageContentLanguage = values;
                    }
                }
                return this.messageContentLanguage;
            }
        }

        public override IDictionary<string, StringValues> Headers => this.zeroMQTransferMessagingContextHandlerContext.ReceivedHeaders;

        public Uri GetRequestUri()
        {
            if (this.requestUri == null)
            {
                this.requestUri = this.zeroMQTransferMessagingContextHandlerContext.ReceivedRequestUri();
            }
            return this.requestUri;
        }

        public string GetAction()
        {
            if (this.action == null)
            {
                MediaContentType messageContentType = this.ContentType;

                // For Soap/ZeroMQ "request" protocol, we will treat Soap11 the same as Soap12
                // this is different than Soap/Http, where a SOAPAction Http header is used with Soap11
                if (messageContentType.MessageVersion.Envelope == EnvelopeVersion.Soap11 ||
                    messageContentType.MessageVersion.Envelope == EnvelopeVersion.Soap12)
                {
                    if (messageContentType.MediaType == "multipart/related" &&
                        messageContentType.Parameters.ContainsKey("start-info"))
                    {
                        this.action = new MediaContentType(messageContentType.Parameters["start-info"]).Parameters["action"];
                    }

                    if (this.action == null)
                    {
                        this.action = messageContentType.Parameters["action"];
                    }
                }

                if (this.action != null)
                {
                    this.action = UriUtils.UrlDecode(this.action, Encoding.UTF8);

                    if (this.action.Length >= 2 && this.action[0] == '"' && this.action[this.action.Length - 1] == '"')
                    {
                        this.action = this.action.Substring(1, this.action.Length - 2);
                    }
                }
            }

            return this.action;
        }

        public string GetFaultAction()
        {
            return this.ContentType.MessageVersion.Addressing.FaultAction;
        }

        //void HttpExtendedRequestMessageProperty.IHttpCookieProvider.CopyCookies(CookieCollection cookies)
        //{
        //    this.zeroMQMessageContextHandlerContext.CopyReceivedCookies(cookies);
        //}

        //void HttpRequestMessageProperty.IHttpHeaderProvider.CopyHeaders(WebHeaderCollection headers)
        //{
        //    this.zeroMQMessageContextHandlerContext.CopyReceivedHeaders(headers);
        //}

        //bool HttpExtendedRequestMessageProperty.IHttpHeaderProvider.TryGetHeader(string name, out string[] values)
        //{
        //    return this.zeroMQMessageContextHandlerContext.TryGetReceivedHeader(name, out values);
        //}

        //void HttpExtendedRequestMessageProperty.IHttpQueryParametersProvider.CopyQueryParameters(NameValueCollection queryParameters)
        //{
        //    this.zeroMQMessageContextHandlerContext.CopyQueryParameters(queryParameters);
        //}

        //bool HttpExtendedRequestMessageProperty.IHttpFormParametersProvider.HasFormParameters => this.zeroMQMessageContextHandlerContext.HasFormParameters();

        //void HttpExtendedRequestMessageProperty.IHttpFormParametersProvider.CopyFormParameters(NameValueCollection formParameters)
        //{
        //    this.zeroMQMessageContextHandlerContext.CopyFormParameters(formParameters);
        //}

        bool IRequestBodyProvider.HasBody => this.zeroMQTransferMessagingContextHandlerContext.HasBody();

        //public override bool IsWebSocketRequest => throw new NotImplementedException();

        Stream IRequestBodyProvider.GetBody() => this.zeroMQTransferMessagingContextHandlerContext.GetBody();

        protected override ZeroMQTransferMessagingInput GetZeroMQTransferMessagingInput()
        {
            return new ZeroMQRequestInputCore(this);
        }

        protected override ZeroMQTransferMessagingOutput GetZeroMQTransferMessagingOutput(Message message)
        {
            ZeroMQTransferMessagingContext receivedMessagingContext = this.zeroMQTransferMessagingContextHandlerContext.ReceivedContext;

            if (receivedMessagingContext.Request.ContentLength.HasValue)
            {
                receivedMessagingContext.Items["KeepAlive"] = this.settings.KeepAliveEnabled;
            }

            ICompressedMessageEncoder compressedMessageEncoder = this.settings.MessageEncoderFactory.Encoder as ICompressedMessageEncoder;

            if (compressedMessageEncoder != null && compressedMessageEncoder.CompressionEnabled)
            {
                string supportedCompressionTypes = receivedMessagingContext.Request.Headers[HttpChannelUtilities.AcceptEncodingHeader];

                compressedMessageEncoder.AddCompressedMessageProperties(message, supportedCompressionTypes);
            }

            this.zeroMQTransferMessagingContextHandlerContext.Output(receivedMessagingContext);

            return ZeroMQRequestOutputCore.Create(this.zeroMQTransferMessagingContextHandlerContext, this.settings, message, this.Method);
        }

        internal static ZeroMQRequestContext CreateContext(ITransferTransportFactorySettings settings, ZeroMQTransferTransportChannelListenerBase requestChannelListener, ZeroMQTransferMessagingContextHandlerContext zeroMQMessageContextHandlerContext, EventTraceActivity eventTraceActivity)
        {
            return new ZeroMQRequestContextProvider(settings, requestChannelListener, zeroMQMessageContextHandlerContext, null, eventTraceActivity);
        }
    }
}
