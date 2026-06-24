using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.SystemPrimitives.Net;

    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using AllVerge.MessagingModel.MessagingFoundation;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.AspNetCore.Http;

    internal class ZeroMQAsynchronousMessagingContextChannelListener :
        BaseProtocolMessagingContextChannelListener<ZeroMQProtocolContext, ChannelMessageContext>
    {
        private Object lockObj;
        private bool canListen = false;
        private ResourceChannelDispatcherManagerPerEndpointAddressTrieMap resourceChannelDispatcherManagersPerEndpointAddressTrieMap;
        private TimeSpan timeout;
        private IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>> protocolContextAccessor;
        private BlockingCollection<ZeroMQProtocolContext> protocolContextQueue;

        public ZeroMQAsynchronousMessagingContextChannelListener() : base()
        {
        }

        public ILogger Logger;

        protected override void OnInit()
        {
            this.lockObj = new Object();
            this.timeout = TimeSpan.FromSeconds(1);
            this.protocolContextAccessor = base.Services.GetService<IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>>>();
            this.protocolContextQueue = new BlockingCollection<ZeroMQProtocolContext>();
        }

        protected override async Task OnStartListeningAsync(IApplicationHostEnvironment hostEnvironment, IList<string> listenAddresses, IServiceProvider services, CancellationToken cancellationToken)
        {
            this.Logger = services.GetService<ILoggerFactory>().CreateLogger(this.GetType());

            IPathEnvironment pathEnvironment = new PathEnvironment(hostEnvironment.HostRootPath, hostEnvironment.ContentRootPath);

            this.resourceChannelDispatcherManagersPerEndpointAddressTrieMap =
                await this.Services.BuildResourceChannelDispatcherManagerPerEndpointAddressTrieMapAsync(
                this.Logger,
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

        public override Task<(bool, ZeroMQProtocolContext)> TryReceiveContext()
        {
            if (!this.canListen)
            {
                lock (this.lockObj)
                {
                    if (!this.canListen)
                    {
                        this.canListen = this.ListenAddresses.Aggregate(0, (listeningCount, address) =>
                        {
                            if (address.StartsWith(TransportProtocolSchemes.ZEROMQ_TCP) || address.StartsWith(TransportProtocolSchemes.ZEROMQ_IPC))
                            {
                                Uri listenAddress = new Uri(address);

                                _ = Task.Run(async () =>
                                {
                                    while (!base.CancellationToken.IsCancellationRequested)
                                    {
                                        CancellationTokenSource cts = new CancellationTokenSource();

                                        cts.CancelAfter(timeout);

                                        try
                                        {
                                            IMessagingContext<ZeroMQProtocolContext> inputContext = 
                                                await this.protocolContextAccessor.GetProtocolContextAsync(listenAddress, cts.Token);

                                            protocolContextQueue.Add(inputContext.InputContext);
                                        }
                                        catch (Exception e)
                                        {
                                            if (!(e is OperationCanceledException) ||
                                                !(e as OperationCanceledException).CancellationToken.IsCancellationRequested)

                                                this.Logger.LogError(e, $"{nameof(ZeroMQAsynchronousMessagingContextChannelListener)}::{nameof(TryReceiveContext)}}}");
                                        }
                                    }
                                });

                                listeningCount++;
                            }

                            return listeningCount;
                        }) > 0;
                    }
                }
            }

            return Task.Run(() =>
            {
                if (this.protocolContextQueue.TryTake(out ZeroMQProtocolContext zeroMQProtocolContext, (int)timeout.TotalMilliseconds))

                    return (true, zeroMQProtocolContext);

                return (false, null);
            });
        }

        public override Task<(bool? success, IReceiveMessagingContextChannel<ChannelMessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync(ZeroMQProtocolContext protocolContext)
        {
            if (canListen)
            {
                base.CancellationToken.ThrowIfCancellationRequested();

                String connectionId = protocolContext.ConnectionId;

                string searchUri = protocolContext.LocalAddress.Uri.AbsoluteUri;

                if (this.resourceChannelDispatcherManagersPerEndpointAddressTrieMap != null &&
                    this.resourceChannelDispatcherManagersPerEndpointAddressTrieMap.TryGetLongestStartsWithMatchValue(
                    searchUri,
                    out ResourceChannelDispatcherManager resourceChannelDispatcherManager))
                {
                    return resourceChannelDispatcherManager.TryGetMessagingContextChannelAsync<ChannelMessageContext>(connectionId, base.CancellationToken);
                }
            }

            return Task.FromResult<(bool? success, IReceiveMessagingContextChannel<ChannelMessageContext> messagingContextChannel)>((false, null));
        }

        public override bool MapToBindingContext(IMessagingContext<ZeroMQProtocolContext> context, MessagingApplication.BindingContext bindingContext)
        {
            ZeroMQProtocolContext protocolContext = context.InputContext;

            bindingContext.ConnectionContext.Map(
                protocolContext.ConnectionId,
                protocolContext.Items,
                protocolContext.LocalEndPoint,
                protocolContext.RemoteEndPoint
            );

            bindingContext.InteractionContext.Map(
                new AddressHeaders(protocolContext.LocalAddress.Uri.Host, protocolContext.RemoteAddress.Uri),
                protocolContext.LocalAddress.Uri.AbsoluteUri,
                null,
                new DateTimeOffset(context.ApplicationContext.StartTimestamp, TimeSpan.Zero),
                protocolContext.TraceIdentifier,
                protocolContext.User);

            return true;
        }

        private class AddressHeaders : InteractionContext.Headers
        {
            public AddressHeaders(string host, Uri uri) : base(null, host, uri)
            {
            }
        }
    }
}