using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AllVerge.SystemPrimitives.Collections;

using System.ServiceModel.Channels;

using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.DependencyInjection;

namespace AllVerge.MessagingModel.HttpChannelMessaging.Listeners
{
    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.MessagingModel.ChannelMessaging.Listeners.Http;
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using AllVerge.MessagingModel.MessagingFoundation;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Http;
    using Microsoft.Extensions.Logging;

    public class HttpAsynchronousRequestResponseChannelMessagingContextChannelListener :
        BaseHttpMessagingContextChannelListener<ChannelMessageContext>,
        IBindingContextMapper<ProtocolContextHost<HttpContext>>
    {
        private IProtocolContextAccessor<ProtocolContextHost<HttpContext>> protocolContextAccessor;
        private ILogger logger;
        private IServiceProvider services;
        private CancellationToken cancellationToken;
        private ResourceChannelDispatcherManagerPerEndpointAddressTrieMap resourceChannelDispatcherManagersPerEndpointAddressTrieMap;

        class Connections : KeyedCollection<string, (BindingContext bindingContext, uint interactions)>
        {
            public Connections() { }

            protected override string GetKeyForItem((BindingContext bindingContext, uint interactions) item)
            {
                return item.bindingContext.ConnectionContext.ConnectionId;
            }

            public (BindingContext bindingContext, uint? interactions) IncrementInteractionsAndGet(string key)
            {
                if (this.Contains(key))
                {
                    var item = this[key];

                    item.interactions++;

                    return item;
                }

                return (null, null);
            }
        }

        private Connections connections = new Connections();

        protected override void OnInit()
        {
            this.protocolContextAccessor = 
                base.Services.GetService<IProtocolContextAccessor<ProtocolContextHost<HttpContext>>>();

            this.protocolContextAccessor.OnConnectionClosed += connectionId => { connections.Remove(connectionId); };
        }

        public override bool MapToBindingContext(ProtocolContextHost<HttpContext> context, BindingContext bindingContext)
        {
            if (base.MapToBindingContext(context, bindingContext))
            {
                connections.Add((bindingContext, 0));

                return true;
            }

            return false;
        }

        public bool MapContext(ChannelMessageContext context, BindingContext bindingContext, out uint? interactionCount)
        {
            if (context.Message != null && context.Message.Properties.TryGetProperty(HttpConnectionInfoMessageProperty.Name, 
                out HttpConnectionInfoMessageProperty httpConnectionInfoMessageProperty))
            {
                (BindingContext _bindingContext, uint? interactions) =
                    connections.IncrementInteractionsAndGet(httpConnectionInfoMessageProperty.Id);

                interactionCount = interactions;

                return bindingContext.TryApply(_bindingContext);
            }

            interactionCount = null;

            return false;
        }

        protected override async Task OnStartListeningAsync(IApplicationHostEnvironment hostEnvironment, IList<string> listenAddresses, IServiceProvider services, CancellationToken cancellationToken)
        {
            this.services = services;
            this.logger = services.GetService<ILoggerFactory>().CreateLogger(this.GetType());
            this.cancellationToken = cancellationToken;

            IPathEnvironment pathEnvironment = new PathEnvironment(hostEnvironment.HostRootPath, hostEnvironment.ContentRootPath);

            this.resourceChannelDispatcherManagersPerEndpointAddressTrieMap =
                await this.services.BuildResourceChannelDispatcherManagerPerEndpointAddressTrieMapAsync(
                    this.logger,
                    pathEnvironment,
                    listenAddresses,
                    true,
                    cancellationToken
                );

            IEnumerable<string> listeningAddresses =
                this.resourceChannelDispatcherManagersPerEndpointAddressTrieMap?.Keys.Select(k => k.AbsoluteUri);

            if (listeningAddresses == null)

                listenAddresses.Clear();

            else
            {
                IEnumerable<String> removeAddresses = listenAddresses.Except(listeningAddresses).ToArray();

                removeAddresses.ForEach(a => listenAddresses.Remove(a));
            }
        }

        public override Task<(bool? success, IReceiveMessagingContextChannel<ChannelMessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync(ProtocolContextHost<HttpContext> protocolContextHost)
        {
            this.cancellationToken.ThrowIfCancellationRequested();

            HttpContext httpContext = protocolContextHost.ProtocolContext;

            String connectionId = httpContext.Connection.Id;

            string searchUri = httpContext.Request.GetRequestUri().AbsoluteUri;

            if (this.resourceChannelDispatcherManagersPerEndpointAddressTrieMap != null &&
                this.resourceChannelDispatcherManagersPerEndpointAddressTrieMap.TryGetLongestStartsWithMatchValue(
                searchUri,
                out ResourceChannelDispatcherManager resourceChannelDispatcherManager))
            {
                return resourceChannelDispatcherManager.TryGetMessagingContextChannelAsync<HttpContext>(connectionId, this.cancellationToken);
            }
            else
            {
                return Task.FromResult<(bool? success, IReceiveMessagingContextChannel<ChannelMessageContext> messagingContextChannel)>((false, null));
            }
        }

        protected override void OnDispose()
        {
            if (this.protocolContextAccessor != null)
            
                this.protocolContextAccessor.OnConnectionClosed -= connectionId => { connections.Remove(connectionId); };
        }
    }
}
