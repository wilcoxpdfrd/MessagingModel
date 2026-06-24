using System;
using System.IO;
using System.Runtime;
using System.ServiceModel.Channels;

using AllVerge.Core.Resource;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.Core.Collections;
    using AllVerge.Core.ServiceModel.Channels;
    using AllVerge.Core.ServiceModel.Transfer;
    using AllVerge.Core.ServiceModel.Faults.Exceptions;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net;
    using System.Net.Mime;
    using System.Runtime.Diagnostics;
    using System.Security.Authentication.ExtendedProtection;
    using System.ServiceModel;
    using System.ServiceModel.Diagnostics;
    using System.ServiceModel.Diagnostics.Application;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using AllVerge.Core.Threading;

    internal enum RequestAbortReason
    {
        None,
        Aborted,
        TimedOut
    }

    internal abstract class ZeroMQTransferMessagingInput
    {
        const string multipartRelatedMediaType = "multipart/related";
        const string startInfoHeaderParam = "start-info";

        BufferManager bufferManager;
        MessageEncoder messageEncoder;
        private ITransferTransportFactorySettings settings;
        private bool enableChannelBinding;
        bool isRequest;
        Stream inputStream;
        bool streamed;
        bool errorGettingInputStream;

        protected ZeroMQTransferMessagingInput(ITransferTransportFactorySettings settings, bool isRequest, bool enableChannelBinding)
        {
            this.settings = settings;
            this.bufferManager = settings.BufferManager;
            this.messageEncoder = settings.MessageEncoderFactory.Encoder;
            //this.webException = null;
            this.isRequest = isRequest;
            this.inputStream = null;
            this.enableChannelBinding = enableChannelBinding;

            if (isRequest)
            {
                this.streamed = TransferModeHelper.IsRequestStreamed(settings.TransferMode);
            }
            else
            {
                this.streamed = TransferModeHelper.IsResponseStreamed(settings.TransferMode);
            }
        }

        // -1 if chunked
        public abstract long? ContentLength { get; }

        protected abstract string ContentTypeCore { get; }

        protected abstract bool HasContent { get; }

        protected abstract string Action { get; }

        protected virtual ChannelBinding ChannelBinding { get { return null; } }

        protected string ContentType
        {
            get
            {
                string contentType = ContentTypeCore;

                if (string.IsNullOrEmpty(contentType))
                {
                    return MediaTypeConstants.APPLICATION_OCTET_STREAM_MEDIA_TYPE;
                }

                return contentType;
            }
        }

        protected abstract Stream GetInputStream();

        public Stream GetInputStream(bool throwOnError)
        {
            if (inputStream == null && (throwOnError || !this.errorGettingInputStream))
            {
                try
                {
                    inputStream = GetInputStream();
                    this.errorGettingInputStream = false;
                }
                catch (Exception e)
                {
                    this.errorGettingInputStream = true;
                    if (throwOnError || Fx.IsFatal(e))
                    {
                        throw;
                    }

                    DiagnosticUtility.TraceHandledException(e, TraceEventType.Warning);
                }
            }

            return inputStream;
        }

        Message DecodeBufferedMessage(ArraySegment<byte> buffer, Stream inputStream)
        {
            try
            {
                // if we're chunked, make sure we've consumed the whole body
                if (ContentLength == -1 && buffer.Count == settings.MaxReceivedMessageSize)
                {
                    byte[] extraBuffer = new byte[1];
                    int extraReceived = inputStream.Read(extraBuffer, 0, 1);
                    if (extraReceived > 0)
                    {
                        ThrowMaxReceivedMessageSizeExceeded();
                    }
                }

                try
                {
                    return messageEncoder.ReadMessage(buffer, bufferManager, ContentType);
                }
                catch (XmlException xmlException)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new ProtocolException(SSR.MessageXmlProtocolError, xmlException));
                }
            }
            finally
            {
                inputStream.Close();
            }
        }

        Message ReadBufferedMessage(Stream inputStream)
        {
            ArraySegment<byte> messageBuffer = GetMessageBuffer();
            byte[] buffer = messageBuffer.Array;
            int offset = 0;
            int count = messageBuffer.Count;

            while (count > 0)
            {
                int bytesRead = inputStream.Read(buffer, offset, count);
                if (bytesRead == 0) // EOF 
                {
                    if (ContentLength != -1)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new ProtocolException(SSR.HttpContentLengthIncorrect));
                    }

                    break;
                }
                count -= bytesRead;
                offset += bytesRead;
            }

            return DecodeBufferedMessage(new ArraySegment<byte>(buffer, 0, offset), inputStream);
        }

        Message ReadChunkedBufferedMessage(Stream inputStream)
        {
            try
            {
                return messageEncoder.ReadMessage(inputStream, bufferManager, settings.MaxBufferSize, ContentType);
            }
            catch (XmlException xmlException)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new ProtocolException(SSR.MessageXmlProtocolError, xmlException));
            }
        }

        Message ReadStreamedMessage(Stream inputStream)
        {
            MaxMessageSizeStream maxMessageSizeStream = new MaxMessageSizeStream(inputStream, settings.MaxReceivedMessageSize);

            try
            {
                return messageEncoder.ReadMessage(maxMessageSizeStream, settings.MaxBufferSize, ContentType);
            }
            catch (XmlException xmlException)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new ProtocolException(SSR.MessageXmlProtocolError, xmlException));
            }
        }

        protected abstract void AddProperties(Message message);

        void ApplyChannelBinding(Message message)
        {
            if (this.enableChannelBinding)
            {
                ChannelBindingUtility.TryAddToMessage(this.ChannelBinding, message, true);
            }
        }

        // makes sure that appropriate message headers are included in the received Message
        Exception ProcessMessageAddressing(Message message)
        {
            Exception result = null;

            AddProperties(message);

            // check if user is receiving WS-1 messages
            if (message.Version.Addressing == AddressingVersion.None)
            {
                bool actionAbsent = false;
                try
                {
                    actionAbsent = (message.Headers.Action == null);
                }
                catch (XmlException e)
                {
                    DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
                }
                catch (CommunicationException e)
                {
                    DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
                }

                if (!actionAbsent)
                {
                    result = new ProtocolException(SSR.Format(SSR.HttpAddressingNoneHeaderOnWire,
                        XD.AddressingDictionary.Action.Value));
                }

                bool toAbsent = false;
                try
                {
                    toAbsent = (message.Headers.To == null);
                }
                catch (XmlException e)
                {
                    DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
                }
                catch (CommunicationException e)
                {
                    DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
                }

                if (!toAbsent)
                {
                    result = new ProtocolException(SSR.Format(SSR.HttpAddressingNoneHeaderOnWire,
                        XD.AddressingDictionary.To.Value));
                }
                message.Headers.To = message.Properties.Via;
            }

            if (isRequest)
            {
                string action = null;

                if (message.Version.Envelope == EnvelopeVersion.Soap11)
                {
                    action = Action;
                }
                else if (message.Version.Envelope == EnvelopeVersion.Soap12 && !String.IsNullOrEmpty(ContentType))
                {
                    ContentType parsedContentType = new MediaContentType(ContentType);

                    if (parsedContentType.MediaType == multipartRelatedMediaType && parsedContentType.Parameters.ContainsKey(startInfoHeaderParam))
                    {
                        // fix to grab action from start-info as stated in RFC2387
                        action = new MediaContentType(parsedContentType.Parameters[startInfoHeaderParam]).Parameters["action"];
                    }
                    if (action == null)
                    {
                        // only if we can't find an action inside start-info
                        action = parsedContentType.Parameters["action"];
                    }
                }

                if (action != null)
                {
                    action = UrlUtility.UrlDecode(action, Encoding.UTF8);

                    if (action.Length >= 2 && action[0] == '"' && action[action.Length - 1] == '"')
                    {
                        action = action.Substring(1, action.Length - 2);
                    }

                    if (message.Version.Addressing == AddressingVersion.None)
                    {
                        message.Headers.Action = action;
                    }

                    try
                    {

                        if (action.Length > 0 && string.Compare(message.Headers.Action, action, StringComparison.Ordinal) != 0)
                        {
                            result = new ActionMismatchAddressingException(SSR.Format(SSR.HttpSoapActionMismatchFault,
                                message.Headers.Action, action), message.Headers.Action, action);
                        }

                    }
                    catch (XmlException e)
                    {
                        DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
                    }
                    catch (CommunicationException e)
                    {
                        DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
                    }
                }
            }

            ApplyChannelBinding(message);

            if (DiagnosticUtility.ShouldUseActivity)
            {
                TraceUtility.TransferFromTransport(message);
            }
            if (DiagnosticUtility.ShouldTraceInformation)
            {
                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.MessageReceived, SSR.TraceCodeMessageReceived,
                    MessageTransmitTraceRecord.CreateReceiveTraceRecord(message), this, null, message);
            }

            // MessageLogger doesn't log AddressingVersion.None in the encoder since we want to make sure we log 
            // as much of the message as possible. Here we log after stamping the addressing information
            if (MessageLogger.LoggingEnabled && message.Version.Addressing == AddressingVersion.None)
            {
                MessageLogger.LogMessage(ref message, MessageLoggingSource.TransportReceive | MessageLoggingSource.LastChance);
            }

            return result;
        }

        void ValidateContentType()
        {
            if (!HasContent)
                return;

            if (string.IsNullOrEmpty(ContentType))
            {
                if (MessageLogger.ShouldLogMalformed)
                {
                    // We pass in throwOnError = false below so that the exception which is eventually thrown is the ProtocolException below, with Http status code 415 "UnsupportedMediaType"
                    Stream stream = this.GetInputStream(false);
                    if (stream != null)
                    {
                        MessageLogger.LogMessage(stream, MessageLoggingSource.Malformed);
                    }
                }
                throw FaultCodes.ClientErrorSenderCode.UnsupportedMediaType.CreateFaultException(this.GetType());
            }
            if (!messageEncoder.IsContentTypeSupported(ContentType))
            {
                if (MessageLogger.ShouldLogMalformed)
                {
                    // We pass in throwOnError = false below so that the exception which is eventually thrown is the ProtocolException below, with Http status code 415 "UnsupportedMediaType"
                    Stream stream = this.GetInputStream(false);
                    if (stream != null)
                    {
                        MessageLogger.LogMessage(stream, MessageLoggingSource.Malformed);
                    }
                }
                string statusDescription = string.Format(CultureInfo.InvariantCulture, HttpChannelUtilities.StatusDescriptionStrings.HttpContentTypeMismatch, ContentType, messageEncoder.ContentType);
                throw new FaultException(statusDescription, FaultCodes.ClientErrorSenderCode.UnsupportedMediaType.WrapFaultCode());
            }
        }

        public IAsyncResult BeginParseIncomingMessage(AsyncCallback callback, object state)
        {
            return this.BeginParseIncomingMessage(null, callback, state);
        }

        public IAsyncResult BeginParseIncomingMessage(ZeroMQRequestMessage requestMessage, AsyncCallback callback, object state)
        {
            bool throwing = true;
            try
            {
                IAsyncResult result = new ParseMessageAsyncResult(requestMessage, this, callback, state);
                throwing = false;
                return result;
            }
            finally
            {
                if (throwing)
                {
                    Close();
                }
            }
        }

        public Message EndParseIncomingMessage(IAsyncResult result, out Exception requestException)
        {
            bool throwing = true;
            try
            {
                Message message = ParseMessageAsyncResult.End(result, out requestException);
                throwing = false;
                return message;
            }
            finally
            {
                if (throwing)
                {
                    Close();
                }
            }
        }

        public Message ParseIncomingMessage(out Exception requestException)
        {
            return this.ParseIncomingMessage(null, out requestException);
        }

        public Message ParseIncomingMessage(ZeroMQRequestMessage requestMessage, out Exception messageAddressingException)
        {
            Message message = null;
            messageAddressingException = null;
            bool throwing = true;
            try
            {
                ValidateContentType();

                ServiceModelActivity activity = null;
                if (DiagnosticUtility.ShouldUseActivity &&
                    ((ServiceModelActivity.Current == null) ||
                     (ServiceModelActivity.Current.ActivityType != ActivityType.ProcessAction)))
                {
                    activity = ServiceModelActivity.CreateBoundedActivity(true);
                }
                using (activity)
                {
                    if (DiagnosticUtility.ShouldUseActivity && activity != null)
                    {
                        // Only update the Start identifier if the activity is not null.
                        ServiceModelActivity.Start(activity, SSR.Format(SSR.ActivityProcessingMessage, TraceUtility.RetrieveMessageNumber()), ActivityType.ProcessMessage);
                    }

                    if (!this.HasContent)
                    {
                        if (this.messageEncoder.MessageVersion == MessageVersion.None)
                        {
                            message = new NullMessage();
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        Stream stream = this.GetInputStream(true);
                        if (streamed)
                        {
                            message = ReadStreamedMessage(stream);
                        }
                        else if (this.ContentLength == -1)
                        {
                            message = ReadChunkedBufferedMessage(stream);
                        }
                        else
                        {
                            if (requestMessage == null)
                            {
                                message = ReadBufferedMessage(stream);
                            }
                            else
                            {
                                message = ReadBufferedMessage(requestMessage);
                            }
                        }
                    }

                    messageAddressingException = ProcessMessageAddressing(message);

                    throwing = false;
                    return message;
                }
            }
            finally
            {
                if (throwing)
                {
                    Close();
                }
            }
        }

        Message ReadBufferedMessage(ZeroMQRequestMessage requestMessage)
        {
            Fx.Assert(requestMessage != null, $"{nameof(requestMessage)} cannot be null.");

            Message message;
            using (MessageContent currentContent = requestMessage.Content)
            {
                int length = (int)this.ContentLength;
                byte[] buffer = this.bufferManager.TakeBuffer(length);
                bool success = false;
                try
                {
                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        currentContent.CopyToAsync(ms).Wait<CommunicationException>();
                        requestMessage.Content = new ByteArrayMessageContent(requestMessage.RequestContext, buffer, 0, length);
                        message = this.messageEncoder.ReadMessage(new ArraySegment<byte>(buffer, 0, length), this.bufferManager, this.ContentType);
                        success = true;
                    }
                }
                finally
                {
                    if (!success)
                    {
                        // We don't have to return it in success case since the buffer will be returned to bufferManager when the message is disposed.
                        this.bufferManager.ReturnBuffer(buffer);
                    }
                }
            }
            return message;
        }

        public ZeroMQRequestMessageZeroMQTransferMessagingInput CreateRequestMessageInput()
        {
            ZeroMQRequestMessage requestMessage = new ZeroMQRequestMessage();

            ZeroMQChannelUtilities.EnsureRequestMessageContentNotNull(requestMessage);

            this.ConfigureRequestMessage(requestMessage);

            ChannelBinding channelBinding = this.enableChannelBinding ? this.ChannelBinding : null;
            
            return new ZeroMQRequestMessageZeroMQTransferMessagingInput(requestMessage, this.settings, this.enableChannelBinding, channelBinding);
        }

        public abstract void ConfigureRequestMessage(ZeroMQRequestMessage message);

        protected virtual void Close()
        {
        }

        void ThrowMaxReceivedMessageSizeExceeded()
        {
            if (TD.MaxReceivedMessageSizeExceededIsEnabled())
            {
                TD.MaxReceivedMessageSizeExceeded(SSR.Format(SSR.MaxReceivedMessageSizeExceeded, settings.MaxReceivedMessageSize));
            }

            if (isRequest)
            {
                throw FaultCodes.ClientErrorSenderCode.RequestEntityTooLarge.CreateFaultException(this.GetType());
            }
            else
            {
                string message = SSR.Format(SSR.MaxReceivedMessageSizeExceeded, settings.MaxReceivedMessageSize);
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new CommunicationException(message, new QuotaExceededException(message)));
            }
        }

        ArraySegment<byte> GetMessageBuffer()
        {
            if (ContentLength.HasValue)
            {
                long count = ContentLength.Value;
                int bufferSize;

                if (count > settings.MaxReceivedMessageSize)
                {
                    ThrowMaxReceivedMessageSizeExceeded();
                }

                bufferSize = (int)count;

                return new ArraySegment<byte>(bufferManager.TakeBuffer(bufferSize), 0, bufferSize);
            }

            return new ArraySegment<byte>(Array.Empty<byte>());
        }

        class ParseMessageAsyncResult : TraceAsyncResult
        {
            ArraySegment<byte> buffer;
            int count;
            int offset;
            ZeroMQTransferMessagingInput requestInput;
            Stream inputStream;
            Message message;
            Exception requestException = null;
            ZeroMQRequestMessage requestMessage;
            static AsyncCallback onRead = Fx.ThunkCallback(new AsyncCallback(OnRead));

            public ParseMessageAsyncResult(
                ZeroMQRequestMessage requestMessage,
                ZeroMQTransferMessagingInput requestInput,
                AsyncCallback callback,
                object state)
                : base(callback, state)
            {
                this.requestInput = requestInput;
                this.requestMessage = requestMessage;
                this.BeginParse();
            }

            void BeginParse()
            {
                requestInput.ValidateContentType();
                this.inputStream = requestInput.GetInputStream(true);

                if (!requestInput.HasContent)
                {
                    if (requestInput.messageEncoder.MessageVersion == MessageVersion.None)
                    {
                        this.message = new NullMessage();
                    }
                    else
                    {
                        base.Complete(true);
                        return;
                    }
                }
                else if (requestInput.streamed || requestInput.ContentLength == -1)
                {
                    if (requestInput.streamed)
                    {
                        this.message = requestInput.ReadStreamedMessage(inputStream);
                    }
                    else
                    {
                        this.message = requestInput.ReadChunkedBufferedMessage(inputStream);
                    }
                }

                if (this.message != null)
                {
                    this.requestException = requestInput.ProcessMessageAddressing(this.message);
                    base.Complete(true);
                    return;
                }

                AsyncCompletionResult result;
                if (requestMessage == null)
                {
                    result = this.DecodeBufferedMessageAsync();
                }
                else
                {
                    result = this.DecodeBufferedHttpRequestMessageAsync();
                }

                if (result == AsyncCompletionResult.Completed)
                {
                    base.Complete(true);
                }
            }

            AsyncCompletionResult DecodeBufferedMessageAsync()
            {
                this.buffer = this.requestInput.GetMessageBuffer();
                this.count = this.buffer.Count;
                this.offset = 0;

                IAsyncResult result = inputStream.BeginRead(buffer.Array, offset, count, onRead, this);
                if (result.CompletedSynchronously)
                {
                    if (ContinueReading(inputStream.EndRead(result)))
                    {
                        return AsyncCompletionResult.Completed;
                    }
                }

                return AsyncCompletionResult.Queued;
            }

            bool ContinueReading(int bytesRead)
            {
                while (true)
                {
                    if (bytesRead == 0) // EOF
                    {
                        break;
                    }
                    else
                    {
                        offset += bytesRead;
                        count -= bytesRead;
                        if (count <= 0)
                        {
                            break;
                        }
                        else
                        {
                            IAsyncResult result = inputStream.BeginRead(buffer.Array, offset, count, onRead, this);
                            if (!result.CompletedSynchronously)
                            {
                                return false;
                            }

                            bytesRead = inputStream.EndRead(result);
                        }
                    }
                }

                using (DiagnosticUtility.ShouldUseActivity ? ServiceModelActivity.BoundOperation(this.CallbackActivity) : null)
                {
                    using (ServiceModelActivity activity = DiagnosticUtility.ShouldUseActivity ? ServiceModelActivity.CreateBoundedActivity(true) : null)
                    {
                        if (DiagnosticUtility.ShouldUseActivity)
                        {
                            ServiceModelActivity.Start(activity, SSR.Format(SSR.ActivityProcessingMessage, TraceUtility.RetrieveMessageNumber()), ActivityType.ProcessMessage);
                        }

                        this.message = this.requestInput.DecodeBufferedMessage(new ArraySegment<byte>(buffer.Array, 0, offset), inputStream);
                        this.requestException = this.requestInput.ProcessMessageAddressing(this.message);
                    }
                    return true;
                }
            }

            static void OnRead(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                    return;

                ParseMessageAsyncResult thisPtr = (ParseMessageAsyncResult)result.AsyncState;

                Exception completionException = null;
                bool completeSelf;
                try
                {
                    completeSelf = thisPtr.ContinueReading(thisPtr.inputStream.EndRead(result));
                }
#pragma warning suppress 56500 // [....], transferring exception to another thread
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    completeSelf = true;
                    completionException = e;
                }

                if (completeSelf)
                {
                    thisPtr.Complete(false, completionException);
                }
            }

            public static Message End(IAsyncResult result, out Exception requestException)
            {
                ParseMessageAsyncResult thisPtr = AsyncResult.End<ParseMessageAsyncResult>(result);
                requestException = thisPtr.requestException;
                return thisPtr.message;
            }

            AsyncCompletionResult DecodeBufferedHttpRequestMessageAsync()
            {
                // Need to consider moving this to async implemenation for HttpContent reading.(CSDMAIN: 229108)
                this.message = this.requestInput.ReadBufferedMessage(this.requestMessage);
                this.requestException = this.requestInput.ProcessMessageAddressing(this.message);
                return AsyncCompletionResult.Completed;
            }
        }
    }

    class ZeroMQRequestMessageZeroMQTransferMessagingInput : ZeroMQTransferMessagingInput //, HttpRequestMessageProperty.IHttpHeaderProvider
    {
        const string action = "action";
        ZeroMQRequestMessage requestMessage;
        ChannelBinding channelBinding;

        public ZeroMQRequestMessageZeroMQTransferMessagingInput(ZeroMQRequestMessage requestMessage, ITransferTransportFactorySettings settings, bool enableChannelBinding, ChannelBinding channelBinding)
            : base(settings, true, enableChannelBinding)
        {
            this.requestMessage = requestMessage;
            this.channelBinding = channelBinding;
        }

        public override long? ContentLength
        {
            get
            {
                if (this.requestMessage.Content.ContentLength == null)
                {
                    // Chunked transfer mode
                    return -1;
                }

                return this.requestMessage.Content.ContentLength.Value;
            }
        }

        protected override ChannelBinding ChannelBinding
        {
            get
            {
                return this.channelBinding;
            }
        }

        public ZeroMQRequestMessage RequestMessage
        {
            get { return this.requestMessage; }
        }

        protected override bool HasContent
        {
            get
            {
                // In Chunked transfer mode, the ContentLength header is null
                // Otherwise we just rely on the ContentLength header
                return this.requestMessage.Content.ContentLength == null || this.requestMessage.Content.ContentLength.Value > 0;
            }
        }

        protected override string ContentTypeCore
        {
            get
            {
                if (!this.HasContent)
                {
                    return null;
                }

                return this.requestMessage.Content.ContentType == null ? null : this.requestMessage.Content.ContentType.MediaType;
            }
        }

        public override void ConfigureRequestMessage(ZeroMQRequestMessage message)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException());
        }

        protected override Stream GetInputStream()
        {
            if (this.requestMessage.Content == null)
            {
                return Stream.Null;
            }

            return this.requestMessage.Content.ReadAsStreamAsync().Result;
        }

        protected override void AddProperties(Message message)
        {
            message.Properties.Add(this.requestMessage.GetType().FullName, this.requestMessage);
            message.Properties.Via = this.requestMessage.Content.RequestUri;

            foreach (KeyValuePair<string, object> property in this.requestMessage.Properties)
            {
                message.Properties.Add(property.Key, property.Value);
            }

            this.requestMessage.Properties.Clear();
        }

        protected override string Action
        {
            get
            {
                if (this.requestMessage.Content.ContentType.Parameters.TryGetValue(action, out string actionValue))
                {
                    return actionValue;
                }

                return null;
            }
        }

        //public void CopyHeaders(WebHeaderCollection headers)
        //{
        //    // No special-casing for the "WWW-Authenticate" header required here,
        //    // because this method is only called for the incoming request
        //    // and the WWW-Authenticate header is a header only applied to responses.
        //    HttpChannelUtilities.CopyHeaders(this.requestMessage, headers.Add);
        //}

        internal void SetHttpRequestMessage(ZeroMQRequestMessage requestMessage)
        {
            Fx.Assert(requestMessage != null, $"{nameof(requestMessage)} should not be null.");
            
            this.requestMessage = requestMessage;
        }
    }

    internal abstract class ZeroMQTransferMessagingOutput
    {
        ITransferTransportFactorySettings settings;
        RequestAbortReason abortReason;
        bool isClosed;
        bool isRequest;
        Message message;
        byte[] bufferToRecycle;
        BufferManager bufferManager;
        MessageEncoder messageEncoder;
        string mtomBoundary;
        Stream outputStream;
        bool supportsConcurrentIO;
        bool canSendCompressedResponses;
        bool streamed;
        static Action<object> onStreamSendTimeout;
        EventTraceActivity eventTraceActivity;

        protected ZeroMQTransferMessagingOutput(ITransferTransportFactorySettings settings, Message message, bool isRequest, bool supportsConcurrentIO)
        {
            this.settings = settings;
            this.message = message;
            this.isRequest = isRequest;
            this.bufferManager = settings.BufferManager;
            this.messageEncoder = settings.MessageEncoderFactory.Encoder;
            ICompressedMessageEncoder compressedMessageEncoder = this.messageEncoder as ICompressedMessageEncoder;
            this.canSendCompressedResponses = compressedMessageEncoder != null && compressedMessageEncoder.CompressionEnabled;
            if (isRequest)
            {
                this.streamed = TransferModeHelper.IsRequestStreamed(settings.TransferMode);
            }
            else
            {
                this.streamed = TransferModeHelper.IsResponseStreamed(settings.TransferMode);
            }
            this.supportsConcurrentIO = supportsConcurrentIO;
            if (FxTrace.Trace.IsEnd2EndActivityTracingEnabled)
            {
                this.eventTraceActivity = EventTraceActivityHelper.TryExtractActivity(message);
            }
        }

        protected abstract void AddMimeVersion(string version);
        protected abstract void SetIsFault(bool isFault);
        protected abstract void SetContentLength(int contentLength);
        protected abstract void SetContentType(string contentType);
        protected abstract void SetContentTypeParameter(string parameter, string contentType);
        protected abstract void SetContentEncoding(IEnumerable<string> contentEncoding);
        protected abstract void SetResponseAction(string action);
        protected abstract void SetResponseTo(Uri to);
        protected abstract void SetResponseRelatesTo(UniqueId relatesTo);
        protected virtual bool IsChannelBindingSupportEnabled { get { return false; } }
        protected virtual ChannelBinding ChannelBinding { get { return null; } }
        protected virtual bool CleanupChannelBinding { get { return true; } }
        protected virtual string Method { get { return null; } }

        protected void Abort()
        {
            Abort(RequestAbortReason.Aborted);
        }

        public virtual void Abort(RequestAbortReason reason)
        {
            if (isClosed)
            {
                return;
            }

            this.abortReason = reason;

            TraceRequestResponseAborted(reason);

            CleanupBuffer();
        }

        private void TraceRequestResponseAborted(RequestAbortReason reason)
        {
            if (isRequest)
            {
                if (TD.HttpChannelRequestAbortedIsEnabled())
                {
                    TD.HttpChannelRequestAborted(this.eventTraceActivity);
                }
            }
            else if (TD.HttpChannelResponseAbortedIsEnabled())
            {
                TD.HttpChannelResponseAborted(this.eventTraceActivity);
            }

            if (DiagnosticUtility.ShouldTraceWarning)
            {
                TraceUtility.TraceEvent(
                    TraceEventType.Warning,
                    isRequest ? TraceCode.HttpChannelRequestAborted : TraceCode.HttpChannelResponseAborted,
                    isRequest ? SSR.TraceCodeHttpChannelRequestAborted : SSR.TraceCodeHttpChannelResponseAborted,
                    this.message);
            }
        }

        public void Close()
        {
            if (isClosed)
            {
                return;
            }

            try
            {
                if (this.outputStream != null)
                {
                    outputStream.Close();
                }
            }
            finally
            {
                CleanupBuffer();
            }
        }

        void CleanupBuffer()
        {
            byte[] bufferToRecycleSnapshot = Interlocked.Exchange<byte[]>(ref this.bufferToRecycle, null);
            
            if (bufferToRecycleSnapshot != null)
            {
                bufferManager.ReturnBuffer(bufferToRecycleSnapshot);
            }

            isClosed = true;
        }


        private void ApplyChannelBinding()
        {
            if (this.IsChannelBindingSupportEnabled)
            {
                ChannelBindingUtility.TryAddToMessage(this.ChannelBinding, this.message, this.CleanupChannelBinding);
            }
        }

        protected abstract Stream GetOutputStream();

        protected virtual bool WillGetOutputStreamCompleteSynchronously
        {
            get { return true; }
        }

        protected bool CanSendCompressedResponses
        {
            get { return this.canSendCompressedResponses; }
        }

        protected virtual IAsyncResult BeginGetOutputStream(AsyncCallback callback, object state)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }

        protected virtual Stream EndGetOutputStream(IAsyncResult result)
        {
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }

        class ByteArrayOutputMessageContent : ByteArrayMessageContent
        {
            BufferManager bufferManager;
            volatile bool cleaned = false;
            ArraySegment<byte> content;

            public ByteArrayOutputMessageContent(byte[] content, int offset, int count, BufferManager bufferManager)
                : base(content, offset, count)
            {
                Fx.Assert(bufferManager != null, "bufferManager should not be null");
                Fx.Assert(content != null, "content should not be null");
                this.content = new ArraySegment<byte>(content, offset, count);
                this.bufferManager = bufferManager;
            }

            public ArraySegment<byte> Content
            {
                get
                {
                    return this.content;
                }
            }

            protected override Task<Stream> CreateContentReadStreamAsync()
            {
                return base.CreateContentReadStreamAsync().ContinueWith<Stream>(t =>
                    new ByteArrayOutputMessageContentStream(t.Result, this));
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                return base.SerializeToStreamAsync(stream, context).ContinueWith(t =>
                {
                    this.Cleanup();
                    HttpChannelUtilities.HandleContinueWithTask(t);
                });
            }

            void Cleanup()
            {
                if (!cleaned)
                {
                    lock (this)
                    {
                        if (!cleaned)
                        {
                            cleaned = true;
                            this.bufferManager.ReturnBuffer(this.content.Array);
                        }
                    }
                }
            }

            class ByteArrayOutputMessageContentStream : DelegatingStream
            {
                ByteArrayOutputMessageContent content;

                public ByteArrayOutputMessageContentStream(Stream innerStream, ByteArrayOutputMessageContent content)
                    : base(innerStream)
                {
                    this.content = content;
                }

                public override void Close()
                {
                    base.Close();
                    this.content.Cleanup();
                }
            }
        }

        class WriteStreamedMessageAsyncResult : AsyncResult
        {
            ZeroMQTransferMessagingOutput requestOutput;
            IOThreadTimer sendTimer;
            static AsyncCallback onWriteStreamedMessage = Fx.ThunkCallback(OnWriteStreamedMessage);
            ZeroMQResponseMessage httpResponseMessage;

            public WriteStreamedMessageAsyncResult(TimeSpan timeout, ZeroMQTransferMessagingOutput requestOutput, ZeroMQResponseMessage httpResponseMessage, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.httpResponseMessage = httpResponseMessage;
                this.requestOutput = requestOutput;
                requestOutput.outputStream = requestOutput.GetWrappedOutputStream();

                // Since HTTP streams don't support timeouts, we can't just use TimeoutStream here. 
                // Rather, we need to run a timer to bound the overall operation
                if (onStreamSendTimeout == null)
                {
                    onStreamSendTimeout = new Action<object>(OnStreamSendTimeout);
                }
                this.SetTimer(timeout);

                bool completeSelf = false;
                bool throwing = true;

                try
                {
                    completeSelf = HandleWriteStreamedMessage(null);
                    throwing = false;
                }
                finally
                {
                    if (completeSelf || throwing)
                    {
                        this.sendTimer.Cancel();
                    }
                }

                if (completeSelf)
                {
                    this.Complete(true);
                }
            }

            bool HandleWriteStreamedMessage(IAsyncResult result)
            {
                if (this.httpResponseMessage == null)
                {
                    if (result == null)
                    {
                        MtomMessageEncoder mtomMessageEncoder = requestOutput.messageEncoder as MtomMessageEncoder;
                        if (mtomMessageEncoder == null)
                        {
                            result = requestOutput.messageEncoder.BeginWriteMessage(requestOutput.message, requestOutput.outputStream, onWriteStreamedMessage, this);
                        }
                        else
                        {
                            result = mtomMessageEncoder.BeginWriteMessage(requestOutput.message, requestOutput.outputStream, requestOutput.mtomBoundary, onWriteStreamedMessage, this);
                        }

                        if (!result.CompletedSynchronously)
                        {
                            return false;
                        }
                    }

                    requestOutput.messageEncoder.EndWriteMessage(result);
                    return true;
                }
                else
                {
                    OpaqueMessageContent content = this.httpResponseMessage.Content as OpaqueMessageContent;
                    if (result == null)
                    {
                        Fx.Assert(this.httpResponseMessage.Content != null, "httpOutput.httpResponseMessage.Content should not be null.");

                        if (content != null)
                        {
                            result = content.BeginWriteToStream(requestOutput.outputStream, onWriteStreamedMessage, this);
                        }
                        else
                        {
                            result = this.httpResponseMessage.Content.CopyToAsync(requestOutput.outputStream).ToApm(onWriteStreamedMessage, this);
                        }

                        if (!result.CompletedSynchronously)
                        {
                            return false;
                        }
                    }

                    if (content != null)
                    {
                        content.EndWriteToStream(result);
                    }

                    return true;
                }
            }

            static void OnWriteStreamedMessage(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                WriteStreamedMessageAsyncResult thisPtr = (WriteStreamedMessageAsyncResult)result.AsyncState;
                Exception completionException = null;
                bool completeSelf = false;

                try
                {
                    completeSelf = thisPtr.HandleWriteStreamedMessage(result);
                }
                catch (Exception ex)
                {
                    if (Fx.IsFatal(ex))
                    {
                        throw;
                    }
                    completeSelf = true;
                    completionException = ex;
                }

                if (completeSelf)
                {
                    thisPtr.sendTimer.Cancel();
                    thisPtr.Complete(false, completionException);
                }
            }

            void SetTimer(TimeSpan timeout)
            {
                Fx.Assert(this.sendTimer == null, "SetTimer should only be called once");

                this.sendTimer = new IOThreadTimer(onStreamSendTimeout, this.requestOutput, true);
                this.sendTimer.Set(timeout);
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<WriteStreamedMessageAsyncResult>(result);
            }
        }

        public IAsyncResult BeginSend(ZeroMQResponseMessage responseMessage, TimeSpan timeout, AsyncCallback callback, object state)
        {
            Fx.Assert(responseMessage != null, "httpResponseMessage should not be null.");
            return this.BeginSendCore(responseMessage, timeout, callback, state);
        }

        public IAsyncResult BeginSend(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return this.BeginSendCore(null, timeout, callback, state);
        }

        IAsyncResult BeginSendCore(ZeroMQResponseMessage responseMessage, TimeSpan timeout, AsyncCallback callback, object state)
        {
            bool throwing = true;
            try
            {
                bool suppressEntityBody;
                if (responseMessage != null)
                {
                    suppressEntityBody = this.PrepareZeroMQRequestSend(responseMessage);
                }
                else
                {
                    suppressEntityBody = PrepareZeroMQRequestSend(message);
                }

                this.TraceHttpSendStart();
                IAsyncResult result = new SendAsyncResult(this, responseMessage, suppressEntityBody, timeout, callback, state);
                throwing = false;
                return result;
            }
            finally
            {
                if (throwing)
                {
                    Abort();
                }
            }
        }

        public virtual void EndSend(IAsyncResult result)
        {
            bool throwing = true;
            try
            {
                SendAsyncResult.End(result);
                throwing = false;
            }
            finally
            {
                if (throwing)
                {
                    Abort();
                }
            }
        }

        protected bool PrepareZeroMQRequestSend(ZeroMQResponseMessage responseMessage)
        {
            this.PrepareZeroMQRequestSendCore(responseMessage);

            return ZeroMQChannelUtilities.IsEmpty(responseMessage);
        }

        protected virtual bool PrepareZeroMQRequestSend(Message message)
        {
            String action = message.Headers.Action;
            Uri to = message.Headers.To;
            UniqueId relatesTo = message.Headers.RelatesTo;

            if (message.Version.Addressing == AddressingVersion.None)
            {
                if (MessageLogger.LogMessagesAtTransportLevel)
                {
                    message.Properties.Add(AddressingProperty.Name, new AddressingProperty(message.Headers));
                }

                message.Headers.Action = null;
                message.Headers.To = null;
                message.Headers.RelatesTo = null;
            }

            string contentType = null;

            if (message.Version == MessageVersion.None)
            {
                if (message.Properties.TryGetProperty(typeof(ZeroMQResponseMessage).FullName, out ZeroMQResponseMessage responseProperty))
                {
                    if (!string.IsNullOrEmpty(responseProperty.Content.ContentType?.MediaType))
                    {
                        contentType = responseProperty.Content.ContentType?.MediaType;
                        if (!messageEncoder.IsContentTypeSupported(contentType))
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                new ProtocolException(SSR.Format(SSR.ResponseContentTypeNotSupported,
                                contentType)));
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(contentType))
            {
                MtomMessageEncoder mtomMessageEncoder = messageEncoder as MtomMessageEncoder;
                if (mtomMessageEncoder == null)
                {
                    contentType = messageEncoder.ContentType;
                }
                else
                {
                    contentType = mtomMessageEncoder.GetContentType(out this.mtomBoundary);
                    // For MTOM messages, add a MIME version header
                    this.SetContentTypeParameter("MIME-Version", "1.0");
                }
            }

            if (isRequest && FxTrace.Trace.IsEnd2EndActivityTracingEnabled)
            {
                EnsureEventTraceActivity(message);
            }

            SetResponseAction(action);
            SetResponseTo(to);
            SetResponseRelatesTo(relatesTo);
            SetContentType(contentType);

            return message is NullMessage;
        }

        protected abstract void PrepareZeroMQRequestSendCore(ZeroMQResponseMessage message);

        private static void EnsureEventTraceActivity(Message message)
        {
            //We need to send this only if there is no message id. 
            if (message.Headers.MessageId == null)
            {
                EventTraceActivity eventTraceActivity = EventTraceActivityHelper.TryExtractActivity(message);
                if (eventTraceActivity == null)
                {
                    //Whoops no activity on the message yet.                         
                    eventTraceActivity = new EventTraceActivity();
                    EventTraceActivityHelper.TryAttachActivity(message, eventTraceActivity);
                }

                string requestMessagePropertyKey = typeof(ZeroMQRequestMessage).FullName;
                ZeroMQRequestMessage requestMessage;
                if (!message.Properties.TryGetValue<ZeroMQRequestMessage>(requestMessagePropertyKey, out requestMessage))
                {
                    requestMessage = new ZeroMQRequestMessage();
                    message.Properties.Add(requestMessagePropertyKey, requestMessage);
                }
                requestMessage.Content.AddHeader(EventTraceActivity.Name, Convert.ToBase64String(eventTraceActivity.ActivityId.ToByteArray()));
            }
        }

        ArraySegment<byte> SerializeBufferedMessage(Message message)
        {
            // by default, the HttpOutput should own the buffer and clean it up
            return SerializeBufferedMessage(message, true);
        }

        ArraySegment<byte> SerializeBufferedMessage(Message message, bool shouldRecycleBuffer)
        {
            ArraySegment<byte> result;

            MtomMessageEncoder mtomMessageEncoder = messageEncoder as MtomMessageEncoder;
            if (mtomMessageEncoder == null)
            {
                result = messageEncoder.WriteMessage(message, int.MaxValue, bufferManager);
            }
            else
            {
                result = mtomMessageEncoder.WriteMessage(message, int.MaxValue, bufferManager, 0, this.mtomBoundary);
            }

            if (shouldRecycleBuffer)
            {
                // Only set this.bufferToRecycle if the HttpOutput owns the buffer, we will clean it up upon httpOutput.Close()
                // Otherwise, caller of SerializeBufferedMessage assumes responsiblity for returning the buffer to the buffer pool
                this.bufferToRecycle = result.Array;
            }
            return result;
        }

        ArraySegment<byte> SerializeBufferedMessage(ZeroMQResponseMessage responseMessage)
        {
            ByteArrayOutputMessageContent content = responseMessage.Content as ByteArrayOutputMessageContent;
            if (content == null)
            {
                byte[] byteArray = responseMessage.Content.ReadAsByteArrayAsync().Result;
                return new ArraySegment<byte>(byteArray, 0, byteArray.Length);
            }
            else
            {
                return content.Content;
            }
        }

        Stream GetWrappedOutputStream()
        {
            const int ChunkSize = 32768;    // buffer size used for synchronous writes
            const int BufferSize = 16384;   // buffer size used for asynchronous writes
            const int BufferCount = 4;      // buffer count used for asynchronous writes

            // Writing an request chunk has a high fixed cost, so use BufferedStream to avoid writing 
            // small ones. 
            return this.supportsConcurrentIO ? (Stream)new BufferedOutputAsyncStream(this.outputStream, BufferSize, BufferCount) : new BufferedStream(this.outputStream, ChunkSize);
        }

        void WriteStreamedMessage(TimeSpan timeout)
        {
            this.outputStream = GetWrappedOutputStream();

            // Since HTTP streams don't support timeouts, we can't just use TimeoutStream here. 
            // Rather, we need to run a timer to bound the overall operation
            if (onStreamSendTimeout == null)
            {
                onStreamSendTimeout = new Action<object>(OnStreamSendTimeout);
            }
            IOThreadTimer sendTimer = new IOThreadTimer(onStreamSendTimeout, this, true);
            sendTimer.Set(timeout);

            try
            {
                MtomMessageEncoder mtomMessageEncoder = messageEncoder as MtomMessageEncoder;
                if (mtomMessageEncoder == null)
                {
                    messageEncoder.WriteMessage(this.message, this.outputStream);
                }
                else
                {
                    mtomMessageEncoder.WriteMessage(this.message, this.outputStream, this.mtomBoundary);
                }
            }
            finally
            {
                sendTimer.Cancel();
            }
        }

        static void OnStreamSendTimeout(object state)
        {
            HttpOutput thisPtr = (HttpOutput)state;
            thisPtr.Abort(HttpAbortReason.TimedOut);
        }

        IAsyncResult BeginWriteStreamedMessage(ZeroMQResponseMessage responseMessage, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new WriteStreamedMessageAsyncResult(timeout, this, responseMessage, callback, state);
        }

        void EndWriteStreamedMessage(IAsyncResult result)
        {
            WriteStreamedMessageAsyncResult.End(result);
        }


        private void TraceHttpSendStart()
        {
            if (TD.HttpSendMessageStartIsEnabled())
            {
                if (streamed)
                {
                    TD.HttpSendStreamedMessageStart(this.eventTraceActivity);
                }
                else
                {
                    TD.HttpSendMessageStart(this.eventTraceActivity);
                }
            }
        }

        void LogMessage()
        {
            if (MessageLogger.LogMessagesAtTransportLevel)
            {
                MessageLogger.LogMessage(ref message, MessageLoggingSource.TransportSend);
            }
        }

        public void Send(ZeroMQResponseMessage responseMessage, TimeSpan timeout)
        {
            bool suppressEntityBody = this.PrepareZeroMQRequestSend(responseMessage);

            TraceHttpSendStart();

            if (suppressEntityBody)
            {
                // requests can't always support an output stream (for GET, etc)
                if (!isRequest)
                {
                    outputStream = this.GetOutputStream();
                }
                else
                {
                    this.SetContentLength(0);
                    LogMessage();
                }
            }
            else if (streamed)
            {
                outputStream = this.GetOutputStream();
                ApplyChannelBinding();

                OpaqueMessageContent content = responseMessage.Content as OpaqueMessageContent;
                if (content != null)
                {
                    content.WriteToStream(this.outputStream);
                }
                else
                {
                    if (!responseMessage.Content.CopyToAsync(this.outputStream).Wait<CommunicationException>(timeout))
                    {
                        throw FxTrace.Exception.AsError(new TimeoutException(SSR.Format(SSR.TimeoutOnSend, timeout)));
                    }
                }
            }
            else
            {
                if (this.IsChannelBindingSupportEnabled)
                {
                    //need to get the Channel binding token (CBT), apply channel binding info to the message and then write the message                    
                    //CBT is only enabled when message security is in the stack, which also requires an HTTP entity body, so we 
                    //should be safe to always get the stream.
                    outputStream = this.GetOutputStream();

                    ApplyChannelBinding();

                    ArraySegment<byte> buffer = SerializeBufferedMessage(responseMessage);

                    Fx.Assert(buffer.Count != 0, "We should always have an entity body in this case...");
                    outputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
                }
                else
                {
                    ArraySegment<byte> buffer = SerializeBufferedMessage(responseMessage);
                    SetContentLength(buffer.Count);

                    // requests can't always support an output stream (for GET, etc)
                    if (!isRequest || buffer.Count > 0)
                    {
                        outputStream = this.GetOutputStream();
                        outputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
                    }
                }
            }

            TraceSend();
        }

        public void Send(TimeSpan timeout)
        {
            bool suppressEntityBody = PrepareZeroMQRequestSend(message);

            TraceHttpSendStart();

            if (suppressEntityBody)
            {
                // requests can't always support an output stream (for GET, etc)
                if (!isRequest)
                {
                    outputStream = GetOutputStream();
                }
                else
                {
                    this.SetContentLength(0);
                    LogMessage();
                }
            }
            else if (streamed)
            {
                outputStream = GetOutputStream();
                ApplyChannelBinding();
                WriteStreamedMessage(timeout);
            }
            else
            {
                if (this.IsChannelBindingSupportEnabled)
                {
                    //need to get the Channel binding token (CBT), apply channel binding info to the message and then write the message                    
                    //CBT is only enabled when message security is in the stack, which also requires an HTTP entity body, so we 
                    //should be safe to always get the stream.
                    outputStream = GetOutputStream();

                    ApplyChannelBinding();

                    ArraySegment<byte> buffer = SerializeBufferedMessage(message);

                    Fx.Assert(buffer.Count != 0, "We should always have an entity body in this case...");
                    outputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
                }
                else
                {
                    ArraySegment<byte> buffer = SerializeBufferedMessage(message);
                    SetContentLength(buffer.Count);

                    // requests can't always support an output stream (for GET, etc)
                    if (!isRequest || buffer.Count > 0)
                    {
                        outputStream = GetOutputStream();
                        outputStream.Write(buffer.Array, buffer.Offset, buffer.Count);
                    }
                }
            }

            TraceSend();
        }

        void TraceSend()
        {
            if (DiagnosticUtility.ShouldTraceInformation)
            {
                TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.MessageSent, SSR.TraceCodeMessageSent,
                    new MessageTraceRecord(this.message), this, null);
            }

            if (TD.HttpSendStopIsEnabled())
            {
                TD.HttpSendStop(this.eventTraceActivity);
            }
        }

        class SendAsyncResult : AsyncResult
        {
            ZeroMQTransferMessagingOutput requestOutput;
            static AsyncCallback onGetOutputStream;
            static Action<object> onWriteStreamedMessageLater;
            static AsyncCallback onWriteStreamedMessage;
            static AsyncCallback onWriteBody;
            bool suppressEntityBody;
            ArraySegment<byte> buffer;
            TimeoutHelper timeoutHelper;
            ZeroMQResponseMessage responseMessage;

            public SendAsyncResult(ZeroMQTransferMessagingOutput requestOutput, ZeroMQResponseMessage responseMessage, bool suppressEntityBody, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.requestOutput = requestOutput;
                this.responseMessage = responseMessage;
                this.suppressEntityBody = suppressEntityBody;

                if (suppressEntityBody)
                {
                    if (requestOutput.isRequest)
                    {
                        requestOutput.SetContentLength(0);
                        this.requestOutput.TraceSend();
                        this.requestOutput.LogMessage();
                        base.Complete(true);
                        return;
                    }
                }

                this.timeoutHelper = new TimeoutHelper(timeout);
                Send();
            }

            void Send()
            {
                if (requestOutput.IsChannelBindingSupportEnabled)
                {
                    SendWithChannelBindingToken();
                }
                else
                {
                    SendWithoutChannelBindingToken();
                }
            }

            void SendWithoutChannelBindingToken()
            {
                if (!suppressEntityBody && !requestOutput.streamed)
                {
                    if (this.responseMessage != null)
                    {
                        buffer = requestOutput.SerializeBufferedMessage(this.responseMessage);
                    }
                    else
                    {
                        buffer = requestOutput.SerializeBufferedMessage(requestOutput.message);
                    }

                    requestOutput.SetContentLength(buffer.Count);
                }


                if (this.requestOutput.WillGetOutputStreamCompleteSynchronously)
                {
                    requestOutput.outputStream = requestOutput.GetOutputStream();
                }
                else
                {
                    if (onGetOutputStream == null)
                    {
                        onGetOutputStream = Fx.ThunkCallback(new AsyncCallback(OnGetOutputStream));
                    }

                    IAsyncResult result = requestOutput.BeginGetOutputStream(onGetOutputStream, this);

                    if (!result.CompletedSynchronously)
                        return;

                    requestOutput.outputStream = requestOutput.EndGetOutputStream(result);
                }

                if (WriteMessage(true))
                {
                    this.requestOutput.TraceSend();
                    base.Complete(true);
                }
            }

            void SendWithChannelBindingToken()
            {
                if (this.requestOutput.WillGetOutputStreamCompleteSynchronously)
                {
                    requestOutput.outputStream = requestOutput.GetOutputStream();
                    requestOutput.ApplyChannelBinding();
                }
                else
                {
                    if (onGetOutputStream == null)
                    {
                        onGetOutputStream = Fx.ThunkCallback(new AsyncCallback(OnGetOutputStream));
                    }

                    IAsyncResult result = requestOutput.BeginGetOutputStream(onGetOutputStream, this);

                    if (!result.CompletedSynchronously)
                        return;

                    requestOutput.outputStream = requestOutput.EndGetOutputStream(result);
                    requestOutput.ApplyChannelBinding();
                }

                if (!requestOutput.streamed)
                {
                    if (this.responseMessage != null)
                    {
                        buffer = requestOutput.SerializeBufferedMessage(this.responseMessage);
                    }
                    else
                    {
                        buffer = requestOutput.SerializeBufferedMessage(requestOutput.message);
                    }

                    requestOutput.SetContentLength(buffer.Count);
                }

                if (WriteMessage(true))
                {
                    this.requestOutput.TraceSend();
                    base.Complete(true);
                }
            }

            bool WriteMessage(bool isStillSynchronous)
            {
                if (suppressEntityBody)
                {
                    return true;
                }
                if (requestOutput.streamed)
                {
                    if (isStillSynchronous)
                    {
                        if (onWriteStreamedMessageLater == null)
                        {
                            onWriteStreamedMessageLater = new Action<object>(OnWriteStreamedMessageLater);
                        }
                        ActionItem.Schedule(onWriteStreamedMessageLater, this);
                        return false;
                    }
                    else
                    {
                        return WriteStreamedMessage();
                    }
                }
                else
                {
                    if (onWriteBody == null)
                    {
                        onWriteBody = Fx.ThunkCallback(new AsyncCallback(OnWriteBody));
                    }

                    IAsyncResult writeResult =
                        requestOutput.outputStream.BeginWrite(buffer.Array, buffer.Offset, buffer.Count, onWriteBody, this);

                    if (!writeResult.CompletedSynchronously)
                    {
                        return false;
                    }

                    CompleteWriteBody(writeResult);
                }

                return true;
            }

            bool WriteStreamedMessage()
            {
                // return a bool to determine if we are [....]. 

                if (onWriteStreamedMessage == null)
                {
                    onWriteStreamedMessage = Fx.ThunkCallback(OnWriteStreamedMessage);
                }

                return HandleWriteStreamedMessage(null); // completed synchronously
            }

            bool HandleWriteStreamedMessage(IAsyncResult result)
            {
                if (result == null)
                {
                    result = requestOutput.BeginWriteStreamedMessage(this.responseMessage, timeoutHelper.RemainingTime(), onWriteStreamedMessage, this);
                    if (!result.CompletedSynchronously)
                    {
                        return false;
                    }
                }

                requestOutput.EndWriteStreamedMessage(result);

                return true;
            }

            static void OnWriteStreamedMessage(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                SendAsyncResult thisPtr = (SendAsyncResult)result.AsyncState;
                Exception completionException = null;
                bool completeSelf = false;

                try
                {
                    completeSelf = thisPtr.HandleWriteStreamedMessage(result);
                }
                catch (Exception ex)
                {
                    if (Fx.IsFatal(ex))
                    {
                        throw;
                    }
                    completeSelf = true;
                    completionException = ex;
                }

                if (completeSelf)
                {
                    if (completionException != null)
                    {
                        thisPtr.requestOutput.TraceSend();
                    }
                    thisPtr.Complete(false, completionException);
                }
            }

            void CompleteWriteBody(IAsyncResult result)
            {
                requestOutput.outputStream.EndWrite(result);
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<SendAsyncResult>(result);
            }

            static void OnGetOutputStream(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                    return;

                SendAsyncResult thisPtr = (SendAsyncResult)result.AsyncState;

                Exception completionException = null;
                bool completeSelf = false;
                try
                {
                    thisPtr.requestOutput.outputStream = thisPtr.requestOutput.EndGetOutputStream(result);
                    thisPtr.requestOutput.ApplyChannelBinding();

                    if (!thisPtr.requestOutput.streamed && thisPtr.requestOutput.IsChannelBindingSupportEnabled)
                    {
                        thisPtr.buffer = thisPtr.requestOutput.SerializeBufferedMessage(thisPtr.requestOutput.message);
                        thisPtr.requestOutput.SetContentLength(thisPtr.buffer.Count);
                    }

                    if (thisPtr.WriteMessage(false))
                    {
                        thisPtr.requestOutput.TraceSend();
                        completeSelf = true;
                    }
                }
#pragma warning suppress 56500 // [....], transferring exception to another thread
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }
                    completeSelf = true;
                    completionException = e;
                }
                if (completeSelf)
                {
                    thisPtr.Complete(false, completionException);
                }
            }

            static void OnWriteStreamedMessageLater(object state)
            {
                SendAsyncResult thisPtr = (SendAsyncResult)state;

                bool completeSelf = false;
                Exception completionException = null;
                try
                {
                    completeSelf = thisPtr.WriteStreamedMessage();
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }
                    completeSelf = true;
                    completionException = e;
                }

                if (completeSelf)
                {
                    if (completionException != null)
                    {
                        thisPtr.requestOutput.TraceSend();
                    }
                    thisPtr.Complete(false, completionException);
                }
            }

            static void OnWriteBody(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                    return;

                SendAsyncResult thisPtr = (SendAsyncResult)result.AsyncState;

                Exception completionException = null;
                try
                {
                    thisPtr.CompleteWriteBody(result);
                    thisPtr.requestOutput.TraceSend();
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
        }

        internal static ZeroMQTransferMessagingOutput CreateZeroMQRequestOutput(ZeroMQListenerResponse listenerResponse, ZeroMQTransferTransportChannelListenerBase listener, Message message, string method)
        {
            return new ListenerResponseZeroMQRequestOutput(listenerResponse, listener, message, method);
        }

        class ListenerResponseZeroMQRequestOutput : ZeroMQTransferMessagingOutput
        {
            ZeroMQListenerResponse listenerResponse;
            string method;

            public ListenerResponseZeroMQRequestOutput(ZeroMQListenerResponse listenerResponse, ITransferTransportFactorySettings settings, Message message, string method)
                : base(settings, message, false, true)
            {
                this.listenerResponse = listenerResponse;
                this.method = method;
                this.SetIsFault(message.IsFault);
            }

            protected override string Method
            {
                get { return this.method; }
            }

            public override void Abort(RequestAbortReason abortReason)
            {
                listenerResponse.Abort();
                base.Abort(abortReason);
            }

            protected override void AddMimeVersion(string version)
            {
                listenerResponse.MIMEVersion = version;
            }

            protected override bool PrepareZeroMQRequestSend(Message message)
            {
                bool suppressEntityBody = base.PrepareZeroMQRequestSend(message);

                if (this.CanSendCompressedResponses)
                {
                    string contentType = this.listenerResponse.ContentType.MediaType;
                    if (HttpChannelUtilities.GetHttpResponseTypeAndEncodingForCompression(
                        ref contentType, out string contentEncoding))
                    {
                        if (contentType != this.listenerResponse.ContentType.MediaType)
                        {
                            this.SetContentType(contentType);
                        }
                        this.SetContentEncoding(contentEncoding.ToEnumerable());
                    }
                }

                ZeroMQResponseMessage responseMessage = message.Properties.GetValue<ZeroMQResponseMessage>(typeof(ZeroMQResponseMessage).FullName);
                bool httpResponseMessagePropertyFound = responseMessage != null;
                bool httpMethodIsHead = string.Compare(this.method, "HEAD", StringComparison.OrdinalIgnoreCase) == 0;

                if (httpResponseMessagePropertyFound)
                {
                    if (httpMethodIsHead || responseMessage.SuppressEntityBody)
                    {
                        suppressEntityBody = true;
                        this.SetContentLength(0);
                        this.SetContentType(null);
                        listenerResponse.SendChunked = false;
                    }
                    else
                    {
                        this.SetIsFault(responseMessage.IsFault);
                        this.SetContentLength((int)(responseMessage.Content.ContentLength ?? 0));
                        this.SetContentType(responseMessage.Content.ContentType.ToString());
                        this.SetContentEncoding(responseMessage.Content.ContentEncoding);
                    }
                }

                return suppressEntityBody;
            }

            protected override void PrepareZeroMQRequestSendCore(ZeroMQResponseMessage responseMessage)
            {
                this.SetIsFault(responseMessage.IsFault);
                this.SetContentLength((int)(responseMessage.Content.ContentLength ?? 0));
                this.SetContentType(responseMessage.Content.ContentType.ToString());
                this.SetContentEncoding(responseMessage.Content.ContentEncoding);
            }

            protected override void SetIsFault(bool isFault)
            {
                listenerResponse.IsFault = isFault;
            }

            protected override void SetContentLength(int contentLength)
            {
                listenerResponse.ContentLength64 = contentLength;
            }

            protected override void SetContentEncoding(IEnumerable<string> contentEncoding)
            {
                this.listenerResponse.ContentEncoding = contentEncoding;
            }
            protected override void SetContentType(string contentType)
            {
                listenerResponse.ContentType = new MediaContentType(contentType);
            }

            protected override void SetContentTypeParameter(string parameter, string value)
            {
                listenerResponse.ContentType.Parameters.Add(parameter, value);
            }

            protected override void SetResponseAction(string action)
            {
                listenerResponse.Action = action;
            }

            protected override void SetResponseTo(Uri to)
            {
                listenerResponse.To = to;
            }

            protected override void SetResponseRelatesTo(UniqueId relatesTo)
            {
                listenerResponse.RelatesTo = relatesTo;
            }

            protected override Stream GetOutputStream()
            {
                return new ZeroMQListenerResponseOutputStream(listenerResponse);
            }

            class ZeroMQListenerResponseOutputStream : BytesReadPositionStream
            {
                public ZeroMQListenerResponseOutputStream(ZeroMQListenerResponse listenerResponse)
                    : base(listenerResponse.OutputStream)
                {
                }

                public override void Close()
                {
                    try
                    {
                        base.Close();
                    }
                    catch (Exception exception)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(exception)));
                    }
                }

                public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
                {
                    try
                    {
                        return base.BeginWrite(buffer, offset, count, callback, state);
                    }
                    catch (IOException ioException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(ioException)));
                    }
                    catch (ArgumentException argumentException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(argumentException)));
                    }
                    catch (ObjectDisposedException objectDisposedException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(objectDisposedException)));
                    }
                    catch (NotSupportedException notSupportedException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(notSupportedException)));
                    }
                    catch (ApplicationException applicationException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new CommunicationObjectAbortedException(SSR.HttpResponseAborted,
                            applicationException));
                    }
                }

                public override void EndWrite(IAsyncResult result)
                {
                    try
                    {
                        base.EndWrite(result);
                    }
                    catch (IOException ioException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(ioException)));
                    }
                    catch (ArgumentException argumentException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(argumentException)));
                    }
                    catch (InvalidOperationException invalidOperationException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(invalidOperationException)));
                    }
                    catch (ApplicationException applicationException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new CommunicationObjectAbortedException(SSR.HttpResponseAborted,
                            applicationException));
                    }
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    try
                    {
                        base.Write(buffer, offset, count);
                    }
                    catch (IOException ioException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(ioException)));
                    }
                    catch (ArgumentException argumentException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(argumentException)));
                    }
                    catch (NotSupportedException notSupportedException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(notSupportedException)));
                    }
                    catch (ObjectDisposedException objectDisposedException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            ZeroMQChannelUtilities.CreateCommunicationException(
                                new ZeroMQListenerException(objectDisposedException)));
                    }
                    catch (ApplicationException applicationException)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new CommunicationObjectAbortedException(SSR.HttpResponseAborted,
                            applicationException));
                    }
                }
            }
        }
    }

    abstract class ZeroMQCloseOutputOnEofStream : DetectEofStream
    {
        ZeroMQTransferMessagingOutput output;
        bool isOutputClosed;

        /// <summary>
        /// Indicates whether the output should be closed when this stream is closed. 
        /// In the streamed case,  we'll leave the HttpOutput opened 
        /// (and it will be closed by the ZeroMQRequestContext, so we won't leak it).
        /// </summary>
        bool closeOutput;

        // Sometimes we can't flush the output until we're done reading the end of the incoming stream of the input
        protected ZeroMQCloseOutputOnEofStream(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <exception cref="ZeroMQListenerException" />
        /// <returns></returns>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            try
            {
                return base.BeginRead(buffer, offset, count, callback, state);
            }
            catch (IOException ioException)
            {
                throw new ZeroMQListenerException(ioException);
            }
            catch (ArgumentException argumentException)
            {
                throw new ZeroMQListenerException(argumentException);
            }
            catch (ObjectDisposedException objectDisposedException)
            {
                throw new ZeroMQListenerException(objectDisposedException);
            }
            catch (NotSupportedException notSupportedException)
            {
                throw new ZeroMQListenerException(notSupportedException);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <exception cref="ZeroMQListenerException" />
        /// <returns></returns>
        public override int EndRead(IAsyncResult asyncResult)
        {
            try
            {
                return base.EndRead(asyncResult);
            }
            catch (IOException ioException)
            {
                throw new ZeroMQListenerException(ioException);
            }
            catch (ArgumentException argumentException)
            {
                throw new ZeroMQListenerException(argumentException);
            }
            catch (InvalidOperationException invalidOperationException)
            {
                throw new ZeroMQListenerException(invalidOperationException);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <exception cref="ZeroMQListenerException" />
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return base.Read(buffer, offset, count);
            }
            catch (IOException ioException)
            {
                throw new ZeroMQListenerException(ioException);
            }
            catch (ArgumentException argumentException)
            {
                throw new ZeroMQListenerException(argumentException);
            }
            catch (ObjectDisposedException objectDisposedException)
            {
                throw new ZeroMQListenerException(objectDisposedException);
            }
            catch (NotSupportedException notSupportedException)
            {
                throw new ZeroMQListenerException(notSupportedException);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ZeroMQListenerException" />
        /// <returns></returns>
        public override int ReadByte()
        {
            try
            {
                return base.ReadByte();
            }
            catch (ObjectDisposedException objectDisposedException)
            {
                throw new ZeroMQListenerException(objectDisposedException);
            }
            catch (NotSupportedException notSupportedException)
            {
                throw new ZeroMQListenerException(notSupportedException);
            }
        }

        public bool EnableDelayedAccept(ZeroMQTransferMessagingOutput output, bool closeOutput)
        {
            if (IsAtEof)
            {
                return false;
            }

            this.closeOutput = closeOutput;
            this.output = output;
            return true;
        }

        protected override void OnReceivedEof()
        {
            if (this.closeOutput)
            {
                CloseHttpOutput();
            }
        }

        public override void Close()
        {
            if (this.closeOutput)
            {
                CloseHttpOutput();
            }

            base.Close();
        }

        void CloseHttpOutput()
        {
            if (this.output != null && !this.isOutputClosed)
            {
                this.output.Close();
                this.isOutputClosed = true;
            }
        }
    }

    public class ZeroMQListenerException : Exception
    {
        public ZeroMQListenerException(Exception innerException) : 
            base($"A {nameof(ZeroMQListenerException)} has occurred.", innerException)
        {
        }

        public ZeroMQListenerException(String message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}