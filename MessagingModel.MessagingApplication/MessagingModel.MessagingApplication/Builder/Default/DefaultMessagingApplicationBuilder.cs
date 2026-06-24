using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

using AllVerge.MessagingModel.MessagingApplication.Hosting;
using Microsoft.Extensions.Primitives;
using AllVerge.SystemPrimitives.Collections;
using System.Xml.Xsl;

namespace AllVerge.MessagingModel.MessagingApplication.Builder.Default
{
    using AVLogging =  AllVerge.SystemPrimitives.Logging;

    public abstract class AbstractMessagingApplicationBuilder<MessageContext> :
        IAbstractMessagingApplicationBuilder<MessageContext>
    {
        protected readonly IList<Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>>> _protocolMessagingContextMiddlewareComponents =
            new List<Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>>>();

        protected AbstractMessagingApplicationBuilder(object serverFeatures, IServiceProvider applicationServices, ILogger logger, DiagnosticListener diagnosticListener)
        {
            Properties = new Dictionary<string, object>(StringComparer.Ordinal);
            SetProperty(Constants.BuilderProperties.ServerFeatures, serverFeatures);
            SetProperty(Constants.BuilderProperties.ApplicationServices, applicationServices);
            SetProperty(Constants.BuilderProperties.Logger, AVLogging.Logger.GetInstance(this.GetType()));
            SetProperty(Constants.BuilderProperties.DiagnosticLogger, logger);
            SetProperty(Constants.BuilderProperties.DiagnosticListener, diagnosticListener);
        }

        protected AbstractMessagingApplicationBuilder(AbstractMessagingApplicationBuilder<MessageContext> builder)
        {
            Properties = new CopyOnWriteDictionary<string, object>(builder.Properties, StringComparer.Ordinal);
        }

        public virtual IFeatureCollection ServerFeatures => GetProperty<IFeatureCollection>(Constants.BuilderProperties.ServerFeatures);

        public IServiceProvider ApplicationServices
        {
            set => SetProperty(Constants.BuilderProperties.ApplicationServices, value);
            get => GetProperty<IServiceProvider>(Constants.BuilderProperties.ApplicationServices);
        }

        public AVLogging.Logger Logger
        {
            set => SetProperty(Constants.BuilderProperties.Logger, value);
            get => GetProperty<AVLogging.Logger>(Constants.BuilderProperties.Logger);
        }

        public ILogger DiagnosticLogger
        {
            set => SetProperty(Constants.BuilderProperties.DiagnosticLogger, value);
            get => GetProperty<ILogger>(Constants.BuilderProperties.DiagnosticLogger);
        }

        public DiagnosticListener DiagnosticListener
        {
            set => SetProperty(Constants.BuilderProperties.DiagnosticListener, value);
            get => GetProperty<DiagnosticListener>(Constants.BuilderProperties.DiagnosticListener);
        }

        public IDictionary<string, object> Properties
        {
            get;
        }

        protected T GetProperty<T>(string key)
        {
            if (!Properties.TryGetValue(key, out object value))
            {
                return default(T);
            }
            return (T)value;
        }

        protected void SetProperty<T>(string key, T value)
        {
            Properties[key] = value;
        }

        Task IAbstractMessagingApplicationBuilder<MessageContext>.InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext)
        {
            return InvokeMessagingContextCallbackAsync(messagingContext);
        }

        protected abstract Task InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext);

        Task IAbstractMessagingApplicationBuilder<MessageContext>.PrepareRejectedMessagingContextAsync(IMessagingContext<MessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders)
        {
            return PrepareRejectedMessagingContextAsync(messagingContext, rejectionCode, rejectionHeaders);
        }

        protected abstract Task PrepareRejectedMessagingContextAsync(IMessagingContext<MessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders = null);

        /// <summary>
        /// Dictionary that reads the sourceDictionary as the innerDictionary provided nothing is written; 
        /// on the first write however, the innerDictionary is swapped with a copy of the source dictionary, which is used from then on.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        class CopyOnWriteDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
        {
            private readonly IDictionary<TKey, TValue> _sourceDictionary;

            private readonly IEqualityComparer<TKey> _comparer;

            private IDictionary<TKey, TValue> _innerDictionary;

            private IDictionary<TKey, TValue> ReadDictionary => _innerDictionary ?? _sourceDictionary;

            private IDictionary<TKey, TValue> WriteDictionary
            {
                get
                {
                    if (_innerDictionary == null)
                    {
                        _innerDictionary = new Dictionary<TKey, TValue>(_sourceDictionary, _comparer);
                    }
                    return _innerDictionary;
                }
            }

            public virtual ICollection<TKey> Keys => ReadDictionary.Keys;

            public virtual ICollection<TValue> Values => ReadDictionary.Values;

            public virtual int Count => ReadDictionary.Count;

            public virtual bool IsReadOnly => false;

            public virtual TValue this[TKey key]
            {
                get
                {
                    return ReadDictionary[key];
                }
                set
                {
                    WriteDictionary[key] = value;
                }
            }

            public CopyOnWriteDictionary(IDictionary<TKey, TValue> sourceDictionary, IEqualityComparer<TKey> comparer)
            {
                if (sourceDictionary == null)
                {
                    throw new ArgumentNullException("sourceDictionary");
                }
                if (comparer == null)
                {
                    throw new ArgumentNullException("comparer");
                }
                _sourceDictionary = sourceDictionary;
                _comparer = comparer;
            }

            public virtual bool ContainsKey(TKey key)
            {
                return ReadDictionary.ContainsKey(key);
            }

            public virtual void Add(TKey key, TValue value)
            {
                WriteDictionary.Add(key, value);
            }

            public virtual bool Remove(TKey key)
            {
                return WriteDictionary.Remove(key);
            }

            public virtual bool TryGetValue(TKey key, out TValue value)
            {
                return ReadDictionary.TryGetValue(key, out value);
            }

            public virtual void Add(KeyValuePair<TKey, TValue> item)
            {
                WriteDictionary.Add(item);
            }

            public virtual void Clear()
            {
                WriteDictionary.Clear();
            }

            public virtual bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return ReadDictionary.Contains(item);
            }

            public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            {
                ReadDictionary.CopyTo(array, arrayIndex);
            }

            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                return WriteDictionary.Remove(item);
            }

            public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return ReadDictionary.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }

    public class DefaultMessagingApplicationBuilder<MessageContext> :
        AbstractMessagingApplicationBuilder<MessageContext>, 
        IMessagingApplicationBuilder<MessageContext> 
    {
        public DefaultMessagingApplicationBuilder(object serverFeatures, IServiceProvider applicationServices, IMessagingContextReceiver<MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener) :
            base(serverFeatures, applicationServices, logger, diagnosticListener)
        {
            SetProperty(Constants.BuilderProperties.ProtocolMessagingContextReceiver, protocolMessagingContextReceiver);
        }

        public DefaultMessagingApplicationBuilder(DefaultMessagingApplicationBuilder<MessageContext> builder) :
            base(builder)
        {
        }

        public virtual IMessagingContextReceiver<MessageContext> ProtocolMessagingContextReceiver =>
            GetProperty<IMessagingContextReceiver<MessageContext>>(Constants.BuilderProperties.ProtocolMessagingContextReceiver);

        protected override Task PrepareRejectedMessagingContextAsync(IMessagingContext<MessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders = null) => 
            ProtocolMessagingContextReceiver.PrepareRejectedMessagingContextAsync(messagingContext, rejectionCode, rejectionHeaders);

        protected override Task InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext) => 
            ProtocolMessagingContextReceiver.InvokeMessagingContextCallbackAsync(messagingContext);        

        public IMessagingApplicationBuilder<MessageContext> Use(Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>> middlewareComponent)
        {
            _protocolMessagingContextMiddlewareComponents.Add(middlewareComponent);

            return this;
        }

        public virtual IMessagingApplicationBuilder<MessageContext> New()
        {
            return new DefaultMessagingApplicationBuilder<MessageContext>(this);
        }

        public async Task<MessagingContextMiddlewareDelegate<MessageContext>> BuildMessagingContextMiddlewareAsync()
        {
            // Configure final middleware ...

            // Call Protocol messaging context callback if middleware pipeline result is handled (not Nothandled);
            // Otherwise call not handled callback.

            this.Use(next =>
            {
                return async messagingContext =>
                {
                    if (messagingContext != null)
                    {
                        if (messagingContext.Result == MiddlewarePipelineResult.NotHandled)
                        {
                            await this.PrepareRejectedMessagingContextAsync(messagingContext, RejectCode.NotHandled);
                        }
                        await this.InvokeMessagingContextCallbackAsync(messagingContext);
                    }

                    if (next != null)

                        await next(messagingContext);
                };
            });

            MessagingContextDiagnostics<MessageContext> _diagnostics =
                new MessagingContextDiagnostics<MessageContext>(this.DiagnosticLogger, this.DiagnosticListener);

            // Build protocol messaging middleware pipeline in reverse order ...

            MessagingContextMiddlewareDelegate<MessageContext> protocolMessagingContextMiddlewarePipline =
                async messagingContext =>
                {
                    _diagnostics.EndRequest(messagingContext, null);

                    //    _protocolMessagingContextFactory.Dispose(messageHandlerContext);
                    //    _diagnostics.ContextDisposed(context);

                    await Task.CompletedTask;
                };

            foreach (Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>> protocolMessagingContextMiddlewareComponent in
                _protocolMessagingContextMiddlewareComponents.Reverse())
            {
                protocolMessagingContextMiddlewarePipline =
                    protocolMessagingContextMiddlewareComponent(protocolMessagingContextMiddlewarePipline);
            }

            await ProtocolMessagingContextReceiver.StartAsync(ServerFeatures.Get<IServerAddressesFeature>());

            // Add diagnostics begin messagingContext ...

            return async messagingContext =>
            {
                ProtocolMessagingContext<IMessagingContext<MessageContext>>.Current = messagingContext;

                _diagnostics.BeginRequest(messagingContext);

                await protocolMessagingContextMiddlewarePipline(messagingContext);
            };
        }
    }

    public class DefaultMessagingApplicationBuilder<ProtocolContext, MessageContext> :
        AbstractMessagingApplicationBuilder<MessageContext>,
        IMessagingApplicationBuilder<ProtocolContext, MessageContext>
    {
        private readonly IList<Func<ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>, ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>>> _protocolContextMiddlewareComponents = 
            new List<Func<ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>, ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>>>();

        public DefaultMessagingApplicationBuilder(object serverFeatures, IServiceProvider applicationServices, IMessagingContextReceiver<ProtocolContext, MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener) :
            base(serverFeatures, applicationServices, logger, diagnosticListener)
        {
            SetProperty(Constants.BuilderProperties.ProtocolMessagingContextReceiver, protocolMessagingContextReceiver);
        }

        public DefaultMessagingApplicationBuilder(DefaultMessagingApplicationBuilder<ProtocolContext, MessageContext> builder) : 
            base(builder)
        {
        }

        public virtual IMessagingContextReceiver<ProtocolContext, MessageContext> ProtocolMessagingContextReceiver => 
            GetProperty<IMessagingContextReceiver<ProtocolContext, MessageContext>>(Constants.BuilderProperties.ProtocolMessagingContextReceiver);
        
        protected virtual IEnumerable<Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>>> MiddlewareComponents => _protocolMessagingContextMiddlewareComponents;

        Task<bool> IMessagingApplicationBuilder<ProtocolContext, MessageContext>.TryBindToChannelAsync(IMessagingContext<ProtocolContext> protocolContext)
        {
            return TryBindToChannelAsync(protocolContext);
        }

        protected virtual Task<bool> TryBindToChannelAsync(IMessagingContext<ProtocolContext> protocolContext) =>
            ProtocolMessagingContextReceiver.TryBindToChannelAsync(protocolContext);

        Task<IMessagingContext<MessageContext>> IMessagingApplicationBuilder<ProtocolContext, MessageContext>.ReceiveMessagingContextAsync()
        {
            return ReceiveMessagingContextAsync();
        }

        protected virtual Task<IMessagingContext<MessageContext>> ReceiveMessagingContextAsync() =>
            ProtocolMessagingContextReceiver.ReceiveMessagingContextAsync();

        private async Task<IMessagingContext<MessageContext>> PrepareBindingUnreachableMessagingContextAsync(IMessagingContext<ProtocolContext> protocolContext)
        {
            IMessagingContext<MessageContext> messagingContext = new MessagingContext<MessageContext>(protocolContext.BindingContext);

            messagingContext.Items.Add<IMessagingContext<ProtocolContext>>(protocolContext);

            await this.PrepareRejectedMessagingContextAsync(messagingContext, RejectCode.BindingUnreachable);

            return messagingContext;
        }

        protected override Task PrepareRejectedMessagingContextAsync(IMessagingContext<MessageContext> context, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders = null) => 
            ProtocolMessagingContextReceiver.PrepareRejectedMessagingContextAsync(context, rejectionCode, rejectionHeaders);

        protected override Task InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext) => 
            ProtocolMessagingContextReceiver.InvokeMessagingContextCallbackAsync(messagingContext);

        public IMessagingApplicationBuilder<ProtocolContext, MessageContext> Use(Func<ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>, ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>> middlewareComponent)
        {
            _protocolContextMiddlewareComponents.Add(middlewareComponent);

            return this;
        }

        public IMessagingApplicationBuilder<ProtocolContext, MessageContext> Use(Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>> middlewareComponent)
        {
            _protocolMessagingContextMiddlewareComponents.Add(middlewareComponent);

            return this;
        }

        public virtual IMessagingApplicationBuilder<ProtocolContext, MessageContext> New()
        {
            return new DefaultMessagingApplicationBuilder<ProtocolContext, MessageContext>(this);
        }

        Task<ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>> IMessagingApplicationBuilder<ProtocolContext, MessageContext>.BuildContextMiddlewareAsync()
        {
            return BuildContextMiddlewareAsync();
        }

        /// <summary>
        /// Builds the middleware pipeline used by this application to process input protocol binding contexts and returns the head middleware component delegate in the pipeline.
        /// </summary>
        /// <returns></returns>
        protected async Task<ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>> BuildContextMiddlewareAsync()
        {
            // Configure final middleware ...

            // Call Protocol messaging context callback if middleware pipeline result is not Nothandled;
            // otherwise Map messagingContext back to protocolContext and call not handled callback.

            this.Use(next =>
            {
                return async messagingContext =>
                {
                    if (messagingContext != null)
                    {
                        if (messagingContext.Result == MiddlewarePipelineResult.NotHandled)
                        {
                            await this.PrepareRejectedMessagingContextAsync(messagingContext, RejectCode.NotHandled);
                        }
                        await InvokeMessagingContextCallbackAsync(messagingContext);
                    }

                    if (next != null)

                        await next(messagingContext);
                };
            });

            MessagingContextDiagnostics<MessageContext> _diagnostics =
                new MessagingContextDiagnostics<MessageContext>(this.DiagnosticLogger, this.DiagnosticListener);

            // Build protocol messaging middleware pipeline in reverse order ...

            MessagingContextMiddlewareDelegate<MessageContext> protocolMessagingContextMiddlewarePipline =
                async messagingContext =>
            {
                _diagnostics.EndRequest(messagingContext, null);

                //    _protocolMessagingContextFactory.Dispose(messageHandlerContext);
                //    _diagnostics.ContextDisposed(context);

                await Task.CompletedTask;
            };

            foreach (Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>> protocolMessagingContextMiddlewareComponent in 
                _protocolMessagingContextMiddlewareComponents.Reverse())
            {
                protocolMessagingContextMiddlewarePipline = 
                    protocolMessagingContextMiddlewareComponent(protocolMessagingContextMiddlewarePipline);
            }

            // Add diagnostics begin messagingContext ...

            MessagingContextMiddlewareDelegate<MessageContext> headProtocolMessagingContextMiddlewareDelegate = 
                async messagingContext =>
            {
                _diagnostics.BeginRequest(messagingContext);

                await protocolMessagingContextMiddlewarePipline(messagingContext);
            };

            await ProtocolMessagingContextReceiver.StartAsync(ServerFeatures.Get<IServerAddressesFeature>());

            return async (protocolContext) =>
            {
                if (protocolContext != null)
                {
                    // Build protocol binding context middleware pipeline in reverse order;
                    // make last protocol binding context middleware component produce a protocol messaging context and
                    // begin running the protocol messaging context middleware pipeline
                    ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>> headProtocolContextMiddlewareDelegate = async (pc) => 
                    {
                        ProtocolMessagingContext<IMessagingContext<ProtocolContext>>.Current = pc;

                        IMessagingContext<MessageContext> messagingContext = null;

                        if (pc.Result == MiddlewarePipelineResult.NotHandled)
                        {
                            if (await TryBindToChannelAsync(pc))
                            {
                                bool receiving = true;

                                while (receiving)
                                {
                                    messagingContext =
                                        await ReceiveMessagingContextAsync();

                                    if (messagingContext == null)

                                        break;

                                    await headProtocolMessagingContextMiddlewareDelegate(messagingContext);
                                }
                            }
                            else
                            {
                                messagingContext = await this.PrepareBindingUnreachableMessagingContextAsync(pc);

                                await headProtocolMessagingContextMiddlewareDelegate(messagingContext);
                            }
                        }
                    };

                    foreach (Func<ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>, ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>> protocolContextMiddlewareComponent in _protocolContextMiddlewareComponents.Reverse())
                    {
                        headProtocolContextMiddlewareDelegate = protocolContextMiddlewareComponent(headProtocolContextMiddlewareDelegate);
                    }

                    await headProtocolContextMiddlewareDelegate(protocolContext);
                }
            };
        }
    }

    public class DefaultMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> :
        AbstractMessagingApplicationBuilder<MessageContext>,
        IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> 
        where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
    {
        private readonly IList<Func<ContextMiddlewareDelegate<ProtocolContextHost>, ContextMiddlewareDelegate<ProtocolContextHost>>> _protocolContextMiddlewareComponents =
            new List<Func<ContextMiddlewareDelegate<ProtocolContextHost>, ContextMiddlewareDelegate<ProtocolContextHost>>>();

        public DefaultMessagingApplicationBuilder(object serverFeatures, IServiceProvider applicationServices, IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext> protocolMessagingContextReceiver, ILogger logger, DiagnosticListener diagnosticListener) :
            base(serverFeatures, applicationServices, logger, diagnosticListener)
        {
            SetProperty(Constants.BuilderProperties.ProtocolMessagingContextReceiver, protocolMessagingContextReceiver);
        }

        public DefaultMessagingApplicationBuilder(DefaultMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> builder) :
            base(builder)
        {
        }

        public virtual IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext> ProtocolMessagingContextReceiver =>
            GetProperty<IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext>>(Constants.BuilderProperties.ProtocolMessagingContextReceiver);

        protected virtual IEnumerable<Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>>> MiddlewareComponents => _protocolMessagingContextMiddlewareComponents;

        Task<bool> IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>.TryBindToChannelAsync(ProtocolContextHost protocolContextHost)
        {
            return TryBindToChannelAsync(protocolContextHost);
        }

        protected virtual Task<bool> TryBindToChannelAsync(ProtocolContextHost protocolContextHost) =>
            ProtocolMessagingContextReceiver.TryBindToChannelAsync(protocolContextHost);

        Task<IMessagingContext<MessageContext>> IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>.ReceiveMessagingContextAsync(TimeSpan timeout)
        {
            return ReceiveProtocolMessagingContextAsync(timeout);
        }

        protected virtual Task<IMessagingContext<MessageContext>> ReceiveProtocolMessagingContextAsync(TimeSpan timeout) =>
            ProtocolMessagingContextReceiver.ReceiveMessagingContextAsync(timeout);

        protected override Task PrepareRejectedMessagingContextAsync(IMessagingContext<MessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders = null) => 
            ProtocolMessagingContextReceiver.PrepareRejectedMessagingContextAsync(messagingContext, rejectionCode, rejectionHeaders);

        protected override Task InvokeMessagingContextCallbackAsync(IMessagingContext<MessageContext> messagingContext) => 
            ProtocolMessagingContextReceiver.InvokeMessagingContextCallbackAsync(messagingContext);

        private async Task<IMessagingContext<MessageContext>> PrepareExceptionMessagingContextAsync(ProtocolContextHost protocolContextHost, Exception exception)
        {
            IMessagingContext<MessageContext> messagingContext = new MessagingContext<MessageContext>();

            messagingContext.Items.Add<ProtocolContextHost>(protocolContextHost);

            if (exception is TimeoutException)

                await this.PrepareRejectedMessagingContextAsync(messagingContext, RejectCode.Timeout);

            else
            {
                messagingContext.Items.Add<Exception>(exception);

                await this.PrepareRejectedMessagingContextAsync(messagingContext, RejectCode.Faulted);
            }

            return messagingContext;
        }

        private async Task<IMessagingContext<MessageContext>> PrepareCouldNotBeBoundMessagingContextAsync(ProtocolContextHost protocolContextHost)
        {
            IMessagingContext<MessageContext> messagingContext = new MessagingContext<MessageContext>();

            messagingContext.Items.Add<ProtocolContextHost>(protocolContextHost);

            await this.PrepareRejectedMessagingContextAsync(messagingContext, RejectCode.BindingUnreachable);

            return messagingContext;
        }

        public IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> Use(Func<ContextMiddlewareDelegate<ProtocolContextHost>, ContextMiddlewareDelegate<ProtocolContextHost>> middlewareComponent)
        {
            _protocolContextMiddlewareComponents.Add(middlewareComponent);

            return this;
        }

        public IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> Use(Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>> middlewareComponent)
        {
            _protocolMessagingContextMiddlewareComponents.Add(middlewareComponent);

            return this;
        }

        public virtual IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> New()
        {
            return new DefaultMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>(this);
        }

        Task<ContextMiddlewareDelegate<ProtocolContextHost>> IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>.BuildContextMiddlewareAsync()
        {
            return BuildContextMiddlewareAsync();
        }

        /// <summary>
        /// Builds the middleware pipeline used by this application to process input protocol binding contexts and returns the head middleware component delegate in the pipeline.
        /// </summary>
        /// <returns></returns>
        protected async Task<ContextMiddlewareDelegate<ProtocolContextHost>> BuildContextMiddlewareAsync()
        {
            // Build middleware in reverse order ...

            this.Use(next =>
            {
                // Configure final middleware component ...

                return async messagingContext =>
                {
                    if (messagingContext != null)
                    {
                        if (messagingContext.Result == MiddlewarePipelineResult.NotHandled)
                        {
                            await this.PrepareRejectedMessagingContextAsync(messagingContext, RejectCode.NotHandled);
                        }
                        await this.InvokeMessagingContextCallbackAsync(messagingContext);
                    }

                    if (next != null)

                        await next(messagingContext);
                };
            });

            MessagingContextDiagnostics<MessageContext> _diagnostics =
                new MessagingContextDiagnostics<MessageContext>(this.DiagnosticLogger, this.DiagnosticListener);

            // Add a final protocol messaging middleware pipeline component (diagnostics end messagingContext) ...

            MessagingContextMiddlewareDelegate<MessageContext> protocolMessagingContextMiddlewarePipline =
                async messagingContext =>
                {
                    _diagnostics.EndRequest(messagingContext, null);

                    //    _protocolMessagingContextFactory.Dispose(messageHandlerContext);
                    //    _diagnostics.ContextDisposed(context);

                    await Task.CompletedTask;
                };

            // Build protocol messaging middleware pipeline in reverse order ...

            foreach (Func<MessagingContextMiddlewareDelegate<MessageContext>, MessagingContextMiddlewareDelegate<MessageContext>> protocolMessagingContextMiddlewareComponent in
                _protocolMessagingContextMiddlewareComponents.Reverse())
            {
                protocolMessagingContextMiddlewarePipline =
                    protocolMessagingContextMiddlewareComponent(protocolMessagingContextMiddlewarePipline);
            }

            // Add diagnostics begin messagingContext ...

            MessagingContextMiddlewareDelegate<MessageContext> headProtocolMessagingContextMiddlewareDelegate =
                async messagingContext =>
                {
                    _diagnostics.BeginRequest(messagingContext);

                    await protocolMessagingContextMiddlewarePipline(messagingContext);
                };

            await ProtocolMessagingContextReceiver.StartAsync(ServerFeatures.Get<IServerAddressesFeature>());

            return async (protocolContextHost) =>
            {
                if (protocolContextHost != null)
                {
                    // Build protocol binding context middleware pipeline in reverse order;
                    // make last protocol binding context middleware component produce a protocol messaging context and
                    // begin running the protocol messaging context middleware pipeline
                    ContextMiddlewareDelegate<ProtocolContextHost> headProtocolContextMiddlewareDelegate = async (pch) =>
                    {
                        ProtocolMessagingContext<ProtocolContextHost>.Current = pch;

                        if (await TryBindToChannelAsync(pch))
                        {
                            IMessagingContext<MessageContext> messagingContext;

                            try
                            {
                                messagingContext =
                                    await ReceiveProtocolMessagingContextAsync(TimeSpan.FromMilliseconds(UInt32.MaxValue - 2));
                            }
                            catch (Exception e)
                            {
                                messagingContext = await PrepareExceptionMessagingContextAsync(pch, e);
                            }

                            if (messagingContext != null)
                            {
                                if (messagingContext.Result == MiddlewarePipelineResult.NotHandled)
                                {
                                    messagingContext.Items.Add<ProtocolContextHost>(protocolContextHost);

                                    await headProtocolMessagingContextMiddlewareDelegate(messagingContext);

                                    if (messagingContext.BindingContext.ConnectionContext.IsChannelUpgraded(out CancellationToken upgradedChannelClosedToken))
                                    {
                                        await Task.Run(async () =>
                                        {
                                            while (!upgradedChannelClosedToken.IsCancellationRequested)
                                            {
                                                try
                                                {
                                                    IMessagingContext<MessageContext> upgradedProtocolMessagingContext =
                                                        await this.ReceiveProtocolMessagingContextAsync(TimeSpan.FromSeconds(2));

                                                    if (upgradedProtocolMessagingContext != null)
                                                    {
                                                        upgradedProtocolMessagingContext.Items.Add(typeof(ProtocolContextHost).Name, protocolContextHost);

                                                        await headProtocolMessagingContextMiddlewareDelegate(upgradedProtocolMessagingContext);
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    this.Logger.Log(e);
                                                }
                                            }

                                            this.Logger.Trace("Upgraded channel closed");
                                        });
                                    }
                                }
                                else

                                    await headProtocolMessagingContextMiddlewareDelegate(messagingContext);
                            }
                        }
                        else
                        {
                            IMessagingContext<MessageContext> messagingContext = 
                                await PrepareCouldNotBeBoundMessagingContextAsync(pch);

                            await headProtocolMessagingContextMiddlewareDelegate(messagingContext);
                        }
                    };

                    foreach (Func<ContextMiddlewareDelegate<ProtocolContextHost>, ContextMiddlewareDelegate<ProtocolContextHost>> protocolContextMiddlewareComponent in _protocolContextMiddlewareComponents.Reverse())
                    {
                        headProtocolContextMiddlewareDelegate = protocolContextMiddlewareComponent(headProtocolContextMiddlewareDelegate);
                    }

                    await headProtocolContextMiddlewareDelegate(protocolContextHost);
                }
            };
        }
    }
}