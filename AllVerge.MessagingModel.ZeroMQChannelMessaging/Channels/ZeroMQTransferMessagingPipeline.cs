using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.Core.ServiceModel.Channels;
    using AllVerge.Core.ServiceModel.Transfer;
    using AllVerge.Core.ServiceModel.Faults.Exceptions;
    using AllVerge.Core.Threading;
    using System.ServiceModel;
    using System.ServiceModel.Diagnostics;
    using System.ServiceModel.Diagnostics.Application;

    abstract class ZeroMQTransferMessagingPipeline : TransferMessagingPipeline<ZeroMQRequestMessage, ZeroMQResponseMessage>
    {
        ZeroMQRequestContext requestContext;
        ZeroMQTransferMessagingInput requestInput;

        /// <summary>
        /// Indicates wheather the pipeline is closed (or closing) and it's used to prevent the Close method to be called concurrently.
        /// 0 = the pipeline is not closed (or closing)
        /// 1 = the pipeline is closed (or closing)
        /// </summary>
        private int isClosed = 0;

        public ZeroMQTransferMessagingPipeline(ZeroMQRequestContext requestContext)
        {
            this.requestContext = requestContext;
        }

        public ZeroMQTransferMessagingInput ZeroMQTransferMessagingInput
        {
            get
            {
                if (this.requestInput == null)
                {
                    this.requestInput = this.GetZeroMQTransferMessagingInput();
                }

                return this.requestInput;
            }
        }

        internal bool IsRequestInputInitialized
        {
            get
            {
                return this.requestInput != null;
            }
        }

        public override EventTraceActivity EventTraceActivity
        {
            get
            {
                return this.requestContext.EventTraceActivity;
            }
        }

        protected ZeroMQRequestContext ZeroMQRequestContext
        {
            get
            {
                return this.requestContext;
            }
        }

        public static ZeroMQTransferMessagingPipeline CreateZeroMQRequestPipeline(ZeroMQRequestContext requestContext, TransferMessagingIntegrationHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> requestIntegrationHandler)
        {
            if (requestIntegrationHandler == null)
            {
                if (requestContext.ZeroMQMessagesSupported)
                {
                    return new ZeroMQMessagesSupportedTransferMessagingPipeline(requestContext);
                }

                return new EmptyZeroMQTransferMessagingPipeline(requestContext);
            }

            return NormalZeroMQRequestPipeline.CreateNormalZeroMQRequestPipeline(requestContext, requestIntegrationHandler);
        }

        public abstract void SendReply(Message message, TimeSpan timeout);

        public virtual AsyncCompletionResult SendAsyncReply(Message message, Action<object, ZeroMQResponseMessage> asyncSendCallback, object state)
        {
            this.TraceProcessResponseStop();

            return AsyncCompletionResult.Completed;
        }

        public void Close()
        {
            if (Interlocked.Exchange(ref this.isClosed, 1) == 0)
            {
                this.OnClose();
            }
        }

        public override void Cancel()
        {
            this.requestContext.Abort();
        }

        internal abstract IAsyncResult BeginProcessInboundRequest(ReplyChannelAcceptor replyChannelAcceptor, Action dequeuedCallback, AsyncCallback callback, object state);

        internal abstract void EndProcessInboundRequest(IAsyncResult result);

        protected abstract IAsyncResult BeginParseIncomingMessage(AsyncCallback asynCallback, object state);

        protected abstract Message EndParseIncomingMesssage(IAsyncResult result, out Exception requestException);

        protected abstract void OnParseComplete(Message message, Exception requestException);

        protected virtual void OnClose()
        {
        }

        protected void TraceProcessInboundRequestStart()
        {
            if (TD.HttpPipelineProcessInboundRequestStartIsEnabled())
            {
                TD.HttpPipelineProcessInboundRequestStart(this.EventTraceActivity);
            }
        }

        protected void TraceBeginProcessInboundRequestStart()
        {
            if (TD.HttpPipelineBeginProcessInboundRequestStartIsEnabled())
            {
                TD.HttpPipelineBeginProcessInboundRequestStart(this.EventTraceActivity);
            }
        }

        protected void TraceProcessInboundRequestStop()
        {
            if (TD.HttpPipelineProcessInboundRequestStopIsEnabled())
            {
                TD.HttpPipelineProcessInboundRequestStop(this.EventTraceActivity);
            }
        }

        protected void TraceProcessResponseStart()
        {
            if (TD.HttpPipelineProcessResponseStartIsEnabled())
            {
                TD.HttpPipelineProcessResponseStart(this.EventTraceActivity);
            }
        }

        protected void TraceBeginProcessResponseStart()
        {
            if (TD.HttpPipelineBeginProcessResponseStartIsEnabled())
            {
                TD.HttpPipelineBeginProcessResponseStart(this.EventTraceActivity);
            }
        }

        protected void TraceProcessResponseStop()
        {
            if (TD.HttpPipelineProcessResponseStopIsEnabled())
            {
                TD.HttpPipelineProcessResponseStop(this.EventTraceActivity);
            }
        }

        protected virtual ZeroMQTransferMessagingInput GetZeroMQTransferMessagingInput()
        {
            return this.requestContext.GetZeroMQTransferMessagingInput(true);
        }

        protected ZeroMQTransferMessagingOutput GetZeroMQRequestOutput(Message message)
        {
            return this.requestContext.GetZeroMQRequestOutput(message);
        }

        class EmptyZeroMQTransferMessagingPipeline : ZeroMQTransferMessagingPipeline
        {
            static Action<object> onRequestInitializationTimeout = Fx.ThunkCallback<object>(OnRequestInitializationTimeout);
            IOThreadTimer requestInitializationTimer;
            bool requestInitializationTimerCancelled;

            public EmptyZeroMQTransferMessagingPipeline(ZeroMQRequestContext requestContext)
                : base(requestContext)
            {
                if (this.requestContext.Listener.RequestInitializationTimeout != HttpTransportDefaults.RequestInitializationTimeout)
                {
                    this.requestInitializationTimer = new IOThreadTimer(onRequestInitializationTimeout, this, false);
                    this.requestInitializationTimer.Set(this.requestContext.Listener.RequestInitializationTimeout);
                }
            }

            public override void SendReply(Message message, TimeSpan timeout)
            {
                // Make sure the timer was cancelled in the case we need to send the response back due to some errors happened during the time
                // we are processing the incoming request. From here the operation will be guarded by the SendTimeout.
                this.CancelRequestInitializationTimer();
                this.SendReplyCore(message, timeout);
            }

            public override Task<ZeroMQResponseMessage> Dispatch(ZeroMQRequestMessage requestMessage)
            {
                // This method should never be called for an EmptyPipeline.
                throw FxTrace.Exception.AsError(new NotSupportedException());
            }

            internal override IAsyncResult BeginProcessInboundRequest(
                ReplyChannelAcceptor replyChannelAcceptor,
                Action dequeuedCallback,
                AsyncCallback callback,
                object state)
            {
                this.TraceBeginProcessInboundRequestStart();
                return new EnqueueMessageAsyncResult(replyChannelAcceptor, dequeuedCallback, this, callback, state);
            }

            internal override void EndProcessInboundRequest(IAsyncResult result)
            {
                EnqueueMessageAsyncResult.End(result);
                this.TraceProcessInboundRequestStop();
            }

            protected override IAsyncResult BeginParseIncomingMessage(AsyncCallback asynCallback, object state)
            {
                return this.ZeroMQTransferMessagingInput.BeginParseIncomingMessage(asynCallback, state);
            }

            protected override Message EndParseIncomingMesssage(IAsyncResult result, out Exception requestException)
            {
                return this.ZeroMQTransferMessagingInput.EndParseIncomingMessage(result, out requestException);
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage(FxCop.Category.ReliabilityBasic, "Reliability103:ThrowWrappedExceptionsRule",
                    Justification = "The exceptions wrapped here will be thrown out later.")]
            protected override void OnParseComplete(Message message, Exception requestException)
            {
                if (!this.CancelRequestInitializationTimer() && requestException == null)
                {
                    requestException = FxTrace.Exception.AsError(new TimeoutException(SSR.Format(
                                                    SSR.RequestInitializationTimeoutReached,
                                                    this.ZeroMQRequestContext.Listener.RequestInitializationTimeout,
                                                    "RequestInitializationTimeout",
                                                    typeof(HttpTransportBindingElement).Name)));
                }

                this.ZeroMQRequestContext.SetMessage(message, requestException);
            }

            protected virtual void SendReplyCore(Message message, TimeSpan timeout)
            {
                this.TraceProcessResponseStart();
                ThreadTrace.Trace("Begin sending ZeroMQTransferMessaging reply");
                ZeroMQTransferMessagingOutput zeroMQRequestOutput = 
                    this.GetZeroMQRequestOutput(message);
                zeroMQRequestOutput.Send(timeout);
                ThreadTrace.Trace("End sending ZeroMQTransferMessaging reply");
                this.TraceProcessResponseStop();
            }

            protected bool CancelRequestInitializationTimer()
            {
                if (this.requestInitializationTimer == null)
                {
                    return true;
                }

                if (this.requestInitializationTimerCancelled)
                {
                    return false;
                }

                bool result = this.requestInitializationTimer.Cancel();
                this.requestInitializationTimerCancelled = true;

                return result;
            }

            protected override void OnClose()
            {
                this.CancelRequestInitializationTimer();
            }

            static void OnRequestInitializationTimeout(object obj)
            {
                Fx.Assert(obj != null, "obj should not be null.");
                HttpPipeline thisPtr = (HttpPipeline)obj;
                thisPtr.Cancel();
            }
        }

        class ZeroMQMessagesSupportedTransferMessagingPipeline : EmptyZeroMQTransferMessagingPipeline
        {
            ZeroMQRequestMessageZeroMQTransferMessagingInput zeroMQRequestMessageZeroMQTransferMessagingInput;

            public ZeroMQMessagesSupportedTransferMessagingPipeline(ZeroMQRequestContext requestContext) : 
                base(requestContext)
            {
            }

            public ZeroMQRequestMessageZeroMQTransferMessagingInput ZeroMQRequestMessageZeroMQTransferMessagingInput
            {
                get
                {
                    if (this.zeroMQRequestMessageZeroMQTransferMessagingInput == null)
                    {
                        this.zeroMQRequestMessageZeroMQTransferMessagingInput = (ZeroMQRequestMessageZeroMQTransferMessagingInput)this.ZeroMQTransferMessagingInput;
                        Fx.Assert(this.zeroMQRequestMessageZeroMQTransferMessagingInput != null, "The 'HttpInput' field should always be of type 'ZeroMQRequestMessageZeroMQTransferMessagingInput'.");
                    }

                    return this.zeroMQRequestMessageZeroMQTransferMessagingInput;
                }
            }

            public ZeroMQRequestMessage ZeroMQRequestMessage
            {
                get
                {
                    return this.ZeroMQRequestMessageZeroMQTransferMessagingInput.RequestMessage;
                }
            }

            protected override IAsyncResult BeginParseIncomingMessage(AsyncCallback asynCallback, object state)
            {
                return this.ZeroMQRequestMessageZeroMQTransferMessagingInput.BeginParseIncomingMessage(this.ZeroMQRequestMessage, asynCallback, state);
            }

            protected override void SendReplyCore(Message message, TimeSpan timeout)
            {
                this.TraceProcessResponseStart();
                ThreadTrace.Trace("Begin sending ZeroMQ reply");
                ZeroMQTransferMessagingOutput zeroMQTransferMessagingOutput = this.GetZeroMQRequestOutput(message);

                ZeroMQResponseMessage response = null;// HttpResponseMessageProperty.GetHttpResponseMessageFromMessage(message);
                
                if (response != null)
                {
                    zeroMQTransferMessagingOutput.Send(response, timeout);
                }
                else
                {
                    zeroMQTransferMessagingOutput.Send(timeout);
                }

                ThreadTrace.Trace("End sending ZeroMQ reply");
                this.TraceProcessResponseStop();
            }

            protected override ZeroMQTransferMessagingInput GetZeroMQTransferMessagingInput()
            {
                return base.GetZeroMQTransferMessagingInput().CreateRequestMessageInput();
            }
        }

        class NormalZeroMQRequestPipeline : ZeroMQTransferMessagingPipeline
        {
            static Action<object> onCreateMessageAndEnqueue = Fx.ThunkCallback<object>(OnCreateMessageAndEnqueue);
            static AsyncCallback onEnqueued = Fx.ThunkCallback(OnEnqueued);

            ZeroMQRequestMessage requestMessage;
            TransferMessagingIntegrationHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> transferMessagingIntegrationHandler;
            Task<ZeroMQResponseMessage> transferMessagingIntegrationHandlerTask;
            TaskCompletionSource<ZeroMQResponseMessage> channelModelIntegrationHandlerTcs;
            ReplyChannelAcceptor replyChannelAcceptor;
            Action dequeuedCallback;
            bool isShortCutResponse = true;
            bool wasProcessInboundRequestSuccessful;
            bool isAsyncReply = false;
            TimeSpan defaultSendTimeout;
            ZeroMQTransferMessagingOutput transferMessagingOutput;
            object thisLock = new object();
            CallbackCancellationTokenSource cancellationTokenSource;

            Action<object, ZeroMQResponseMessage> asyncSendCallback;
            object asyncSendState;

            public NormalZeroMQRequestPipeline(ZeroMQRequestContext requestContext, TransferMessagingIntegrationHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> requestIntegrationHandler)
                : base(requestContext)
            {
                this.defaultSendTimeout = requestContext.DefaultSendTimeout;

                this.cancellationTokenSource = new CallbackCancellationTokenSource(s => (s as ZeroMQRequestContext).Abort(), requestContext);
                Fx.Assert(requestIntegrationHandler != null, $"{nameof(requestIntegrationHandler)} should not be null.");
                this.transferMessagingIntegrationHandler = requestIntegrationHandler;
            }

            object ThisLock
            {
                get
                {
                    return this.thisLock;
                }
            }

            public override void SendReply(Message message, TimeSpan timeout)
            {
                this.TraceProcessResponseStart();
                TimeoutHelper helper = new TimeoutHelper(timeout);

                if (!this.isShortCutResponse)
                {
                    this.CompleteChannelModelIntegrationHandlerTask(message);

                    bool lockTaken = false;
                    try
                    {
                        // We need this lock only in [....] reply case. In this case, we hopped the thread in the request side, so it's possible to send the response here
                        // before the TransportIntegrationHandler is ready on another thread (thus a race condition). So we use the lock here. In the incoming path, we won't
                        // release the lock until the TransportIntegrationHandler is ready. Once we get the lock on the outgoing path, we can then call Wait() on this handler safely.
                        Monitor.TryEnter(this.ThisLock, TimeoutHelper.ToMilliseconds(helper.RemainingTime()), ref lockTaken);
                        if (!lockTaken)
                        {
                            throw FxTrace.Exception.AsError(new TimeoutException(SR.Format(SR.TimeoutOnSend, timeout)));
                        }

                        this.WaitTransportIntegrationHandlerTask(helper.RemainingTime());
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            Monitor.Exit(this.ThisLock);
                        }
                    }

                    if (this.transferMessagingIntegrationHandlerTask.Result != null)
                    {
                        this.transferMessagingOutput.Send(this.transferMessagingIntegrationHandlerTask.Result, helper.RemainingTime());
                    }
                }

                this.TraceProcessResponseStop();
            }

            public override AsyncCompletionResult SendAsyncReply(Message message, Action<object, ZeroMQResponseMessage> asyncSendCallback, object state)
            {
                this.TraceBeginProcessResponseStart();
                this.isAsyncReply = true;
                this.asyncSendCallback = asyncSendCallback;
                this.asyncSendState = state;

                this.CompleteChannelModelIntegrationHandlerTask(message);
                return AsyncCompletionResult.Queued;
            }

            public override Task<ZeroMQResponseMessage> Dispatch(ZeroMQRequestMessage requestMessage)
            {
                this.requestMessage = requestMessage;
                ((ZeroMQRequestMessageZeroMQTransferMessagingInput)this.ZeroMQTransferMessagingInput).SetHttpRequestMessage(requestMessage);
                Fx.Assert(this.channelModelIntegrationHandlerTcs == null, "channelModelIntegrationHandlerTask should be null.");
                this.channelModelIntegrationHandlerTcs = new TaskCompletionSource<ZeroMQResponseMessage>();
                ActionItem.Schedule(NormalZeroMQRequestPipeline.onCreateMessageAndEnqueue, this);
                return this.channelModelIntegrationHandlerTcs.Task;
            }

            public override void Cancel()
            {
                if (this.cancellationTokenSource.Token.CanBeCanceled)

                    this.cancellationTokenSource.Cancel();
            }

            internal override IAsyncResult BeginProcessInboundRequest(ReplyChannelAcceptor replyChannelAcceptor, Action dequeuedCallback, AsyncCallback callback, object state)
            {
                try
                {
                    this.wasProcessInboundRequestSuccessful = false;
                    this.TraceProcessInboundRequestStart();
                    this.replyChannelAcceptor = replyChannelAcceptor;
                    this.dequeuedCallback = dequeuedCallback;
                    ZeroMQRequestMessageZeroMQTransferMessagingInput requestMessageInput = (ZeroMQRequestMessageZeroMQTransferMessagingInput)this.ZeroMQTransferMessagingInput;

                    this.requestMessage = requestMessageInput.RequestMessage;
                    TransferMessagingPipeline.AddRequestPipeline(this.requestMessage, this);

                    lock (this.ThisLock)
                    {
                        this.transferMessagingIntegrationHandlerTask = this.transferMessagingIntegrationHandler.ProcessPipelineAsync(this.requestMessage, this.cancellationTokenSource.Token);
                    }

                    this.SendZeroMQTransferMessagingPipelineResponse();
                    this.TraceProcessInboundRequestStop();
                    this.wasProcessInboundRequestSuccessful = true;

                    return new CompletedAsyncResult(callback, state);
                }
                catch (OperationCanceledException)
                {
                    if (TD.HttpPipelineFaultedIsEnabled())
                    {
                        TD.HttpPipelineFaulted(this.EventTraceActivity);
                    }

                    this.Cancel();

                    throw;
                }
                catch (Exception ex)
                {
                    if (!Fx.IsFatal(ex))
                    {
                        if (TD.HttpPipelineFaultedIsEnabled())
                        {
                            TD.HttpPipelineFaulted(this.EventTraceActivity);
                        }

                        this.SendAndClose(CreateResponseMessage(FaultCodes.ServerErrorReceiverCode.InternalServerError.CreateFaultMessage(this.requestMessage?.Content?.ContentType?.MessageVersion), this.requestMessage));
                    }

                    throw;
                }
            }

            internal override void EndProcessInboundRequest(IAsyncResult result)
            {
                CompletedAsyncResult.End(result);
            }

            protected override IAsyncResult BeginParseIncomingMessage(AsyncCallback asynCallback, object state)
            {
                return this.ZeroMQTransferMessagingInput.BeginParseIncomingMessage(this.requestMessage, asynCallback, state);
            }

            protected override Message EndParseIncomingMesssage(IAsyncResult result, out Exception requestException)
            {
                return this.ZeroMQTransferMessagingInput.EndParseIncomingMessage(result, out requestException);
            }

            protected override void OnParseComplete(Message message, Exception requestException)
            {
                this.cancellationTokenSource.CancelAfter(Timeout.Infinite);
                this.requestContext.SetMessage(message, requestException);
                this.isShortCutResponse = false;
            }

            protected virtual void SetPipelineIncomingTimeout()
            {
                if (requestContext.Listener.RequestInitializationTimeout != HttpTransportDefaults.RequestInitializationTimeout)
                {
                    this.cancellationTokenSource.CancelAfter(requestContext.Listener.RequestInitializationTimeout);
                }
            }

            // The Close() method from the base class makes sure that this method is only called once.
            protected override void OnClose()
            {
                this.cancellationTokenSource.Dispose();

                // In ZeroMQRequestPipeline shortcut scenario or WebSocket scenario, we need to call the dequeueCallback in selfhost case
                // to start another receive loop on transport. Note that this dequeue callback should not be invoked earlier, else it
                // will lead to a potential DOS attack to the system.
                // ZeroMQRequestPipeline.Close() will always be called by ZeroMQRequestContext.Abort() or Close()
                // But if the ProcessInboundRequest method call was not successful, the SharedHttpTransportManager will start the receiving loop.
                if (this.isShortCutResponse && this.wasProcessInboundRequestSuccessful && this.dequeuedCallback != null)
                {
                    this.dequeuedCallback.Invoke();
                }

                base.OnClose();
            }

            internal static NormalZeroMQRequestPipeline CreateNormalZeroMQRequestPipeline(ZeroMQRequestContext requestContext, TransferMessagingIntegrationHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> requestIntegrationHandler)
            {
                NormalZeroMQRequestPipeline pipeline = new NormalZeroMQRequestPipeline(requestContext, requestIntegrationHandler);
                pipeline.SetPipelineIncomingTimeout();
                return pipeline;
            }

            protected override ZeroMQTransferMessagingInput GetZeroMQTransferMessagingInput()
            {
                ZeroMQTransferMessagingInput input = base.GetZeroMQTransferMessagingInput();

                return input.CreateRequestMessageInput();
            }

            protected virtual void SendZeroMQTransferMessagingPipelineResponse()
            {
                this.transferMessagingIntegrationHandlerTask.ContinueWith(
                    t =>
                    {
                        if (t.Result != null)
                        {
                            if (this.isShortCutResponse)
                            {
                                this.cancellationTokenSource.Dispose();
                                this.wasProcessInboundRequestSuccessful = true;
                                //// shortcut scenario
                                //// Currently we are always doing [....] send even async send is enabled. 
                                this.SendAndClose(t.Result);
                            }
                            else if (this.isAsyncReply)
                            {
                                this.asyncSendCallback.Invoke(this.asyncSendState, t.Result);
                            }
                        }
                    },
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
            }

            protected void SendAndClose(ZeroMQResponseMessage responseMessage)
            {
                this.ZeroMQRequestContext.SendResponseAndClose(responseMessage);
            }

            static void OnCreateMessageAndEnqueue(object state)
            {
                try
                {
                    NormalZeroMQRequestPipeline pipeline = (NormalZeroMQRequestPipeline)state;
                    Fx.Assert(pipeline != null, "pipeline should not be null.");
                    pipeline.CreateMessageAndEnqueue();
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

            static void OnEnqueued(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                try
                {
                    EnqueueMessageAsyncResult.End(result);
                }
                catch (Exception ex)
                {
                    if (Fx.IsFatal(ex))
                    {
                        throw;
                    }

                    FxTrace.Exception.TraceHandledException(ex, TraceEventType.Error);
                }
            }

            void CreateMessageAndEnqueue()
            {
                bool success = false;
                try
                {
                    Fx.Assert(this.replyChannelAcceptor != null, "acceptor should not be null.");
                    IAsyncResult result = new EnqueueMessageAsyncResult(this.replyChannelAcceptor, this.dequeuedCallback, this, onEnqueued, this);
                    if (result.CompletedSynchronously)
                    {
                        EnqueueMessageAsyncResult.End(result);
                    }

                    success = true;
                }
                catch (Exception ex)
                {
                    if (Fx.IsFatal(ex))
                    {
                        throw;
                    }

                    FxTrace.Exception.TraceUnhandledException(ex);
                }

                if (!success)
                {
                    this.SendAndClose(CreateResponseMessage(FaultCodes.ServerErrorReceiverCode.InternalServerError.CreateFaultMessage(this.requestMessage?.Content?.ContentType?.MessageVersion), this.requestMessage));
                }
            }

            ZeroMQResponseMessage CreateResponseMessage(Message message, ZeroMQRequestMessage requestMessage)
            {
                return new ZeroMQResponseMessage(message, requestMessage);
            }

            void CompleteChannelModelIntegrationHandlerTask(Message replyMessage)
            {
                if (this.channelModelIntegrationHandlerTcs != null)
                {
                    // If Service Model (or service instance) sent us null then we create a 202 HTTP response
                    
                    ZeroMQResponseMessage responseMessage = null;
                    
                    this.transferMessagingOutput = this.GetZeroMQRequestOutput(replyMessage);

                    if (replyMessage == null)
                    {
                        replyMessage = new NullMessage();
                    }

                    responseMessage = this.CreateResponseMessage(replyMessage, this.requestMessage);

                    this.cancellationTokenSource.CancelAfter(TimeoutHelper.ToMilliseconds(this.defaultSendTimeout));

                    this.channelModelIntegrationHandlerTcs.TrySetResult(responseMessage);
                }

                this.TraceProcessResponseStop();
            }

            void WaitTransportIntegrationHandlerTask(TimeSpan timeout)
            {
                Fx.Assert(this.transferMessagingIntegrationHandlerTask != null, $"{nameof(transferMessagingIntegrationHandlerTask)} should not be null.");
                this.transferMessagingIntegrationHandlerTask.Wait(timeout, null, null);
                this.wasProcessInboundRequestSuccessful = true;
            }
        }

        class EnqueueMessageAsyncResult : TraceAsyncResult
        {
            ZeroMQTransferMessagingPipeline pipeline;
            ReplyChannelAcceptor acceptor;
            Action dequeuedCallback;

            public EnqueueMessageAsyncResult(
                ReplyChannelAcceptor acceptor,
                Action dequeuedCallback,
                ZeroMQTransferMessagingPipeline pipeline,
                AsyncCallback callback,
                object state)
                : base(callback, state)
            {
                this.pipeline = pipeline;
                this.acceptor = acceptor;
                this.dequeuedCallback = dequeuedCallback;

                AsyncCallback asynCallback = PrepareAsyncCompletion(HandleParseIncomingMessage);
                IAsyncResult result = this.pipeline.BeginParseIncomingMessage(asynCallback, this);
                this.SyncContinue(result);
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<EnqueueMessageAsyncResult>(result);
            }

            static bool HandleParseIncomingMessage(IAsyncResult result)
            {
                EnqueueMessageAsyncResult thisPtr = (EnqueueMessageAsyncResult)result.AsyncState;
                thisPtr.CompleteParseAndEnqueue(result);
                return true;
            }

            void CompleteParseAndEnqueue(IAsyncResult result)
            {
                using (DiagnosticUtility.ShouldUseActivity ? ServiceModelActivity.BoundOperation(this.CallbackActivity) : null)
                {
                    Exception requestException;
                    Message message = this.pipeline.EndParseIncomingMesssage(result, out requestException);
                    if ((message == null) && (requestException == null))
                    {
                        throw FxTrace.Exception.AsError(
                                new ProtocolException(
                                    SR.MessageXmlProtocolError,
                                    new XmlException(SR.MessageIsEmpty)));
                    }

                    this.pipeline.OnParseComplete(message, requestException);
                    this.acceptor.Enqueue(this.pipeline.ZeroMQRequestContext, this.dequeuedCallback, true);
                }
            }
        }
    }
}
