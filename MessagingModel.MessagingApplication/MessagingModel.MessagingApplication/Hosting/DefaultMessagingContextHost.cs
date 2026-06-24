using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using AllVerge.MessagingModel.MessagingApplication.Hosting.Builder;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.AspNetCore.Http.Features;
    using AllVerge.MessagingModel.MessagingApplication.Builder;
#if NETSTANDARD2_0
    using Microsoft.AspNetCore.Hosting.Internal;
    using AutoMapper.Features;
#endif

    public abstract class AbstractMessagingContextHost : IMessagingHost
    {
        private static readonly string DeprecatedServerUrlsKey = "server.urls";

        private readonly IServiceCollection _appServices;
        private CancellationTokenSource _appCts;
        private IHostApplicationLifetime _hostAppLifetime;
        private HostedServiceExecutor _hostedServiceExecutor;

        private readonly IServiceProvider _hostingServiceProvider;
        private readonly HostOptions _hostOptions;
        private readonly IConfiguration _configuration;
        private readonly AggregateException _hostingStartupAssemblyErrors;

        private ILogger _logger = NullLogger.Instance;
        private IServiceProvider _appServiceProvider;
        private ExceptionDispatchInfo _appServicesException;

        private bool _stopped;
        private bool _startedServer;
        private AggregateException _serverStartupErrors;

        private IMessagingApplicationStartup _startup;
        private bool disposedValue;

        private AbstractMessagingContextHost() { }

        protected AbstractMessagingContextHost(
            IConfiguration configuration,
            HostOptions hostOptions,
            IServiceProvider hostingServiceProvider,
            IServiceCollection applicationServices,
            AggregateException hostingStartupAssemblyErrors)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (hostingServiceProvider == null)
            {
                throw new ArgumentNullException(nameof(hostingServiceProvider));
            }

            if (applicationServices == null)
            {
                throw new ArgumentNullException(nameof(applicationServices));
            }

            _configuration = configuration;
            _hostOptions = hostOptions;
            _hostingServiceProvider = hostingServiceProvider;
            _appServices = applicationServices;
            _hostingStartupAssemblyErrors = hostingStartupAssemblyErrors;
            _appServices.AddSingleton<IHostApplicationLifetime, HostApplicationLifetime>();
            _appServices.AddSingleton<HostedServiceExecutor>();

#if NET8_0_OR_GREATER
            _appServices.AddMetrics();
#endif
        }

        public IServiceProvider Services => _appServiceProvider ?? _hostingServiceProvider;

        public IFeatureCollection ServerFeatures
        {
            get
            {
                EnsureServer();
                return Server?.Features;
            }
        }

        protected HostOptions Options => _hostOptions;
        protected IServer Server { get; private set; }
        protected ILogger Logger { get => _logger; }

        protected ExceptionDispatchInfo ApplicationServicesException { get => _appServicesException; }

        // Called immediately after the constructor so the properties can rely on it.
        public void Initialize()
        {
            try
            {
                EnsureApplicationServices();

                _appCts = new CancellationTokenSource();

                _hostAppLifetime = this.Services.GetRequiredService<IHostApplicationLifetime>();

                DisposalExtensions.SetApplicationStoppingToken(
                    _hostAppLifetime.ApplicationStopping);

                this.RegisterForDisposal();
            }
            catch (Exception ex)
            {
                // EnsureApplicationServices may have failed due to a missing or throwing Startup class.
                if (_appServiceProvider == null)
                {
                    _appServiceProvider = _appServices.BuildServiceProvider();
                }

                if (!_hostOptions.CaptureStartupErrors)
                {
                    throw;
                }

                _appServicesException = ExceptionDispatchInfo.Capture(ex);
            }
        }

        public void Start()
        {
            StartAsync(_appCts.Token).GetAwaiter().GetResult();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            HostingEventSource.Log.HostStart();
            _logger = this.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ServiceModel.MessagingApplication.Hosting");
            _logger.HostStarting();

            _hostedServiceExecutor = this.Services.GetRequiredService<HostedServiceExecutor>();

            // Fire IHostedService.Start
            await _hostedServiceExecutor.StartAsync(cancellationToken);

            var diagnosticSource = this.Services.GetRequiredService<DiagnosticListener>();

            Task startTask = StartServerAsync(diagnosticSource, cancellationToken);

            await startTask;

            if (startTask.IsFaulted)
            {
                this._serverStartupErrors = startTask.Exception;

                _startedServer = false;
            }
            else if (startTask.IsCanceled)

                _startedServer = false;

            else

                _startedServer = true;

            if (_startedServer)
            {
                (_hostAppLifetime as HostApplicationLifetime)?.NotifyStarted();

                _logger.HostStarted();
            }
            else if (_serverStartupErrors != null)
            {
                foreach (var exception in _serverStartupErrors.InnerExceptions)
                {
                    _logger.ServerStartupError(exception);
                }
            }

            if (_hostingStartupAssemblyErrors != null)
            {
                foreach (var exception in _hostingStartupAssemblyErrors.InnerExceptions)
                {
                    _logger.HostingStartupAssemblyError(exception);
                }
            }
            else
            {
                // Log the fact that we did load hosting startup assemblies.
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    foreach (var assembly in _hostOptions.GetFinalHostingStartupAssemblies())
                    {
                        _logger.LogDebug("Loaded hosting startup assembly {assemblyName}", assembly);
                    }
                }
            }
        }

        protected abstract Task StartServerAsync(DiagnosticListener diagnosticSource, CancellationToken cancellationToken);

        private void EnsureApplicationServices()
        {
            if (_appServiceProvider == null)
            {
                EnsureStartup();

                _appServiceProvider = _startup.ConfigureServices(_appServices);
            }
        }

        private void EnsureStartup()
        {
            if (_startup != null)
            {
                return;
            }

            _startup = OnEnsureMessagingApplicationStartup();

            if (_startup == null)
            {
                throw new InvalidOperationException($"No application configured. Please specify startup via IWebHostBuilder.UseStartup, IWebHostBuilder.Configure, injecting {nameof(IMessagingApplicationStartup)} or specifying the startup assembly via {nameof(WebHostDefaults.StartupAssemblyKey)} in the web host configuration.");
            }
        }

        protected abstract IMessagingApplicationStartup OnEnsureMessagingApplicationStartup();

        protected void EnsureServer()
        {
            if (Server == null)
            {
                Server = this.Services.GetRequiredService<IServer>();

                var serverAddressesFeature = Server.Features?.Get<IServerAddressesFeature>();
                var addresses = serverAddressesFeature?.Addresses;

                serverAddressesFeature.PreferHostingUrls = HostUtilities.ParseBool(_configuration, WebHostDefaults.PreferHostingUrlsKey);

                if (addresses != null && !addresses.IsReadOnly && serverAddressesFeature.PreferHostingUrls)
                {
                    serverAddressesFeature.Addresses.Clear();

                    var urls = _configuration[WebHostDefaults.ServerUrlsKey] ?? _configuration[DeprecatedServerUrlsKey];
                    
                    if (!string.IsNullOrEmpty(urls))
                    {
                        foreach (var value in urls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            addresses.Add(value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This is called from <see cref="WebHostExtensions.RunAsync(IWebHost, CancellationToken)"/>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_stopped)
            {
                return;
            }
            _stopped = true;

            try
            {
                _logger.HostShutdown();

                var timeoutToken = new CancellationTokenSource(Options.ShutdownTimeout).Token;
                if (!cancellationToken.CanBeCanceled)
                {
                    cancellationToken = timeoutToken;
                }
                else
                {
                    cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken).Token;
                }

                if (Server != null && _startedServer)
                {
                    await StopServerAsync(cancellationToken);
                }

                // Fire the IHostedService.Stop
                if (_hostedServiceExecutor != null)
                {
                    await _hostedServiceExecutor.StopAsync(cancellationToken);
                }

                (_hostAppLifetime as HostApplicationLifetime)?.NotifyStopped();

                HostingEventSource.Log.HostStop();
            }
            catch (Exception ex)
            {
                _logger.ServerShutdownError(ex);
            }
        }

        protected async virtual Task StopServerAsync(CancellationToken cancellationToken)
        {
            await Server.StopAsync(cancellationToken);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    OnDispose();

                    Dispose(_appServiceProvider);
                    Dispose(_hostingServiceProvider);
                    Dispose(_appCts);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        private void Dispose(Object obj)
        {
            switch (obj)
            {
                case IAsyncDisposable asyncDisposable:
                    DisposeAsync(asyncDisposable).GetAwaiter().GetResult();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }

        private async Task DisposeAsync(IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        protected virtual void OnDispose()
        {
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AbstractMessagingContextHost()
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
            Dispose();
        }
    }

    public class DefaultMessagingContextHost<MessageContext> : 
        AbstractMessagingContextHost
    {
        private IMessagingApplicationStartup<MessageContext> startup;

        public DefaultMessagingContextHost(
            IConfiguration config,
            HostOptions hostOptions,
            IServiceProvider hostingServices,
            IServiceCollection appServiceSet,
            AggregateException hostingStartupErrors) :
            base(config, hostOptions, hostingServices, appServiceSet, hostingStartupErrors)
        {
            this.startup = null;
        }

        protected override IMessagingApplicationStartup OnEnsureMessagingApplicationStartup()
        {
            if (this.startup == null)

                this.startup = this.Services.GetRequiredService<IMessagingApplicationStartup<MessageContext>>();

            return this.startup;
        }

        protected override Task StartServerAsync(DiagnosticListener diagnosticSource, CancellationToken cancellationToken)
        {
            return CreateMessageHandlerHostingApplicationAsync(diagnosticSource).ContinueWith(t =>
            {
                IHttpApplication<IMessagingContext<MessageContext>> hostingApp = t.Result;

                return Server.StartAsync(hostingApp, cancellationToken);
            }).Result;
        }

        private async Task<IHttpApplication<IMessagingContext<MessageContext>>> CreateMessageHandlerHostingApplicationAsync(DiagnosticListener diagnosticListener)
        {
            IMessagingContextReceiver<MessageContext> protocolMessagingContextReceiver =
                this.Services.GetRequiredService<IMessagingContextReceiver<MessageContext>>();

            MessagingContextMiddlewareDelegate<MessageContext> application = await BuildMessagingApplicationAsync(diagnosticListener, protocolMessagingContextReceiver);

            return new DefaultMessagingContextHostApplication<MessageContext>(this.Logger, protocolMessagingContextReceiver.Create, application);
        }

        protected Task<MessagingContextMiddlewareDelegate<MessageContext>> BuildMessagingApplicationAsync(DiagnosticListener diagnosticListener, IMessagingContextReceiver<MessageContext> protocolMessagingContextReceiver)
        {
            try
            {
                this.ApplicationServicesException?.Throw();

                EnsureServer();

                IMessagingApplicationBuilder<MessageContext> builder = (IMessagingApplicationBuilder<MessageContext>)
                    this.Services.GetRequiredService<IMessagingApplicationBuilderFactory<MessageContext>>().CreateMessagingApplicationBuilder(Server.Features, protocolMessagingContextReceiver, this.Logger, diagnosticListener);

                builder.ApplicationServices = this.Services;

                var startupFilters = this.Services.GetService<IEnumerable<IMessagingApplicationStartupFilter<MessageContext>>>();

                Action<IMessagingApplicationBuilder<MessageContext>, IHostApplicationLifetime, IHostEnvironment, ILoggerFactory> configure = this.startup.Configure;

                IHostApplicationLifetime applicationLifetime = this.Services.GetRequiredService<IHostApplicationLifetime>();
                IHostEnvironment hostEnvironment = this.Services.GetRequiredService<IHostEnvironment>();
                ILoggerFactory loggerFactory = this.Services.GetRequiredService<ILoggerFactory>();

                foreach (var filter in startupFilters.Reverse())
                {
                    configure = filter.Configure(configure);
                }

                configure(builder, applicationLifetime, hostEnvironment, loggerFactory);

                return builder.BuildMessagingContextMiddlewareAsync();
            }
            catch (Exception ex)
            {
                if (!this.Options.SuppressStatusMessages)
                {
                    // Write errors to standard out so they can be retrieved when not in development mode.
                    Console.WriteLine("Application startup exception: " + ex.ToString());
                }

                this.Logger.HostedApplicationStartupError(ex);

                if (!this.Options.CaptureStartupErrors)
                {
                    throw;
                }

                EnsureServer();

                if (Server == null)

                    throw;

                return ex.RaiseExceptionMessagingContextMiddlewareDelegateAsync<MessageContext>();
            }
        }

    }

    public class DefaultMessagingContextHost<ProtocolContext, MessageContext> :
        AbstractMessagingContextHost
    {
        private IMessagingApplicationStartup<ProtocolContext, MessageContext> startup;

        public DefaultMessagingContextHost(
            IConfiguration configuration,
            HostOptions hostOptions,
            IServiceProvider hostingServiceProvider,
            IServiceCollection applicationServices,
            AggregateException hostingStartupAssemblyErrors) :
            base(configuration, hostOptions, hostingServiceProvider, applicationServices, hostingStartupAssemblyErrors)
        {
            this.startup = null;
        }

        protected override IMessagingApplicationStartup OnEnsureMessagingApplicationStartup()
        {
            if (this.startup == null)

                this.startup = this.Services.GetRequiredService<IMessagingApplicationStartup<ProtocolContext, MessageContext>>();

            return this.startup;
        }

        protected override Task StartServerAsync(DiagnosticListener diagnosticSource, CancellationToken cancellationToken)
        {
            return CreateMessageHandlerHostingApplicationAsync(diagnosticSource).ContinueWith(t =>
            {
                IHttpApplication<IMessagingContext<ProtocolContext>> hostingApp = t.Result;

                return Server.StartAsync(hostingApp, cancellationToken);
            }).Result;
        }

        private async Task<IHttpApplication<IMessagingContext<ProtocolContext>>> CreateMessageHandlerHostingApplicationAsync(DiagnosticListener diagnosticListener)
        {
            IMessagingContextReceiver<ProtocolContext, MessageContext> protocolMessagingContextReceiver =
                this.Services.GetRequiredService<IMessagingContextReceiver<ProtocolContext, MessageContext>>();

            ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>> application = await BuildMessagingApplicationAsync(diagnosticListener, protocolMessagingContextReceiver);

            return new DefaultMessagingContextHostApplication<ProtocolContext, MessageContext>(this.Logger, protocolMessagingContextReceiver.Create, application);
        }

        protected Task<ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>>> BuildMessagingApplicationAsync(DiagnosticListener diagnosticListener, IMessagingContextReceiver<ProtocolContext, MessageContext> protocolMessagingContextReceiver)
        {
            try
            {
                this.ApplicationServicesException?.Throw();

                EnsureServer();

                IMessagingApplicationBuilder<ProtocolContext, MessageContext> builder;

                builder =
                    this.Services.GetRequiredService<IMessagingApplicationBuilderFactory<ProtocolContext, MessageContext>>().CreateMessagingApplicationBuilder(Server.Features, protocolMessagingContextReceiver, this.Logger, diagnosticListener);

                builder.ApplicationServices = this.Services;

                var startupFilters = this.Services.GetService<IEnumerable<IMessagingApplicationStartupFilter<ProtocolContext, MessageContext>>>();

                Action<IMessagingApplicationBuilder<ProtocolContext, MessageContext>, IHostApplicationLifetime, IHostEnvironment, ILoggerFactory> configure = this.startup.Configure;

                foreach (var filter in startupFilters.Reverse())
                {
                    configure = filter.Configure(configure);
                }

                IHostApplicationLifetime applicationLifetime = this.Services.GetRequiredService<IHostApplicationLifetime>();
                IHostEnvironment hostEnvironment = this.Services.GetRequiredService<IHostEnvironment>();
                ILoggerFactory loggerFactory = this.Services.GetRequiredService<ILoggerFactory>();

                configure(builder, applicationLifetime, hostEnvironment, loggerFactory);

                return builder.BuildContextMiddlewareAsync();
            }
            catch (Exception ex)
            {
                if (!this.Options.SuppressStatusMessages)
                {
                    // Write errors to standard out so they can be retrieved when not in development mode.
                    Console.WriteLine("Application startup exception: " + ex.ToString());
                }

                this.Logger.HostedApplicationStartupError(ex);

                if (!this.Options.CaptureStartupErrors)
                {
                    throw;
                }

                EnsureServer();

                if (Server == null)

                    throw;

                return ex.RaiseExceptionContextMiddlewareDelegateAsync<IMessagingContext<ProtocolContext>>();
            }
        }
    }

    public class DefaultMessagingContextHost<ProtocolContextHost, ProtocolContext, MessageContext> :
        AbstractMessagingContextHost
        where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
    {
        private IMessagingApplicationStartup<ProtocolContextHost, ProtocolContext, MessageContext> startup;

        public DefaultMessagingContextHost(
            IConfiguration configuration,
            HostOptions hostOptions,
            IServiceProvider hostingServiceProvider,
            IServiceCollection applicationServices,
            AggregateException hostingStartupAssemblyErrors) :
            base(configuration, hostOptions, hostingServiceProvider, applicationServices, hostingStartupAssemblyErrors)
        {
            this.startup = null;
        }

        protected override IMessagingApplicationStartup OnEnsureMessagingApplicationStartup()
        {
            if (this.startup == null)

                this.startup = this.Services.GetRequiredService<IMessagingApplicationStartup<ProtocolContextHost, ProtocolContext, MessageContext>>();

            return this.startup;
        }

        protected override Task StartServerAsync(DiagnosticListener diagnosticSource, CancellationToken cancellationToken)
        {
            return CreateMessageHandlerHostingApplicationAsync(diagnosticSource).ContinueWith(t =>
            {
                IHttpApplication<ProtocolContextHost> hostingApp = t.Result;

                return Server.StartAsync(hostingApp, cancellationToken);
            }).Result;
        }

        private async Task<IHttpApplication<ProtocolContextHost>> CreateMessageHandlerHostingApplicationAsync(DiagnosticListener diagnosticListener)
        {
            var protocolMessagingContextReceiver =
                this.Services.GetRequiredService<IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext>>();

            IServerAddressesFeature serverAddressesFeature = this.ServerFeatures.Get<IServerAddressesFeature>();

            (protocolMessagingContextReceiver as IProtocolContextAccessorFactory<ProtocolContextHost>).RegisterListenAddresses(serverAddressesFeature.Addresses);

            ContextMiddlewareDelegate<ProtocolContextHost> application = await BuildMessagingApplicationAsync(diagnosticListener, protocolMessagingContextReceiver);

            return new DefaultMessagingContextHostApplication<ProtocolContextHost, ProtocolContext, MessageContext>(this.Logger, protocolMessagingContextReceiver.Create, application);
        }

        protected Task<ContextMiddlewareDelegate<ProtocolContextHost>> BuildMessagingApplicationAsync(DiagnosticListener diagnosticListener, IMessagingContextReceiver<ProtocolContextHost, ProtocolContext, MessageContext> protocolMessagingContextReceiver)
        {
            try
            {
                this.ApplicationServicesException?.Throw();

                EnsureServer();

                IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext> builder;

                builder =
                    this.Services.GetRequiredService<IMessagingApplicationBuilderFactory<ProtocolContextHost, ProtocolContext, MessageContext>>().CreateMessagingApplicationBuilder(Server.Features, protocolMessagingContextReceiver, this.Logger, diagnosticListener);

                builder.ApplicationServices = this.Services;

                var startupFilters = this.Services.GetService<IEnumerable<IMessagingApplicationStartupFilter<ProtocolContextHost, ProtocolContext, MessageContext>>>();

                Action<IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>, IHostApplicationLifetime, IHostEnvironment, ILoggerFactory> configure = this.startup.Configure;

                foreach (var filter in startupFilters.Reverse())
                {
                    configure = filter.Configure(configure);
                }

                IHostApplicationLifetime applicationLifetime = this.Services.GetRequiredService<IHostApplicationLifetime>();
                IHostEnvironment hostEnvironment = this.Services.GetRequiredService<IHostEnvironment>();
                ILoggerFactory loggerFactory = this.Services.GetRequiredService<ILoggerFactory>();

                configure(builder, applicationLifetime, hostEnvironment, loggerFactory);

                return builder.BuildContextMiddlewareAsync();
            }
            catch (Exception ex)
            {
                if (!this.Options.SuppressStatusMessages)
                {
                    // Write errors to standard out so they can be retrieved when not in development mode.
                    Console.WriteLine("Application startup exception: " + ex.ToString());
                }

                this.Logger.HostedApplicationStartupError(ex);

                if (!this.Options.CaptureStartupErrors)
                {
                    throw;
                }

                EnsureServer();

                if (Server == null)

                    throw;

                return ex.RaiseExceptionContextMiddlewareDelegateAsync<ProtocolContextHost>();
            }
        }
    }
}
