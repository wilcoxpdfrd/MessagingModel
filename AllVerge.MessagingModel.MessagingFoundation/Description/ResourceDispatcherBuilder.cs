using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Diagnostics;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation.Description
{
    internal class ResourceDispatcherBuilder : DispatcherBuilder
    {
        public new void InitializeServiceHost(ServiceDescription description, ServiceHostBase serviceHost)
        {
            if (serviceHost.ImplementedContracts != null && serviceHost.ImplementedContracts.Count > 0)
            {
                EnsureThereAreApplicationEndpoints(description);
            }
            ValidateDescription(description, serviceHost);

            //AspNetEnvironment.Current.AddHostingBehavior(serviceHost, description);

            ServiceBehaviorAttribute instanceSettings = description.Behaviors.Find<ServiceBehaviorAttribute>();
            InitializeServicePerformanceCounters(serviceHost);

            Dictionary<ListenUriInfo, StuffPerListenUriInfo> stuffPerListenUriInfo
                = new Dictionary<ListenUriInfo, StuffPerListenUriInfo>();
            Dictionary<EndpointAddress, Collection<EndpointInfo>> endpointInfosPerEndpointAddress
                = new Dictionary<EndpointAddress, Collection<EndpointInfo>>();

            // Ensure ListenUri and group endpoints per ListenUri
            for (int i = 0; i < description.Endpoints.Count; i++)
            {
                //Ensure ReceiveContextSettings before building channel
                bool requiresReceiveContext = false; //at least one operation had ReceiveContextEnabledAttribute
                ServiceEndpoint endpoint = description.Endpoints[i];

                foreach (OperationDescription operation in endpoint.Contract.Operations)
                {
                    if (operation.Behaviors.Find<ReceiveContextEnabledAttribute>() != null)
                    {
                        requiresReceiveContext = true;
                        break;
                    }
                }

                if (requiresReceiveContext)
                {
                    IReceiveContextSettings receiveContextSettings = endpoint.Binding.GetProperty<IReceiveContextSettings>(new BindingParameterCollection());

                    if (receiveContextSettings == null)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new InvalidOperationException(
                            PublicSR.Format(PublicSR.SFxReceiveContextSettingsPropertyMissing,
                            endpoint.Contract.Name,
                            typeof(ReceiveContextEnabledAttribute).Name,
                            endpoint.Address.Uri.AbsoluteUri,
                            typeof(IReceiveContextSettings).Name)));
                    }
                    //Enable ReceiveContext on the binding.
                    receiveContextSettings.Enabled = true;
                }

                ListenUriInfo listenUriInfo = GetListenUriInfoForEndpoint(serviceHost, endpoint);
                if (!stuffPerListenUriInfo.ContainsKey(listenUriInfo))
                {
                    stuffPerListenUriInfo.Add(listenUriInfo, new StuffPerListenUriInfo());
                }
                stuffPerListenUriInfo[listenUriInfo].Endpoints.Add(endpoint);
            }

            foreach (KeyValuePair<ListenUriInfo, StuffPerListenUriInfo> stuff in stuffPerListenUriInfo)
            {
                Uri listenUri = stuff.Key.ListenUri;
                ListenUriMode listenUriMode = stuff.Key.ListenUriMode;
                BindingParameterCollection parameters = stuff.Value.Parameters;
                Binding binding = stuff.Value.Endpoints[0].Binding;
                EndpointIdentity identity = stuff.Value.Endpoints[0].Address.Identity;
                // same EndpointAddressTable instance must be shared between channelDispatcher and parameters
                ThreadSafeMessageFilterTable<EndpointAddress> endpointAddressTable = new ThreadSafeMessageFilterTable<EndpointAddress>();
                parameters.Add(endpointAddressTable);

                bool supportContextSession = false;
                // add service-level binding parameters
                foreach (IServiceBehavior behavior in description.Behaviors)
                {
                    if (behavior is IContextSessionProvider)
                    {
                        supportContextSession = true;
                    }
                    behavior.AddBindingParameters(description, serviceHost, stuff.Value.Endpoints, parameters);
                }
                for (int i = 0; i < stuff.Value.Endpoints.Count; i++)
                {
                    ServiceEndpoint endpoint = stuff.Value.Endpoints[i];
                    string viaString = listenUri.AbsoluteUri;

                    // ensure all endpoints with this ListenUriInfo have same binding
                    if (endpoint.Binding != binding)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(PublicSR.Format(PublicSR.ABindingInstanceHasAlreadyBeenAssociatedTo1, viaString)));
                    }

                    // ensure all endpoints with this ListenUriInfo have same identity
                    if (!object.Equals(endpoint.Address.Identity, identity))
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(
                                                                                      PublicSR.Format(PublicSR.SFxWhenMultipleEndpointsShareAListenUriTheyMustHaveSameIdentity, viaString)));
                    }

                    // add binding parameters (endpoint scope and below)
#if MSMQ_INTEGRATION
                    AddMsmqIntegrationContractInformation(endpoint);
#endif
                    SecurityContractInformationEndpointBehavior.ServerInstance.AddBindingParameters(endpoint, parameters);
                    AddBindingParameters(endpoint, parameters);
                }

                // build IChannelListener and ChannelDispatcher
                IChannelListener listener;
                Type channelType = this.BuildChannelListener(stuff.Value,
                                                             serviceHost,
                                                             listenUri,
                                                             listenUriMode,
                                                             supportContextSession,
                                                             out listener);

                XmlQualifiedName bindingQname = new XmlQualifiedName(binding.Name, binding.Namespace);
                ChannelDispatcher channelDispatcher = new ChannelDispatcher(listener, bindingQname.ToString(), binding);
                channelDispatcher.SetEndpointAddressTable(endpointAddressTable);
                stuff.Value.ChannelDispatcher = channelDispatcher;

                bool canReceiveInTransaction = false;   // at least one operation is TransactionScopeRequired
                int transactedBatchSize = int.MaxValue;

                for (int i = 0; i < stuff.Value.Endpoints.Count; i++)
                {
                    ServiceEndpoint endpoint = stuff.Value.Endpoints[i];
                    string viaString = listenUri.AbsoluteUri;

                    IMessageFilterTable<EndpointAddress> messageFilterTable = stuff.Value.Parameters.Find<IMessageFilterTable<EndpointAddress>>();

                    bool unAddressedActionSpecified = false;

                    for (int j = 0; j < endpoint.Contract.Operations.Count; j++)
                    {
                        OperationDescription operation = endpoint.Contract.Operations[j];
                        OperationBehaviorAttribute operationBehavior = operation.Behaviors.Find<OperationBehaviorAttribute>();
                        if (null != operationBehavior && operationBehavior.TransactionScopeRequired)
                        {
                            canReceiveInTransaction = true;
                            break;
                        }

                        MessageActionAttribute messageActionAttribute = operation.Behaviors.Find<MessageActionAttribute>();

                        if (null != messageActionAttribute && messageActionAttribute.IsUnAddressedAction)
                        {
                            unAddressedActionSpecified = true;
                            break;
                        }
                    }

                    EndpointFilterProvider provider = new EndpointOperationFilterProvider(unAddressedActionSpecified, messageFilterTable.Where(kv => kv.Value == endpoint.Address).Select(kv => kv.Key));
                    EndpointDispatcher dispatcher = DispatcherBuilder.BuildDispatcher(serviceHost, description, endpoint, endpoint.Contract, provider);

                    if (!endpointInfosPerEndpointAddress.ContainsKey(endpoint.Address))
                    {
                        endpointInfosPerEndpointAddress.Add(endpoint.Address, new Collection<EndpointInfo>());
                    }
                    endpointInfosPerEndpointAddress[endpoint.Address].Add(new EndpointInfo(endpoint, dispatcher, provider));

                    channelDispatcher.Endpoints.Add(dispatcher);

                    TransactedBatchingBehavior batchBehavior = endpoint.Behaviors.Find<TransactedBatchingBehavior>();
                    if (batchBehavior == null)
                    {
                        transactedBatchSize = 0;
                    }
                    else
                    {
                        if (!canReceiveInTransaction)
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(PublicSR.BatchRequiresTransactionScope));
                        transactedBatchSize = System.Math.Min(transactedBatchSize, batchBehavior.MaxBatchSize);
                    }
                    if (PerformanceCounters.PerformanceCountersEnabled || PerformanceCounters.MinimalPerformanceCountersEnabled)
                    {
                        PerformanceCounters.AddPerformanceCountersForEndpoint(serviceHost, endpoint.Contract, dispatcher);
                    }
                } // end foreach "endpoint"

                if (canReceiveInTransaction)
                {
                    BindingElementCollection bindingElements = binding.CreateBindingElements();
                    foreach (BindingElement bindingElement in bindingElements)
                    {
                        ITransactedBindingElement txElement = bindingElement as ITransactedBindingElement;
                        if (null != txElement && txElement.TransactedReceiveEnabled)
                        {
                            channelDispatcher.IsTransactedReceive = true;
                            channelDispatcher.MaxTransactedBatchSize = transactedBatchSize;
                            break;
                        }
                    }
                }

                //Set the mode of operation for ChannelDispatcher based on binding Settings.
                IReceiveContextSettings receiveContextSettings = binding.GetProperty<IReceiveContextSettings>(new BindingParameterCollection());

                if (receiveContextSettings != null)
                {
                    channelDispatcher.ReceiveContextEnabled = receiveContextSettings.Enabled;
                }
                serviceHost.ChannelDispatchers.Add(channelDispatcher);
            } // end foreach "ListenUri/ChannelDispatcher" group

            // run service behaviors
            for (int i = 0; i < description.Behaviors.Count; i++)
            {
                IServiceBehavior serviceBehavior = description.Behaviors[i];
                serviceBehavior.ApplyDispatchBehavior(description, serviceHost);
            }

            foreach (KeyValuePair<ListenUriInfo, StuffPerListenUriInfo> stuff in stuffPerListenUriInfo)
            {
                for (int i = 0; i < stuff.Value.Endpoints.Count; i++)
                {
                    ServiceEndpoint endpoint = stuff.Value.Endpoints[i];
                    // rediscover which dispatcher goes with this endpoint
                    Collection<EndpointInfo> infos = endpointInfosPerEndpointAddress[endpoint.Address];
                    EndpointInfo info = null;
                    foreach (EndpointInfo ei in infos)
                    {
                        if (ei.Endpoint == endpoint)
                        {
                            info = ei;
                            break;
                        }
                    }
                    EndpointDispatcher dispatcher = info.EndpointDispatcher;
                    // run contract behaviors
                    for (int k = 0; k < endpoint.Contract.Behaviors.Count; k++)
                    {
                        IContractBehavior behavior = endpoint.Contract.Behaviors[k];
                        behavior.ApplyDispatchBehavior(endpoint.Contract, endpoint, dispatcher.DispatchRuntime);
                    }
                    // run endpoint behaviors
                    BindingInformationEndpointBehavior.Instance.ApplyDispatchBehavior(endpoint, dispatcher);
                    TransactionContractInformationEndpointBehavior.Instance.ApplyDispatchBehavior(endpoint, dispatcher);
                    for (int j = 0; j < endpoint.Behaviors.Count; j++)
                    {
                        IEndpointBehavior eb = endpoint.Behaviors[j];
                        eb.ApplyDispatchBehavior(endpoint, dispatcher);
                    }
                    // run operation behaviors
                    ResourceDispatcherBuilder.BindOperations(endpoint.Contract, null, dispatcher.DispatchRuntime);
                }
            }

            this.EnsureRequiredRuntimeProperties(endpointInfosPerEndpointAddress);

            // Warn about obvious demux conflicts
            foreach (Collection<EndpointInfo> endpointInfos in endpointInfosPerEndpointAddress.Values)
            {
                // all elements of endpointInfos share the same Address (and thus EndpointListener.AddressFilter)
                if (endpointInfos.Count > 1)
                {
                    for (int i = 0; i < endpointInfos.Count; i++)
                    {
                        for (int j = i + 1; j < endpointInfos.Count; j++)
                        {
                            // if not same ListenUri, won't conflict
                            // if not same ChannelType, may not conflict (some transports demux based on this)
                            // if they share a ChannelDispatcher, this means same ListenUri and same ChannelType
                            if (endpointInfos[i].EndpointDispatcher.ChannelDispatcher ==
                                endpointInfos[j].EndpointDispatcher.ChannelDispatcher)
                            {
                                EndpointFilterProvider iProvider = endpointInfos[i].FilterProvider;
                                EndpointFilterProvider jProvider = endpointInfos[j].FilterProvider;
                                // if not default EndpointFilterProvider, we won't try to throw, you're on your own
                                string commonAction;
                                if (iProvider != null && jProvider != null
                                    && HaveCommonInitiatingActions(iProvider, jProvider, out commonAction))
                                {
                                    // you will definitely get a MultipleFiltersMatchedException at runtime,
                                    // so let's go ahead and throw now
                                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                        new InvalidOperationException(
                                            PublicSR.Format(PublicSR.SFxDuplicateInitiatingActionAtSameVia,
                                                         endpointInfos[i].Endpoint.ListenUri, commonAction)));
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection parameters)
        {
            foreach (IContractBehavior icb in endpoint.Contract.Behaviors)
            {
                icb.AddBindingParameters(endpoint.Contract, endpoint, parameters);
            }
            foreach (IEndpointBehavior ieb in endpoint.Behaviors)
            {
                ieb.AddBindingParameters(endpoint, parameters);
            }
            foreach (OperationDescription op in endpoint.Contract.Operations)
            {
                foreach (IOperationBehavior iob in op.Behaviors)
                {
                    if (iob is IOperationEndpointBehavior)

                        (iob as IOperationEndpointBehavior).AddBindingParameters(endpoint, op, parameters);

                    else

                        iob.AddBindingParameters(op, parameters);
                }
            }
        }

        private static void BindOperations(ContractDescription contract, ClientRuntime proxy, DispatchRuntime dispatch)
        {
            DispatcherBuilder.BindOperationsDual(contract, proxy, dispatch);
        }
    }
}
