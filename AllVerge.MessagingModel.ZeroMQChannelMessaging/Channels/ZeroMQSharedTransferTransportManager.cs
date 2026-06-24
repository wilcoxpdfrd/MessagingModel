using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using AllVerge.Core.ServiceModel.Faults.Exceptions;

    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Diagnostics;
    using System.ServiceModel.Diagnostics.Application;
    using System.ServiceModel.Dispatcher;

    internal class ZeroMQSharedTransferTransportManager: ZeroMQTransferTransportManager 
    {
        private int maxPendingAccepts;
        ReaderWriterLockSlim listenerRWLock;
        ZeroMQRequestListener listener;
        AsyncCallback onGetContext;
        AsyncCallback onContextReceived;
        Action onMessageDequeued;
        Action<object> onCompleteGetContextLater;

        internal ZeroMQSharedTransferTransportManager(Uri listenUri, ZeroMQTransferTransportChannelListenerBase channelListener)
            : base(listenUri, channelListener.HostNameComparisonMode, channelListener.Realm)
        {
            this.onGetContext = Fx.ThunkCallback(new AsyncCallback(OnGetContext));
            this.onMessageDequeued = new Action(OnMessageDequeued);
            this.onContextReceived = new AsyncCallback(this.HandleRequestContextReceived);
            this.listenerRWLock = new ReaderWriterLockSlim();

            this.maxPendingAccepts = channelListener.MaxPendingAccepts;
        }

        void StartListening(bool isThreadPoolThread)
        {
            if (isThreadPoolThread)
            {
                for (int i = 0; i < maxPendingAccepts; i++)
                {
                    IAsyncResult result = this.BeginGetContext(true);
                    if (result.CompletedSynchronously)
                    {
                        if (onCompleteGetContextLater == null)
                        {
                            onCompleteGetContextLater = new Action<object>(OnCompleteGetContextLater);
                        }
                        ActionItem.Schedule(onCompleteGetContextLater, result);
                    }
                }
            }
            else
            {
                // If we're not on a threadpool thread, then we need to post a callback to start our accepting loop
                // Otherwise if the calling thread aborts then the async I/O will get inadvertantly cancelled
                Task.Run(() => this.StartListening(true));
            }
        }

        internal override void OnClose(TimeSpan timeout)
        {
            Cleanup(false, timeout);
        }

        internal override void OnOpen()
        {
            listener = new ZeroMQRequestListener(this.ListenUri);

            string host;

            switch (HostNameComparisonMode)
            {
                case HostNameComparisonMode.Exact:
                    // Uri.DnsSafeHost strips the [], but preserves the scopeid for IPV6 addresses.
                    if (ListenUri.HostNameType == UriHostNameType.IPv6)
                    {
                        host = string.Concat("[", ListenUri.DnsSafeHost, "]");
                    }
                    else
                    {
                        host = ListenUri.NormalizedHost();
                    }
                    break;

                case HostNameComparisonMode.StrongWildcard:
                    host = "+";
                    break;

                case HostNameComparisonMode.WeakWildcard:
                    host = "*";
                    break;

                default:
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SSR.Format(SSR.UnrecognizedHostNameComparisonMode, HostNameComparisonMode.ToString())));
            }

            string path = ListenUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            if (!path.StartsWith("/", StringComparison.Ordinal))
                path = "/" + path;

            if (!path.EndsWith("/", StringComparison.Ordinal))
                path = path + "/";

            string httpListenUrl = string.Concat(Scheme, "://", host, ":", ListenUri.Port, path);

            //listener.UnsafeConnectionNtlmAuthentication = this.unsafeConnectionNtlmAuthentication;
            //listener.AuthenticationSchemeSelectorDelegate =
            //    new AuthenticationSchemeSelector(SelectAuthenticationScheme);

            //if (ExtendedProtectionPolicy.OSSupportsExtendedProtection)
            //{
            //    //This API will throw if on an unsupported platform.
            //    listener.ExtendedProtectionSelectorDelegate =
            //        new HttpListener.ExtendedProtectionSelector(SelectExtendedProtectionPolicy);
            //}

            //if (this.Realm != null)
            //{
            //    listener.Realm = this.Realm;
            //}

            bool success = false;
            try
            {
                //listener.Prefixes.Add(httpListenUrl);
                listener.Start();

                bool startedListening = false;
                try
                {
                    StartListening(Thread.CurrentThread.IsThreadPoolThread);

                    startedListening = true;
                }
                finally
                {
                    if (!startedListening)
                    {
                        listener.Stop();
                    }
                }

                success = true;
            }
            catch (ZeroMQListenerException listenerException)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    ZeroMQChannelUtilities.CreateCommunicationException(listenerException));
            }
            finally
            {
                if (!success)
                {
                    listener.Abort();
                }
            }
        }

        internal override void OnAbort()
        {
            Cleanup(true, TimeSpan.Zero);
            base.OnAbort();
        }

        void Cleanup(bool aborting, TimeSpan timeout)
        {
            using (LockHelper.TakeWriterLock(this.listenerRWLock))
            {
                ZeroMQRequestListener listenerSnapshot = this.listener;
                if (listenerSnapshot == null)
                {
                    return;
                }

                try
                {
                    listenerSnapshot.Stop();
                }
                finally
                {
                    try
                    {
                        listenerSnapshot.Close();
                    }
                    finally
                    {
                        if (!aborting)
                        {
                            base.OnClose(timeout);
                        }
                        else
                        {
                            base.OnAbort();
                        }
                    }
                }

                this.listener = null;
            }
        }

        void OnMessageDequeued()
        {
            ThreadTrace.Trace("message dequeued");
            IAsyncResult result = this.BeginGetContext(false);
            if (result != null && result.CompletedSynchronously)
            {
                if (onCompleteGetContextLater == null)
                {
                    onCompleteGetContextLater = new Action<object>(OnCompleteGetContextLater);
                }
                ActionItem.Schedule(onCompleteGetContextLater, result);
            }
        }

        IAsyncResult BeginGetContext(bool startListening)
        {
            EventTraceActivity eventTraceActivity = null;
            if (FxTrace.Trace.IsEnd2EndActivityTracingEnabled)
            {
                eventTraceActivity = EventTraceActivity.GetFromThreadOrCreate(true);
                if (TD.HttpGetContextStartIsEnabled())
                {
                    TD.HttpGetContextStart(eventTraceActivity);
                }
            }

            while (true)
            {
                Exception unexpectedException = null;
                try
                {
                    try
                    {
                        if (ExecutionContext.IsFlowSuppressed())
                        {
                            return this.BeginGetContextCore(eventTraceActivity);
                        }
                        else
                        {
                            using (ExecutionContext.SuppressFlow())
                            {
                                return this.BeginGetContextCore(eventTraceActivity);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!this.HandleZeroMQRequestException(e))
                        {
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }
                    if (startListening)
                    {
                        // Since we're under a call to StartListening(), just throw the exception up the stack.
                        throw;
                    }
                    unexpectedException = e;
                }

                if (unexpectedException != null)
                {
                    this.Fault(unexpectedException);
                    return null;
                }
            }
        }

        IAsyncResult BeginGetContextCore(EventTraceActivity eventTraceActivity)
        {
            using (LockHelper.TakeReaderLock(this.listenerRWLock))
            {
                if (this.listener == null)
                {
                    return null;
                }

                return this.listener.BeginGetContext(onGetContext, eventTraceActivity);
            }
        }

        void OnGetContext(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            OnGetContextCore(result);
        }

        void OnCompleteGetContextLater(object state)
        {
            OnGetContextCore((IAsyncResult)state);
        }

        void OnGetContextCore(IAsyncResult listenerContextResult)
        {
            Fx.Assert(listenerContextResult != null, "listenerContextResult cannot be null.");
            bool enqueued = false;

            while (!enqueued)
            {
                Exception unexpectedException = null;
                try
                {
                    try
                    {
                        enqueued = this.EnqueueContext(listenerContextResult);
                    }
                    catch (Exception e)
                    {
                        if (!this.HandleZeroMQRequestException(e))
                        {
                            throw;
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    unexpectedException = exception;
                }

                if (unexpectedException != null)
                {
                    this.Fault(unexpectedException);
                }

                // NormalHttpPipeline calls HttpListener.BeginGetContext() by itself (via its dequeuedCallback) in the short-circuit case
                // when there was no error processing the inboud request (see the comments in the NormalHttpPipeline.Close() for details).
                if (!enqueued) // onMessageDequeued will handle this in the enqueued case
                {
                    // Continue the loop with the async result if it completed synchronously.
                    listenerContextResult = this.BeginGetContext(false);
                    if ((listenerContextResult == null) || !listenerContextResult.CompletedSynchronously)
                    {
                        return;
                    }
                }
            }
        }

        bool EnqueueContext(IAsyncResult listenerContextResult)
        {
            EventTraceActivity eventTraceActivity = null;
            ZeroMQRequestListenerContext listenerContext;
            bool enqueued = false;

            if (FxTrace.Trace.IsEnd2EndActivityTracingEnabled)
            {
                eventTraceActivity = (EventTraceActivity)listenerContextResult.AsyncState;
                if (eventTraceActivity == null)
                {
                    eventTraceActivity = EventTraceActivity.GetFromThreadOrCreate(true);
                }
            }

            using (LockHelper.TakeReaderLock(this.listenerRWLock))
            {
                if (this.listener == null)
                {
                    return true;
                }

                listenerContext = this.listener.EndGetContext(listenerContextResult);
            }

            // Grab the activity from the context and set that as the surrounding activity.
            // If a message appears, we will transfer to the message's activity next
            using (DiagnosticUtility.ShouldUseActivity ? ServiceModelActivity.BoundOperation(this.Activity) : null)
            {
                ServiceModelActivity activity = DiagnosticUtility.ShouldUseActivity ? ServiceModelActivity.CreateBoundedActivityWithTransferInOnly(listenerContext.Request.RequestTraceIdentifier) : null;
                try
                {
                    if (activity != null)
                    {
                        StartReceiveBytesActivity(activity, listenerContext.Request.Url);
                    }
                    if (DiagnosticUtility.ShouldTraceInformation)
                    {
                        TraceUtility.TraceHttpConnectionInformation(listenerContext.Request.LocalEndPoint.ToString(),
                            listenerContext.Request.RemoteEndPoint.ToString(), this);
                    }

                    this.TraceMessageReceived(eventTraceActivity, this.ListenUri);

                    ZeroMQTransferTransportChannelListenerBase channelListener;
                    if (this.TryLookupUri(listenerContext.Request.Url,
                                        listenerContext.Request.Method,
                                        this.HostNameComparisonMode,
                                        out channelListener))
                    {
                        ZeroMQRequestContext context = ZeroMQRequestContext.CreateContext(channelListener, listenerContext, eventTraceActivity);

                        IAsyncResult requestContextReceivedResult = 
                            channelListener.BeginZeroMQRequestContextReceived(
                                context,
                                onMessageDequeued,
                                onContextReceived,
                                DiagnosticUtility.ShouldUseActivity ? (object)new ActivityHolder(activity, context) : (object)context);

                        if (requestContextReceivedResult.CompletedSynchronously)
                        {
                            enqueued = EndZeroMQRequestContextReceived(requestContextReceivedResult);
                        }
                        else
                        {
                            // The callback has been enqueued.
                            enqueued = true;
                        }
                    }
                    else
                    {
                        HandleMessageReceiveFailed(listenerContext);
                    }
                }
                finally
                {
                    if (DiagnosticUtility.ShouldUseActivity && activity != null)
                    {
                        if (!enqueued)
                        {
                            // Error during enqueuing
                            activity.Dispose();
                        }
                    }
                }
            }

            return enqueued;
        }

        void HandleRequestContextReceived(IAsyncResult requestContextReceivedResult)
        {
            if (requestContextReceivedResult.CompletedSynchronously)
            {
                return;
            }

            bool enqueued = false;
            Exception unexpectedException = null;
            try
            {
                try
                {
                    enqueued = EndZeroMQRequestContextReceived(requestContextReceivedResult);
                }
                catch (Exception e)
                {
                    if (!this.HandleZeroMQRequestException(e))
                    {
                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                unexpectedException = exception;
            }

            if (unexpectedException != null)
            {
                this.Fault(unexpectedException);
            }

            IAsyncResult listenerContextResult = null;
            if (!enqueued) // onMessageDequeued will handle this in the enqueued case
            {
                listenerContextResult = this.BeginGetContext(false);
                if ((listenerContextResult == null) || !listenerContextResult.CompletedSynchronously)
                {
                    return;
                }

                // Handle the context and continue the receive loop.
                this.OnGetContextCore(listenerContextResult);
            }
        }

        static bool EndZeroMQRequestContextReceived(IAsyncResult requestContextReceivedResult)
        {
            using (DiagnosticUtility.ShouldUseActivity ? (ActivityHolder)requestContextReceivedResult.AsyncState : null)
            {
                ZeroMQTransferTransportChannelListenerBase channelListener =
                    (DiagnosticUtility.ShouldUseActivity ?
                        ((ActivityHolder)requestContextReceivedResult.AsyncState).context :
                        (ZeroMQRequestContext)requestContextReceivedResult.AsyncState).Listener;

                return channelListener.EndZeroMQRequestContextReceived(requestContextReceivedResult);
            }
        }

        bool HandleZeroMQRequestException(Exception e)
        {
            if (e is OutOfMemoryException)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InsufficientMemoryException(SSR.InsufficentMemory, e));
            else
                return ExceptionHandler.HandleTransportExceptionHelper(e);
        }

        static void HandleMessageReceiveFailed(ZeroMQRequestListenerContext listenerContext)
        {
            TraceMessageReceiveFailed();

            listenerContext.Response.SendFault(FaultCodes.ClientErrorSenderCode.NotFound.WrapFaultCode(listenerContext.Request.Version.Envelope));
        }

        static void TraceMessageReceiveFailed()
        {
            if (TD.HttpMessageReceiveStartIsEnabled())
            {
                TD.HttpMessageReceiveFailed();
            }

            if (DiagnosticUtility.ShouldTraceWarning)
            {
                TraceUtility.TraceEvent(TraceEventType.Warning, TraceCode.HttpChannelMessageReceiveFailed,
                    SSR.TraceCodeHttpChannelMessageReceiveFailed, (object)null);
            }
        }
    }
}