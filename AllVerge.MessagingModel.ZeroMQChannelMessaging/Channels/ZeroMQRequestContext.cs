using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Diagnostics;
    using System.ServiceModel.Diagnostics.Application;

    using AllVerge.Core.ServiceModel.Channels;
    using AllVerge.Core.ServiceModel.Transfer;
    using AllVerge.Core.Threading;

    using NetMQ;

    internal abstract class ZeroMQRequestContext : RequestContextBase, IMessageContentPropertiesProvider
    {
        private bool errorGettingRequestInput;
        private Message requestMessage;

        private ZeroMQTransferTransportChannelListenerBase listener;
        private EventTraceActivity eventTraceActivity;
        private ZeroMQTransferMessagingPipeline requestPipeline;
        private ZeroMQTransferMessagingOutput requestOutput;

        protected ZeroMQRequestContext(TimeSpan defaultCloseTimeout, TimeSpan defaultSendTimeout) :
            base(null, defaultCloseTimeout, defaultSendTimeout)
        {
        }

        protected ZeroMQRequestContext(ZeroMQTransferTransportChannelListenerBase listener, Message requestMessage, EventTraceActivity eventTraceActivity)
            : base(requestMessage, listener.InternalCloseTimeout, listener.InternalSendTimeout)
        {
            this.listener = listener;
            this.eventTraceActivity = eventTraceActivity;
        }

        public bool KeepAliveEnabled => listener.KeepAliveEnabled;

        public bool ZeroMQMessagesSupported => true;

        public RoutingKey RoutingKey { get; protected set; }

        public string TraceIdentifier { get; internal set; }

        public virtual string Method => this.listener.Method;

        public Uri RequestUri => this.listener.Uri;

        public abstract IDictionary<string, StringValues> Headers { get; }

        public abstract MediaContentType ContentType { get; }

        public abstract long? ContentLength { get; }

        public abstract ICollection<string> ContentEncoding { get; }

        public abstract ICollection<string> ContentLanguage { get; }

        public override Message RequestMessage
        {
            get 
            {
                if (this.requestMessage == null)
                {
                    ZeroMQTransferMessagingInput requestInput = this.GetZeroMQTransferMessagingInput(true);

                    Message requestMessage = requestInput.ParseIncomingMessage(out Exception messageAddressingException);

                    if (messageAddressingException != null)

                        throw messageAddressingException;

                    this.requestMessage = requestMessage;
                }

                return this.requestMessage;
            }
        }

        internal ZeroMQTransferTransportChannelListenerBase Listener
        {
            get { return this.listener; }
        }

        internal EventTraceActivity EventTraceActivity
        {
            get
            {
                return this.eventTraceActivity;
            }
        }

        Uri IMessageContentPropertiesProvider.RequestUri { get => this.RequestUri; set => throw new NotImplementedException(); }
        long? IMessageContentPropertiesProvider.ContentLength { get => this.ContentLength; set => throw new NotImplementedException(); }
        MediaContentType IMessageContentPropertiesProvider.ContentType { get => this.ContentType; set => throw new NotImplementedException(); }
        ICollection<string> IMessageContentPropertiesProvider.ContentEncoding { get => this.ContentEncoding; set => throw new NotImplementedException(); }
        ICollection<string> IMessageContentPropertiesProvider.ContentLanguage { get => this.ContentLanguage; set => throw new NotImplementedException(); }
        IDictionary<string, StringValues> IMessageContentPropertiesProvider.Headers { get => this.Headers; set => throw new NotImplementedException(); }
        void IMessageContentPropertiesProvider.CopyPropertiesFrom(Transfer.MessageContent propertiesProvider)
        {
            throw new NotImplementedException();
        }

        protected abstract ZeroMQTransferMessagingOutput GetZeroMQTransferMessagingOutput(Message message);

        protected abstract ZeroMQTransferMessagingInput GetZeroMQTransferMessagingInput();

        public ZeroMQTransferMessagingInput GetZeroMQTransferMessagingInput(bool throwOnError)
        {
            ZeroMQTransferMessagingPipeline pipeline = this.requestPipeline;

            if ((pipeline != null) && pipeline.IsRequestInputInitialized)
            {
                return pipeline.ZeroMQTransferMessagingInput;
            }

            ZeroMQTransferMessagingInput requestInput = null;

            if (throwOnError || !this.errorGettingRequestInput)
            {
                try
                {
                    requestInput = GetZeroMQTransferMessagingInput();

                    this.errorGettingRequestInput = false;
                }
                catch (Exception e)
                {
                    this.errorGettingRequestInput = true;

                    if (throwOnError || Fx.IsFatal(e))
                    {
                        throw;
                    }

                    DiagnosticUtility.TraceHandledException(e, TraceEventType.Warning);
                }
            }

            return requestInput;
        }

        public ZeroMQTransferMessagingOutput GetZeroMQRequestOutput(Message message)
        {
            if (this.requestOutput != null)
            {
                return this.requestOutput;
            }

            return this.GetZeroMQTransferMessagingOutput(message);
        }

        public void InitializeRequestPipeline(TransferMessagingIntegrationHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> transportIntegrationHandler)
        {
            this.requestPipeline = ZeroMQTransferMessagingPipeline.CreateZeroMQRequestPipeline(this, transportIntegrationHandler);
        }

        internal IAsyncResult BeginProcessInboundRequest(
                    ReplyChannelAcceptor replyChannelAcceptor,
                    Action acceptorCallback,
                    AsyncCallback callback,
                    object state)
        {
            return this.requestPipeline.BeginProcessInboundRequest(replyChannelAcceptor, acceptorCallback, callback, state);
        }

        internal void EndProcessInboundRequest(IAsyncResult result)
        {
            this.requestPipeline.EndProcessInboundRequest(result);
        }

        bool PrepareReply(ref Message message)
        {
            //bool closeOnReceivedEof = false;

            // null means we're done
            if (message == null)
            {
                // A null message means either a one-way request or that the service operation returned null and
                // hence we can close the HttpOutput. By default we keep the HttpOutput open to allow the writing to the output 
                // even after the HttpInput EOF is received and the HttpOutput will be closed only on close of the HttpRequestContext.
                //closeOnReceivedEof = true;
                //message = CreateAckMessage(HttpStatusCode.Accepted, string.Empty);
                message = new NullMessage();
            }

            if (!listener.ManualAddressing)
            {
                if (message.Version.Addressing == AddressingVersion.WSAddressingAugust2004)
                {
                    if (message.Headers.To == null ||
                        listener.AnonymousUriPrefixMatcher == null ||
                        !listener.AnonymousUriPrefixMatcher.IsAnonymousUri(message.Headers.To))
                    {
                        message.Headers.To = message.Version.Addressing.AnonymousUri;
                    }
                }
                else if (message.Version.Addressing == AddressingVersion.WSAddressing10
                    || message.Version.Addressing == AddressingVersion.None)
                {
                    if (message.Headers.To != null &&
                        (listener.AnonymousUriPrefixMatcher == null ||
                        !listener.AnonymousUriPrefixMatcher.IsAnonymousUri(message.Headers.To)))
                    {
                        message.Headers.To = null;
                    }
                }
                else
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new ProtocolException(SSR.Format(SSR.AddressingVersionNotSupported, message.Version.Addressing)));
                }
            }

            message.Properties.AllowOutputBatching = false;

            this.requestOutput = GetZeroMQRequestOutput(message);

            // Reuse the HttpInput we got previously.
            //ZeroMQRequestInput input = this.httpPipeline.HttpInput;
            //if (input != null)
            //{
            //    HttpDelayedAcceptStream requestStream = input.GetInputStream(false) as HttpDelayedAcceptStream;
            //    if (requestStream != null && TransferModeHelper.IsRequestStreamed(listener.TransferMode)
            //        && requestStream.EnableDelayedAccept(this.httpOutput, closeOnReceivedEof))
            //    {
            //        return false;
            //    }
            //}

            return true;
        }

        protected override void OnReply(Message message, TimeSpan timeout)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            Message responseMessage = message;

            try
            {
                bool closeOutputAfterReply = PrepareReply(ref responseMessage);
                this.requestPipeline.SendReply(responseMessage, timeoutHelper.RemainingTime());

                if (closeOutputAfterReply)
                {
                    this.requestOutput.Close();
                }

                if (TD.MessageSentByTransportIsEnabled())
                {
                    TD.MessageSentByTransport(eventTraceActivity, this.Listener.Uri.AbsoluteUri);
                }
            }
            finally
            {
                if (message != null &&
                    !object.ReferenceEquals(message, responseMessage))
                {
                    responseMessage.Close();
                }
            }
        }

        protected override IAsyncResult OnBeginReply(
            Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new ReplyAsyncResult(this, message, timeout, callback, state);
        }

        protected override void OnEndReply(IAsyncResult result)
        {
            ReplyAsyncResult.End(result);
        }

        protected override void OnAbort()
        {
            if (this.requestOutput != null)
            {
                this.requestOutput.Abort(RequestAbortReason.Aborted);
            }

            this.Cleanup();
        }

        protected override void OnClose(TimeSpan timeout)
        {
            try
            {
                if (this.requestOutput != null)
                {
                    this.requestOutput.Close();
                }
            }
            finally
            {
                this.Cleanup();
            }
        }

        protected virtual void Cleanup()
        {
            if (this.requestPipeline != null)
            {
                this.requestPipeline.Close();
            }
        }

        internal void SetMessage(Message message, Exception requestException)
        {
            if (message == null && requestException == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ProtocolException(SSR.MessageXmlProtocolError, new XmlException(SSR.MessageIsEmpty)));
            }
            this.TraceRequestMessageReceived(message);
            if (requestException != null)
            {
                base.SetRequestMessage(requestException);
                message.Close();
                return;
            }
            //Skip setting SecurityMessageProperty until (if) handling message level security ...
            //message.Properties.Security = ((this.securityProperty != null) ? ((SecurityMessageProperty)this.securityProperty.CreateCopy()) : null);
            base.SetRequestMessage(message);
        }

        void TraceRequestMessageReceived(Message message)
        {
            if (FxTrace.Trace.IsEnd2EndActivityTracingEnabled)
            {
                bool attached = false;
                Guid relatedId = this.eventTraceActivity != null ? this.eventTraceActivity.ActivityId : Guid.Empty;
                HttpRequestMessageProperty httpProperty;

                // Encoder will always add an activity. We need to remove this and read it
                // from the web headers for http since correlation might be propogated.
                if (message.Headers.MessageId == null &&
                    message.Properties.TryGetProperty<HttpRequestMessageProperty>(HttpRequestMessageProperty.Name, out httpProperty))
                {
                    try
                    {
                        string e2eId = httpProperty.Headers[EventTraceActivity.Name];
                        if (!String.IsNullOrEmpty(e2eId))
                        {
                            byte[] data = Convert.FromBase64String(e2eId);
                            if (data != null && data.Length == 16)
                            {
                                Guid id = new Guid(data);
                                this.eventTraceActivity = new EventTraceActivity(id, true);
                                message.Properties[EventTraceActivity.Name] = this.eventTraceActivity;
                                attached = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Fx.IsFatal(ex))
                        {
                            throw;
                        }
                    }
                }

                if (!attached)
                {
                    this.eventTraceActivity = EventTraceActivityHelper.TryExtractActivity(message, true);
                }

                if (TD.MessageReceivedByTransportIsEnabled())
                {
                    TD.MessageReceivedByTransport(
                        this.eventTraceActivity,
                        this.listener != null && this.listener.Uri != null ? this.listener.Uri.AbsoluteUri : string.Empty,
                        relatedId);
                }
            }
        }

        internal void SendResponseAndClose(ProtocolException protocolException)
        {
            throw new NotImplementedException();

            //this.Close();
        }

        internal void SendResponseAndClose(ZeroMQResponseMessage responseMessage)
        {
            if (this.TryInitiateReply())
            {
                // Send the response message.
                try
                {
                    if (this.requestOutput == null)
                    {
                        this.requestOutput = this.GetZeroMQRequestOutput(new NullMessage());
                    }
                    this.requestOutput.Send(responseMessage, this.DefaultSendTimeout);
                }
                catch (Exception ex)
                {
                    if (Fx.IsFatal(ex))
                    {
                        throw;
                    }

                    DiagnosticUtility.TraceHandledException(ex, TraceEventType.Information);
                }
            }

            // Close the request context.
            try
            {
                this.Close(); // this also closes the request Output
            }
            catch (Exception ex)
            {
                if (Fx.IsFatal(ex))
                {
                    throw;
                }

                DiagnosticUtility.TraceHandledException(ex, TraceEventType.Information);
            }
        }

        internal static ZeroMQRequestContext CreateContext(ZeroMQTransferTransportChannelListenerBase listener, ZeroMQRequestListenerContext listenerContext, EventTraceActivity eventTraceActivity)
        {
            return new ListenerZeroMQRequestContext(listener, listenerContext, eventTraceActivity);
        }

        class ReplyAsyncResult : AsyncResult
        {
            static AsyncCallback onSendCompleted;
            static Action<object, ZeroMQResponseMessage> onZeroMQTransferMessagingPipelineSend;

            bool closeOutputAfterReply;
            ZeroMQRequestContext context;
            Message message;
            Message responseMessage;
            TimeoutHelper timeoutHelper;

            public ReplyAsyncResult(ZeroMQRequestContext context, Message message, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.context = context;
                this.message = message;
                this.responseMessage = null;
                this.timeoutHelper = new TimeoutHelper(timeout);

                ThreadTrace.Trace("Begin sending http reply");

                this.responseMessage = this.message;

                if (this.SendResponse())
                {
                    base.Complete(true);
                }
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<ReplyAsyncResult>(result);
            }

            void OnSendResponseCompleted(IAsyncResult result)
            {
                try
                {
                    context.requestOutput.EndSend(result);
                    ThreadTrace.Trace("End sending http reply");

                    if (this.closeOutputAfterReply)
                    {
                        context.requestOutput.Close();
                    }
                }
                finally
                {
                    if (this.message != null &&
                        !object.ReferenceEquals(this.message, this.responseMessage))
                    {
                        this.responseMessage.Close();
                    }
                }
            }

            static void OnSendResponseCompletedCallback(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ReplyAsyncResult thisPtr = (ReplyAsyncResult)result.AsyncState;
                Exception completionException = null;

                try
                {
                    thisPtr.OnSendResponseCompleted(result);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }
                    completionException = e;
                }

                thisPtr.Complete(false, completionException);
            }

            static void OnZeroMQTransferMessagingPipelineSendCallback(object target, ZeroMQResponseMessage httpResponseMessage)
            {
                ReplyAsyncResult thisPtr = (ReplyAsyncResult)target;

                Exception pendingException = null;
                bool completed = false;
                try
                {
                    completed = thisPtr.SendResponse(httpResponseMessage);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    pendingException = e;
                    completed = true;
                }

                if (completed)
                {
                    thisPtr.Complete(false, pendingException);
                }
            }

            public bool SendResponse(ZeroMQResponseMessage responseMessage)
            {
                if (onSendCompleted == null)
                {
                    onSendCompleted = Fx.ThunkCallback(new AsyncCallback(OnSendResponseCompletedCallback));
                }

                bool success = false;

                try
                {
                    return this.SendResponseCore(responseMessage, out success);
                }
                finally
                {
                    if (!success && this.message != null &&
                        !object.ReferenceEquals(this.message, this.responseMessage))
                    {
                        this.responseMessage.Close();
                    }
                }
            }

            public bool SendResponse()
            {
                if (onSendCompleted == null)
                {
                    onSendCompleted = Fx.ThunkCallback(new AsyncCallback(OnSendResponseCompletedCallback));
                }

                bool success = false;

                try
                {
                    this.closeOutputAfterReply = context.PrepareReply(ref this.responseMessage);
                    if (onZeroMQTransferMessagingPipelineSend == null)
                    {
                        onZeroMQTransferMessagingPipelineSend = new Action<object, ZeroMQResponseMessage>(OnZeroMQTransferMessagingPipelineSendCallback);
                    }

                    if (context.requestPipeline.SendAsyncReply(this.responseMessage, onZeroMQTransferMessagingPipelineSend, this) == AsyncCompletionResult.Queued)
                    {
                        //// In Async send + HTTP pipeline path, we will send the response back after the result coming out from the pipeline.
                        //// So we don't need to call it here.
                        success = true;
                        return false;
                    }

                    ZeroMQResponseMessage responseMessage = new ZeroMQRequestMessage(this.context).PrepareResponse(this.responseMessage);
                    return this.SendResponseCore(responseMessage, out success);
                }
                finally
                {
                    if (!success && this.message != null &&
                        !object.ReferenceEquals(this.message, this.responseMessage))
                    {
                        this.responseMessage.Close();
                    }
                }
            }

            bool SendResponseCore(ZeroMQResponseMessage responseMessage, out bool success)
            {
                success = false;
                IAsyncResult result;
                if (responseMessage == null)
                {
                    result = context.requestOutput.BeginSend(this.timeoutHelper.RemainingTime(), onSendCompleted, this);
                }
                else
                {
                    result = context.requestOutput.BeginSend(responseMessage, this.timeoutHelper.RemainingTime(), onSendCompleted, this);
                }

                success = true;
                if (!result.CompletedSynchronously)
                {
                    return false;
                }

                this.OnSendResponseCompleted(result);
                return true;
            }
        }

        class ListenerZeroMQRequestContext : ZeroMQRequestContext//, HttpRequestMessageProperty.IHttpHeaderProvider
        {
            ZeroMQRequestListenerContext listenerContext;
            //byte[] webSocketInternalBuffer;

            public ListenerZeroMQRequestContext(
                ZeroMQTransferTransportChannelListenerBase listener,
                ZeroMQRequestListenerContext listenerContext,
                EventTraceActivity eventTraceActivity)
                : base(listener, null, eventTraceActivity)
            {
                this.listenerContext = listenerContext;
                this.RoutingKey = listenerContext.Request.RoutingKey;
            }

            protected override ZeroMQTransferMessagingInput GetZeroMQTransferMessagingInput()
            {
                return new ListenerContextZeroMQRequestInput(this);
            }

            protected override ZeroMQTransferMessagingOutput GetZeroMQTransferMessagingOutput(Message message)
            {
                // work around http.sys keep alive bug with chunked requests, see MB 49676, this is fixed in Vista
                //if (listenerContext.Request.ContentLength64 == -1 && !OSEnvironmentHelper.IsVistaOrGreater)
                //{
                //    listenerContext.Response.KeepAlive = false;
                //}
                //else
                //{
                listenerContext.Response.KeepAlive = listener.KeepAliveEnabled;
                //}
                //ICompressedMessageEncoder compressedMessageEncoder = listener.MessageEncoderFactory.Encoder as ICompressedMessageEncoder;
                //if (compressedMessageEncoder != null && compressedMessageEncoder.CompressionEnabled)
                //{
                //    string acceptEncoding = listenerContext.Request.Headers[HttpChannelUtilities.AcceptEncodingHeader];
                //    compressedMessageEncoder.AddCompressedMessageProperties(message, acceptEncoding);
                //}

                return ZeroMQTransferMessagingOutput.CreateZeroMQRequestOutput(listenerContext.Response, Listener, message, this.Method);
            }

            //protected override SecurityMessageProperty OnProcessAuthentication()
            //{
            //    return Listener.ProcessAuthentication(listenerContext);
            //}

            //protected override HttpStatusCode ValidateAuthentication()
            //{
            //    return Listener.ValidateAuthentication(listenerContext);
            //}

            protected override void OnAbort()
            {
                listenerContext.Response.Abort();

                // CSDMain 259910, we should remove this and call base.OnAbort() instead to improve maintainability
                this.Cleanup();
            }

            protected override void OnClose(TimeSpan timeout)
            {
                TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

                try
                {
                    base.OnClose(timeoutHelper.RemainingTime());
                }
                catch (Exception)
                {
                    throw;
                }

                try
                {
                    listenerContext.Close();
                }
                catch (ZeroMQListenerException listenerException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        ZeroMQChannelUtilities.CreateCommunicationException(listenerException));
                }
            }

            public override IDictionary<string, StringValues> Headers => this.listenerContext.Request.Headers;

            public override long? ContentLength => this.listenerContext.Request.ContentLength64;

            public override MediaContentType ContentType => this.listenerContext.Request.ContentType;

            public override ICollection<string> ContentEncoding => this.listenerContext.Request.ContentEncoding;

            public override ICollection<string> ContentLanguage => this.listenerContext.Request.ContentLanguage;

            //void HttpRequestMessageProperty.IHttpHeaderProvider.CopyHeaders(WebHeaderCollection headers)
            //{
            //    HttpListenerRequest listenerRequest = this.listenerContext.Request;
            //    headers.Add(listenerRequest.Headers);

            //    // MB 57988 - System.Net strips off user-agent from the headers collection
            //    if (listenerRequest.UserAgent != null && headers[HttpRequestHeader.UserAgent] == null)
            //    {
            //        headers.Add(HttpRequestHeader.UserAgent, listenerRequest.UserAgent);
            //    }
            //}

            class ListenerContextZeroMQRequestInput : ZeroMQTransferMessagingInput
            {
                ListenerZeroMQRequestContext listenerRequestContext;
                string cachedContentType; // accessing the header in System.Net involves a native transition
                byte[] preReadBuffer;

                public ListenerContextZeroMQRequestInput(ListenerZeroMQRequestContext listenerRequestContext)
                    : base(listenerRequestContext.Listener, true, listenerRequestContext.listener.IsChannelBindingSupportEnabled)
                {
                    this.listenerRequestContext = listenerRequestContext;
                    if (this.listenerRequestContext.listenerContext.Request.ContentLength64 == -1)
                    {
                        this.preReadBuffer = new byte[1];
                        if (this.listenerRequestContext.listenerContext.Request.InputStream.Read(preReadBuffer, 0, 1) == 0)
                        {
                            this.preReadBuffer = null;
                        }
                    }
                }

                public override long? ContentLength
                {
                    get
                    {
                        return this.listenerRequestContext.listenerContext.Request.ContentLength64;
                    }
                }

                protected override string ContentTypeCore
                {
                    get
                    {
                        if (this.cachedContentType == null)
                        {
                            this.cachedContentType = this.listenerRequestContext.listenerContext.Request.ContentType.ToMediaTypePlusParameters();
                        }

                        return this.cachedContentType;
                    }
                }

                protected override bool HasContent
                {
                    get { return (this.preReadBuffer != null || this.ContentLength > 0); }
                }

                protected override string Action
                {
                    get
                    {
                        return this.listenerRequestContext.listenerContext.Request.SoapAction;
                    }
                }

                //protected override ChannelBinding ChannelBinding
                //{
                //    get
                //    {
                //        return ChannelBindingUtility.GetToken(this.listenerHttpContext.listenerContext.Request.TransportContext);
                //    }
                //}

                protected override void AddProperties(Message message)
                {
                    message.Properties.Add(this.listenerRequestContext.GetType().FullName, this.listenerRequestContext);
                }

                public override void ConfigureRequestMessage(ZeroMQRequestMessage message)
                {
                    message.TrySetRequestContext(this.listenerRequestContext);
                }

                protected override Stream GetInputStream()
                {
                    if (this.preReadBuffer != null)
                    {
                        return new ListenerContextInputStream(listenerRequestContext, preReadBuffer);
                    }
                    else
                    {
                        return new ListenerContextInputStream(listenerRequestContext);
                    }
                }

                class ListenerContextInputStream : ZeroMQCloseOutputOnEofStream
                {
                    public ListenerContextInputStream(ListenerZeroMQRequestContext listenerHttpContext)
                        : base(listenerHttpContext.listenerContext.Request.InputStream)
                    {
                    }

                    public ListenerContextInputStream(ListenerZeroMQRequestContext listenerHttpContext, byte[] preReadBuffer)
                        : base(new PreReadStream(listenerHttpContext.listenerContext.Request.InputStream, preReadBuffer))
                    {
                    }

                    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
                    {
                        try
                        {
                            return base.BeginRead(buffer, offset, count, callback, state);
                        }
                        catch (ZeroMQListenerException listenerException)
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                ZeroMQChannelUtilities.CreateCommunicationException(listenerException));
                        }
                    }

                    public override int EndRead(IAsyncResult result)
                    {
                        try
                        {
                            return base.EndRead(result);
                        }
                        catch (ZeroMQListenerException listenerException)
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                ZeroMQChannelUtilities.CreateCommunicationException(listenerException));
                        }
                    }

                    public override int Read(byte[] buffer, int offset, int count)
                    {
                        try
                        {
                            return base.Read(buffer, offset, count);
                        }
                        catch (ZeroMQListenerException listenerException)
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                ZeroMQChannelUtilities.CreateCommunicationException(listenerException));
                        }
                    }

                    public override int ReadByte()
                    {
                        try
                        {
                            return base.ReadByte();
                        }
                        catch (ZeroMQListenerException listenerException)
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                ZeroMQChannelUtilities.CreateCommunicationException(listenerException));
                        }
                    }
                }
            }
        }
    }
}