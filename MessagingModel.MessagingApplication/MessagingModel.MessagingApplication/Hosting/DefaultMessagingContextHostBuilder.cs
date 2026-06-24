using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

using AllVerge.MessagingModel.MessagingApplication.Hosting.Builder;
using AllVerge.SystemPrimitives.Reflection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Hosting.Internal;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    /// <summary>
    /// An abstract builder for <see cref="IWebHost"/>
    /// </summary>
    public abstract class AbstractMessagingContextHostBuilder :
        IMessagingHostBuilder
    {
        private Action<MessagingHostBuilderContext, IConfigurationBuilder> _configureAppConfigurationBuilder;
        private Action<MessagingHostBuilderContext, IServiceCollection> _configureHostServices;
        private Action<IServiceCollection> _configureServices;

        private IConfiguration _config;
        private HostOptions _options;
        private MessagingHostBuilderContext _context;
        private bool _webHostBuilt;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractMessagingContextHostBuilder"/> class.
        /// </summary>
        protected AbstractMessagingContextHostBuilder()
        {
            _config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            if (string.IsNullOrEmpty(GetSetting(WebHostDefaults.EnvironmentKey)))
            {
                // Try adding legacy environment keys, never remove these.
                UseSetting(WebHostDefaults.EnvironmentKey, Environment.GetEnvironmentVariable("Hosting:Environment")
                    ?? Environment.GetEnvironmentVariable("ASPNET_ENV"));
            }

            if (string.IsNullOrEmpty(GetSetting(WebHostDefaults.ServerUrlsKey)))
            {
                // Try adding legacy url key, never remove this.
                UseSetting(WebHostDefaults.ServerUrlsKey, Environment.GetEnvironmentVariable("ASPNETCORE_SERVER.URLS"));
            }

            _context = new MessagingHostBuilderContext
            {
                Configuration = _config
            };
        }

        protected HostOptions Options => _options;

        /// <summary>
        /// Get the setting value from the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to look up.</param>
        /// <returns>The value the setting currently contains.</returns>
        public string GetSetting(string key)
        {
            return _config[key];
        }

        /// <summary>
        /// Add or replace a setting in the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to add or replace.</param>
        /// <param name="value">The value of the setting to add or replace.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        public IWebHostBuilder UseSetting(string key, string value)
        {
            _config[key] = value;
            return this;
        }

        /// <summary>
        /// Adds a delegate for configuring the <see cref="IConfigurationBuilder"/> that will construct an <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder" /> that will be used to construct an <see cref="IConfiguration" />.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        /// <remarks>
        /// The <see cref="IConfiguration"/> and <see cref="ILoggerFactory"/> on the <see cref="WebHostBuilderContext"/> are uninitialized at this stage.
        /// The <see cref="IConfigurationBuilder"/> is pre-populated with the settings of the <see cref="IMessagingHostBuilder"/>.
        /// </remarks>
        public IMessagingHostBuilder ConfigureAppConfiguration(Action<MessagingHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _configureAppConfigurationBuilder += configureDelegate;

            return this;
        }

        /// <summary>
        /// Adds a delegate for configuring the <see cref="IConfigurationBuilder"/> that will construct an <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder" /> that will be used to construct an <see cref="IConfiguration" />.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        /// <remarks>
        /// The <see cref="IConfiguration"/> and <see cref="ILoggerFactory"/> on the <see cref="WebHostBuilderContext"/> are uninitialized at this stage.
        /// The <see cref="IConfigurationBuilder"/> is pre-populated with the settings of the <see cref="IMessagingHostBuilder"/>.
        /// </remarks>
        public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            throw new NotImplementedException(nameof(ConfigureAppConfiguration));
        }

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. This may be called
        /// multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            if (configureServices == null)
            {
                throw new ArgumentNullException(nameof(configureServices));
            }

            _configureServices += configureServices;

            return this;
        }

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. 
        /// This may be called multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        public IMessagingHostBuilder ConfigureServices(Action<MessagingHostBuilderContext, IServiceCollection> configureServices)
        {
            if (configureServices == null)
            {
                throw new ArgumentNullException(nameof(configureServices));
            }

            _configureHostServices += configureServices;

            return this;
        }

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. 
        /// This may be called multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            throw new NotImplementedException(nameof(ConfigureServices));
        }

        /// <summary>
        /// Builds the required services and an <see cref="IWebHost"/> which hosts a web application.
        /// </summary>
        public IWebHost Build()
        {
            if (_webHostBuilt)
            {
                throw new InvalidOperationException(Resources.WebHostBuilder_SingleInstance);
            }
            _webHostBuilt = true;

            var hostingServices = BuildCommonServices(out var hostingStartupAssemblyErrors);
            var applicationServices = hostingServices.Clone();
            var hostingServiceProvider = GetProviderFromFactory(hostingServices);

            if (!_options.SuppressStatusMessages)
            {
                // Warn about deprecated environment variables
                if (Environment.GetEnvironmentVariable("Hosting:Environment") != null)
                {
                    Console.WriteLine("The environment variable 'Hosting:Environment' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
                }

                if (Environment.GetEnvironmentVariable("ASPNET_ENV") != null)
                {
                    Console.WriteLine("The environment variable 'ASPNET_ENV' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
                }

                if (Environment.GetEnvironmentVariable("ASPNETCORE_SERVER.URLS") != null)
                {
                    Console.WriteLine("The environment variable 'ASPNETCORE_SERVER.URLS' is obsolete and has been replaced with 'ASPNETCORE_URLS'");
                }
            }

            AddApplicationServices(applicationServices, hostingServiceProvider);

            IMessagingHost host =
                CreateMessagingHost(_config, _options, hostingServiceProvider, applicationServices, hostingStartupAssemblyErrors);

            try
            {
                host.Initialize();

                // resolve configuration explicitly once to mark it as resolved within the
                // service provider, ensuring it will be properly disposed with the provider
                _ = host.Services.GetService<IConfiguration>();

                var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(host.GetType());

                // Warn about duplicate HostingStartupAssemblies
                foreach (var assemblyName in _options.GetFinalHostingStartupAssemblies().GroupBy(a => a, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                {
                    logger.LogWarning($"The assembly {assemblyName} was specified multiple times. Hosting startup assemblies should only be specified once.");
                }

                return host;
            }
            catch
            {
                // Dispose the host if there's a failure to initialize, this should dispose
                // services that were constructed until the exception was thrown
                host.Dispose();
                throw;
            }

            IServiceProvider GetProviderFromFactory(IServiceCollection collection)
            {
                var provider = collection.BuildServiceProvider();
                var factory = provider.GetService<IServiceProviderFactory<IServiceCollection>>();

                if (factory != null && !(factory is DefaultServiceProviderFactory))
                {
                    using (provider)
                    {
                        return factory.CreateServiceProvider(factory.CreateBuilder(collection));
                    }
                }

                return provider;
            }
        }

        protected abstract IMessagingHost CreateMessagingHost(IConfiguration configuration, HostOptions hostOptions, IServiceProvider hostingServiceProvider, IServiceCollection applicationServices, AggregateException hostingStartupAssemblyErrors);

        private IServiceCollection BuildCommonServices(out AggregateException hostingStartupAssemblyErrors)
        {
            hostingStartupAssemblyErrors = null;

            _options = new HostOptions(_config, Assembly.GetEntryAssembly()?.GetName().Name);

            if (!_options.PreventHostingStartup)
            {
                var exceptions = new List<Exception>();

                // Execute the hosting startup assemblies
                foreach (var assemblyName in _options.GetFinalHostingStartupAssemblies().Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        var assembly = Assembly.Load(new AssemblyName(assemblyName));

                        foreach (var attribute in assembly.GetCustomAttributes<HostingStartupAttribute>())
                        {
                            var hostingStartup = (IHostingStartup)Activator.CreateInstance(attribute.HostingStartupType);
                            hostingStartup.Configure(this);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Capture any errors that happen during startup
                        exceptions.Add(new InvalidOperationException($"Startup assembly {assemblyName} failed to execute. See the inner exception for more details.", ex));
                    }
                }

                if (exceptions.Count > 0)
                {
                    hostingStartupAssemblyErrors = new AggregateException(exceptions);
                }
            }

            var contentRootPath = ResolveContentRootPath(_options.ContentRootPath, AppContext.BaseDirectory);

            // Initialize the host environment
            ApplicationHostEnvironment hostEnvironment = new ApplicationHostEnvironment();

            hostEnvironment.Initialize(contentRootPath, _options);

            _context.HostEnvironment = hostEnvironment;

            var services = new ServiceCollection();

            services.AddSingleton(_context);

            services.AddSingleton<IApplicationHostEnvironment>(hostEnvironment);

            services.AddSingleton<IHostEnvironment>((s) => s.GetService<IApplicationHostEnvironment>());

            services.AddSingleton(_options);
#if NETSTANDARD2_0
            services.AddSingleton(new WebHostOptions(_config, Assembly.GetEntryAssembly()?.GetName().Name));
#elif NET6_0
            Type webHostOptionsType = typeof(WebHostBuilder).Assembly.GetType("Microsoft.AspNetCore.Hosting.WebHostOptions");

            services.AddSingleton(webHostOptionsType, Activator.CreateInstance(webHostOptionsType, _config, Assembly.GetEntryAssembly()?.GetName().Name));
#elif NET8_0_OR_GREATER
            Type webHostOptionsType = typeof(WebHostBuilder).Assembly.GetType("Microsoft.AspNetCore.Hosting.WebHostOptions");

            services.AddSingleton(webHostOptionsType, Activator.CreateInstance(webHostOptionsType, _config, null, hostEnvironment));
#endif
            var builder = new ConfigurationBuilder()
                .SetBasePath(hostEnvironment.ContentRootPath)
                .AddConfiguration(_config);

            _configureAppConfigurationBuilder?.Invoke(_context, builder);

            var configuration = builder.Build();
            // register configuration as factory to make it dispose with the service provider
            services.AddSingleton<IConfiguration>(_ => configuration);
            _context.Configuration = configuration;

            var listener = new DiagnosticListener("Microsoft.AspNetCore");
            services.AddSingleton<DiagnosticListener>(listener);
            services.AddSingleton<DiagnosticSource>(listener);

            Type messagingApplicationBuilderFactoryServiceType = 
                GetMessagingApplicationBuilderFactoryServiceType(out Type messagingApplicationBuilderImplementationType);

            services.AddTransient(messagingApplicationBuilderFactoryServiceType, messagingApplicationBuilderImplementationType);

            AddMessagingMiddlewareFactory(services);

            services.AddOptions();
            if (_options.DetailedErrors)
                services.AddLogging(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace));
            else
                services.AddLogging();

            services.AddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();

            if (!string.IsNullOrEmpty(_options.StartupAssembly))
            {
                AddMessagingApplicationStartup(hostEnvironment, services);
            }

            _configureServices?.Invoke(services);
            _configureHostServices?.Invoke(_context, services);

            return services;
        }

        protected abstract void AddMessagingApplicationStartup(ApplicationHostEnvironment hostEnvironment, ServiceCollection services);

        protected abstract void AddMessagingMiddlewareFactory(ServiceCollection services);

        protected abstract Type GetMessagingApplicationBuilderFactoryServiceType(out Type messagingApplicationBuilderImplementationType);

        private void AddApplicationServices(IServiceCollection services, IServiceProvider hostingServiceProvider)
        {
            // We are forwarding services from hosting container so hosting container
            // can still manage their lifetime (disposal) shared instances with application services.
            // NOTE: This code overrides original services lifetime. Instances would always be singleton in
            // application container.
            var listener = hostingServiceProvider.GetService<DiagnosticListener>();
            services.Replace(ServiceDescriptor.Singleton(typeof(DiagnosticListener), listener));
            services.Replace(ServiceDescriptor.Singleton(typeof(DiagnosticSource), listener));
        }

        private string ResolveContentRootPath(string contentRootPath, string basePath)
        {
            if (string.IsNullOrEmpty(contentRootPath))
            {
                return basePath;
            }
            if (Path.IsPathRooted(contentRootPath))
            {
                return contentRootPath;
            }
            return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
        }
    }

    /// <summary>
    /// A builder for <see cref="IWebHost"/>
    /// </summary>
    public class DefaultMessagingContextHostBuilder<MessageContext> :
        AbstractMessagingContextHostBuilder,
        IProtocolMessagingContextMessagingHostBuilder<MessageContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessagingContextHostBuilder"/> class.
        /// </summary>
        public DefaultMessagingContextHostBuilder() : base()
        {
        }

        protected override void AddMessagingApplicationStartup(ApplicationHostEnvironment hostEnvironment, ServiceCollection services)
        {
            try
            {
                var startupType = MessagingContextHostStartupLoader<MemberAccessException>.FindStartupType(Options.StartupAssembly, hostEnvironment.EnvironmentName);

                if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                {
                    services.AddSingleton(typeof(IMessagingApplicationStartup<MessageContext>), startupType);
                }
                else
                {
                    services.AddSingleton(typeof(IMessagingApplicationStartup<MessageContext>), sp =>
                    {
                        var methods = MessagingContextHostStartupLoader<MessageContext>.LoadMethods(sp, startupType, hostEnvironment.EnvironmentName);

                        return new ConventionBasedMessagingContextHostStartup<MessageContext>(methods);
                    });
                }
            }
            catch (Exception ex)
            {
                var capture = ExceptionDispatchInfo.Capture(ex);
                services.AddSingleton<IMessagingApplicationStartup<MessageContext>>(_ =>
                {
                    capture.Throw();
                    return null;
                });
            }
        }

        protected override void AddMessagingMiddlewareFactory(ServiceCollection services)
        {
            services.AddScoped<IMessagingMiddlewareFactory<MessageContext>, MessagingMiddlewareFactory<MessageContext>>();
        }

        protected override IMessagingHost CreateMessagingHost(IConfiguration configuration, HostOptions hostOptions, IServiceProvider hostingServiceProvider, IServiceCollection applicationServices, AggregateException hostingStartupAssemblyErrors)
        {
            return new
                DefaultMessagingContextHost<MessageContext>(
                    configuration,
                    hostOptions,
                    hostingServiceProvider,
                    applicationServices,
                    hostingStartupAssemblyErrors);
        }

        protected override Type GetMessagingApplicationBuilderFactoryServiceType(out Type concreteMessagingApplicationBuilderType)
        {
            var protocolType = ImplementationTypeInfo<IMessagingApplicationBuilderFactory<MessageContext>>.GetImplementationTypeInfo<DefaultMessagingApplicationBuilderFactory<MessageContext>>();
            concreteMessagingApplicationBuilderType = protocolType.ImplementationType;
            return protocolType.AbstractType;
        }
    }

    /// <summary>
    /// A builder for <see cref="IWebHost"/>
    /// </summary>
    public class DefaultMessagingContextHostBuilder<ProtocolContext, MessageContext> :
        AbstractMessagingContextHostBuilder,
        IProtocolMessagingContextMessagingHostBuilder<ProtocolContext, MessageContext>
    {
        //private Action<MessagingHostBuilderContext, IConfigurationBuilder> _configureAppConfigurationBuilder;
        //private Action<MessagingHostBuilderContext, IServiceCollection> _configureHostServices;
        //private Action<IServiceCollection> _configureServices;

        //private IConfiguration _config;
        //private HostOptions _options;
        //private MessagingHostBuilderContext _context;
        //private bool _webHostBuilt;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessagingContextHostBuilder"/> class.
        /// </summary>
        public DefaultMessagingContextHostBuilder() : base()
        {
        }

        /// <summary>
        /// Get the setting value from the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to look up.</param>
        /// <returns>The value the setting currently contains.</returns>
        //public string GetSetting(string key)
        //{
        //    return _config[key];
        //}

        /// <summary>
        /// Add or replace a setting in the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to add or replace.</param>
        /// <param name="value">The value of the setting to add or replace.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        //public IWebHostBuilder UseSetting(string key, string value)
        //{
        //    _config[key] = value;
        //    return this;
        //}

        /// <summary>
        /// Adds a delegate for configuring the <see cref="IConfigurationBuilder"/> that will construct an <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder" /> that will be used to construct an <see cref="IConfiguration" />.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        /// <remarks>
        /// The <see cref="IConfiguration"/> and <see cref="ILoggerFactory"/> on the <see cref="WebHostBuilderContext"/> are uninitialized at this stage.
        /// The <see cref="IConfigurationBuilder"/> is pre-populated with the settings of the <see cref="IMessagingHostBuilder"/>.
        /// </remarks>
        //public IMessagingHostBuilder ConfigureAppConfiguration(Action<MessagingHostBuilderContext, IConfigurationBuilder> configureDelegate)
        //{
        //    _configureAppConfigurationBuilder += configureDelegate;

        //    return this;
        //}

        /// <summary>
        /// Adds a delegate for configuring the <see cref="IConfigurationBuilder"/> that will construct an <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder" /> that will be used to construct an <see cref="IConfiguration" />.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        /// <remarks>
        /// The <see cref="IConfiguration"/> and <see cref="ILoggerFactory"/> on the <see cref="WebHostBuilderContext"/> are uninitialized at this stage.
        /// The <see cref="IConfigurationBuilder"/> is pre-populated with the settings of the <see cref="IMessagingHostBuilder"/>.
        /// </remarks>
        //public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        //{
        //    throw new NotImplementedException(nameof(ConfigureAppConfiguration));
        //}

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. This may be called
        /// multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        //public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        //{
        //    if (configureServices == null)
        //    {
        //        throw new ArgumentNullException(nameof(configureServices));
        //    }

        //    _configureServices += configureServices;

        //    return this;
        //}

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. 
        /// This may be called multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        //public IMessagingHostBuilder ConfigureServices(Action<MessagingHostBuilderContext, IServiceCollection> configureServices)
        //{
        //    if (configureServices == null)
        //    {
        //        throw new ArgumentNullException(nameof(configureServices));
        //    }

        //    _configureHostServices += configureServices;
            
        //    return this;
        //}

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. 
        /// This may be called multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IMessagingHostBuilder"/>.</returns>
        //public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        //{
        //    throw new NotImplementedException(nameof(ConfigureServices));
        //}

        /// <summary>
        /// Builds the required services and an <see cref="IWebHost"/> which hosts a web application.
        /// </summary>
        //public IWebHost Build()
        //{
        //    if (_webHostBuilt)
        //    {
        //        throw new InvalidOperationException(Resources.WebHostBuilder_SingleInstance);
        //    }
        //    _webHostBuilt = true;

        //    var hostingServices = BuildCommonServices(out var hostingStartupAssemblyErrors);
        //    var applicationServices = hostingServices.Clone();
        //    var hostingServiceProvider = GetProviderFromFactory(hostingServices);

        //    if (!_options.SuppressStatusMessages)
        //    {
        //        // Warn about deprecated environment variables
        //        if (Environment.GetEnvironmentVariable("Hosting:Environment") != null)
        //        {
        //            Console.WriteLine("The environment variable 'Hosting:Environment' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
        //        }

        //        if (Environment.GetEnvironmentVariable("ASPNET_ENV") != null)
        //        {
        //            Console.WriteLine("The environment variable 'ASPNET_ENV' is obsolete and has been replaced with 'ASPNETCORE_ENVIRONMENT'");
        //        }

        //        if (Environment.GetEnvironmentVariable("ASPNETCORE_SERVER.URLS") != null)
        //        {
        //            Console.WriteLine("The environment variable 'ASPNETCORE_SERVER.URLS' is obsolete and has been replaced with 'ASPNETCORE_URLS'");
        //        }
        //    }

        //    AddApplicationServices(applicationServices, hostingServiceProvider);

        //    IMessagingHost host = 
        //        CreateMessageHandlerHost(applicationServices, hostingServiceProvider, _options, _config, hostingStartupAssemblyErrors);

        //    try
        //    {
        //        host.Initialize();

        //        // resolve configuration explicitly once to mark it as resolved within the
        //        // service provider, ensuring it will be properly disposed with the provider
        //        _ = host.Services.GetService<IConfiguration>();

        //        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(host.GetType());

        //        // Warn about duplicate HostingStartupAssemblies
        //        foreach (var assemblyName in _options.GetFinalHostingStartupAssemblies().GroupBy(a => a, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        //        {
        //            logger.LogWarning($"The assembly {assemblyName} was specified multiple times. Hosting startup assemblies should only be specified once.");
        //        }

        //        return host;
        //    }
        //    catch
        //    {
        //        // Dispose the host if there's a failure to initialize, this should dispose
        //        // services that were constructed until the exception was thrown
        //        host.Dispose();
        //        throw;
        //    }

        //    IServiceProvider GetProviderFromFactory(IServiceCollection collection)
        //    {
        //        var provider = collection.BuildServiceProvider();
        //        var factory = provider.GetService<IServiceProviderFactory<IServiceCollection>>();

        //        if (factory != null && !(factory is DefaultServiceProviderFactory))
        //        {
        //            using (provider)
        //            {
        //                return factory.CreateServiceProvider(factory.CreateBuilder(collection));
        //            }
        //        }

        //        return provider;
        //    }
        //}

        //protected virtual IMessagingHost CreateMessageHandlerHost(IServiceCollection applicationServices, IServiceProvider hostingServiceProvider, HostOptions hostOptions, IConfiguration configuration, AggregateException hostingStartupAssemblyErrors)
        //{
        //    return new 
        //        DefaultMessagingContextHost<ProtocolContext, MessageContext>(
        //            applicationServices,
        //            hostingServiceProvider,
        //            hostOptions,
        //            configuration,
        //            hostingStartupAssemblyErrors);
        //}

        //private IServiceCollection BuildCommonServices(out AggregateException hostingStartupAssemblyErrors)
        //{
        //    hostingStartupAssemblyErrors = null;

        //    _options = new HostOptions(_config, Assembly.GetEntryAssembly()?.GetName().Name);

        //    if (!_options.PreventHostingStartup)
        //    {
        //        var exceptions = new List<Exception>();

        //        // Execute the hosting startup assemblies
        //        foreach (var assemblyName in _options.GetFinalHostingStartupAssemblies().Distinct(StringComparer.OrdinalIgnoreCase))
        //        {
        //            try
        //            {
        //                var assembly = Assembly.Load(new AssemblyName(assemblyName));

        //                foreach (var attribute in assembly.GetCustomAttributes<HostingStartupAttribute>())
        //                {
        //                    var hostingStartup = (IHostingStartup)Activator.CreateInstance(attribute.HostingStartupType);
        //                    hostingStartup.Configure(this);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                // Capture any errors that happen during startup
        //                exceptions.Add(new InvalidOperationException($"Startup assembly {assemblyName} failed to execute. See the inner exception for more details.", ex));
        //            }
        //        }

        //        if (exceptions.Count > 0)
        //        {
        //            hostingStartupAssemblyErrors = new AggregateException(exceptions);
        //        }
        //    }

        //    var contentRootPath = ResolveContentRootPath(_options.ContentRootPath, AppContext.BaseDirectory);

        //    // Initialize the hosting environment
        //    HostEnvironment hostEnvironment = new HostEnvironment();

        //    hostEnvironment.Initialize(contentRootPath, _options);

        //    _context.HostEnvironment = hostEnvironment;

        //    var services = new ServiceCollection();

        //    services.AddSingleton(_options);
        //    services.AddSingleton(new WebHostOptions(_config, Assembly.GetEntryAssembly()?.GetName().Name));
        //    services.AddSingleton<IHostEnvironment>(hostEnvironment);
        //    services.AddSingleton<IHostingEnvironment, HostingEnvironment>();
        //    services.AddSingleton(_context);

        //    var builder = new ConfigurationBuilder()
        //        .SetBasePath(hostEnvironment.ContentRootPath)
        //        .AddConfiguration(_config);

        //    _configureAppConfigurationBuilder?.Invoke(_context, builder);

        //    var configuration = builder.Build();
        //    // register configuration as factory to make it dispose with the service provider
        //    services.AddSingleton<IConfiguration>(_ => configuration);
        //    _context.Configuration = configuration;

        //    var listener = new DiagnosticListener("Microsoft.AspNetCore");
        //    services.AddSingleton<DiagnosticListener>(listener);
        //    services.AddSingleton<DiagnosticSource>(listener);

        //    Type abstractType = GetMessagingApplicationBuilderType(out Type concreteType);

        //    services.AddTransient(abstractType, concreteType);

        //    services.AddScoped<IMessagingMiddlewareFactory<MessageContext>, MessagingMiddlewareFactory<MessageContext>>();
        //    services.AddOptions();
        //    if (_options.DetailedErrors)
        //        services.AddLogging(loggingBuilder => loggingBuilder.SetMinimumLevel(LogLevel.Trace));
        //    else
        //        services.AddLogging();

        //    services.AddTransient<IServiceProviderFactory<IServiceCollection>, DefaultServiceProviderFactory>();

        //    if (!string.IsNullOrEmpty(_options.StartupAssembly))
        //    {
        //        try
        //        {
        //            var startupType = MessagingContextHostStartupLoader<ProtocolContext, MemberAccessException>.FindStartupType(_options.StartupAssembly, hostEnvironment.EnvironmentName);

        //            if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
        //            {
        //                services.AddSingleton(typeof(IMessagingApplicationStartup<ProtocolContext, MessageContext>), startupType);
        //            }
        //            else
        //            {
        //                services.AddSingleton(typeof(IMessagingApplicationStartup<ProtocolContext, MessageContext>), sp =>
        //                {
        //                    var methods = MessagingContextHostStartupLoader<ProtocolContext, MessageContext>.LoadMethods(sp, startupType, hostEnvironment.EnvironmentName);
                            
        //                    return new ConventionBasedMessagingContextHostStartup<ProtocolContext, MessageContext>(methods);
        //                });
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var capture = ExceptionDispatchInfo.Capture(ex);
        //            services.AddSingleton<IMessagingApplicationStartup<ProtocolContext, MessageContext>>(_ =>
        //            {
        //                capture.Throw();
        //                return null;
        //            });
        //        }
        //    }

        //    _configureServices?.Invoke(services);
        //    _configureHostServices?.Invoke(_context, services);

        //    return services;
        //}

        //protected virtual Type GetMessagingApplicationBuilderType(out Type concreteType) 
        //{
        //    if (typeof(ProtocolContext) == typeof(MessageContext))
        //    {
        //        var protocolType = ImplementationTypeInfo<IMessagingApplicationBuilderFactory<ProtocolContext>>.GetConcreteImplementationTypeInfo<DefaultMessagingApplicationBuilderFactory<ProtocolContext>>();
        //        concreteType = protocolType.ConcreteType;
        //        return protocolType.AbstractType;
        //    }
        //    var protocolMessagingType = ImplementationTypeInfo<IMessagingApplicationBuilderFactory<ProtocolContext, MessageContext>>.GetConcreteImplementationTypeInfo<DefaultMessagingApplicationBuilderFactory<ProtocolContext, MessageContext>>();
        //    concreteType = protocolMessagingType.ConcreteType;
        //    return protocolMessagingType.AbstractType;
        //}

        //private void AddApplicationServices(IServiceCollection services, IServiceProvider hostingServiceProvider)
        //{
        //    // We are forwarding services from hosting container so hosting container
        //    // can still manage their lifetime (disposal) shared instances with application services.
        //    // NOTE: This code overrides original services lifetime. Instances would always be singleton in
        //    // application container.
        //    var listener = hostingServiceProvider.GetService<DiagnosticListener>();
        //    services.Replace(ServiceDescriptor.Singleton(typeof(DiagnosticListener), listener));
        //    services.Replace(ServiceDescriptor.Singleton(typeof(DiagnosticSource), listener));
        //}

        //private string ResolveContentRootPath(string contentRootPath, string basePath)
        //{
        //    if (string.IsNullOrEmpty(contentRootPath))
        //    {
        //        return basePath;
        //    }
        //    if (Path.IsPathRooted(contentRootPath))
        //    {
        //        return contentRootPath;
        //    }
        //    return Path.Combine(Path.GetFullPath(basePath), contentRootPath);
        //}

        protected override IMessagingHost CreateMessagingHost(IConfiguration configuration, HostOptions hostOptions, IServiceProvider hostingServiceProvider, IServiceCollection applicationServices, AggregateException hostingStartupAssemblyErrors)
        {
            return new
                DefaultMessagingContextHost<ProtocolContext, MessageContext>(
                    configuration,
                    hostOptions,
                    hostingServiceProvider,
                    applicationServices,
                    hostingStartupAssemblyErrors);
        }

        protected override void AddMessagingApplicationStartup(ApplicationHostEnvironment hostEnvironment, ServiceCollection services)
        {
            try
            {
                var startupType = MessagingContextHostStartupLoader<ProtocolContext, MemberAccessException>.FindStartupType(Options.StartupAssembly, hostEnvironment.EnvironmentName);

                if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                {
                    services.AddSingleton(typeof(IMessagingApplicationStartup<ProtocolContext, MessageContext>), startupType);
                }
                else
                {
                    services.AddSingleton(typeof(IMessagingApplicationStartup<ProtocolContext, MessageContext>), sp =>
                    {
                        var methods = MessagingContextHostStartupLoader<ProtocolContext, MessageContext>.LoadMethods(sp, startupType, hostEnvironment.EnvironmentName);

                        return new ConventionBasedMessagingContextHostStartup<ProtocolContext, MessageContext>(methods);
                    });
                }
            }
            catch (Exception ex)
            {
                var capture = ExceptionDispatchInfo.Capture(ex);
                services.AddSingleton<IMessagingApplicationStartup<ProtocolContext, MessageContext>>(_ =>
                {
                    capture.Throw();
                    return null;
                });
            }
        }

        protected override void AddMessagingMiddlewareFactory(ServiceCollection services)
        {
            services.AddScoped<IMessagingMiddlewareFactory<MessageContext>, MessagingMiddlewareFactory<MessageContext>>();
        }

        protected override Type GetMessagingApplicationBuilderFactoryServiceType(out Type concreteMessagingApplicationBuilderType)
        {
            var protocolMessagingType = ImplementationTypeInfo<IMessagingApplicationBuilderFactory<ProtocolContext, MessageContext>>.GetImplementationTypeInfo<DefaultMessagingApplicationBuilderFactory<ProtocolContext, MessageContext>>();
            concreteMessagingApplicationBuilderType = protocolMessagingType.ImplementationType;
            return protocolMessagingType.AbstractType;
        }
    }

    /// <summary>
    /// A builder for <see cref="IWebHost"/>
    /// </summary>
    public class DefaultMessagingContextHostBuilder<ProtocolContextHost, ProtocolContext, MessageContext> :
        AbstractMessagingContextHostBuilder,
        IProtocolMessagingContextMessagingHostBuilder<ProtocolContextHost, ProtocolContext, MessageContext>
        where ProtocolContextHost : IProtocolContextHost<ProtocolContext>
    {
        //private Action<MessagingHostBuilderContext, IConfigurationBuilder> _configureAppConfigurationBuilder;
        //private Action<MessagingHostBuilderContext, IServiceCollection> _configureHostServices;
        //private Action<IServiceCollection> _configureServices;

        //private IConfiguration _config;
        //private HostOptions _options;
        //private MessagingHostBuilderContext _context;
        //private bool _webHostBuilt;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMessagingContextHostBuilder"/> class.
        /// </summary>
        public DefaultMessagingContextHostBuilder() : base()
        {
        }

        protected override IMessagingHost CreateMessagingHost(IConfiguration configuration, HostOptions hostOptions, IServiceProvider hostingServiceProvider, IServiceCollection applicationServices, AggregateException hostingStartupAssemblyErrors)
        {
            return new
                DefaultMessagingContextHost<ProtocolContextHost, ProtocolContext, MessageContext>(
                    configuration,
                    hostOptions,
                    hostingServiceProvider,
                    applicationServices,
                    hostingStartupAssemblyErrors);
        }

        protected override void AddMessagingApplicationStartup(ApplicationHostEnvironment hostEnvironment, ServiceCollection services)
        {
            try
            {
                var startupType = MessagingContextHostStartupLoader<ProtocolContextHost, ProtocolContext, MemberAccessException>.FindStartupType(Options.StartupAssembly, hostEnvironment.EnvironmentName);

                if (typeof(IStartup).GetTypeInfo().IsAssignableFrom(startupType.GetTypeInfo()))
                {
                    services.AddSingleton(typeof(IMessagingApplicationStartup<ProtocolContextHost, ProtocolContext, MessageContext>), startupType);
                }
                else
                {
                    services.AddSingleton(typeof(IMessagingApplicationStartup<ProtocolContextHost, ProtocolContext, MessageContext>), sp =>
                    {
                        var methods = MessagingContextHostStartupLoader<ProtocolContextHost, ProtocolContext, MessageContext>.LoadMethods(sp, startupType, hostEnvironment.EnvironmentName);

                        return new ConventionBasedMessagingContextHostStartup<ProtocolContextHost, ProtocolContext, MessageContext>(methods);
                    });
                }
            }
            catch (Exception ex)
            {
                var capture = ExceptionDispatchInfo.Capture(ex);
                services.AddSingleton<IMessagingApplicationStartup<ProtocolContextHost, ProtocolContext, MessageContext>>(_ =>
                {
                    capture.Throw();
                    return null;
                });
            }
        }

        protected override void AddMessagingMiddlewareFactory(ServiceCollection services)
        {
            services.AddScoped<IMessagingMiddlewareFactory<MessageContext>, MessagingMiddlewareFactory<MessageContext>>();
        }

        protected override Type GetMessagingApplicationBuilderFactoryServiceType(out Type concreteMessagingApplicationBuilderFactoryType)
        {
            var protocolMessagingType = ImplementationTypeInfo<IMessagingApplicationBuilderFactory<ProtocolContextHost, ProtocolContext, MessageContext>>.GetImplementationTypeInfo<DefaultMessagingApplicationBuilderFactory<ProtocolContextHost, ProtocolContext, MessageContext>>();
            concreteMessagingApplicationBuilderFactoryType = protocolMessagingType.ImplementationType;
            return protocolMessagingType.AbstractType;
        }
    }
}
