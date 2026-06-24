using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using AllVerge.MessagingModel.MessagingApplication;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    using AllVerge.SystemPrimitives.Threading;

    public abstract class AbstractMessagingServer :
        IServer
    {
        private CancellationTokenSource _stopServerSource = new CancellationTokenSource();
        private int _state;

        protected AbstractMessagingServer(IOptions<MessagingServerOptions> options, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (applicationLifetime == null)
            {
                throw new ArgumentNullException(nameof(applicationLifetime));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            Options = (options.Value ?? new MessagingServerOptions());
            Features = new FeatureCollection();
            Logger = loggerFactory.CreateLogger(this.GetType().GetTypeInfo().Namespace);
            LoggerScope = Logger.BeginScope(null);
            ServerAddressesFeature serverAddressesFeature = 
                Options.UrlPrefixes.Aggregate(new ServerAddressesFeature(), (a, u) => { a.Addresses.Add(u.AbsoluteUri); return a; });
            Features.Set<IServerAddressesFeature>(serverAddressesFeature);
            _state = 0;
        }

        public IFeatureCollection Features
        {
            get;
        }

        public MessagingServerOptions Options
        {
            get;
        }

        public ILogger Logger { get; }

        protected IDisposable LoggerScope { get; }

        public CancellationToken StopServerToken => _stopServerSource.Token;

        public abstract Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken);

        protected bool SetStartingState()
        {
            return Interlocked.CompareExchange(ref this._state, 1, 0) == 0;
        }

        protected bool IsStartingState()
        {
            return Interlocked.CompareExchange(ref this._state, 1, 1) == 1;
        }

        protected void SetStartedState()
        {
            Interlocked.CompareExchange(ref this._state, 2, 1);
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref this._state, 1, 2) > 1)
            {
                Logger.HostShuttingdown();

                try
                {
                    _stopServerSource.Cancel(false);
                }
                catch (Exception e)
                {
                    Logger.ServerShutdownError(e);
                }
            }

            Interlocked.CompareExchange(ref this._state, 0, 1);

            Logger.HostShutdown();

            return Task.CompletedTask;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~NetMQMessageHandlerServer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }

    public abstract class MessagingContextServer<MessageContext> :
        AbstractMessagingServer
        where MessageContext : IMessageContext
    {
        protected MessagingContextServer(IOptions<MessagingServerOptions> options, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory) : 
            base(options, applicationLifetime, loggerFactory)
        {
        }

        public override Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            if (typeof(IHttpApplication<IMessagingContext<MessageContext>>).IsAssignableFrom(application.GetType()))
            {
                if (!this.SetStartingState())

                    return Task.FromException(new InvalidOperationException(CoreStrings.ServerAlreadyStarted));

                return StartServerAsync((IHttpApplication<IMessagingContext<MessageContext>>)application, cancellationToken);
            }

            return Task.FromException(new ArgumentException($"{typeof(IHttpApplication<IMessagingContext<MessageContext>>)} not assignable from {application.GetType()}.", nameof(application)));
        }

        private Task StartServerAsync(IHttpApplication<IMessagingContext<MessageContext>> application, CancellationToken startingCancellationToken)
        {
            if (!IsStartingState())

                throw new InvalidOperationException(CoreStrings.ServerNotStarted);

            Logger.HostStarting();

            ValidateOptions();

            IHostEnvironment hostEnvironment = Options.ApplicationServices.GetRequiredService<IHostEnvironment>();

            IServerAddressesFeature serverAddressesFeature = Features.Get<IServerAddressesFeature>();

            IEnumerable<String> serverAddresses;

            if (serverAddressesFeature.PreferHostingUrls)

                serverAddresses = serverAddressesFeature.Addresses;

            else

                serverAddresses = Options.UrlPrefixes.Select(i => i.AbsoluteUri);

            TaskCompletionSource<VoidType> serverStartedSource = new TaskCompletionSource<VoidType>();

            Task.Run(async () =>
            {
                this.SetStartedState();

                Logger.HostStarted();

                serverStartedSource.SetResult(VoidType.Void);

                while (true)
                {
                    IMessagingContext<MessageContext> messagingContext;

                    try
                    {
                        messagingContext = await application.CreateContext(this.Features).InputReceivedAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        if (StopServerToken.IsCancellationRequested)

                            break;

                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        if (StopServerToken.IsCancellationRequested)

                            break;

                        continue;
                    }
                    catch (Exception e)
                    {
                        Logger.ApplicationError((EventId)14, "An error occured creating a protocol messaging context.", e);

                        if (StopServerToken.IsCancellationRequested)

                            break;

                        continue;
                    }

                    await Task.Yield();

                    _ = application.ProcessRequestAsync(messagingContext).ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            application.DisposeContext(messagingContext, t.Exception);
                        else if (t.IsCanceled)
                            application.DisposeContext(messagingContext, new TaskCanceledException());
                        else
                            application.DisposeContext(messagingContext, null);
                    });
                }

                LoggerScope.Dispose();
            });

            return serverStartedSource.Task;
        }

        protected virtual void ValidateOptions()
        {
        }
    }

    public abstract class MessagingContextServer<ProtocolContext, MessageContext> :
        AbstractMessagingServer,
        IServer where MessageContext : IMessageContext
    {
        protected MessagingContextServer(IOptions<MessagingServerOptions> options, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory) :
            base(options, applicationLifetime, loggerFactory)
        {
        }

        public override Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) 
        {
            if (typeof(IHttpApplication<IMessagingContext<ProtocolContext>>).IsAssignableFrom(application.GetType()))
            {
                if (!this.SetStartingState())

                    return Task.FromException(new InvalidOperationException(CoreStrings.ServerAlreadyStarted));

                return StartServerAsync((IHttpApplication<IMessagingContext<ProtocolContext>>)application, cancellationToken);
            }

            return Task.FromException(new ArgumentException($"{typeof(IHttpApplication<ProtocolContext>)} not assignable from {application.GetType()}.", nameof(application)));
        }

        private Task StartServerAsync(IHttpApplication<IMessagingContext<ProtocolContext>> application, CancellationToken startingCancellationToken)
        {
            if (!this.IsStartingState())

                throw new InvalidOperationException(CoreStrings.ServerNotStarted);
            
            Logger.HostStarting();

            ValidateOptions();

            IHostEnvironment hostEnvironment = Options.ApplicationServices.GetRequiredService<IHostEnvironment>();

            IServerAddressesFeature serverAddressesFeature = Features.Get<IServerAddressesFeature>();

            IEnumerable<String> serverAddresses;

            if (serverAddressesFeature.PreferHostingUrls)

                serverAddresses = serverAddressesFeature.Addresses;

            else

                serverAddresses = Options.UrlPrefixes.Select(i => i.AbsoluteUri);

            TaskCompletionSource<VoidType> serverStartedSource = new TaskCompletionSource<VoidType>();

            Task.Run(async () =>
            {
                this.SetStartedState();

                Logger.HostStarted();

                serverStartedSource.SetResult(VoidType.Void);

                while (true)
                {
                    IMessagingContext<ProtocolContext> protocolContext;

                    try
                    {
                        protocolContext = await application.CreateContext(this.Features).InputReceivedAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        if (StopServerToken.IsCancellationRequested)

                            break;

                        continue;
                    }
                    catch (Exception e)
                    {
                        Logger.ApplicationError((EventId)14, "An error occured creating a protocol messaging context.", e);

                        if (StopServerToken.IsCancellationRequested)

                            break;

                        continue;
                    }

                    await Task.Yield();

                    _ = application.ProcessRequestAsync(protocolContext).ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            application.DisposeContext(protocolContext, t.Exception);
                        else if (t.IsCanceled)
                            application.DisposeContext(protocolContext, new TaskCanceledException());
                        else
                            application.DisposeContext(protocolContext, null);
                    });
                }

                LoggerScope.Dispose();
            });

            return serverStartedSource.Task;
        }

        protected virtual void ValidateOptions()
        {
        }
    }
}
