using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace AllVerge.MessagingModel.MessagingFoundation
{
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.SystemPrimitives.Net;
    using AllVerge.SystemPrimitives.Threading;
    using AllVerge.SystemPrimitives.Threading.Tasks;
    using System.Collections.Concurrent;

    public interface IResourceDispatcherHostMap: IEnumerable<ResourceDispatcherHost>
    {
        ResourceDispatcherHost Lookup(String serverAddress);
        void CloseResourceDisatcherHosts();
    }

    class ResourceDispatcherHostMap : IResourceDispatcherHostMap
    {
        Dictionary<String, KeyValuePair<Type, ResourceDispatcherHost>> map;

        public ResourceDispatcherHostMap(Dictionary<Type, ResourceDispatcherHost> items)
        {
            this.map =
                items.Aggregate(
                    new Dictionary<String, KeyValuePair<Type, ResourceDispatcherHost>>(), (map, kv) =>
                    {
                        ResourceDispatcherHost resourceDispatcherHost = kv.Value;

                        IEnumerable<String> baseAddresses = resourceDispatcherHost.BaseAddresses.Select(u => u.AbsoluteUri);

                        baseAddresses.Aggregate(map, (m, hostAddress) => 
                        {
                            m.Add(hostAddress, kv);
                            
                            return m;
                        });

                        return map;
                    });
        }

        public void CloseResourceDisatcherHosts()
        {
            this.map.ForEach(k =>
            {
                if (k.Value.Value.State == CommunicationState.Opened)
                {
                    k.Value.Value.Close();
                }
                else
                {
                    k.Value.Value.Abort();
                }
            });
        }

        public ResourceDispatcherHost Lookup(string serverAddress)
        {
            if (this.map.ContainsKey(serverAddress))
                return this.map[serverAddress].Value;
            return null;
        }

        IEnumerator<ResourceDispatcherHost> IEnumerable<ResourceDispatcherHost>.GetEnumerator()
        {
            return map.Values.Select(v => v.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return map.GetEnumerator();
        }
    }

    class ResourceDispatcherHostBuilder : Dictionary<Type, ResourceDispatcherHostBuilder.ResourceDispatcherHostConfiguration>
    {
        public class ResourceDispatcherHostConfiguration
        {
            List<Uri> baseAddresses = new List<Uri>();
            List<(String implementedContract, Binding binding, Uri address, Uri listenUri)> endPoints = new List<(string, Binding, Uri, Uri)>();

            public Uri[] BaseAddresses => baseAddresses.ToArray();

            public (string implementedContract, Binding binding, Uri address, Uri listenUri)[] EndPoints => endPoints.ToArray();

            public void AddBaseAddresses(IEnumerable<Uri> baseAddresses)
            {
                baseAddresses.Aggregate(this.baseAddresses, (l, a) =>
                {
                    if (!l.Contains(a))
                        l.Add(a);
                    return l;
                });
            }

            internal void AddServiceEndpoint(string implementedContract, Binding binding, Uri address, Uri listenUri)
            {
                this.endPoints.Add((implementedContract, binding, address, listenUri));
            }
        }

        public void TryAddBaseAddresses(Type resourceContractKey, params Uri[] baseAddresses)
        {
            this.TryAddBaseAddresses(resourceContractKey, (IEnumerable<Uri>)baseAddresses);
        }

        public void TryAddBaseAddresses(Type resourceContractKey, IEnumerable<Uri> baseAddresses)
        {
            if (!this.ContainsKey(resourceContractKey))

                this.Add(resourceContractKey, new ResourceDispatcherHostConfiguration());

            this[resourceContractKey].AddBaseAddresses(baseAddresses);
        }

        public void AddEndpoint(Type resourceContractKey, string implementedContract, Binding binding, Uri address, Uri listenUri = null)
        {
            if (listenUri != null && listenUri.IsAbsoluteUri)
            {
                Uri baseAddressUri = listenUri.ToBaseAddressUri();

                this.TryAddBaseAddresses(resourceContractKey, baseAddressUri);
            }
            else if (address.IsAbsoluteUri)
            {
                Uri baseAddressUri = address.ToBaseAddressUri();

                this.TryAddBaseAddresses(resourceContractKey, baseAddressUri);
            }
            else if (!this.ContainsKey(resourceContractKey))

                this.Add(resourceContractKey, new ResourceDispatcherHostConfiguration());

            this[resourceContractKey].AddServiceEndpoint(implementedContract, binding, address, listenUri);
        }

        internal Dictionary<Type, ResourceDispatcherHost> Build()
        {
            Dictionary<Type, ResourceDispatcherHost> resourceDispatcherHostForContractMap =
                this.Aggregate(new Dictionary<Type, ResourceDispatcherHost>(), (m, c) =>
                {
                    ResourceDispatcherHost resourceDispatcherHost = new ResourceDispatcherHost(c.Key, c.Value.BaseAddresses);

                    c.Value.EndPoints.Aggregate(resourceDispatcherHost, (h, e) =>
                    {
                        if (e.listenUri == null)

                            h.AddServiceEndpoint(e.implementedContract, e.binding, e.address);

                        else

                            h.AddServiceEndpoint(e.implementedContract, e.binding, e.address, e.listenUri);

                        return h;
                    });

                    m.Add(c.Key, resourceDispatcherHost);

                    return m;
                });

            return resourceDispatcherHostForContractMap;
        }
    }

    public class ResourceDispatcherHostConfigurationBuilder
    {
        private IWebHostBuilder hostBuilder;
        private ResourceDispatcherHostBuilder resourceDispatcherHostsBuilder = new ResourceDispatcherHostBuilder();

        internal ResourceDispatcherHostConfigurationBuilder(IWebHostBuilder hostBuilder)
        {
            this.hostBuilder = hostBuilder;
        }

        /// <summary>
        /// Adds the provided <paramref name="baseAddresses"/> to a <see cref="ResourceDispatcherHost"/> for the <typeparamref name="ResourceContract"/> service.
        /// </summary>
        /// <remarks>
        /// <br/>Each base address per <typeparamref name="ResourceContract"/> <b>must</b> specify a unique scheme.
        /// <br/>Each service added to the <see cref="ResourceDispatcherHost"/> to the  must specify a unique <see cref="Uri.Authority"/>/<see cref="Uri.Port"/> combination.
        /// <br/>Note: only needed when one or more ResourceDispatcher Endpoints are specified with a relative address for the <typeparamref name="ResourceContract"/> service.
        /// </remarks>
        /// <typeparam name="ResourceContract"></typeparam>
        /// <param name="baseAddresses"></param>
        /// <returns></returns>
        public ResourceDispatcherHostConfigurationBuilder AddResourceDispatcherHostBaseAddresses<ResourceContract>(params string[] baseAddresses)
            where ResourceContract : IMessagingDispatcher
        {
            return this.AddResourceDispatcherHostBaseAddresses<ResourceContract>(baseAddresses.Select(a => new Uri(a)));
        }

        /// <summary>
        /// Adds the provided <paramref name="baseAddresses"/> to a <see cref="ResourceDispatcherHost"/> for the <typeparamref name="ResourceContract"/> service.
        /// </summary>
        /// <remarks>
        /// <br/>Each base address per <typeparamref name="ResourceContract"/> <b>must</b> specify a unique scheme.
        /// <br/>Each service added to the <see cref="ResourceDispatcherHost"/> to the  must specify a unique <see cref="Uri.Authority"/>/<see cref="Uri.Port"/> combination.
        /// <br/>Note: only needed when one or more ResourceDispatcher Endpoints are specified with a relative address for the <typeparamref name="ResourceContract"/> service.
        /// </remarks>
        /// <typeparam name="ResourceContract"></typeparam>
        /// <param name="baseAddresses"></param>
        /// <returns></returns>
        public ResourceDispatcherHostConfigurationBuilder AddResourceDispatcherHostBaseAddresses<ResourceContract>(params Uri[] baseAddresses)
            where ResourceContract : IMessagingDispatcher
        {
            return this.AddResourceDispatcherHostBaseAddresses<ResourceContract>(baseAddresses);
        }

        /// <summary>
        /// Adds the provided <paramref name="baseAddresses"/> to a <see cref="ResourceDispatcherHost"/> for the <typeparamref name="ResourceContract"/> service.
        /// </summary>
        /// <remarks>
        /// <br/>Each base address per <typeparamref name="ResourceContract"/> <b>must</b> specify a unique scheme.
        /// <br/>Each service added to the <see cref="ResourceDispatcherHost"/> to the  must specify a unique <see cref="Uri.Authority"/>/<see cref="Uri.Port"/> combination.
        /// <br/>Note: only needed when one or more ResourceDispatcher Endpoints are specified with a relative address for the <typeparamref name="ResourceContract"/> service.
        /// </remarks>
        /// <typeparam name="ResourceContract"></typeparam>
        /// <param name="baseAddresses"></param>
        /// <returns></returns>
        public ResourceDispatcherHostConfigurationBuilder AddResourceDispatcherHostBaseAddresses<ResourceContract>(IEnumerable<Uri> baseAddresses)
            where ResourceContract : IMessagingDispatcher
        {
            Type resourceContractKey = typeof(ResourceContract);

            resourceDispatcherHostsBuilder.TryAddBaseAddresses(resourceContractKey, baseAddresses);

            return this;
        }

        /// <summary>
        /// Adds a ResourceDisatcher Endpoint for <typeparamref name="ResourceContract"/> (implementing <typeparamref name="IImplementedContract"/>) with the provided <paramref name="binding"/> and <paramref name="address"/>.
        /// Note: If <paramref name="address"/> is relative, a base address must be provided for <typeparamref name="ResourceContract"/> via a <see cref="AddResourceDisptacherHostBaseAddresses"/> overload.
        /// </summary>
        /// <typeparam name="ResourceContract"></typeparam>
        /// <param name="binding"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public ResourceDispatcherHostConfigurationBuilder AddResourceDispatcherEndpoint<ResourceContract, IImplementedContract>(Binding binding, String address)
            where ResourceContract : IImplementedContract, IMessagingDispatcher
        {
            return this.AddResourceDispatcherEndpoint<ResourceContract, IImplementedContract>(binding, new Uri(address), null);
        }

        /// <summary>
        /// Adds a ResourceDisatcher Endpoint for <typeparamref name="ResourceContract"/> (implementing <typeparamref name="IImplementedContract"/>) with the provided <paramref name="binding"/> and <paramref name="address"/>.
        /// Note: If <paramref name="address"/>/<paramref name="listenUri"/> is relative, a base address must be provided for <typeparamref name="ResourceContract"/> via a <see cref="AddResourceDisptacherHostBaseAddresses"/> overload.
        /// </summary>
        /// <typeparam name="ResourceContract"></typeparam>
        /// <param name="binding"></param>
        /// <param name="address"></param>
        /// <param name="listenUri"></param>
        /// <returns></returns>
        public ResourceDispatcherHostConfigurationBuilder AddResourceDispatcherEndpoint<ResourceContract, IImplementedContract>(Binding binding, String address, Uri listenUri)
            where ResourceContract : IImplementedContract, IMessagingDispatcher
        {
            return this.AddResourceDispatcherEndpoint<ResourceContract, IImplementedContract>(binding, new Uri(address), listenUri);
        }

        /// <summary>
        /// Adds a ResourceDisatcher Endpoint for <typeparamref name="ResourceContract"/> (implementing <typeparamref name="IImplementedContract"/>) with the provided <paramref name="binding"/> and <paramref name="address"/>.
        /// Note: If <paramref name="address"/> is relative, a base address must be provided for <typeparamref name="ResourceContract"/> via a <see cref="AddResourceDisptacherHostBaseAddresses"/> overload.
        /// </summary>
        /// <typeparam name="ResourceContract"></typeparam>
        /// <param name="binding"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public ResourceDispatcherHostConfigurationBuilder AddResourceDispatcherEndpoint<ResourceContract, IImplementedContract>(Binding binding, Uri address)
            where ResourceContract : IImplementedContract, IMessagingDispatcher
        {
            return this.AddResourceDispatcherEndpoint<ResourceContract, IImplementedContract>(binding, address, null);
        }

        /// <summary>
        /// Adds a ResourceDisatcher Endpoint for <typeparamref name="ResourceContract"/> (implementing <typeparamref name="IImplementedContract"/>) with the provided <paramref name="binding"/> and <paramref name="address"/>.
        /// Note: If <paramref name="address"/>/<paramref name="listenUri"/> is relative, a base address must be provided for <typeparamref name="ResourceContract"/> via a <see cref="AddResourceDisptacherHostBaseAddresses"/> overload.
        /// </summary>
        /// <typeparam name="ResourceContract"></typeparam>
        /// <param name="binding"></param>
        /// <param name="address"></param>
        /// <param name="listenUri"></param>
        /// <returns></returns>
        public ResourceDispatcherHostConfigurationBuilder AddResourceDispatcherEndpoint<ResourceContract, IImplementedContract>(Binding binding, Uri address, Uri listenUri)
            where ResourceContract : IImplementedContract, IMessagingDispatcher
        {
            Type resourceContractKey = typeof(ResourceContract);
            Type implementedContractType = typeof(IImplementedContract);

            String implementedContract = implementedContractType.FullName;

            if (!implementedContractType.IsInterface)

                throw new InvalidOperationException($"The type parameter {implementedContract} must be an interface.");

            this.resourceDispatcherHostsBuilder.AddEndpoint(resourceContractKey, implementedContract, binding, address, listenUri);

            return this;
        }

        public IWebHostBuilder Apply()
        {
            this.hostBuilder.PreferHostingUrls(true);
            return this.hostBuilder.UseResourceDispatchersHosts(this.resourceDispatcherHostsBuilder.Build());
        }
    }

    public static class ResourceDispatcherHostExtensions
    {
        /// <summary>
        /// Returns a configuration builder with which to configure middleware implementing 
        /// <see cref="MessageDispatcher{MessagingContext}"/> that has been added to the Host (<see cref="IWebHost"/>).
        /// Note: At least one ResoureDispatcher Endpoint must be added using this configuration builder.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static ResourceDispatcherHostConfigurationBuilder ConfigureResourceDispatcherHost(this IWebHostBuilder hostBuilder)
        {
            return new ResourceDispatcherHostConfigurationBuilder(hostBuilder);
        }

        internal static IWebHostBuilder UseResourceDispatchersHosts(this IWebHostBuilder hostBuilder, Dictionary<Type, ResourceDispatcherHost> serviceHosts)
        {
            return hostBuilder
                .UseMoreUrls(serviceHosts.SelectMany(p => p.Value.BaseAddresses).Select(u => u.AbsoluteUri).ToArray())
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IResourceDispatcherHostMap>(new ResourceDispatcherHostMap(serviceHosts));
                });
        }

        /// <summary>
        /// Uniquely adds the authority portion of each of the <paramref name="urls"/> to <paramref name="hostBuilder"/>.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="urls"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseMoreUrls(this IWebHostBuilder hostBuilder, params String[] urls)
        {
            String serverUrlsValue = hostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);

            List<String> serverUrls;
            
            if (serverUrlsValue == null)
                
                serverUrls = new List<string>();

            else

                serverUrls = new List<string>(hostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey).Split(';'));

            serverUrls.AddRange(urls);
            
            return hostBuilder.UseUrls(serverUrls.Distinct().ToArray());
        }

        public static IWebHostBuilder UseInstanceContext(this IWebHostBuilder hostBuilder, params IExtension<InstanceContext>[] extensions)
        {
            return hostBuilder
                .ConfigureServices(services =>
                {
                    services.AddScoped(typeof(InstanceContext), (sp) =>
                    {
                        InstanceContext instanceContext = new InstanceContext(null);

                        foreach (IExtension<InstanceContext> extension in extensions)

                            instanceContext.Extensions.Add(extension);

                        return instanceContext;
                    });
                });
        }

        private static void TryAddResourceChannelDispatcherManagerPerEndpointAddressTrieMap(Task<ResourceDispatcherHost> t, ResourceChannelDispatcherManagerPerEndpointAddressTrieMap resourceChannelDispatcherManagerPerEndpointAddressTrieMap, IServiceProvider serviceProvider, IPathEnvironment pathEnvironment, bool includeDuplexChannelListenersOnly, CancellationToken cancellationToken)
        {
            if (t.IsCompletedSuccessfully(out object asyncState, out Exception exception))
            {
                (Object AsyncState, ResourceDispatcherHost _) = ((Object, ResourceDispatcherHost))asyncState;

                string serverAddress = AsyncState as String;

                ResourceDispatcherHost resourceDispatcherHost = t.Result;

                resourceDispatcherHost.ThrowIfDisposedOrNotOpen();

                ChannelDispatcher channelDispatcher =
                    (ChannelDispatcher)resourceDispatcherHost.ChannelDispatchers.FirstOrDefault(d => d.Listener.Uri.AbsoluteUri.StartsWith(serverAddress));

                ResourceChannelDispatcherManager resourceChannelDispatcherManager =
                    new ResourceChannelDispatcherManager(channelDispatcher, resourceDispatcherHost, new InstanceContext(resourceDispatcherHost), cancellationToken);

                if (resourceChannelDispatcherManager.IsDuplexChannelListener == includeDuplexChannelListenersOnly)
                {
                    resourceChannelDispatcherManager.ConfigureEnvironment(pathEnvironment, serviceProvider);

                    resourceChannelDispatcherManagerPerEndpointAddressTrieMap.Add(new Uri(serverAddress), resourceChannelDispatcherManager);
                }
            }
            else

                throw exception;
        }

        /// <summary>
        /// Builds an <see cref="EndpointAddress"/> address TrieMap <see cref="UriTrieKeyedDictionary{ResourceChannelDispatcherManager}"/> of <see cref="ResourceChannelDispatcherManager"/>.
        /// Note: Any address in <paramref name="serverAddresses"/> not found in the resource dispatcher host map are ignored.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="pathEnvironment"></param>
        /// <param name="serverAddresses"></param>
        /// <param name="includeDuplexChannelListenersOnly"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<ResourceChannelDispatcherManagerPerEndpointAddressTrieMap> BuildResourceChannelDispatcherManagerPerEndpointAddressTrieMapAsync(
            this IServiceProvider serviceProvider,
            ILogger logger,
            IPathEnvironment pathEnvironment, 
            IEnumerable<string> serverAddresses, 
            bool includeDuplexChannelListenersOnly,
            CancellationToken cancellationToken)
        {
            ResourceChannelDispatcherManagerPerEndpointAddressTrieMap resourceChannelDispatcherManagerPerEndpointAddressTrieMap =
                new ResourceChannelDispatcherManagerPerEndpointAddressTrieMap();

            IResourceDispatcherHostMap resourceDispatcherHostMap = serviceProvider.GetRequiredService<IResourceDispatcherHostMap>();

            Singleton<ResourceDispatcherHost, (String address, ResourceDispatcherHost resourceDispatcherHost)>.ValueFactory = 
                (a) =>
                {
                    if (a.resourceDispatcherHost.Description.Behaviors.Find<ServiceProviderBehavior>() == null)

                        a.resourceDispatcherHost.Description.Behaviors.Add(new ServiceProviderBehavior(serviceProvider));

                    return new TaskFactory().FromAsync(a.resourceDispatcherHost.BeginOpen, a.resourceDispatcherHost.EndOpen, a.address).CompleteWith(a.resourceDispatcherHost);
                };

            List<Task> outerTasks = new List<Task>();

            foreach (String serverAddress in serverAddresses)
            {
                ResourceDispatcherHost resourceDispatcherHost = resourceDispatcherHostMap.Lookup(serverAddress);

                if (resourceDispatcherHost == null)

                    continue;

                outerTasks.Add(
                    Singleton<ResourceDispatcherHost, (string, ResourceDispatcherHost)>.GetValueAsync((serverAddress, resourceDispatcherHost)).ContinueWith((t) =>
                    {
                        TryAddResourceChannelDispatcherManagerPerEndpointAddressTrieMap(t, resourceChannelDispatcherManagerPerEndpointAddressTrieMap, serviceProvider, pathEnvironment, includeDuplexChannelListenersOnly, cancellationToken);
                    }));
            }

            await Task.WhenAll(outerTasks);

            return resourceChannelDispatcherManagerPerEndpointAddressTrieMap;
        }

        private static void TryMapResourceContractMethodsToDispatcherTables(OperationDescription operationDescription, DispatchOperation dispatchOperation, Type resourceType, XmlQualifiedName resourceContractName, ActionMessageFilterTable<DispatchOperationDescription> actionMessageFilterTable, UriTemplateTables<DispatchOperationDescription> uriTemplateTables, UriTemplateTables<DispatchOperationDescription> wildcardTemplateTables, ref String catchAllOperationName)
        {
            MessageActionAttribute resourceActionMethodAttribute = operationDescription.Behaviors.Find<MessageActionAttribute>();

            if (resourceActionMethodAttribute != null)
            {
                ContractDescription resourceContract = operationDescription.DeclaringContract;

                if (String.IsNullOrEmpty(resourceActionMethodAttribute.Name))

                    resourceActionMethodAttribute.Name = operationDescription.OperationMethod.Name;

                if (String.IsNullOrWhiteSpace(resourceActionMethodAttribute.Action))
                {
                    if (String.IsNullOrWhiteSpace(resourceContract.Namespace))

                        resourceContract.Namespace = UriUtils.TEMP_URI.AbsoluteUri;

                    if (String.IsNullOrWhiteSpace(resourceContract.Name))

                        resourceContract.Name = resourceContract.ContractType.Name;

                    resourceActionMethodAttribute.Action = UriUtils.CreateUri(resourceContract.Namespace, resourceContract.Name, resourceActionMethodAttribute.Name);
                }

                actionMessageFilterTable.Add(
                    new ActionMessageFilter(
                        UriUtils.CreateUriOrWildCard(
                            resourceActionMethodAttribute.Action, 
                            resourceActionMethodAttribute.Name)), 
                    new DispatchOperationDescription(
                        resourceType, 
                        resourceContractName, 
                        operationDescription, 
                        dispatchOperation));
            }

            ResourceActionTemplateAttribute resourceTemplateMethodAttribute = operationDescription.Behaviors.Find<ResourceActionTemplateAttribute>();

            if (resourceTemplateMethodAttribute != null)
            {
                // ToDo:  Ensure method message direction in input ...

                if (String.IsNullOrEmpty(resourceTemplateMethodAttribute.Name))

                    resourceTemplateMethodAttribute.Name = operationDescription.OperationMethod.Name;

                String method = resourceTemplateMethodAttribute.ResourceAction;
                String template = resourceTemplateMethodAttribute.Template ?? String.Empty;

                if (UriTemplateHelpers.IsWildcardPath(template))
                {
                    if (method == "*")
                    {
                        if (catchAllOperationName != null)
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                new InvalidOperationException(
                                    PublicSR.Format(PublicSR.MultipleOperationsInContractWithPathMethod, new object[]
                                    {
                                        operationDescription.OperationMethod,
                                        template,
                                        method
                                    })));
                        }

                        catchAllOperationName = operationDescription.OperationMethod.Name;
                    }

                    UriTemplate uriTemplate = new UriTemplate(template);

                    wildcardTemplateTables.Add(
                        method,
                        uriTemplate,
                        new DispatchOperationDescription(
                            resourceType,
                            resourceContractName,
                            operationDescription,
                            dispatchOperation));
                }
                else
                {
                    UriTemplate uriTemplate = new UriTemplate(template);

                    uriTemplateTables.Add(
                        method,
                        uriTemplate,
                        new DispatchOperationDescription(
                            resourceType,
                            resourceContractName,
                            operationDescription,
                            dispatchOperation));
                }

            }
        }

        private static void TryMapResourceContractMethodsToDispatcherTables(OperationDescription operationDescription, DispatchOperation dispatchOperation, DispatchOperationDescription duplexOutputOperation, Type resourceType, XmlQualifiedName resourceContractName, ActionMessageFilterTable<DispatchOperationDescription> actionMessageFilterTable, UriTemplateTables<DispatchOperationDescription> uriTemplateTables, UriTemplateTables<DispatchOperationDescription> wildcardTemplateTables, ref string catchAllOperationName)
        {
            TryMapResourceContractMethodsToDispatcherTables(
                operationDescription,
                dispatchOperation,
                resourceType,
                resourceContractName,
                actionMessageFilterTable,
                uriTemplateTables,
                wildcardTemplateTables,
                ref catchAllOperationName);

            DispatchOperationDescription dispatchOperationDescription = actionMessageFilterTable.First(kv => kv.Value == operationDescription).Value;

            dispatchOperationDescription.DuplexOutputOperation = duplexOutputOperation;
        }

        /// <summary>
        /// Closes each ResourceDispatcherHost in the IResourceDispatcherHostMap required service.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void CloseResourceDispatcherHosts(this IServiceProvider serviceProvider)
        {
            IResourceDispatcherHostMap resourceDispatcherHostMap = serviceProvider.GetRequiredService<IResourceDispatcherHostMap>();

            resourceDispatcherHostMap.CloseResourceDisatcherHosts();
        }
    }
}
