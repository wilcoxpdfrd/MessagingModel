
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Diagnostics;
using System.ServiceModel.Dispatcher;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.SystemPrimitives.Collections.Concurrent;
    using AllVerge.SystemPrimitives.Logging;
    using AllVerge.SystemPrimitives.Threading;
    using AllVerge.SystemPrimitives.Threading.Tasks;

    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.MessagingFoundation.Http;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions;

    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.MessagingModel.ChannelMessaging.Channels;
    using AllVerge.SystemPrimitives.Reflection;

    public class ResourceChannelDispatcherManager : IChannelDispatcherManager
    {
        String CatchAllOperationName;
        private ChannelDispatcher channelDispatcher;
        private TimeSpan openTimeout;
        private TimeSpan receiveTimeout;
        private TimeSpan sendTimeout;
        private TimeSpan closeTimeout;
        private ServiceDescription description;
        private Dictionary<DispatcherBuilder.ListenUriInfo, Collection<ServiceEndpoint>> endpointsMap;
        private InstanceContext instanceContext;
        private CancellationToken cancellationToken;
        private Logger logger;

        private IServiceProvider services;
        private Object singletonDispatcher;
        private ConcurrentDictionaryAsync<String, IReceiveMessagingContextChannel<ChannelMessageContext>> messagingContextChannels;
        private bool disposedValue;

        public ResourceChannelDispatcherManager(ChannelDispatcher channelDispatcher, ResourceDispatcherHost resourceDispatcherHost, InstanceContext instanceContext, CancellationToken cancellationToken)
        {
            this.channelDispatcher = channelDispatcher;
            this.openTimeout = this.channelDispatcher.DefaultCommunicationTimeouts.OpenTimeout;
            this.receiveTimeout = this.channelDispatcher.DefaultCommunicationTimeouts.ReceiveTimeout;
            this.sendTimeout = this.channelDispatcher.DefaultCommunicationTimeouts.SendTimeout;
            this.closeTimeout = this.channelDispatcher.DefaultCommunicationTimeouts.CloseTimeout;
            this.description = resourceDispatcherHost.Description;
            this.endpointsMap = resourceDispatcherHost.EndpointsByListenUriInfo;
            this.instanceContext = instanceContext;
            this.cancellationToken = cancellationToken;
            this.messagingContextChannels = new ConcurrentDictionaryAsync<String, IReceiveMessagingContextChannel<ChannelMessageContext>>();
        }

        internal IPathEnvironment PathEnvironment { get; private set; }
        internal UriTemplateTables<DispatchOperationDescription> UriTemplateTables { get; private set; }
        internal UriTemplateTables<DispatchOperationDescription> WildcardTemplateTables { get; private set; }

        /// <summary>
        /// Gets an instance of the <see cref="Logging.Logger"/> with which to perform logging.
        /// </summary>
        public Logger Logger
        {
            get
            {
                if (this.logger == null)
                {
                    this.logger = Logger.GetInstance(this.GetType());

                    this.logger.Log(LoggerType.Info, Severity.TRACE, 1, "Logger initialized.");
                }
                return this.logger;
            }
        }

        public bool IsDuplexChannelListener => this.channelDispatcher.Listener is IChannelListener<IDuplexSessionChannel> || this.channelDispatcher.Listener is IChannelListener<IDuplexChannel>;

        public bool IsReplyChannelListenerRunning { get; private set; }

        public InstanceContext InstanceContext => this.instanceContext;

        public TimeSpan OpenTimeout { get => this.openTimeout; }

        public TimeSpan ReceiveTimeout { get => this.receiveTimeout; }

        public TimeSpan SendTimeout { get => this.sendTimeout; }

        public TimeSpan CloseTimeout { get => this.closeTimeout; }

        public void ConfigureEnvironment(IPathEnvironment pathEnvironment, IServiceProvider services)
        {
            this.PathEnvironment = pathEnvironment;
            this.services = services;
        }

        public Message CreateUnmatchedOperationFaultMessage(MessageVersion messageVersion, Exception exception)
        {
            // TODO Create subCode from exception ...

            FaultCode faultCode = messageVersion.Envelope.CreateSenderFaultCode(HttpStatusCode.NotFound.GetPredefinedFaultCodeFromHttpStatusCode(null, out FaultReason faultReason));

            FaultException faultException = new FaultException(faultReason, faultCode);

            return CreateFaultExceptionMessage(messageVersion, faultException);
        }

        private Message CreateFaultExceptionMessage(MessageVersion messageVersion, FaultException faultException)
        {
            using (Message message = Message.CreateMessage(messageVersion, "*"))
            {
                EndpointDispatcher endpointDispatcher = this.channelDispatcher.EndpointDispatcherTable.Lookup(message, out bool addressMatched);

                Message _message = message;

                DispatchOperationRuntime dispatchOperationRuntime = endpointDispatcher.DispatchRuntime.GetOperation(ref _message);

                if (_message != message)

                    (_message as IDisposable).Dispose();

                MessageFault messageFault = dispatchOperationRuntime.FaultFormatter.Serialize(faultException, out String action);

                return Message.CreateMessage(messageVersion, messageFault, action);
            }
        }

        internal ServiceEndpoint FindServiceEndpoint(Uri uri)
        {
            DispatcherBuilder.ListenUriInfo listenUriInfo = new DispatcherBuilder.ListenUriInfo(uri, ListenUriMode.Explicit);

            if (this.endpointsMap.TryGetValue(listenUriInfo, out Collection<ServiceEndpoint> serviceEndpoints))

                return serviceEndpoints.FirstOrDefault();

            return null;
        }

        /// <summary>
        /// Tries to match a specific resource dispatcher runtime to a <paramref name="receivedMessage"/>; 
        /// if successful, returns true and outputs the <paramref name="dispatchOperationRuntime"/>.
        /// Otherwise, returns false.
        /// </summary>
        /// <param name="receivedMessage"></param>
        /// <param name="dispatcherRuntime">Instance of a resource dispatcher runtime with which to process the incoming message.</param>
        /// <returns></returns>
        public bool TryMatchDispatcherOperation(ref Message receivedMessage, out IDispatcherRuntime dispatcherRuntime)
        {
            EndpointDispatcher endpointDispatcher =
                this.channelDispatcher.EndpointDispatcherTable.Lookup(receivedMessage, out bool addressMatched);

            if (addressMatched && endpointDispatcher != null)
            {
                DispatchOperationRuntime dispatchOperationRuntime = endpointDispatcher.DispatchRuntime.GetOperation(ref receivedMessage);

                if (endpointDispatcher.DispatchRuntime.CallbackOperationMap.TryGetValue(dispatchOperationRuntime.Name, out (string waitForReplyAsyncMethod, string callbackOutputOperationName) callBackOperations))
                {
                    DispatchOperation waitForReplyAsyncOperation =
                        endpointDispatcher.DispatchRuntime.Operations[callBackOperations.waitForReplyAsyncMethod];

                    ProxyOperationRuntime callbackOperationRuntime =
                        endpointDispatcher.DispatchRuntime.CallbackClientRuntime.GetRuntime().GetOperationByName(callBackOperations.callbackOutputOperationName);

                    dispatcherRuntime = new ResourceDispatcherRuntime(this, endpointDispatcher.DispatchRuntime, dispatchOperationRuntime, waitForReplyAsyncOperation, callbackOperationRuntime);
                }
                else

                    dispatcherRuntime = new ResourceDispatcherRuntime(this, endpointDispatcher.DispatchRuntime, dispatchOperationRuntime);
            }
            else

                dispatcherRuntime = null;

            return dispatcherRuntime != null;
        }

        public bool TryMatchDispatcherOperation(IncomingMessageEventArgs incomingMessageEventArgs, out IDispatcherRuntime dispatcherRuntime, out Exception exception)
        {
            try
            {
                exception = null;

                if (!TryMatchDispatcherOperation(incomingMessageEventArgs, out dispatcherRuntime))
                {
                    dispatcherRuntime = null;
                }
            }
            catch (Exception e)
            {
                exception = e;

                dispatcherRuntime = null;
            }

            return dispatcherRuntime != null;
        }

        private bool TryMatchDispatcherOperation(IncomingMessageEventArgs incomingMessageEventArgs, out IDispatcherRuntime dispatcherRuntime)
        {
            if (incomingMessageEventArgs == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(incomingMessageEventArgs));
            }

            if (TryMatchDispatcherOperation(incomingMessageEventArgs, out dispatcherRuntime, out bool uriMatched))
            {
                String operationName = dispatcherRuntime.DispatchOperationName;

                incomingMessageEventArgs.Properties.Add(IncomingMessageEventArgs.DispatchOperationUriMatchedPropertyName, uriMatched);

                incomingMessageEventArgs.Properties.Add(IncomingMessageEventArgs.DispatchOperationNamePropertyName, operationName);

                if (DiagnosticUtility.ShouldTraceInformation)
                {
                    TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.WebRequestMatchesOperation, PublicSR.Format(PublicSR.TraceCodeWebRequestMatchesOperation, incomingMessageEventArgs.Headers.To, operationName));
                }

                return true;
            }

            return false;
        }

        private bool TryMatchDispatcherOperation(IncomingMessageEventArgs incomingMessageEventArgs, out IDispatcherRuntime dispatcherRuntime, out bool addressMatched)
        {
            if (incomingMessageEventArgs == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(incomingMessageEventArgs));
            }

            using (Message incomingMessage = incomingMessageEventArgs.IncomingMessage.CreateMessage())
            {
                EndpointDispatcher endpointDispatcher =
                    this.channelDispatcher.EndpointDispatcherTable.Lookup(incomingMessage, out addressMatched);

                Message message = incomingMessage;

                DispatchOperationRuntime dispatchOperationRuntime = endpointDispatcher.DispatchRuntime.GetOperation(ref message);

                if (endpointDispatcher.DispatchRuntime.CallbackOperationMap.TryGetValue(dispatchOperationRuntime.Name, out (string waitForReplyAsyncMethod, string callbackOutputOperationName) callBackOperations))
                {
                    DispatchOperation waitForReplyAsyncOperation =
                        endpointDispatcher.DispatchRuntime.Operations[callBackOperations.waitForReplyAsyncMethod];

                    ProxyOperationRuntime callbackOperationRuntime =
                        endpointDispatcher.DispatchRuntime.CallbackClientRuntime.GetRuntime().GetOperationByName(callBackOperations.callbackOutputOperationName);

                    dispatcherRuntime = new ResourceDispatcherRuntime(this, endpointDispatcher.DispatchRuntime, dispatchOperationRuntime, waitForReplyAsyncOperation, callbackOperationRuntime);
                }
                else

                    dispatcherRuntime = new ResourceDispatcherRuntime(this, endpointDispatcher.DispatchRuntime, dispatchOperationRuntime);

                if (message != incomingMessage)

                    (message as IDisposable).Dispose();
            }

            return addressMatched;
        }

        internal SynchronizedCollection<IDispatchMessageInspector> GetMessageInspectors()
        {
            throw new NotImplementedException("TBD");

            //return this.EndpointDispatcher.DispatchRuntime.MessageInspectors;
        }

        private string TrySetDispatchOperationAccessControlProperties(IncomingMessageEventArgs incomingMessageEventArgs, HttpRequestMessageProperty httpRequestMessageProperty, MessageEncodingFormat messageFormat, Uri to, out bool uriMatched)
        {
            string method = httpRequestMessageProperty.Method;

            if (method == ResourceActions.OPTIONS)
            {
                String accessControlRequestMethod =
                    httpRequestMessageProperty.Headers[HttpHeaderNames.AccessControlRequestMethod];

                if (accessControlRequestMethod != null &&
                    this.WildcardTemplateTables.TryGetValue(method, out UriTemplateTable wildcardTemplateTable) &&
                    wildcardTemplateTable.CanUriMatch(this, to, method, messageFormat, httpRequestMessageProperty, incomingMessageEventArgs.Properties, false, out Object dispatcher, out String accessControlRequestOperation))
                {
                    if (this.CanDispatchToMethods(incomingMessageEventArgs, to, this.PathEnvironment, this.services, out List<String> _allowedMethods))
                    {
                        _allowedMethods.Add(ResourceActions.OPTIONS);

                        String[] allowedMethods = _allowedMethods.ToArray();

                        String[] accessControlRequestHeaders =
                            httpRequestMessageProperty.Headers.GetValues(HttpHeaderNames.AccessControlRequestHeaders);

                        // ToDo: Add list of allowed/forbidden header names to ResourceMethodAttribute ...
                        // (Objtain the ResorceMethodAttribute from uriTemplateTable.KeyValuePairs::Value ...)
                        // Then can validate requestHeaders against the AccessControlRequestHeaders ...

                        String[] allowedHeaders = accessControlRequestHeaders;

                        incomingMessageEventArgs.Properties.Add(
                            IncomingMessageEventArgs.DispatchAccessControlOperationNamePropertyName,
                            accessControlRequestOperation);

                        incomingMessageEventArgs.Properties.Add(
                            IncomingMessageEventArgs.DispatchOperationAccessDataPropertyName,
                            new DispatchOperationAccessData
                            {
                                AllowedMethods = allowedMethods,
                                AllowedHeaders = allowedHeaders
                            }
                        );

                        uriMatched = true;
                    }
                    else

                        uriMatched = false;
                }
                else

                    uriMatched = false;
            }
            else

                uriMatched = false;

            return method;
        }

        public bool ShouldRedirectToUriWithSlashAtTheEnd(Uri to, String method, MessageProperties messageProperties)
        {
            UriBuilder uriBuilder = new UriBuilder(to);

            if (uriBuilder.Path.EndsWith("/", StringComparison.Ordinal))
            {
                return false;
            }

            uriBuilder.Path += "/";

            Uri uri = uriBuilder.Uri;

            bool flag = false;

            if (this.UriTemplateTables != null)
            {
                if (this.UriTemplateTables.TryGetValue(method, out UriTemplateTable uriTemplateTable))
                {
                    if (uriTemplateTable.MatchSingle(uri) != null)

                        flag = true;
                }
            }

            if (flag)
            {
                string authority = GetAuthority(messageProperties);

                uri = UriTemplate.RewriteUri(uriBuilder.Uri, authority);

                messageProperties.Add(IncomingMessageEventArgs.DispatchOperationRedirectUriPropertyName, uri);
            }

            return flag;
        }

        public Task<(bool?, IReceiveMessagingContextChannel<ChannelMessageContext>)> TryGetMessagingContextChannelAsync<ProtoolContext>(String connectionId, CancellationToken cancellationToken)
        {
            var messagingContextChannel = TryGetOrAcceptChannelAsync<ProtoolContext>(connectionId);

            return messagingContextChannel.ContinueWith(t =>
            {
                if (t.Result == null || t.IsFaulted || t.IsCanceled)

                    return ((Nullable<bool>)null, null);

                return ((Nullable<bool>)true, t.Result);
            });
        }

        private Task<IReceiveMessagingContextChannel<ChannelMessageContext>> TryGetOrAcceptChannelAsync<ProtoolContext>(string connectionId)
        {
            if (this.IsDuplexChannelListener)
                return messagingContextChannels.GetOrAddAsync(connectionId, (_connectionId) =>
                {
                    Task<IReceiveMessagingContextChannel<ChannelMessageContext>> acceptTask =
                        this.TryAcceptChannelMessagingContextDuplexChannelAsync<ProtoolContext>(this.cancellationToken);

                    acceptTask.ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            if (t.Result != null)

                                t.Result.Closed = (r) => messagingContextChannels.RemoveAsync(_connectionId);
                        }
                    });

                    return acceptTask;
                });
            else
                return messagingContextChannels.GetOrAddAsync(connectionId, (_connectionId) =>
                {
                    Task<IReceiveMessagingContextChannel<ChannelMessageContext>> acceptTask =
                        this.TryAcceptChannelMessagingContextReplyChannelAsync<ProtoolContext>(this.cancellationToken);

                    acceptTask.ContinueWith(t =>
                    {
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            if (t.Result != null)

                                t.Result.Closed = (r) => messagingContextChannels.RemoveAsync(_connectionId);
                        }
                    });

                    return acceptTask;
                });
        }

        private Task<IReceiveMessagingContextChannel<ChannelMessageContext>> TryAcceptChannelMessagingContextDuplexChannelAsync<ProtocolContext>(
            CancellationToken cancellationToken)
        {
            IChannelListener<IDuplexSessionChannel> duplexSessionChannelListener =
                this.channelDispatcher.Listener as IChannelListener<IDuplexSessionChannel>;

            if (duplexSessionChannelListener != null)
            {
                return Task<IReceiveMessagingContextChannel<ChannelMessageContext>>.Run(() =>
                {
                    TimeoutHelper openTimeoutHelper = new TimeoutHelper(this.openTimeout);

                    try
                    {
                        IDuplexSessionChannel duplexSessionChannel =
                            duplexSessionChannelListener.AcceptChannel(openTimeoutHelper.RemainingTime());

                        IReceiveMessagingContextChannel<ChannelMessageContext> receiveMessagingContextChannel = 
                            (IReceiveMessagingContextChannel<ChannelMessageContext>)new AsynchronousRequestResponseChannelMessagingContextChannel(duplexSessionChannel, duplexSessionChannelListener, this, cancellationToken);

                        receiveMessagingContextChannel.Open(openTimeoutHelper.RemainingTime());

                        return receiveMessagingContextChannel;
                    }
                    catch (TimeoutException)
                    {
                        return null;
                    }
                });
            }
            else
            {
                IChannelListener<IDuplexChannel> duplexChannelListener =
                    this.channelDispatcher.Listener as IChannelListener<IDuplexChannel>;

                if (duplexChannelListener != null)
                {
                    return Task<IReceiveMessagingContextChannel<ChannelMessageContext>>.Run(async () =>
                    {
                        TimeoutHelper openTimeoutHelper = new TimeoutHelper(this.openTimeout);

                        try
                        {
                            IDuplexChannel duplexChannel = duplexChannelListener.AcceptChannel(openTimeoutHelper.RemainingTime());

                            IReceiveMessagingContextChannel<ChannelMessageContext> receiveMessagingContextChannel =
                                (IReceiveMessagingContextChannel<ChannelMessageContext>)new AsynchronousRequestResponseChannelMessagingContextChannel(duplexChannel, duplexChannelListener, this, cancellationToken);

                            receiveMessagingContextChannel.Open(openTimeoutHelper.RemainingTime());

                            return receiveMessagingContextChannel;
                        }
                        catch (TimeoutException)
                        {
                            return null;
                        }
                    });
                }
            }

            return (Task<IReceiveMessagingContextChannel<ChannelMessageContext>>)Task.FromException(new InvalidOperationException("Unexpected dispatch channel listener shape."));
        }

        private Task<IReceiveMessagingContextChannel<ChannelMessageContext>> TryAcceptChannelMessagingContextReplyChannelAsync<ProtocolContext>(
            CancellationToken cancellationToken)
        {
            IChannelListener<IReplyChannel> replyChannelListener = this.channelDispatcher.Listener as IChannelListener<IReplyChannel>;

            if (replyChannelListener != null)
            {
                TimeoutHelper openTimeoutHelper = new TimeoutHelper(this.openTimeout);

                return Task.Factory.FromAsync(replyChannelListener.BeginAcceptChannel, replyChannelListener.EndAcceptChannel, openTimeoutHelper.RemainingTime(), null).ContinueWith(t =>
                {
                    try
                    {
                        IReplyChannel replyChannel = t.Result;

                        if (replyChannel == null)

                            throw new TimeoutException();

                        IReceiveMessagingContextChannel<ChannelMessageContext> receiveMessagingContextReplyChannel =
                            new ReceiveChannelMessagingContextChannel(replyChannel, replyChannelListener, this, cancellationToken);

                        receiveMessagingContextReplyChannel.Open(openTimeoutHelper.RemainingTime());

                        return receiveMessagingContextReplyChannel;
                    }
                    catch (TimeoutException)
                    {
                        return (IReceiveMessagingContextChannel<ChannelMessageContext>)null;
                    }
                });
            }

            return (Task<IReceiveMessagingContextChannel<ChannelMessageContext>>)Task.FromException(new InvalidOperationException("Unexpected Channel dispatcher listener shape."));
        }

        public Task<bool> TryReceiveReplyChannelMessagesAsync(
            String listenUri,
            Action<RequestContext> receivedRequestContextAction)
        {
            IChannelListener<IReplyChannel> replyChannelListener = this.channelDispatcher.Listener as IChannelListener<IReplyChannel>;

            if (replyChannelListener != null)
            {
                return Task.Run<bool>(() =>
                {
                    TimeoutHelper openTimeoutHelper = new TimeoutHelper(this.openTimeout);

                    try
                    {
                        IReplyChannel replyChannel = replyChannelListener.AcceptChannel(openTimeoutHelper.RemainingTime());

                        if (replyChannel == null)

                            throw new TimeoutException();

                        replyChannel.Open(openTimeoutHelper.RemainingTime());

                        Task.Run(() =>
                        {
                            while (!cancellationToken.IsCancellationRequested)
                            {
                                try
                                {
                                    if (replyChannel.State == CommunicationState.Opened)
                                    {
                                        RequestContext requestContext = replyChannel.ReceiveRequest(this.receiveTimeout);

                                        if (requestContext == null)

                                            throw new TimeoutException();

                                        receivedRequestContextAction(requestContext);
                                    }
                                    else

                                        break;
                                }
                                catch (CommunicationException e)
                                {
                                    Logger.Log(e);
                                }
                                catch (TimeoutException e)
                                {
                                    Logger.Log(e);
                                }
                            }

                            if (replyChannel.State == CommunicationState.Opened)

                                replyChannel.Close();
                        });
                    }
                    catch (TimeoutException)
                    {
                        return false;
                    }

                    return true;
                });
            }

            return (Task<bool>)Task.FromException(new InvalidOperationException("Channel dispatcher listener is not an expected shape."));
        }

        public Task<bool> TryReceiveReplyChannelMessageAsync(
            String listenUri,
            Object asyncState,
            Func<Message, Task<Message>> onDispatchMessageAsync,
            Func<Task<Boolean>, Boolean> onDispositionResultAsync)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(this.channelDispatcher.DefaultCommunicationTimeouts.ReceiveTimeout);

            IChannelListener<IReplyChannel> replyChannelListener = this.channelDispatcher.Listener as IChannelListener<IReplyChannel>;

            if (replyChannelListener != null)
            {
                IReplyChannel replyChannel = replyChannelListener.AcceptChannel(timeoutHelper.RemainingTime());

                replyChannel.Open(timeoutHelper.RemainingTime());

                try
                {
                    RequestContext requestContext = replyChannel.ReceiveRequest(timeoutHelper.RemainingTime());

                    onDispatchMessageAsync(requestContext.RequestMessage)
                        .ContinueUsingAsyncStateWith(
                            dispatchedTask =>
                            {
                                if (dispatchedTask.TryFindAsyncState(out RequestContext requestContext1))
                                {
                                    requestContext1.Reply(dispatchedTask.Result);

                                    return true;
                                }

                                throw new DependencyNotFoundException(nameof(RequestContext));
                            }, false, asyncState, requestContext)
                        .ContinuePropogatingAsyncStateWith(repliedTask =>
                        {
                            return onDispositionResultAsync(repliedTask);
                        }, true);
                }
                catch (TimeoutException)
                {
                }
            }

            return Task.FromResult(false);
        }

        private static string GetAuthority(MessageProperties messageProperties)
        {
            string text = null;

            if (messageProperties.TryGetProperty(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))
            {
                text = httpRequestMessageProperty.Headers[HttpRequestHeader.Host];

                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }

            // ToDo: try get local host ...

            return text;
        }

        private bool CanDispatchToMethods(IncomingMessageEventArgs incomingMessageEventArgs, Uri to, IPathEnvironment pathEnvironment, IServiceProvider serviceProvider, out List<String> allowedMethods)
        {
            allowedMethods = new List<string>();

            if (this.UriTemplateTables != null)
            {
                foreach (UriTemplateTable methodSpecificUriTemplateTable in this.UriTemplateTables.Values)
                {
                    foreach (KeyValuePair<UriTemplate, Object> current in methodSpecificUriTemplateTable.KeyValuePairs)
                    {
                        DispatchOperationDescription dispatchOperation = (DispatchOperationDescription)current.Value;

                        String currentMethod = dispatchOperation.Method;

                        if (currentMethod != "*")
                        {
                            if (methodSpecificUriTemplateTable.MatchSingle(to) != null)
                            {
                                if (!allowedMethods.Contains(currentMethod))
                                {
                                    allowedMethods.Add(currentMethod);
                                }
                            }
                        }
                    }
                }
            }

            return allowedMethods.Count > 0;
        }

        internal object GetSingletonDispatcher(Action<object> configureSingletonDispatcherAction)
        {
            if (this.singletonDispatcher == null)
            {
                this.singletonDispatcher = Activator.CreateInstance(this.description.ServiceType);

                configureSingletonDispatcherAction(this.singletonDispatcher);
            }

            return this.singletonDispatcher;
        }

        internal static void ConfigureSingletonDispatcher(Object singletonInstance, ResourceChannelDispatcherManager resourceDispatcherSelector)
        {
            if (singletonInstance is IMessagingDispatcher)
            {
                IMessagingDispatcher messagingDispatcher = (IMessagingDispatcher)singletonInstance;

                messagingDispatcher.ConfigureSingletonDispatcher(singletonInstance, resourceDispatcherSelector.PathEnvironment, resourceDispatcherSelector.services, resourceDispatcherSelector.channelDispatcher.Listener.Uri);
            }
        }

        internal static Object CloneDispatcherInstanceFromSingleton(Object singletonInstance)
        {
            IMessagingDispatcher messagingDispatcher = (IMessagingDispatcher)singletonInstance;

            return messagingDispatcher.CloneDispatcherInstanceFromSingleton(singletonInstance);
        }
    }

    class AsynchronousRequestResponseChannelMessagingContextChannel :
        AbstractAsynchronousRequestResponseMessagingContextChannel<ChannelMessageContext>
    {
        private IDuplexChannel duplexChannel;
        private IChannelListener channelListener;
        private int processingMessagesCount;
        private long receivedMessageId;

        public AsynchronousRequestResponseChannelMessagingContextChannel(IDuplexChannel duplexChannel, IChannelListener channelListener, ResourceChannelDispatcherManager resourceChannelDispatcherManager, CancellationToken cancellationToken) :
            base()
        {
            this.duplexChannel = duplexChannel;
            this.channelListener = channelListener;
            this.ResourceChannelDispatcherManager = resourceChannelDispatcherManager;
            this.ListenUri = this.channelListener.Uri;
            this.CancellationToken = cancellationToken;
        }

        public ResourceChannelDispatcherManager ResourceChannelDispatcherManager { get; }

        public TimeSpan ReceiveTimeout => this.ResourceChannelDispatcherManager.ReceiveTimeout;

        public TimeSpan SendTimeout => this.ResourceChannelDispatcherManager.SendTimeout;

        public TimeSpan CloseTimeout => this.ResourceChannelDispatcherManager.CloseTimeout;

        protected CancellationToken CancellationToken { get; }

        public override void ConfigureChannelProperties(IMessagingContext<ChannelMessageContext> receivedMessagingContext)
        {
            this.CancellationToken.RegisterWithAutoUnregister(() =>
            {
                if (this.duplexChannel.State == CommunicationState.Opened)
                {
                    _ = this.ResourceChannelDispatcherManager.CloseTimeout.WhenConditionMetOrTimeoutDo(
                        () => Thread.VolatileRead(ref this.processingMessagesCount) == 0, 
                        () => this.duplexChannel.Close());
                }
            });

            TaskEventCompletionSource tecs = new TaskEventCompletionSource(e => this.duplexChannel.Closed += e, e => this.duplexChannel.Closed -= e);

            receivedMessagingContext.BindingContext.ConnectionContext.SetUpgradeChannelClosedTECS(tecs);

            receivedMessagingContext.BindingContext.ConnectionContext.Items.Add<AbstractAsynchronousRequestResponseMessagingContextChannel<ChannelMessageContext>>(this);

            receivedMessagingContext.Items.Add<IChannelDispatcherManager, ResourceChannelDispatcherManager>(ResourceChannelDispatcherManager);
        }

        protected override Task OnOpenAsync(TimeSpan timeSpan)
        {
            return Task.Factory.FromAsync(this.duplexChannel.BeginOpen, this.duplexChannel.EndOpen, timeSpan, null);
        }

        protected override Task OnCloseAsync(TimeSpan timeSpan)
        {
            return Task.Factory.FromAsync(this.duplexChannel.BeginClose, this.duplexChannel.EndClose, timeSpan, null);
        }

        protected override void OnAbort()
        {
            this.duplexChannel.Abort();
        }

        public override Task<IMessagingContext<ChannelMessageContext>> ReceiveMessagingContextAsync()
        {
            return TryReceiveMessagingContextAsync(this.ReceiveTimeout);
        }

        private IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, Object state)
        {
            Interlocked.Increment(ref this.processingMessagesCount);

            return this.duplexChannel.BeginReceive(timeout, callback, state);
        }

        private ChannelMessageContext EndReceive(IAsyncResult result)
        {
            Message receivedMessage = this.duplexChannel.EndReceive(result);

            return ChannelMessageContext.Create(this.duplexChannel.GetProperty<ITransportProtocolContext>(), receivedMessage, Interlocked.Increment(ref this.receivedMessageId), DateTime.Now);
        }

        public override Task<IMessagingContext<ChannelMessageContext>> TryReceiveMessagingContextAsync(TimeSpan timeout)
        {
            if (this.CancellationToken.IsCancellationRequested)

                return null;

            this.duplexChannel.ThrowIfDisposedOrNotOpen();

            return Task.Factory.FromAsync<TimeSpan, ChannelMessageContext>(this.BeginReceive, this.EndReceive, timeout, null).ContinueWith(t => {

                Exception exception = null;

                if (t.IsCompletedSuccessfully((s, e) => exception = e))
                {
                    MessagingContext<ChannelMessageContext> messagingContext = new ChannelMessagingContext();

                    messagingContext.Input(t.Result);

                    return (IMessagingContext<ChannelMessageContext>)messagingContext;
                }

                if (exception != null)

                    this.ResourceChannelDispatcherManager.Logger.Log(exception);

                this.duplexChannel.ThrowIfDisposedOrNotOpen();

                return null;
            });
        }

        public override Task SendMessagingContextAsync(ChannelMessageContext messagingContext)
        {
            return this.TrySendMessagingContextAsync(messagingContext, this.SendTimeout);
        }

        public override Task TrySendMessagingContextAsync(ChannelMessageContext messagingContext, TimeSpan timeout)
        {
            this.duplexChannel.ThrowIfDisposedOrNotOpen();

            return Task.Factory.FromAsync<Message, TimeSpan>(this.BeginSend, this.EndSend, messagingContext.Message, timeout, null);
        }

        private IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.duplexChannel.ThrowIfDisposedOrNotOpen();

            return this.duplexChannel.BeginSend(message, timeout, callback, state);
        }

        private void EndSend(IAsyncResult result)
        {
            this.duplexChannel.EndSend(result);

            Interlocked.Decrement(ref this.processingMessagesCount);
        }

        public override Task HandledMessagingCallBackAsync(IMessagingContext<ChannelMessageContext> messagingContext)
        {
            this.ResourceChannelDispatcherManager.Logger.Debug($"Handled: {messagingContext}");

            return Task.CompletedTask;
        }

        protected override void MapConnection(ConnectionContext connectionContext)
        {
        }
    }

    class ReceiveChannelMessagingContextChannel :
        AbstractRequestResponseMessagingContextChannel<ChannelMessageContext>
    {
        private IReplyChannel replyChannel;
        private IChannelListener<IReplyChannel> replyChannelListener;

        public ReceiveChannelMessagingContextChannel(IReplyChannel replyChannel, IChannelListener<IReplyChannel> replyChannelListener, ResourceChannelDispatcherManager resourceChannelDispatcherManager, CancellationToken cancellationToken) :
            base()
        {
            this.ResourceChannelDispatcherManager = resourceChannelDispatcherManager;
            this.replyChannel = replyChannel;
            this.replyChannelListener = replyChannelListener;
            this.replyChannel.Closed += this.ReplyChannel_Closed;
            this.ListenUri = replyChannelListener.Uri;
            this.CancellationToken = cancellationToken;
        }

        public ResourceChannelDispatcherManager ResourceChannelDispatcherManager { get; }

        public override bool IsOpen => this.replyChannel.State == CommunicationState.Opened;

        public TimeSpan ReceiveTimeout => this.ResourceChannelDispatcherManager.ReceiveTimeout;

        public TimeSpan SendTimeout => this.ResourceChannelDispatcherManager.SendTimeout;

        protected CancellationToken CancellationToken { get; }

        private void ReplyChannel_Closed(object sender, EventArgs e)
        {
            this.replyChannel.Closed -= this.ReplyChannel_Closed;
            if (this.Closed != null)
                this.Closed(this);
        }

        public override void ConfigureChannelProperties(IMessagingContext<ChannelMessageContext> receivedMessagingContext)
        {
            receivedMessagingContext.Items.Add<IChannelDispatcherManager, ResourceChannelDispatcherManager>(ResourceChannelDispatcherManager);
        }

        protected override Task OnOpenAsync(TimeSpan timeSpan)
        {
            return Task.Factory.FromAsync(this.replyChannel.BeginOpen, this.replyChannel.EndOpen, timeSpan, null);
        }

        protected override Task OnCloseAsync(TimeSpan timeSpan)
        {
            return Task.Factory.FromAsync(this.replyChannel.BeginClose, this.replyChannel.EndClose, timeSpan, null);
        }

        protected override void OnAbort()
        {
            this.replyChannel.Abort();
        }

        public override Task<IReceivedMessagingContextChannel<ChannelMessageContext>> ReceiveMessagingContextChannelAsync(long received)
        {
            return this.TryReceiveMessagingContextChannelAsync(received, this.ReceiveTimeout);
        }

        public override Task<IReceivedMessagingContextChannel<ChannelMessageContext>> TryReceiveMessagingContextChannelAsync(long received, TimeSpan timeout)
        {
            try
            {
                if (this.CancellationToken.IsCancellationRequested)

                    return null;

                this.replyChannel.ThrowIfDisposedOrNotOpen();

                Func<IAsyncResult, IReceivedMessagingContextChannel<ChannelMessageContext>> endTryReceiveRequest = r =>
                {
                    if (this.replyChannel.EndTryReceiveRequest(r, out RequestContext requestContext))
                    {
                        requestContext.RequestMessage.Properties.TryGetProperty<ITransportProtocolContext>(out ITransportProtocolContext transportProtocolContext);

                        return new ReceivedChannelMessagingContextChannel(this.ResourceChannelDispatcherManager, transportProtocolContext, received, requestContext);
                    }

                    throw new TimeoutException();
                };

                return Task.Factory.FromAsync<TimeSpan, IReceivedMessagingContextChannel<ChannelMessageContext>>(
                    this.replyChannel.BeginTryReceiveRequest, endTryReceiveRequest, timeout, null);
            }
            catch (CommunicationException e)
            {
                ResourceChannelDispatcherManager.Logger.Log(e);

                throw;
            }
            catch (TimeoutException e)
            {
                ResourceChannelDispatcherManager.Logger.Log(e);

                throw;
            }
        }

        public override Task HandledMessagingCallBackAsync(IMessagingContext<ChannelMessageContext> messagingContext)
        {
            this.ResourceChannelDispatcherManager.Logger.Debug($"Handled: {messagingContext}");

            return Task.CompletedTask;
        }

        protected override void MapConnection(ConnectionContext connectionContext)
        {
        }

        private class ReceivedChannelMessagingContextChannel :
            AbstractMessagingContextChannel<ChannelMessageContext>,
            IReceivedMessagingContextChannel<ChannelMessageContext>
        {
            private Logger logger;
            private ITransportProtocolContext transportProtocolContext;
            private long requestId;
            private RequestContext requestContext;
            private DateTime requestContextReceived;
            private IMessagingContext<ChannelMessageContext> receivedMessagingContext;

            public ReceivedChannelMessagingContextChannel(ResourceChannelDispatcherManager resourceChannelDispatcherManager, ITransportProtocolContext transportProtocolContext, long requestIndex, RequestContext requestContext) : 
                base(MessagingChannelInteractions.Received)
            {
                this.ResourceChannelDispatcherManager = resourceChannelDispatcherManager;
                this.logger = resourceChannelDispatcherManager.Logger;
                this.transportProtocolContext = transportProtocolContext;
                this.requestId = requestIndex;
                this.requestContext = requestContext;
                this.requestContextReceived = DateTime.Now;
            }

            public ResourceChannelDispatcherManager ResourceChannelDispatcherManager { get; }

            public TimeSpan SendTimeout => this.ResourceChannelDispatcherManager.SendTimeout;

            public IMessagingContext<ChannelMessageContext> ReceivedMessagingContext
            {
                get
                {
                    if (this.receivedMessagingContext == null)
                    {
                        this.receivedMessagingContext = new ChannelMessagingContext();

                        receivedMessagingContext.Input(ChannelMessageContext.Create(this.transportProtocolContext, this.requestContext.RequestMessage, this.requestId, this.requestContextReceived));
                    }

                    return this.receivedMessagingContext;
                }
            }

            public void ConfigureChannelProperties(IMessagingContext<ChannelMessageContext> messagingContext)
            {
                messagingContext.BindingContext.ConnectionContext.Items.Add<IReceivedMessagingContextChannel<ChannelMessageContext>>(this);

                messagingContext.Items.Add<IChannelDispatcherManager, ResourceChannelDispatcherManager>(ResourceChannelDispatcherManager);
            }

            public Task HandledMessagingCallBackAsync(IMessagingContext<ChannelMessageContext> messagingContext)
            {
                this.logger.Debug($"Handled: {messagingContext}");

                return Task.CompletedTask;
            }

            public Task SendMessagingContextAsync(ChannelMessageContext messagingContext)
            {
                return TrySendMessagingContextAsync(messagingContext, SendTimeout);
            }

            public Task TrySendMessagingContextAsync(ChannelMessageContext messagingContext, TimeSpan timeout)
            {
                return Task.Factory.FromAsync(this.requestContext.BeginReply, this.requestContext.EndReply, messagingContext.Message, timeout, null);
            }

            protected override Task OnOpenAsync(TimeSpan timeSpan)
            {
                return Task.CompletedTask;
            }

            protected override Task OnCloseAsync(TimeSpan timeSpan)
            {
                return Task.Run(() => this.requestContext.Close(timeSpan));
            }

            protected override void OnAbort()
            {
                this.requestContext.Abort();
            }

            protected override void MapConnection(ConnectionContext connectionContext)
            {
            }
        }
    }

    static class ResourceChannelDispatchManagerExtensions
    {
        public static bool CanUriMatch(this UriTemplateTable uriTemplateTable, ResourceChannelDispatcherManager serviceDispatcherFactory, Uri to, String method, MessageEncodingFormat messageFormat, HttpRequestMessageProperty httpRequestMessageProperty, MessageProperties messageProperties, bool persistMatch, out object dispatcher, out String operationName)
        {
            operationName = null;

            ICollection<UriTemplateMatch> matches = uriTemplateTable.Match(to);

            foreach (UriTemplateMatch match in matches)
            {
                DispatchOperationDescription dispatchOperation = (DispatchOperationDescription)match.Data;

                if (dispatchOperation.Method == method)
                {
                    if (dispatchOperation.CanResourceMethodHandleContentFormat(messageFormat))
                    {
                        Object singletonDispatcher = dispatchOperation.GetSingletonDispatcher((s) => ResourceChannelDispatcherManager.ConfigureSingletonDispatcher(s, serviceDispatcherFactory));

                        dispatcher = ResourceChannelDispatcherManager.CloneDispatcherInstanceFromSingleton(singletonDispatcher);

                        operationName = dispatchOperation.Name;

                        if (persistMatch)
                        {
                            match.SetBaseUri(match.BaseUri, httpRequestMessageProperty);

                            messageProperties.Add(IncomingMessageEventArgs.DispatchOperationUriTemplateMatchResultsPropertyName, match);
                        }

                        return true;
                    }
                }
            }


            dispatcher = null;

            return false;
        }
    }
}
