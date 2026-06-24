using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting.Server.Features;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.SystemPrimitives.Threading;
    using AllVerge.SystemPrimitives.Threading.Tasks;

    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;

    using AllVerge.MessagingModel.ChannelMessaging.Controllers;
    using AllVerge.MessagingModel.ChannelMessaging.Channels;
    using AllVerge.MessagingModel.ChannelMessaging.Listeners;

    using Microsoft.AspNetCore.Http.Features;
    using AllVerge.SystemPrimitives.Collections.Generic;
    using Polly;

    public abstract class AbstractMessagingContextReceiver<MessageContext> :
        IAbstractMessagingContextReceiver<MessageContext> where MessageContext: IMessageContext
    {
        private IApplicationHostEnvironment hostEnvironment;
        private IHostApplicationLifetime hostApplicationLifetime;
        private IServiceProvider services;
        private CancellationTokenSource cancellationTokenSource;
        private PollRateController<MessageContext> pollRateController;
        private RequestResponseController<MessageContext> requestResponseController;
        private AsynchronousRequestResponseController<MessageContext> asynchronousRequestResponseController;
        private BlockingCollection<(IMessagingContext<MessageContext>, Action)> receivedMessagingContextQueue;
        private int disposingCount = 0;
        private bool disposedValue;

        protected AbstractMessagingContextReceiver(IApplicationHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime,  IServiceProvider services)
        {
            this.hostEnvironment = hostEnvironment;
            this.hostApplicationLifetime = hostApplicationLifetime;
            this.services = services;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.Logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(this.GetType().AssemblyQualifiedName);
            this.pollRateController = null;
            this.requestResponseController = null;
            this.asynchronousRequestResponseController = null;
            this.receivedMessagingContextQueue =
                new BlockingCollection<(IMessagingContext<MessageContext>, Action)>();

            DisposalExtensions.RegisterForDisposal(this);
        }

        protected CancellationToken CancellationToken => this.cancellationTokenSource.Token;
        public IApplicationHostEnvironment HostEnvironment => hostEnvironment;
        public IServiceProvider Services => services;
        public ILogger Logger { get; }
        internal PollRateController<MessageContext> PollRateController
        {
            get
            {
                if (this.pollRateController == null)
                {
                    IOptions<MessagingPollOptions> options = this.services.GetService<IOptions<MessagingPollOptions>>();

                    this.pollRateController = 
                        new PollRateController<MessageContext>(this.Logger, options.Value, this.PrepareRejectedMessagingContextAsync, this.ReceivedMessagingContextAsync, this.cancellationTokenSource.Token);
                }

                return this.pollRateController;
            }
        }

        internal RequestResponseController<MessageContext> RequestResponseController
        {
            get
            {
                if (this.requestResponseController == null)
                {
                    IOptions<MessagingReceiveOptions> options = this.services.GetService<IOptions<MessagingReceiveOptions>>();

                    this.requestResponseController = 
                        new RequestResponseController<MessageContext>(this.Logger, options.Value, this.PrepareRejectedMessagingContextAsync, this.ReceivedMessagingContextAsync, this.cancellationTokenSource.Token);
                }

                return this.requestResponseController;
            }
        }

        internal AsynchronousRequestResponseController<MessageContext> AsynchronousRequestResponseController
        {
            get
            {
                if (this.asynchronousRequestResponseController == null)
                {
                    IOptions<MessagingReceiveOptions> options = this.services.GetService<IOptions<MessagingReceiveOptions>>();

                    this.asynchronousRequestResponseController = 
                        new AsynchronousRequestResponseController<MessageContext>(this.Logger, options.Value, this.PrepareRejectedMessagingContextAsync, this.ReceivedMessagingContextAsync, this.cancellationTokenSource.Token);
                }

                return this.asynchronousRequestResponseController;
            }
        }

        public BlockingCollection<(IMessagingContext<MessageContext>, Action)> ReceivedProtocolMessagingContextQueue => receivedMessagingContextQueue;

        Task IAbstractMessagingContextReceiver<MessageContext>.StartAsync(IServerAddressesFeature serverAddresses)
        {
            // serverAddresses.Addresses will be raw addresses at this point (which we want);
            // we copy them into a new List; if Server Start up binds with Kestrel, the serverAddresses.Addresses
            // may be re-written via Microsoft.AspNetCore.Server.Kestrel.Core.ServerAddress.FromUrl::toString ...

            return OnStartAsync(new List<String>(serverAddresses.Addresses));
        }

        protected abstract Task OnStartAsync(IEnumerable<String> serverAddresses);

        /// <summary>
        /// Creates an instance of <see cref="MessageContext"/> that represents a "null" message context.
        /// </summary>
        /// <param name="messagingContext">A related (possibly incoming) messaging context.</param>
        /// <returns></returns>
        protected abstract MessageContext GetNullMessageContext(IMessagingContext<MessageContext> messagingContext);

        /// <summary>
        /// Creates an instance of <typeparamref name="MessageContext"/> that represents a fault message context reflecting the <paramref name="rejectionCode"/> and <paramref name="rejectionHeaders"/>.  
        /// </summary>
        /// <param name="messagingContext">The received messaging context.</param>
        /// <param name="rejectionCode">The <see cref="RejectCode"/>.</param>
        /// <param name="faultAction">The fault action.</param>
        /// <returns></returns>
        protected abstract MessageContext GetRejectedMessageContext(IMessagingContext<MessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders, string faultAction = null);

        Task IAbstractMessagingContextReceiver<MessageContext>.PrepareRejectedMessagingContextAsync(IMessagingContext<MessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders)
        {
            return this.PrepareRejectedMessagingContextAsync(messagingContext, rejectionCode, rejectionHeaders);
        }

        protected private Task PrepareRejectedMessagingContextAsync(IMessagingContext<MessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders)
        {
            MessageContext rejectedMessageContext = GetRejectedMessageContext(messagingContext, rejectionCode, rejectionHeaders);

            IDictionary<MiddlewarePipelineResultHeaders, StringValues> resultHeaders = rejectionHeaders?.Aggregate(
                new Dictionary<MiddlewarePipelineResultHeaders, StringValues>(),
                (r, h) =>
                {
                    switch (h.Key)
                    {
                        case RejectHeaders.Authenticate:
                            r.Add(MiddlewarePipelineResultHeaders.Authenticate, h.Value);
                            break;
                        case RejectHeaders.RetryAfter:
                            r.Add(MiddlewarePipelineResultHeaders.RetryAfter, h.Value);
                            break;
                    }

                    return r;
                });

            switch (rejectionCode)
            {
                case RejectCode.BindingUnreachable:
                    messagingContext.Output(rejectedMessageContext, MiddlewarePipelineResult.Unreachable);
                    break;
                case RejectCode.TooBusy:
                    messagingContext.Output(rejectedMessageContext, MiddlewarePipelineResult.TooBusy, resultHeaders);
                    break;
                case RejectCode.NotAuthorized:
                    messagingContext.Output(rejectedMessageContext, MiddlewarePipelineResult.NotAuthorized, resultHeaders);
                    break;
                case RejectCode.Timeout:
                    messagingContext.Output(rejectedMessageContext, MiddlewarePipelineResult.Timeout);
                    break;
                case RejectCode.Faulted:
                    messagingContext.Output(rejectedMessageContext, MiddlewarePipelineResult.Faulted);
                    break;
                case RejectCode.NotHandled:
                    messagingContext.Output(rejectedMessageContext, MiddlewarePipelineResult.NotHandled);
                    break;
            }

            return Task.CompletedTask;
        }

        Task IAbstractMessagingContextReceiver<MessageContext>.InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext)
        {
            return this.InvokeMessagingContextCallbackAsync(messagingContext);
        }

        protected abstract Task InvokeMessagingContextCallbackAsync
            (IMessagingContext<MessageContext> messagingContext);

        protected Task ReceivedMessagingContextAsync(IMessagingContext<MessageContext> receivedMessagingContext, Action onAckowldegeReceived, Action onRejectReceived)
        {
            Logger.LogTrace($"{nameof(ReceivedMessagingContextAsync)}: {receivedMessagingContext.BindingContext?.InteractionContext?.TraceIdentifier}");

            if (this.receivedMessagingContextQueue != null && !this.receivedMessagingContextQueue.IsAddingCompleted)
            {
                this.receivedMessagingContextQueue.Add((receivedMessagingContext, onAckowldegeReceived));
            }
            else
            {
                onRejectReceived();
            }

            return Task.CompletedTask;
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AbstractProtocolMessagingContextChannelReceiver()
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

        void IDisposable.Dispose()
        {
            this.Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (Interlocked.Increment(ref disposingCount) == 1)
                {
                    if (disposing)
                    {
                        this.cancellationTokenSource.Cancel();

                        if (this.receivedMessagingContextQueue != null)
                        {
                            this.receivedMessagingContextQueue.CompleteAdding();

                            Task.Run(async () =>
                            {
                                while (this.receivedMessagingContextQueue.Count > 0)
                                    await Task.Delay(10);

                                this.receivedMessagingContextQueue.Dispose();

                                this.receivedMessagingContextQueue = null;

                            }).GetAwaiter().GetResult();
                        }

                        this.OnDispose();

                        if (this.pollRateController != null)

                            this.pollRateController.Dispose();

                        if (this.requestResponseController != null)

                            this.requestResponseController.Dispose();

                        if (this.asynchronousRequestResponseController != null)

                            this.asynchronousRequestResponseController.Dispose();
                    }

                    disposedValue = true;
                }
            }
        }

        protected virtual void OnDispose()
        {
        }

        protected void ThrowIfDisposed()
        {
            if (this.disposedValue)

                throw new ObjectDisposedException(this.GetType().Name);
        }
    }

    /// <summary>
    /// Base class to implement a messaging context receiver.
    /// <see cref="MessageContext"/> must expose a parameterless constructor which produces a NULL messaging context instance, 
    /// and a constructor which accepts an <see cref="Exception"/> which produces a FAULT messaging context instance.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public abstract class BaseMessagingContextReceiver<MessageContext> :
        AbstractMessagingContextReceiver<MessageContext>,
        IMessagingContextReceiver<MessageContext>
        where MessageContext : IMessageContext
    {
        private TaskCompletionSource<VoidType> startTaskCompletionSource;
        private IEnumerable<IMessagingContextChannelListener<MessageContext>> messagingChannelListeners;

        public BaseMessagingContextReceiver(IApplicationHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider services) : 
            base(hostEnvironment, hostApplicationLifetime, services)
        {
        }

        protected override async Task OnStartAsync(IEnumerable<String> serverAddresses)
        {
            if (Interlocked.CompareExchange(ref this.startTaskCompletionSource, new TaskCompletionSource<VoidType>(), null) == null)
            {
                this.messagingChannelListeners =
                    this.Services.GetServices<IMessagingContextChannelListener<MessageContext>>();

                int listenerCount = messagingChannelListeners.Count();

                AwaitableBlockingCollection<Task<IMessagingContextChannelListener<MessageContext>>> startTasks =
                    new AwaitableBlockingCollection<Task<IMessagingContextChannelListener<MessageContext>>>(listenerCount);

                _ = Task.Run(() => {
                    try
                    {
                        ParallelOptions po = new ParallelOptions();
                        po.CancellationToken = base.CancellationToken;
                        po.MaxDegreeOfParallelism = listenerCount;

                        Parallel.ForEach(messagingChannelListeners, po, async messagingChannelListener => {

                            messagingChannelListener.Init(HostEnvironment, serverAddresses, Services, base.CancellationToken);

                            Task<IMessagingContextChannelListener<MessageContext>> startTask =
                                messagingChannelListener.StartListeningAsync().CompleteWith(messagingChannelListener);

                            startTasks.TryAdd(startTask);

                            await startTask.ContinueWith(async t => {

                                if (t.IsCompleted)

                                    while (!base.CancellationToken.IsCancellationRequested)
                                    {
                                        (bool success, IReceiveMessagingContextChannel<MessageContext> messagingContextChannel) result =
                                            await messagingChannelListener.TryAcceptMessagingContextChannelAsync();

                                        if (result.success)
                                        {
                                            IReceiveMessagingContextChannel<MessageContext> messagingContextChannel = result.messagingContextChannel;

                                            if (!messagingContextChannel.IsOpen)
                                            {
                                                base.Logger.LogError(new InvalidOperationException("Channel is not open."), $"{nameof(OnStartAsync)}: {messagingContextChannel.ToString()} is not open and cannot be used.");
                                            }
                                            else if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.Poll))
                                            {
                                                try
                                                {
                                                    AbstractPollMessagingContextChannel<MessageContext> pollMessagingContextChannel =
                                                        messagingContextChannel as AbstractPollMessagingContextChannel<MessageContext>;

                                                    await this.PollRateController.AcceptChannelAsync(messagingChannelListener, pollMessagingContextChannel);
                                                }
                                                catch (Exception e)
                                                {
                                                    Logger.LogError(e, $"{nameof(OnStartAsync)}: {messagingContextChannel.Interactions}");
                                                }
                                            }
                                            else if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.AsynchronousRequestResponse))
                                            {
                                                try
                                                {
                                                    AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> asyncRequestResponseMessagingContextChannel =
                                                        messagingContextChannel as AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext>;

                                                    await this.AsynchronousRequestResponseController.AcceptChannelAsync(
                                                        (protocolContext) => (messagingChannelListener as IBindingContextMapper<MessageContext>).MapToBindingContext(
                                                            protocolContext.InputContext, 
                                                            protocolContext.BindingContext), 
                                                        asyncRequestResponseMessagingContextChannel);
                                                }
                                                catch (Exception e)
                                                {
                                                    Logger.LogError(e, $"{nameof(OnStartAsync)}: {messagingContextChannel.Interactions}");
                                                }
                                            }
                                            else if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.RequestResponse))
                                            {
                                                try
                                                {
                                                    AbstractRequestResponseMessagingContextChannel<MessageContext> requestResponseMessagingContextChannel =
                                                        messagingContextChannel as AbstractRequestResponseMessagingContextChannel<MessageContext>;

                                                    await this.RequestResponseController.AcceptChannelAsync(
                                                        (protocolContext) => 
                                                            messagingChannelListener.MapToBindingContext(
                                                                protocolContext.InputContext, 
                                                                protocolContext.BindingContext), 
                                                        requestResponseMessagingContextChannel);
                                                }
                                                catch (Exception e)
                                                {
                                                    Logger.LogError(e, $"{nameof(OnStartAsync)}: {messagingContextChannel.Interactions}");
                                                }
                                            }
                                            else
                                            {
                                                Logger.LogError(new NotSupportedException(messagingContextChannel.Interactions.ToString()), $"{nameof(OnStartAsync)}: {nameof(IReceiveMessagingContextChannel<MessageContext>)} Interaction is not currently supported.");
                                            }
                                        }
                                    }
                            });
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });

                IEnumerable<Task<IMessagingContextChannelListener<MessageContext>>> startingTasks =
                    await startTasks.WaitUntilReachedCapacity();

                IEnumerable<Task<IMessagingContextChannelListener<MessageContext>>>
                    completedStartTasks = await startingTasks.WhenAllTasks();

                completedStartTasks.ForEach(t =>
                {
                    t.IsCompletedSuccessfully((o, e) => base.Logger.LogError(e, $"StartListeningAsync faulted for {o}."));
                });

                startTasks.Dispose();

                this.startTaskCompletionSource.SetResult(VoidType.Void);
            }

            await this.startTaskCompletionSource.Task;
        }

        IMessagingContext<MessageContext> IProtocolContextFactory<IMessagingContext<MessageContext>>.Create(IFeatureCollection contextFeatures)
        {
            return this.Create(contextFeatures);
        }

        protected virtual IMessagingContext<MessageContext> Create(IFeatureCollection contextFeatures)
        {
            base.ThrowIfDisposed();

            if (this.ReceivedProtocolMessagingContextQueue.Count > 0)
            {
                (IMessagingContext<MessageContext> receivedProtocolMessagingContext, Action onDequeued) =
                    this.ReceivedProtocolMessagingContextQueue.Take();

                Logger.LogTrace($"{nameof(Create)}: {receivedProtocolMessagingContext.BindingContext.InteractionContext.TraceIdentifier}");

                onDequeued();

                return receivedProtocolMessagingContext;
            }

            CancellationToken.ThrowIfCancellationRequested();

            return Task.Run<IMessagingContext<MessageContext>>(() =>
            {
                (IMessagingContext<MessageContext> receivedProtocolMessagingContext, Action onDequeued) =
                    this.ReceivedProtocolMessagingContextQueue.Take(CancellationToken);

                Logger.LogTrace($"{nameof(Create)}: {receivedProtocolMessagingContext.BindingContext.InteractionContext.TraceIdentifier}");

                onDequeued();

                return receivedProtocolMessagingContext;

            }).GetAwaiter().GetResult();
        }

        protected override Task InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext)
        {
            Logger.LogTrace($"{nameof(InvokeMessagingContextCallbackAsync)}: {messagingContext.BindingContext.InteractionContext.TraceIdentifier}");

            if (messagingContext.BindingContext.ConnectionContext.Items.TryGetValue(out IMessagingContextChannel<MessageContext> messagingContextChannel))
            {
                if (!messagingContextChannel.IsOpen)
                {
                    base.Logger.LogError(new InvalidOperationException("Channel is not open."), $"{nameof(InvokeMessagingContextCallbackAsync)}: {messagingContextChannel.ToString()} is not open and cannot be used.");
                }
                else if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.Poll))
                {
                    try
                    {
                        AbstractPollMessagingContextChannel<MessageContext> pollMessagingContextChannel =
                            messagingContextChannel as AbstractPollMessagingContextChannel<MessageContext>;

                        pollMessagingContextChannel.HandledMessagingCallBackAsync(messagingContext);

                        pollMessagingContextChannel.AcknowledgeReceivedMessagingContextAsync(messagingContext.OutputContext);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, $"{nameof(InvokeMessagingContextCallbackAsync)}: {messagingContext.BindingContext.InteractionContext.TraceIdentifier}");
                    }
                }
                else if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.AsynchronousRequestResponse))
                {
                    try
                    {
                        AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> asyncRequestResponseMessagingContextChannel =
                            messagingContextChannel as AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext>;

                        asyncRequestResponseMessagingContextChannel.HandledMessagingCallBackAsync(messagingContext);

                        asyncRequestResponseMessagingContextChannel.SendMessagingContextAsync(messagingContext.OutputContext);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, $"{nameof(InvokeMessagingContextCallbackAsync)}: {messagingContext.BindingContext.InteractionContext.TraceIdentifier}");
                    }
                }
                else if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.Received))
                {
                    // RequestResponse channel switches to a Received channel for the individual request ...

                    try
                    {
                        IReceivedMessagingContextChannel<MessageContext> receivedMessagingContextChannel = 
                            messagingContextChannel as IReceivedMessagingContextChannel<MessageContext>;

                        receivedMessagingContextChannel.HandledMessagingCallBackAsync(messagingContext);

                        receivedMessagingContextChannel.SendMessagingContextAsync(messagingContext.OutputContext);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, $"{nameof(InvokeMessagingContextCallbackAsync)}: {messagingContext.BindingContext.InteractionContext.TraceIdentifier}");
                    }
                }
                else
                {
                    Logger.LogError(new NotSupportedException(messagingContextChannel.Interactions.ToString()), $"CallbackProtocolContextAsync: {nameof(IReceiveMessagingContextChannel<MessageContext>)}::nameof(IMessagingContextChannel<MessageContext>.Interactions) is not currently supported.");
                }
            }

            return Task.CompletedTask;
        }

        void IProtocolContextFactory<IMessagingContext<MessageContext>>.Dispose(IMessagingContext<MessageContext> context)
        {
            OnDispose(context);
        }

        protected virtual void OnDispose(IMessagingContext<MessageContext> context)
        {
            context?.Dispose();
        }

        void IProtocolContextFactory<IMessagingContext<MessageContext>>.Dispose(IMessagingContext<MessageContext> context, Exception exception)
        {
            OnDispose(context, exception);
        }

        protected virtual void OnDispose(IMessagingContext<MessageContext> context, Exception exception)
        {
            context?.Dispose();
        }

        protected override void OnDispose()
        {
            if (messagingChannelListeners != null)

                messagingChannelListeners.ForEach(messagingChannelListener => messagingChannelListener.Dispose());
        }
    }

    /// <summary>
    /// Base class to implement a messaging context receiver that binds a protocol context to a channel.
    /// <see cref="MessageContext"/> must expose a parameterless constructor which produces a NULL messaging context instance, 
    /// and a constructor which accepts an <see cref="Exception"/> which produces a FAULT messaging context instance.
    /// </summary>
    /// <typeparam name="ProtocolContext"></typeparam>
    /// <typeparam name="MessageContext"></typeparam>
    public abstract class BaseMessagingContextReceiver<ProtocolContext, MessageContext> :
        AbstractMessagingContextReceiver<MessageContext>,
        IMessagingContextReceiver<ProtocolContext, MessageContext>
        where MessageContext: IMessageContext
    {
        private TaskCompletionSource<VoidType> startTaskCompletionSource;
        private IEnumerable<IMessagingContextChannelListener<ProtocolContext, MessageContext>> messagingChannelListeners;
        private BlockingCollection<ProtocolContext> receivedProtocolContextQueue;

        public BaseMessagingContextReceiver(IApplicationHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider services)
            : base(hostEnvironment, hostApplicationLifetime, services)
        {
            this.receivedProtocolContextQueue = new BlockingCollection<ProtocolContext>();
        }

        protected abstract void RegisterListenAddresses(IEnumerable<string> addresses);

        protected abstract void UnregisterListenAddresses(IEnumerable<string> addresses);
        
        protected abstract IProtocolContextAccessor<IMessagingContext<ProtocolContext>> ProtocolContextAccessor { get; }

        protected BlockingCollection<ProtocolContext> ReceivedProtocolContextQueue { get => receivedProtocolContextQueue; }

        protected override async Task OnStartAsync(IEnumerable<String> serverAddresses)
        {
            if (Interlocked.CompareExchange(ref this.startTaskCompletionSource, new TaskCompletionSource<VoidType>(), null) == null)
            {
                this.messagingChannelListeners =
                    this.Services.GetServices<IMessagingContextChannelListener<ProtocolContext, MessageContext>>();

                int listenerCount = this.messagingChannelListeners.Count();

                AwaitableBlockingCollection<Task<IMessagingContextChannelListener<ProtocolContext, MessageContext>>> startTasks =
                    new AwaitableBlockingCollection<Task<IMessagingContextChannelListener<ProtocolContext, MessageContext>>>(listenerCount);

                _ = Task.Run(() => {
                    try
                    {
                        ParallelOptions po = new ParallelOptions();
                        po.CancellationToken = base.CancellationToken;
                        po.MaxDegreeOfParallelism = listenerCount;

                        Parallel.ForEach(this.messagingChannelListeners, po, async messagingChannelListener => {

                            messagingChannelListener.Init(HostEnvironment, serverAddresses, Services, base.CancellationToken);

                            Task<IMessagingContextChannelListener<ProtocolContext, MessageContext>> startTask = messagingChannelListener.StartListeningAsync().CompleteWith(messagingChannelListener);

                            startTasks.TryAdd(startTask);

                            _ = startTask.ContinueWith(async t =>
                            {
                                if (t.IsCompletedSuccessfully(out _, out _))
                                {
                                    this.RegisterListenAddresses(messagingChannelListener.ListenAddresses);

                                    while (!base.CancellationToken.IsCancellationRequested)
                                    {
                                        (bool success, ProtocolContext protocolContext) = await messagingChannelListener.TryReceiveContext();

                                        if (success)
                                        {
                                            this.receivedProtocolContextQueue.Add(protocolContext);
                                        }
                                    }
                                }
                            });

                            await startTask;
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });

                IEnumerable<Task<IMessagingContextChannelListener<ProtocolContext, MessageContext>>> startingTasks =
                    await startTasks.WaitUntilReachedCapacity();

                IEnumerable<Task<IMessagingContextChannelListener<ProtocolContext, MessageContext>>>
                    completedStartTasks = await startingTasks.WhenAllTasks();

                var startedTasks = completedStartTasks.Where(t =>
                {
                    return (t.IsCompletedSuccessfully((o, e) => base.Logger.LogError(e, $"StartListeningAsync faulted for {o}.")));
                });

                List<String> listeningAddresses = new List<string>();

                startedTasks.ForEach(t => t.Result.ListenAddresses.Aggregate(listeningAddresses, (l, a) => { l.Add(a); return l; }));

                this.UnregisterListenAddresses(serverAddresses.Except(listeningAddresses));

                startTasks.Dispose();

                this.startTaskCompletionSource.SetResult(new VoidType());
            }

            await this.startTaskCompletionSource.Task;
        }


        IMessagingContext<ProtocolContext> IProtocolContextFactory<IMessagingContext<ProtocolContext>>.Create(IFeatureCollection contextFeatures) =>
            this.Create(contextFeatures);

        protected abstract IMessagingContext<ProtocolContext> Create(IFeatureCollection contextFeatures);

        void IProtocolContextAccessorFactory<IMessagingContext<ProtocolContext>>.RegisterListenAddresses(ICollection<string> addresses)
        {
            this.RegisterListenAddresses(addresses);
        }

        void IProtocolContextAccessorFactory<IMessagingContext<ProtocolContext>>.GetProtocolContextAccessor(out IProtocolContextAccessor<IMessagingContext<ProtocolContext>> protocolContextAccessor)
        {
            protocolContextAccessor = this.ProtocolContextAccessor;
        }

        Task<bool> IMessagingContextReceiver<ProtocolContext, MessageContext>.TryBindToChannelAsync(IMessagingContext<ProtocolContext> protocolContext)
        {
            return TryBindToChannelAsync(protocolContext);
        }

        protected virtual async Task<bool> TryBindToChannelAsync(IMessagingContext<ProtocolContext> protocolContext)
        {
            if (CancellationToken.IsCancellationRequested)

                return false;

            IMessagingContextChannel<MessageContext> messagingContextChannel =
                await this.TryBindToChannel(protocolContext);

            if (messagingContextChannel == null)
            {
                base.Logger.LogError(new NullReferenceException(nameof(messagingContextChannel)), "There is no channel configured that can bind to the requested endpoint.");

                return false;
            }

            if (!messagingContextChannel.IsOpen)
            {
                base.Logger.LogError(new InvalidOperationException($"{nameof(messagingContextChannel)} is not open."), $"{messagingContextChannel.ToString()} is not open and cannot be used.");

                return false;
            }

            // the protocolContext will be disposed after binding;
            // we prepare a new bindingContext that will persist and be applied to each messaging context in the callback below.

            BindingContext bindingContext = new BindingContext();

            bindingContext.TryApply(protocolContext.BindingContext);

            if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.AsynchronousRequestResponse))
            {
                AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> asyncRequestResponseMessagingContextChannel =
                    messagingContextChannel as AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext>;

                try
                {
                    Logger.LogTrace($"->{nameof(TryBindToChannelAsync)}: {protocolContext.CorrelationId}");

                    await this.AsynchronousRequestResponseController.AcceptChannelAsync(
                        (messagingContext) =>
                        {
                            // Once we've accepted the channel, each subsequent messagingContext received will be processed by this callback.
                            // we need to apply the binding context to it.
                            return messagingContext.BindingContext.TryApply(bindingContext);
                        },
                        asyncRequestResponseMessagingContextChannel);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"{nameof(ReceiveMessagingContextAsync)}: {protocolContext.CorrelationId}");

                    return false;
                }
            }
            else if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.RequestResponse))
            {
                try
                {
                    Logger.LogTrace($"->{nameof(TryBindToChannelAsync)}: {protocolContext.CorrelationId}");

                    AbstractRequestResponseMessagingContextChannel<MessageContext> receiveMessagingContextChannel =
                        messagingContextChannel as AbstractRequestResponseMessagingContextChannel<MessageContext>;

                    await this.RequestResponseController.AcceptChannelAsync(
                        (messagingContext) =>
                        {
                            return messagingContext.BindingContext.TryApply(bindingContext);
                        },
                        receiveMessagingContextChannel);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"{nameof(ReceiveMessagingContextAsync)}: {protocolContext.CorrelationId}");

                    return false;
                }
            }
            else
            {
                Logger.LogError(
                    new NotImplementedException(messagingContextChannel.Interactions.ToString()), 
                    $"{nameof(ReceiveMessagingContextAsync)}: {protocolContext.CorrelationId}");
            }

            return true;
        }

        Task<IMessagingContext<MessageContext>> IMessagingContextReceiver<ProtocolContext, MessageContext>.ReceiveMessagingContextAsync()
        {
            return ReceiveMessagingContextAsync();
        }

        protected virtual Task<IMessagingContext<MessageContext>> ReceiveMessagingContextAsync()
        {
            base.ThrowIfDisposed();

            if (this.ReceivedProtocolMessagingContextQueue == null)
            {
                return Task.FromResult((IMessagingContext<MessageContext>)null);
            }

            if (this.ReceivedProtocolMessagingContextQueue.Count > 0)
            {
                (IMessagingContext<MessageContext> messagingContext, Action onAcknowledgeReceive) = 
                    this.ReceivedProtocolMessagingContextQueue.Take();

                onAcknowledgeReceive();

                return Task.FromResult(messagingContext);
            }

            base.CancellationToken.ThrowIfCancellationRequested();

            return Task.Run<IMessagingContext<MessageContext>>(() =>
            {
                (IMessagingContext<MessageContext> messagingContext, Action onAcknowledgeReceive) =
                    this.ReceivedProtocolMessagingContextQueue.Take(CancellationToken);

                onAcknowledgeReceive();

                return Task.FromResult(messagingContext);
            });
        }

        private async Task<IMessagingContextChannel<MessageContext>> TryBindToChannel(IMessagingContext<ProtocolContext> protocolContext)
        {
            IMessagingContextChannel<MessageContext>[] messagingContextChannels = 
                await Task.WhenAll(messagingChannelListeners.Select(
                    async l =>
                    {
                        (bool? success, IMessagingContextChannel<MessageContext> messagingContextChannel) tryAcceptChannelResult = 
                            await l.TryAcceptMessagingContextChannelAsync(protocolContext.InputContext);

                        if (tryAcceptChannelResult.success == true)
                        {
                            l.MapToBindingContext(protocolContext, protocolContext.BindingContext);

                            tryAcceptChannelResult.messagingContextChannel.MapConnection(protocolContext.BindingContext.ConnectionContext);

                            return tryAcceptChannelResult.messagingContextChannel;
                        }

                        return null;
                    }));

            IMessagingContextChannel<MessageContext> messagingContextChannel =
                messagingContextChannels.OrderBy(c => c == null ? 1 : 0).FirstOrDefault();

            return messagingContextChannel;
        }

        protected override async Task InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext)
        {
            Console.WriteLine($"Output: {messagingContext}");

            if (messagingContext.BindingContext.ConnectionContext.Items.TryGetValue<AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext>>(out AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> asyncReceiveMessagingContextChannel))
            {
                await asyncReceiveMessagingContextChannel.SendMessagingContextAsync(messagingContext.OutputContext);
            }
            else if (messagingContext.BindingContext.ConnectionContext.Items.TryGetValue<IReceivedMessagingContextChannel<MessageContext>>(out IReceivedMessagingContextChannel<MessageContext> receivedMessagingContextChannel))
            {
                await receivedMessagingContextChannel.SendMessagingContextAsync(messagingContext.OutputContext);
            }
            else
            {
                await InvokeProtocolMessagingContextCallbackAsync(messagingContext);
            }
        }

        protected abstract Task InvokeProtocolMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext);

        void IProtocolContextFactory<IMessagingContext<ProtocolContext>>.Dispose(IMessagingContext<ProtocolContext> protocolContext) =>
            this.Dispose(protocolContext);

        protected abstract void Dispose(IMessagingContext<ProtocolContext> protocolContext);

        void IProtocolContextFactory<IMessagingContext<ProtocolContext>>.Dispose(IMessagingContext<ProtocolContext> protocolContext, Exception e) =>
            this.Dispose(protocolContext, e);

        protected abstract void Dispose(IMessagingContext<ProtocolContext> protocolContext, Exception e);

        protected override void OnDispose()
        {
        }
    }

    /// <summary>
    /// Base class to implement a messaging context receiver that binds a protocol hosting context to a channel.
    /// <see cref="MessageContext"/> must expose a parameterless constructor which produces a NULL messaging context instance, 
    /// and a constructor which accepts an <see cref="Exception"/> which produces a FAULT messaging context instance.
    /// </summary>
    /// <typeparam name="ProtocolHostContext"></typeparam>
    /// <typeparam name="ProtocolContext"></typeparam>
    /// <typeparam name="MessageContext"></typeparam>
    public abstract class BaseMessagingContextReceiver<ProtocolHostContext, ProtocolContext, MessageContext> :
        AbstractMessagingContextReceiver<MessageContext>,
        IMessagingContextReceiver<ProtocolHostContext, ProtocolContext, MessageContext>
        where ProtocolHostContext: IProtocolContextHost<ProtocolContext>
        where MessageContext : IMessageContext
    {
        private TaskCompletionSource<VoidType> startTaskCompletionSource;
        private IEnumerable<IMessagingContextChannelListener<ProtocolHostContext, ProtocolContext, MessageContext>> messagingChannelListeners;

        public BaseMessagingContextReceiver(IApplicationHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider services)
            : base(hostEnvironment, hostApplicationLifetime, services)
        {
        }

        protected abstract IProtocolContextAccessor<ProtocolHostContext> ProtocolContextAccessor { get; }

        protected override async Task OnStartAsync(IEnumerable<String> serverAddresses)
        {
            if (Interlocked.CompareExchange(ref this.startTaskCompletionSource, new TaskCompletionSource<VoidType>(), null) == null)
            {
                this.messagingChannelListeners =
                    this.Services.GetServices<IMessagingContextChannelListener<ProtocolHostContext, ProtocolContext, MessageContext>>();

                int listenerCount = this.messagingChannelListeners.Count();

                AwaitableBlockingCollection<Task<IMessagingContextChannelListener<ProtocolHostContext, ProtocolContext, MessageContext>>> startTasks =
                    new AwaitableBlockingCollection<Task<IMessagingContextChannelListener<ProtocolHostContext, ProtocolContext, MessageContext>>>(listenerCount);

                _ = Task.Run(() => {
                    try
                    {
                        ParallelOptions po = new ParallelOptions();
                        po.CancellationToken = base.CancellationToken;
                        po.MaxDegreeOfParallelism = listenerCount;

                        Parallel.ForEach(this.messagingChannelListeners, po, messagingChannelListener => {

                            messagingChannelListener.Init(this.HostEnvironment, serverAddresses, this.Services, base.CancellationToken);

                            Task<IMessagingContextChannelListener<ProtocolHostContext, ProtocolContext, MessageContext>> startTask =
                                messagingChannelListener.StartListeningAsync().CompleteWith(messagingChannelListener);

                            startTasks.TryAdd(startTask);
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });

                IEnumerable<Task<IMessagingContextChannelListener<ProtocolHostContext, ProtocolContext, MessageContext>>> startingTasks =
                    await startTasks.WaitUntilReachedCapacity();

                IEnumerable<Task<IMessagingContextChannelListener<ProtocolHostContext, ProtocolContext, MessageContext>>>
                    completedStartTasks = await startingTasks.WhenAllTasks();

                var startedTasks = completedStartTasks.Where(t =>
                {
                    return (t.IsCompletedSuccessfully((o, e) => base.Logger.LogError(e, $"StartListeningAsync faulted for {o}.")));
                });

                List<String> listeningAddresses = new List<string>();

                startedTasks.ForEach(t => t.Result.ListenAddresses.Aggregate(listeningAddresses, (l, a) => { l.Add(a); return l; }));

                this.UnregisterListenAddresses(serverAddresses.Except(listeningAddresses));

                startTasks.Dispose();

                this.startTaskCompletionSource.SetResult(new VoidType());
            }

            await startTaskCompletionSource.Task;
        }

        ProtocolHostContext IProtocolContextFactory<ProtocolHostContext>.Create(IFeatureCollection contextFeatures) =>
            this.Create(contextFeatures);

        private ProtocolHostContext Create(IFeatureCollection contextFeatures)
        {
            ProtocolHostContext protocolContextHost = this.OnCreate(contextFeatures);

            this.ProtocolContextAccessor.SetProtocolContextAsync(protocolContextHost);

            return protocolContextHost;
        }

        protected abstract ProtocolHostContext OnCreate(IFeatureCollection contextFeatures);

        void IProtocolContextAccessorFactory<ProtocolHostContext>.RegisterListenAddresses(ICollection<string> addresses)
        {
            this.RegisterListenAddresses(addresses);
        }

        protected abstract void RegisterListenAddresses(IEnumerable<string> addresses);

        protected abstract void UnregisterListenAddresses(IEnumerable<string> addresses);

        void IProtocolContextAccessorFactory<ProtocolHostContext>.GetProtocolContextAccessor(out IProtocolContextAccessor<ProtocolHostContext> protocolContextAccessor)
        {
            protocolContextAccessor = this.ProtocolContextAccessor;
        }

        Task<bool> IMessagingContextReceiver<ProtocolHostContext, ProtocolContext, MessageContext>.TryBindToChannelAsync(ProtocolHostContext protocolContextHost)
        {
            return TryBindToChannelAsync(protocolContextHost);
        }

        protected virtual async Task<bool> TryBindToChannelAsync(ProtocolHostContext protocolContextHost)
        {
            if (base.CancellationToken.IsCancellationRequested)

                return false;

            BindingContext bindingContext = new BindingContext();

            // The bindingContext will be populated if a channel is found and accepted (and is returned not null)

            IMessagingContextChannel<MessageContext> messagingContextChannel =
                await this.TryBindToChannel(protocolContextHost, bindingContext);

            if (messagingContextChannel == null)
            {
                base.Logger.LogError(new NullReferenceException(nameof(messagingContextChannel)), "There is no channel configured that can bind to the requested endpoint.");

                return false;
            }

            if (!messagingContextChannel.IsOpen)
            {
                base.Logger.LogError(new InvalidOperationException($"{nameof(messagingContextChannel)} is not open."), $"{messagingContextChannel.ToString()} is not open and cannot be used.");

                return false;
            }

            string connectionId = bindingContext.ConnectionContext.ConnectionId;

            if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.AsynchronousRequestResponse))
            {
                AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> asyncRequestResponseMessagingContextChannel =
                    messagingContextChannel as AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext>;

                try
                {
                    Logger.LogTrace($"->{nameof(TryBindToChannelAsync)}: {connectionId}");

                    await this.AsynchronousRequestResponseController.AcceptChannelAsync(
                        (protocolContext) =>
                        {
                            return protocolContext.BindingContext.TryApply(bindingContext);
                        }, 
                        asyncRequestResponseMessagingContextChannel);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"{nameof(TryBindToChannelAsync)}: {connectionId}");

                    return false;
                }

                Logger.LogTrace($"<-{nameof(TryBindToChannelAsync)}: {connectionId}");
            }
            else if (messagingContextChannel.Interactions.IsEqual(MessagingChannelInteractions.RequestResponse))
            {
                try
                {
                    Logger.LogTrace($"->{nameof(TryBindToChannelAsync)}: {connectionId}");

                    AbstractRequestResponseMessagingContextChannel<MessageContext> requestResponseMessagingContextChannel =
                        messagingContextChannel as AbstractRequestResponseMessagingContextChannel<MessageContext>;

                    await this.RequestResponseController.AcceptChannelAsync(
                        (protocolContext) => protocolContext.BindingContext.TryApply(bindingContext), 
                        requestResponseMessagingContextChannel);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"{nameof(TryBindToChannelAsync)}: {connectionId}");

                    return false;
                }

                Logger.LogTrace($"<-{nameof(TryBindToChannelAsync)}: {connectionId}");
            }
            else
            {
                Logger.LogError(
                    new NotImplementedException(messagingContextChannel.Interactions.ToString()),
                    $"{nameof(TryBindToChannelAsync)}: {connectionId}");
            }

            return true;
        }

        Task<IMessagingContext<MessageContext>> IMessagingContextReceiver<ProtocolHostContext, ProtocolContext, MessageContext>.ReceiveMessagingContextAsync(TimeSpan timeout)
        {
            return ReceiveProtocolMessagingContextAsync(timeout);
        }

        protected virtual Task<IMessagingContext<MessageContext>> ReceiveProtocolMessagingContextAsync(TimeSpan timeout)
        {
            base.ThrowIfDisposed();

            if (this.ReceivedProtocolMessagingContextQueue == null)
            {
                return Task.FromResult((IMessagingContext<MessageContext>)null);
            }

            if (this.ReceivedProtocolMessagingContextQueue.Count > 0)
            {
                IMessagingContext<MessageContext> messagingContext = 
                    TryTakeProtocolMessagingContext(timeout);

                return Task.FromResult(messagingContext);
            }

            return Task.Run<IMessagingContext<MessageContext>>(() =>
            {
                IMessagingContext<MessageContext> messagingContext =
                    TryTakeProtocolMessagingContext(timeout);

                return messagingContext;
            });
        }

        private IMessagingContext<MessageContext> TryTakeProtocolMessagingContext(TimeSpan timeout)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout);

                if (this.ReceivedProtocolMessagingContextQueue == null || this.ReceivedProtocolMessagingContextQueue.IsCompleted)
                {
                    return null;
                }

                (IMessagingContext<MessageContext> messagingContext, Action onAcknowledgeReceived) =
                    this.ReceivedProtocolMessagingContextQueue.Take(cancellationTokenSource.Token);

                onAcknowledgeReceived();

                return messagingContext;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (InvalidOperationException e)
            {
                if (e.Message == "The collection argument is empty and has been marked as complete with regards to additions.")
                
                    return null;

                throw e;
            }
        }

        private async Task<IMessagingContextChannel<MessageContext>> TryBindToChannel(ProtocolHostContext protocolContextHost, BindingContext bindingContext)
        {
            IMessagingContextChannel<MessageContext>[] messagingContextChannels =
                await Task.WhenAll(
                    this.messagingChannelListeners.Select(
                    async l =>
                    {
                        (bool? success, IMessagingContextChannel<MessageContext> messagingContextChannel) tryAcceptChannelResult =
                            await l.TryAcceptMessagingContextChannelAsync(protocolContextHost);

                        if (tryAcceptChannelResult.success == true)
                        {
                            l.MapToBindingContext(protocolContextHost, bindingContext);

                            tryAcceptChannelResult.messagingContextChannel.MapConnection(bindingContext.ConnectionContext);

                            return tryAcceptChannelResult.messagingContextChannel;
                        }

                        return null;
                    }));

            IMessagingContextChannel<MessageContext> messagingContextChannel =
                messagingContextChannels.OrderBy(c => c == null ? 1 : 0).FirstOrDefault();

            return messagingContextChannel;
        }

        protected override async Task InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext)
        {
            Console.WriteLine($"Output: {messagingContext}");

            if (messagingContext.BindingContext.ConnectionContext.Items.TryGetValue<AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext>>(out AbstractAsynchronousRequestResponseMessagingContextChannel<MessageContext> asyncReceiveMessagingContextChannel))
            {
                await asyncReceiveMessagingContextChannel.SendMessagingContextAsync(messagingContext.OutputContext);
            }
            else if (messagingContext.BindingContext.ConnectionContext.Items.TryGetValue<IReceivedMessagingContextChannel<MessageContext>>(out IReceivedMessagingContextChannel<MessageContext> receivedMessagingContextChannel))
            {
                await receivedMessagingContextChannel.SendMessagingContextAsync(messagingContext.OutputContext);
            }
            else
            {
                await InvokeProtocolMessagingContextCallbackAsync(messagingContext);
            }
        }

        protected abstract Task InvokeProtocolMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext);

        void IProtocolContextFactory<ProtocolHostContext>.Dispose(ProtocolHostContext context) =>
            this.Dispose(context);

        protected abstract void Dispose(ProtocolHostContext context);

        void IProtocolContextFactory<ProtocolHostContext>.Dispose(ProtocolHostContext context, Exception e) =>
            this.Dispose(context, e);

        protected abstract void Dispose(ProtocolHostContext context, Exception e);

        protected override void OnDispose()
        {
        }
    }
}
