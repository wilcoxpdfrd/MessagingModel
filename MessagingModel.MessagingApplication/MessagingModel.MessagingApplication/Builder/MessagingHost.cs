using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace AllVerge.MessagingModel.MessagingApplication.Builder
{
    using AllVerge.MessagingModel.MessagingApplication.Hosting;

    /// <summary>
    /// Provides convenience methods for creating instances of <see cref="IWebHost"/> and <see cref="IWebHostBuilder"/>.
    /// </summary>
    public static class MessagingHost
    {
        public static IWebHostBuilder CreateDefaultBuilder<MessageContext>() => 
            MessagingHost<MessageContext>.CreateDefaultBuilder();

        public static IWebHostBuilder CreateDefaultBuilder<MessageContext, Startup>(string[] args) 
            where Startup : class =>
            MessagingHost<MessageContext>.CreateDefaultBuilder<Startup>(args);

        public static IWebHostBuilder CreateDefaultBuilder<MessageContext, Startup>() 
            where Startup : class =>
            MessagingHost<MessageContext>.CreateDefaultBuilder<Startup>();

        public static IWebHostBuilder CreateDefaultBuilder<MessageContext>(string[] args) =>
            MessagingHost<MessageContext>.CreateDefaultBuilder(args);

        public static IWebHostBuilder CreateBuilder<ProtocolContextMessagingHostBuilder, MessageContext>()
            where ProtocolContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<MessageContext>, new() =>
            MessagingHost<MessageContext>.CreateBuilder<ProtocolContextMessagingHostBuilder>();

        public static IWebHostBuilder CreateBuilder<ProtocolContextMessagingHostBuilder, MessageContext, Startup>()
            where ProtocolContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<MessageContext>, new()
            where Startup : class =>
            MessagingHost<MessageContext>.CreateBuilder<ProtocolContextMessagingHostBuilder, Startup>();

        public static IWebHostBuilder CreateBuilder<ProtocolContextMessagingHostBuilder, MessageContext, Startup>(string[] args)
            where ProtocolContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<MessageContext>, new()
            where Startup : class =>
            MessagingHost<MessageContext>.CreateBuilder<ProtocolContextMessagingHostBuilder, Startup>(args);

        public static IWebHostBuilder CreateBuilderForProtocol<MessagingContextMessagingHostBuilder, ProtocolContext, MessageContext>() 
            where MessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContext, MessageContext>, new() =>
            MessagingHost<ProtocolContext, MessageContext>.CreateBuilder<MessagingContextMessagingHostBuilder>();

        public static IWebHostBuilder CreateBuilderForProtocol<MessagingContextMessagingHostBuilder, ProtocolContext, MessageContext, Startup>() 
            where MessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContext, MessageContext>, new() 
            where Startup : class =>
            MessagingHost<ProtocolContext, MessageContext>.CreateBuilder<MessagingContextMessagingHostBuilder, Startup>();

        public static IWebHostBuilder CreateBuilderForProtocol<MessagingContextMessagingHostBuilder, ProtocolContext, MessageContext, Startup>(string[] args) 
            where MessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContext, MessageContext>, new() 
            where Startup : class =>
            MessagingHost<ProtocolContext, MessageContext>.CreateBuilder<MessagingContextMessagingHostBuilder, Startup>(args);

        public static IWebHostBuilder CreateBuilderForProtocol<MessagingContextMessagingHostBuilder, ProtocolContextHost, ProtocolContext, MessageContext, Startup>(string[] args)
            where MessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContextHost, ProtocolContext, MessageContext>, new()
            where ProtocolContextHost : IProtocolContextHost<ProtocolContext>
            where Startup : class =>
            MessagingHost<ProtocolContextHost, ProtocolContext, MessageContext>.CreateBuilder<MessagingContextMessagingHostBuilder, Startup>(args);
    }

    /// <summary>
    /// Provides convenience methods for creating instances of <see cref="IWebHost"/> and <see cref="IWebHostBuilder"/> with pre-configured defaults.
    /// </summary>
    internal static class MessagingHost<MessageContext>
    {
        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="app">A delegate that handles requests to the application.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost Start(MessagingContextMiddlewareDelegate<MessageContext> app) =>
            Start(url: null, app: app);

        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="url">The URL the hosted application will listen on.</param>
        /// <param name="app">A delegate that handles requests to the application.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost Start(string url, MessagingContextMiddlewareDelegate<MessageContext> app)
        {
            var startupAssemblyName = app.GetMethodInfo().DeclaringType.GetTypeInfo().Assembly.GetName().Name;
            return StartWith(url: url, configureServices: null, app: appBuilder => appBuilder.Run(app), applicationName: startupAssemblyName);
        }

        ///// <summary>
        ///// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        ///// See <see cref="CreateDefaultBuilder()"/> for details.
        ///// </summary>
        ///// <param name="routeBuilder">A delegate that configures the router for handling requests to the application.</param>
        ///// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        //public static IWebHost Start(Action<IRouteBuilder> routeBuilder) =>
        //    Start(url: null, routeBuilder: routeBuilder);

        ///// <summary>
        ///// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        ///// See <see cref="CreateDefaultBuilder()"/> for details.
        ///// </summary>
        ///// <param name="url">The URL the hosted application will listen on.</param>
        ///// <param name="routeBuilder">A delegate that configures the router for handling requests to the application.</param>
        ///// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        //public static IWebHost Start(string url, Action<IRouteBuilder> routeBuilder)
        //{
        //    var startupAssemblyName = routeBuilder.GetMethodInfo().DeclaringType.GetTypeInfo().Assembly.GetName().Name;
        //    return StartWith(url, services => services.AddRouting(), appBuilder => appBuilder.UseRouter(routeBuilder), applicationName: startupAssemblyName);
        //}

        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="app">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost StartWith(Action<IMessagingApplicationBuilder<MessageContext>> app) =>
            StartWith(url: null, app: app);

        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="url">The URL the hosted application will listen on.</param>
        /// <param name="app">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost StartWith(string url, Action<IMessagingApplicationBuilder<MessageContext>> app) =>
            StartWith(url: url, configureServices: null, app: app, applicationName: null);

        private static IWebHost StartWith(string url, Action<IServiceCollection> configureServices, Action<IMessagingApplicationBuilder<MessageContext>> app, string applicationName)
        {
            IWebHostBuilder builder = CreateDefaultBuilder();

            if (!string.IsNullOrEmpty(url))
            {
                builder.UseUrls(url);
            }

            if (configureServices != null)
            {
                builder.ConfigureServices(configureServices);
            }

            builder.Configure(b => app.Invoke((IMessagingApplicationBuilder<MessageContext>)b));

            if (!string.IsNullOrEmpty(applicationName))
            {
                builder.UseSetting(WebHostDefaults.ApplicationKey, applicationName);
            }

            var host = builder.Build();

            host.Start();

            return host;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder() =>
            CreateDefaultBuilder(args: null);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <param name="args">The command line args.</param>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder(string[] args) =>
            CreateBuilder<DefaultMessagingContextHostBuilder<MessageContext>>(args);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>() 
            where ProtocolMessagingContextMessagingHostBuilder : 
                IProtocolMessagingContextMessagingHostBuilder<MessageContext>, new()
            => CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>(args: null);

        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>(string[] args) 
            where ProtocolMessagingContextMessagingHostBuilder : 
                IProtocolMessagingContextMessagingHostBuilder<MessageContext>, new()
        {
            var builder = new ProtocolMessagingContextMessagingHostBuilder();

            if (string.IsNullOrEmpty(builder.GetSetting(WebHostDefaults.ContentRootKey)))
            {
                builder.UseContentRoot(Directory.GetCurrentDirectory());
            }
            if (args != null)
            {
                builder.UseConfiguration(new ConfigurationBuilder().AddCommandLine(args).Build());
            }

            builder.ConfigureAppConfiguration((messagingHostBuilderContext, config) =>
            {
                IHostEnvironment env = messagingHostBuilderContext.HostEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment())
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureLogging((messagingHostBuilderContext, logging) =>
            {
                logging.AddConfiguration(messagingHostBuilderContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostEnvironment.IsDevelopment();
            });

            ConfigureWebDefaults(builder);

            return builder;
        }

        internal static void ConfigureWebDefaults(IWebHostBuilder builder)
        {
            //builder.ConfigureAppConfiguration((ctx, cb) =>
            //{
            //    if (ctx.HostingEnvironment.IsDevelopment())
            //    {
            //        StaticWebAssetsLoader.UseStaticWebAssets(ctx.HostingEnvironment, ctx.Configuration);
            //    }
            //});
            //builder.UseKestrel((builderContext, options) =>
            //{
            //    options.Configure(builderContext.Configuration.GetSection("Kestrel"));
            //})
            //.ConfigureServices((messagingHostBuilderContext, services) =>
            //{
            //    // Fallback
            //    services.PostConfigure<HostFilteringOptions>(options =>
            //    {
            //        if (options.AllowedHosts == null || options.AllowedHosts.Count == 0)
            //        {
            //            // "AllowedHosts": "localhost;127.0.0.1;[::1]"
            //            var hosts = messagingHostBuilderContext.Configuration["AllowedHosts"]?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            //            // Fall back to "*" to disable.
            //            options.AllowedHosts = (hosts?.Length > 0 ? hosts : new[] { "*" });
            //        }
            //    });
            //    // Change notification
            //    services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(
            //                new ConfigurationChangeTokenSource<HostFilteringOptions>(messagingHostBuilderContext.Configuration));

            //    services.AddTransient<IStartupFilter, HostFilteringStartupFilter>();

            //    if (string.Equals("true", messagingHostBuilderContext.Configuration["ForwardedHeaders_Enabled"], StringComparison.OrdinalIgnoreCase))
            //    {
            //        services.Configure<ForwardedHeadersOptions>(options =>
            //        {
            //            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            //            // Only loopback proxies are allowed by default. Clear that restriction because forwarders are
            //            // being enabled by explicit configuration.
            //            options.KnownNetworks.Clear();
            //            options.KnownProxies.Clear();
            //        });

            //        services.AddTransient<IStartupFilter, ForwardedHeadersStartupFilter>();
            //    }

            //    services.AddRouting();
            //})
            //.UseIIS()
            //.UseIISIntegration();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder<TStartup>() where TStartup : class =>
            CreateDefaultBuilder().UseStartup<MessageContext, TStartup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <param name="args">The command line args.</param>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder<TStartup>(string[] args) where TStartup : class =>
            CreateDefaultBuilder(args).UseStartup<MessageContext, TStartup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name="ProtocolMessagingContextMessagingHostBuilder">The type that builds the ProtocolMessagingContextMessagingHost.</typeparam>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder, TStartup>()
            where ProtocolMessagingContextMessagingHostBuilder :
                IProtocolMessagingContextMessagingHostBuilder<MessageContext>, new()
            where TStartup : class =>
            CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>().UseStartup<MessageContext, TStartup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name="ProtocolMessagingContextMessagingHostBuilder">The type that builds the ProtocolMessagingContextMessagingHost.</typeparam>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <param name="args">The command line args.</param>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder, TStartup>(string[] args) 
            where ProtocolMessagingContextMessagingHostBuilder :
                IProtocolMessagingContextMessagingHostBuilder<MessageContext>, new() where TStartup : class =>
            CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>(args).UseStartup<MessageContext, TStartup>();
    }



    /// <summary>
    /// Provides convenience methods for creating instances of <see cref="IWebHost"/> and <see cref="IWebHostBuilder"/> with pre-configured defaults.
    /// </summary>
    internal static class MessagingHost<ProtocolContext, MessageContext>
    {
        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="app">A delegate that handles requests to the application.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost Start(MessagingContextMiddlewareDelegate<MessageContext> app) =>
            Start(url: null, app: app);

        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="url">The URL the hosted application will listen on.</param>
        /// <param name="app">A delegate that handles requests to the application.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost Start(string url, MessagingContextMiddlewareDelegate<MessageContext> app)
        {
            var startupAssemblyName = app.GetMethodInfo().DeclaringType.GetTypeInfo().Assembly.GetName().Name;
            return StartWith(url: url, configureServices: null, app: appBuilder => appBuilder.Run(app), applicationName: startupAssemblyName);
        }

        ///// <summary>
        ///// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        ///// See <see cref="CreateDefaultBuilder()"/> for details.
        ///// </summary>
        ///// <param name="routeBuilder">A delegate that configures the router for handling requests to the application.</param>
        ///// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        //public static IWebHost Start(Action<IRouteBuilder> routeBuilder) =>
        //    Start(url: null, routeBuilder: routeBuilder);

        ///// <summary>
        ///// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        ///// See <see cref="CreateDefaultBuilder()"/> for details.
        ///// </summary>
        ///// <param name="url">The URL the hosted application will listen on.</param>
        ///// <param name="routeBuilder">A delegate that configures the router for handling requests to the application.</param>
        ///// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        //public static IWebHost Start(string url, Action<IRouteBuilder> routeBuilder)
        //{
        //    var startupAssemblyName = routeBuilder.GetMethodInfo().DeclaringType.GetTypeInfo().Assembly.GetName().Name;
        //    return StartWith(url, services => services.AddRouting(), appBuilder => appBuilder.UseRouter(routeBuilder), applicationName: startupAssemblyName);
        //}

        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="app">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost StartWith(Action<IMessagingApplicationBuilder<ProtocolContext, MessageContext>> app) =>
            StartWith(url: null, app: app);

        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="url">The URL the hosted application will listen on.</param>
        /// <param name="app">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost StartWith(string url, Action<IMessagingApplicationBuilder<ProtocolContext, MessageContext>> app) =>
            StartWith(url: url, configureServices: null, app: app, applicationName: null);

        private static IWebHost StartWith(string url, Action<IServiceCollection> configureServices, Action<IMessagingApplicationBuilder<ProtocolContext, MessageContext>> app, string applicationName)
        {
            var builder = CreateDefaultBuilder();

            if (!string.IsNullOrEmpty(url))
            {
                builder.UseUrls(url);
            }

            if (configureServices != null)
            {
                builder.ConfigureServices(configureServices);
            }

            builder.Configure(b => app.Invoke((IMessagingApplicationBuilder<ProtocolContext, MessageContext>)b));

            if (!string.IsNullOrEmpty(applicationName))
            {
                builder.UseSetting(WebHostDefaults.ApplicationKey, applicationName);
            }

            var host = builder.Build();

            host.Start();

            return host;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder() =>
            CreateDefaultBuilder(args: null);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <param name="args">The command line args.</param>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder(string[] args) =>
            CreateBuilder<DefaultMessagingContextHostBuilder<ProtocolContext, MessageContext>>(args);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>() where ProtocolMessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContext, MessageContext>, new()
            => CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>(args: null);

        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>(string[] args) where ProtocolMessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContext, MessageContext>, new()
        {
            var builder = new ProtocolMessagingContextMessagingHostBuilder();

            if (string.IsNullOrEmpty(builder.GetSetting(WebHostDefaults.ContentRootKey)))
            {
                builder.UseContentRoot(Directory.GetCurrentDirectory());
            }
            if (args != null)
            {
                builder.UseConfiguration(new ConfigurationBuilder().AddCommandLine(args).Build());
            }

            builder.ConfigureAppConfiguration((messagingHostBuilderContext, config) =>
            {
                IHostEnvironment env = messagingHostBuilderContext.HostEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment())
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureLogging((messagingHostBuilderContext, logging) =>
            {
                logging.AddConfiguration(messagingHostBuilderContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostEnvironment.IsDevelopment();
            });

            ConfigureWebDefaults(builder);

            return builder;
        }

        internal static void ConfigureWebDefaults(IWebHostBuilder builder)
        {
            //builder.ConfigureAppConfiguration((ctx, cb) =>
            //{
            //    if (ctx.HostingEnvironment.IsDevelopment())
            //    {
            //        StaticWebAssetsLoader.UseStaticWebAssets(ctx.HostingEnvironment, ctx.Configuration);
            //    }
            //});
            //builder.UseKestrel((builderContext, options) =>
            //{
            //    options.Configure(builderContext.Configuration.GetSection("Kestrel"));
            //})
            //.ConfigureServices((hostingContext, services) =>
            //{
            //    // Fallback
            //    services.PostConfigure<HostFilteringOptions>(options =>
            //    {
            //        if (options.AllowedHosts == null || options.AllowedHosts.Count == 0)
            //        {
            //            // "AllowedHosts": "localhost;127.0.0.1;[::1]"
            //            var hosts = messagingHostBuilderContext.Configuration["AllowedHosts"]?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            //            // Fall back to "*" to disable.
            //            options.AllowedHosts = (hosts?.Length > 0 ? hosts : new[] { "*" });
            //        }
            //    });
            //    // Change notification
            //    services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(
            //                new ConfigurationChangeTokenSource<HostFilteringOptions>(messagingHostBuilderContext.Configuration));

            //    services.AddTransient<IStartupFilter, HostFilteringStartupFilter>();

            //    if (string.Equals("true", messagingHostBuilderContext.Configuration["ForwardedHeaders_Enabled"], StringComparison.OrdinalIgnoreCase))
            //    {
            //        services.Configure<ForwardedHeadersOptions>(options =>
            //        {
            //            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            //            // Only loopback proxies are allowed by default. Clear that restriction because forwarders are
            //            // being enabled by explicit configuration.
            //            options.KnownNetworks.Clear();
            //            options.KnownProxies.Clear();
            //        });

            //        services.AddTransient<IStartupFilter, ForwardedHeadersStartupFilter>();
            //    }

            //    services.AddRouting();
            //})
            //.UseIIS()
            //.UseIISIntegration();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder<TStartup>() where TStartup : class =>
            CreateDefaultBuilder().UseStartup<ProtocolContext, MessageContext, TStartup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <param name="args">The command line args.</param>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder<TStartup>(string[] args) where TStartup : class =>
            CreateDefaultBuilder(args).UseStartup<ProtocolContext, MessageContext, TStartup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name="ProtocolMessagingContextMessagingHostBuilder">The type that builds the ProtocolMessagingContextMessagingHost.</typeparam>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder, TStartup>() 
            where ProtocolMessagingContextMessagingHostBuilder : 
                IProtocolMessagingContextMessagingHostBuilder<ProtocolContext, MessageContext>, new() 
            where TStartup : class =>
            CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>().UseStartup<ProtocolContext, MessageContext, TStartup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name="ProtocolMessagingContextMessagingHostBuilder">The type that builds the ProtocolMessagingContextMessagingHost.</typeparam>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <param name="args">The command line args.</param>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder, TStartup>(string[] args) where ProtocolMessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContext, MessageContext>, new() where TStartup : class =>
            CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>(args).UseStartup<ProtocolContext, MessageContext, TStartup>();
    }

    /// <summary>
    /// Provides convenience methods for creating instances of <see cref="IWebHost"/> and <see cref="IWebHostBuilder"/> with pre-configured defaults.
    /// </summary>
    internal static class MessagingHost<ProtocolContextHost, ProtocolContext, MessageContext> 
        where ProtocolContextHost : IProtocolContextHost<ProtocolContext>
    {
        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="app">A delegate that handles requests to the application.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost Start(MessagingContextMiddlewareDelegate<MessageContext> app) =>
            Start(url: null, app: app);

        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="url">The URL the hosted application will listen on.</param>
        /// <param name="app">A delegate that handles requests to the application.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost Start(string url, MessagingContextMiddlewareDelegate<MessageContext> app)
        {
            var startupAssemblyName = app.GetMethodInfo().DeclaringType.GetTypeInfo().Assembly.GetName().Name;
            return StartWith(url: url, configureServices: null, app: appBuilder => appBuilder.Run(app), applicationName: startupAssemblyName);
        }

        ///// <summary>
        ///// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        ///// See <see cref="CreateDefaultBuilder()"/> for details.
        ///// </summary>
        ///// <param name="routeBuilder">A delegate that configures the router for handling requests to the application.</param>
        ///// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        //public static IWebHost Start(Action<IRouteBuilder> routeBuilder) =>
        //    Start(url: null, routeBuilder: routeBuilder);

        ///// <summary>
        ///// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        ///// See <see cref="CreateDefaultBuilder()"/> for details.
        ///// </summary>
        ///// <param name="url">The URL the hosted application will listen on.</param>
        ///// <param name="routeBuilder">A delegate that configures the router for handling requests to the application.</param>
        ///// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        //public static IWebHost Start(string url, Action<IRouteBuilder> routeBuilder)
        //{
        //    var startupAssemblyName = routeBuilder.GetMethodInfo().DeclaringType.GetTypeInfo().Assembly.GetName().Name;
        //    return StartWith(url, services => services.AddRouting(), appBuilder => appBuilder.UseRouter(routeBuilder), applicationName: startupAssemblyName);
        //}

        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="app">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost StartWith(Action<IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>> app) =>
            StartWith(url: null, app: app);

        /// <summary>
        /// Initializes and starts a new <see cref="IWebHost"/> with pre-configured defaults.
        /// See <see cref="CreateDefaultBuilder()"/> for details.
        /// </summary>
        /// <param name="url">The URL the hosted application will listen on.</param>
        /// <param name="app">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A started <see cref="IWebHost"/> that hosts the application.</returns>
        public static IWebHost StartWith(string url, Action<IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>> app) =>
            StartWith(url: url, configureServices: null, app: app, applicationName: null);

        private static IWebHost StartWith(string url, Action<IServiceCollection> configureServices, Action<IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>> app, string applicationName)
        {
            var builder = CreateDefaultBuilder();

            if (!string.IsNullOrEmpty(url))
            {
                builder.UseUrls(url);
            }

            if (configureServices != null)
            {
                builder.ConfigureServices(configureServices);
            }

            builder.Configure(b => app.Invoke((IMessagingApplicationBuilder<ProtocolContextHost, ProtocolContext, MessageContext>)b));

            if (!string.IsNullOrEmpty(applicationName))
            {
                builder.UseSetting(WebHostDefaults.ApplicationKey, applicationName);
            }

            var host = builder.Build();

            host.Start();

            return host;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder() =>
            CreateDefaultBuilder(args: null);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <param name="args">The command line args.</param>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder(string[] args) =>
            CreateBuilder<DefaultMessagingContextHostBuilder<ProtocolContextHost, ProtocolContext, MessageContext>>(args);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     adds the HostFiltering middleware,
        ///     adds the ForwardedHeaders middleware if ASPNETCORE_FORWARDEDHEADERS_ENABLED=true,
        ///     and enable IIS integration.
        /// </remarks>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>() 
            where ProtocolMessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContextHost, ProtocolContext, MessageContext>, new()
            => CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>(args: null);

        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>(string[] args) 
            where ProtocolMessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContextHost, ProtocolContext, MessageContext>, new()
        {
            var builder = new ProtocolMessagingContextMessagingHostBuilder();

            if (string.IsNullOrEmpty(builder.GetSetting(WebHostDefaults.ContentRootKey)))
            {
                builder.UseContentRoot(Directory.GetCurrentDirectory());
            }
            if (args != null)
            {
                builder.UseConfiguration(new ConfigurationBuilder().AddCommandLine(args).Build());
            }

            builder.ConfigureAppConfiguration((messagingHostBuilderContext, config) =>
            {
                IHostEnvironment env = messagingHostBuilderContext.HostEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment())
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureLogging((messagingHostBuilderContext, logging) =>
            {
                logging.AddConfiguration(messagingHostBuilderContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                logging.AddDebug();
                logging.AddEventSourceLogger();
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostEnvironment.IsDevelopment();
            });

            ConfigureWebDefaults(builder);

            return builder;
        }

        internal static void ConfigureWebDefaults(IWebHostBuilder builder)
        {
            //builder.ConfigureAppConfiguration((ctx, cb) =>
            //{
            //    if (ctx.HostingEnvironment.IsDevelopment())
            //    {
            //        StaticWebAssetsLoader.UseStaticWebAssets(ctx.HostingEnvironment, ctx.Configuration);
            //    }
            //});
            //builder.UseKestrel((builderContext, options) =>
            //{
            //    options.Configure(builderContext.Configuration.GetSection("Kestrel"));
            //})
            //.ConfigureServices((messagingHostBuilderContext, services) =>
            //{
            //    // Fallback
            //    services.PostConfigure<HostFilteringOptions>(options =>
            //    {
            //        if (options.AllowedHosts == null || options.AllowedHosts.Count == 0)
            //        {
            //            // "AllowedHosts": "localhost;127.0.0.1;[::1]"
            //            var hosts = messagingHostBuilderContext.Configuration["AllowedHosts"]?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            //            // Fall back to "*" to disable.
            //            options.AllowedHosts = (hosts?.Length > 0 ? hosts : new[] { "*" });
            //        }
            //    });
            //    // Change notification
            //    services.AddSingleton<IOptionsChangeTokenSource<HostFilteringOptions>>(
            //                new ConfigurationChangeTokenSource<HostFilteringOptions>(messagingHostBuilderContext.Configuration));

            //    services.AddTransient<IStartupFilter, HostFilteringStartupFilter>();

            //    if (string.Equals("true", messagingHostBuilderContext.Configuration["ForwardedHeaders_Enabled"], StringComparison.OrdinalIgnoreCase))
            //    {
            //        services.Configure<ForwardedHeadersOptions>(options =>
            //        {
            //            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            //            // Only loopback proxies are allowed by default. Clear that restriction because forwarders are
            //            // being enabled by explicit configuration.
            //            options.KnownNetworks.Clear();
            //            options.KnownProxies.Clear();
            //        });

            //        services.AddTransient<IStartupFilter, ForwardedHeadersStartupFilter>();
            //    }

            //    services.AddRouting();
            //})
            //.UseIIS()
            //.UseIISIntegration();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder<TStartup>() where TStartup : class =>
            CreateDefaultBuilder().UseStartup<ProtocolContextHost, ProtocolContext, MessageContext, TStartup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <param name="args">The command line args.</param>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateDefaultBuilder<TStartup>(string[] args) where TStartup : class =>
            CreateDefaultBuilder(args).UseStartup<ProtocolContextHost, ProtocolContext, MessageContext, TStartup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name="ProtocolMessagingContextMessagingHostBuilder">The type that builds the ProtocolMessagingContextMessagingHost.</typeparam>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder, TStartup>()
            where ProtocolMessagingContextMessagingHostBuilder :
                IProtocolMessagingContextMessagingHostBuilder<ProtocolContextHost, ProtocolContext, MessageContext>, new()
            where TStartup : class =>
            CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>().UseStartup<ProtocolContextHost, ProtocolContext, MessageContext, TStartup>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHostBuilder"/> class with pre-configured defaults using typed Startup.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="WebHostBuilder"/>:
        ///     use Kestrel as the web server and configure it using the application's configuration providers,
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        ///     enable IIS integration.
        /// </remarks>
        /// <typeparam name="ProtocolMessagingContextMessagingHostBuilder">The type that builds the ProtocolMessagingContextMessagingHost.</typeparam>
        /// <typeparam name ="TStartup">The type containing the startup methods for the application.</typeparam>
        /// <param name="args">The command line args.</param>
        /// <returns>The initialized <see cref="IWebHostBuilder"/>.</returns>
        internal static IWebHostBuilder CreateBuilder<ProtocolMessagingContextMessagingHostBuilder, TStartup>(string[] args) where ProtocolMessagingContextMessagingHostBuilder : IProtocolMessagingContextMessagingHostBuilder<ProtocolContextHost, ProtocolContext, MessageContext>, new() where TStartup : class =>
            CreateBuilder<ProtocolMessagingContextMessagingHostBuilder>(args).UseStartup<ProtocolContextHost, ProtocolContext, MessageContext, TStartup>();
    }
}
