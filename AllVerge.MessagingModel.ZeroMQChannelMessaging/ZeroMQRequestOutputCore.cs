using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using global::System.ServiceModel;
    using global::System.ServiceModel.Channels;

    using AllVerge.Core.ServiceModel.Channels;
    using AllVerge.Core.ServiceModel.Methods;
    using AllVerge.Core.ServiceModel.Faults.Exceptions;
    using AllVerge.Core.ServiceModel.ZeroMQ.Channels;
    using AllVerge.Core.Collections;
    using System.Xml;

    internal class ZeroMQRequestOutputCore : ZeroMQTransferMessagingOutput
    {
        private class OutputStream : BytesReadPositionStream
        {
            public OutputStream(Stream outputStream) : base(outputStream)
            {
            }

            public override void Close()
            {
                try
                {
                    base.Close();
                }
                catch (HttpListenerException listenerException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        HttpChannelUtilities.CreateCommunicationException(listenerException));
                }
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                IAsyncResult result;
                try
                {
                    result = base.BeginWrite(buffer, offset, count, callback, state);
                }
                catch (HttpListenerException listenerException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        HttpChannelUtilities.CreateCommunicationException(listenerException));
                }
                catch (ApplicationException innerException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new CommunicationObjectAbortedException(SSR.HttpResponseAborted, innerException));
                }
                return result;
            }

            public override void EndWrite(IAsyncResult result)
            {
                try
                {
                    base.EndWrite(result);
                }
                catch (HttpListenerException listenerException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError
                        (HttpChannelUtilities.CreateCommunicationException(listenerException));
                }
                catch (ApplicationException innerException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new CommunicationObjectAbortedException(SSR.HttpResponseAborted, innerException));
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                try
                {
                    base.Write(buffer, offset, count);
                }
                catch (HttpListenerException listenerException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        HttpChannelUtilities.CreateCommunicationException(listenerException));
                }
                catch (ApplicationException innerException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new CommunicationObjectAbortedException(SSR.HttpResponseAborted, innerException));
                }
            }
        }

        private ZeroMQTransferMessagingContextHandlerContext messagingHandlerContext;

        private string method;

        public ZeroMQRequestOutputCore(ZeroMQTransferMessagingContextHandlerContext messagingHandlerContext, ITransferTransportFactorySettings settings, Message message, string method) :
            base(settings, message, false, false)
        {
            this.messagingHandlerContext = messagingHandlerContext;
            this.method = method;
        }

        protected override string Method
        {
            get
            {
                return this.method;
            }
        }

        public override void Abort(RequestAbortReason abortReason)
        {
            this.messagingHandlerContext.Abort();
            base.Abort(abortReason);
        }

        protected override void AddMimeVersion(string version)
        {
            this.SetContentTypeParameter("MIME-Version", version);
        }

        protected override bool PrepareZeroMQRequestSend(Message message)
        {
            bool isNoContentMessage = base.PrepareZeroMQRequestSend(message);
            if (!isNoContentMessage && base.CanSendCompressedResponses)
            {
                string contentType = this.GetContentType();

                if (HttpChannelUtilities.GetHttpResponseTypeAndEncodingForCompression(ref contentType, out string contentEncoding))
                {
                    if (contentType != this.GetContentType())
                    {
                        this.SetContentType(contentType);
                    }
                    this.SetContentEncoding(contentEncoding.ToEnumerable());
                }
            }
            bool isHeadMethod = string.Compare(this.method, ResourceMethods.HEAD, StringComparison.OrdinalIgnoreCase) == 0;

            if (isHeadMethod)
            {
                isNoContentMessage = true;
            }

            return isNoContentMessage;
        }

        protected override void PrepareZeroMQRequestSendCore(ZeroMQResponseMessage message)
        {
        }

        protected override void SetContentType(string contentType)
        {
            this.messagingHandlerContext.SetResponseContentType(contentType);
        }

        protected override void SetContentTypeParameter(string parameterName, string value)
        {
            this.messagingHandlerContext.SetResponseContentTypeParameter(parameterName, value);
        }

        protected override void SetIsFault(bool isFault)
        {
            throw new NotImplementedException();
        }

        protected String GetContentType()
        {
            return this.messagingHandlerContext.GetResponseContentType();
        }

        protected override void SetContentLength(int contentLength)
        {
            this.SetContentLength(contentLength);
        }

        protected void SetContentLength(long contentLength)
        {
            this.messagingHandlerContext.SetResponseContentLength(contentLength);
        }

        protected long? GetContentLength()
        {
            return this.messagingHandlerContext.GetResponseContentLength();
        }

        protected override void SetContentEncoding(IEnumerable<string> contentEncoding)
        {
            this.messagingHandlerContext.SetContentEncoding(contentEncoding);
        }

        protected string GetContentEncoding()
        {
            return this.messagingHandlerContext.GetContentEncoding();
        }

        protected override void SetResponseAction(string action)
        {
            this.messagingHandlerContext.SetResponseAction(action);
        }

        protected override void SetResponseTo(Uri to)
        {
            this.messagingHandlerContext.SetResponseTo(to);
        }

        protected override void SetResponseRelatesTo(UniqueId relatesTo)
        {
            this.messagingHandlerContext.SetResponseRelatesTo(relatesTo);
        }

        internal static ZeroMQTransferMessagingOutput Create(ZeroMQTransferMessagingContextHandlerContext zeroMQTransferMessagingContextHandlerContext, ITransferTransportFactorySettings settings, Message message, string httpMethod)
        {
            return new ZeroMQRequestOutputCore(zeroMQTransferMessagingContextHandlerContext, settings, message, httpMethod);
        }

        protected void SetChunked(bool sendChunked)
        {
            this.messagingHandlerContext.SetChunked(sendChunked);
        }

        protected bool GetChunked()
        {
            return this.messagingHandlerContext.GetChunked();
        }

        protected void SetKeepAlive(bool keepAlive)
        {
            this.messagingHandlerContext.SetKeepAlive(keepAlive);
        }

        protected bool GetKeepAlive()
        {
            return this.messagingHandlerContext.GetKeepAlive();
        }

        protected void SetRedirect(Uri url)
        {
            this.messagingHandlerContext.SetRedirect(url);
        }

        protected override Stream GetOutputStream()
        {
            return new OutputStream(this.messagingHandlerContext.GetResponseStream());
        }
    }
}
