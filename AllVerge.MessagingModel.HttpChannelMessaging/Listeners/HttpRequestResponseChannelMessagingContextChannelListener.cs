using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace AllVerge.MessagingModel.HttpChannelMessaging.Listeners
{
    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.MessagingModel.ChannelMessaging.Http;
    using AllVerge.MessagingModel.ChannelMessaging.Listeners.Http;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using AllVerge.MessagingModel.MessagingFoundation;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Http;
    using AllVerge.SystemPrimitives.Collections;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class HttpRequestResponseChannelMessagingContextChannelListener :
        BaseHttpMessagingContextChannelListener<ChannelMessageContext>
    {
        private IServiceProvider services;
        private ILogger logger;
        private CancellationToken cancellationToken;
        private ResourceChannelDispatcherManagerPerEndpointAddressTrieMap resourceChannelDispatcherManagersPerEndpointAddressTrieMap;

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
                    false,
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

        public override Task<(bool? success, IReceiveMessagingContextChannel<ChannelMessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync(ProtocolContextHost<HttpContext> hostingApplicationContext)
        {
            this.cancellationToken.ThrowIfCancellationRequested();

            HttpContext httpContext = hostingApplicationContext.ProtocolContext;

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
            services.CloseResourceDispatcherHosts();
        }
    }
}
