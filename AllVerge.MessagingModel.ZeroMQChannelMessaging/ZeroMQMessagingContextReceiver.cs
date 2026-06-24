using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using AllVerge.MessagingModel.MessagingFoundation;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels;
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.SystemPrimitives.Threading.Tasks;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Xml;
    using System.Xml.Linq;

    internal class ZeroMQMessagingContextReceiver:
        BaseMessagingContextReceiver<ZeroMQProtocolContext, ChannelMessageContext>
    {
        private static ConcurrentDictionary<String, SlidingDelayTask<String>> _connections;
        private static TransportManager<BlockingCollection<IMessagingContext<ZeroMQProtocolContext>>> _protocolContextQueues;
        private IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>> _protocolContextAccessor;
        private IProtocolContextFactory<ZeroMQProtocolContext> _protocolContextFactory;

        private class ZeroMQProtocolContextAccessor : IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>>
        {
            static ZeroMQProtocolContextAccessor()
            {
                ProtocolMessagingContext.RegisterMapper((MessagingContext<ZeroMQProtocolContext> protocolContext) =>
                    protocolContext.InputContext as ITransportProtocolContext);
            }

            public event Action<string> OnConnectionClosed;

            public Task<string> WaitForConnectionTimeoutAsync(string connectionId)
            {
                if (_connections.TryGetValue(connectionId, out SlidingDelayTask<String> t))

                    return t.CompletionTask;

                return Task.FromResult(connectionId);
            }

            public Task<IMessagingContext<ZeroMQProtocolContext>> GetProtocolContextAsync(Uri listenUri, CancellationToken cancellationToken)
            {
                return Task<IMessagingContext<ZeroMQProtocolContext>>.Run(() =>
                    {
                        if (_protocolContextQueues.TryLookupUri(listenUri, System.ServiceModel.HostNameComparisonMode.StrongWildcard, out BlockingCollection<IMessagingContext<ZeroMQProtocolContext>> c))

                            return c.Take(cancellationToken);

                        throw new Exception($"{listenUri.AbsoluteUri} is not a registered listen address.");
                    }
                );
            }

            public void SetProtocolContextAsync(IMessagingContext<ZeroMQProtocolContext> protocolContext)
            {
                ConnectionContext connectionContext = protocolContext.BindingContext.ConnectionContext;

                InteractionContext interactionContext = protocolContext.BindingContext.InteractionContext;

                Console.WriteLine(interactionContext.InputLocation);

                var slidingDelayTask = _connections.GetOrAdd(connectionContext.ConnectionId, _connectionId =>
                {
                    var _slidingDelayTask = new SlidingDelayTask<String>(() => _connectionId, TimeSpan.FromMinutes(2), default(CancellationToken));

                    _slidingDelayTask.CompletionTask.ContinueWith(t =>
                    {
                        if (OnConnectionClosed != null)

                            OnConnectionClosed.Invoke(_connectionId);

                        _connections.TryRemove(_connectionId, out _);
                    });

                    return _slidingDelayTask;
                });

                slidingDelayTask.ExtendDelay(TimeSpan.FromMinutes(2));

                Uri inputLocation = new Uri(protocolContext.BindingContext.InteractionContext.InputLocation);

                if (_protocolContextQueues.TryLookupUri(inputLocation, System.ServiceModel.HostNameComparisonMode.StrongWildcard, out BlockingCollection<IMessagingContext<ZeroMQProtocolContext>> c))

                    c.Add(protocolContext);
            }
        }

        public ZeroMQMessagingContextReceiver(IApplicationHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider services) :
            base(hostEnvironment, hostApplicationLifetime, services)
        {
            _connections = new ConcurrentDictionary<string, SlidingDelayTask<string>>();
            _protocolContextQueues = new TransportManager<BlockingCollection<IMessagingContext<ZeroMQProtocolContext>>>();
            _protocolContextFactory = new ZeroMQProtocolContextFactory();
        }

        protected override IProtocolContextAccessor<IMessagingContext<ZeroMQProtocolContext>> ProtocolContextAccessor
        {
            get
            {
                if (this._protocolContextAccessor == null)
                {
                    this._protocolContextAccessor = new ZeroMQProtocolContextAccessor();
                }

                return this._protocolContextAccessor;
            }
        }

        protected override void RegisterListenAddresses(IEnumerable<string> addresses)
        {
            addresses.Aggregate(_protocolContextQueues as ITransportManager<BlockingCollection<IMessagingContext<ZeroMQProtocolContext>>>, (q, a) => { q.Register(new Uri(a), new BlockingCollection<IMessagingContext<ZeroMQProtocolContext>>()); return q; });
        }

        protected override void UnregisterListenAddresses(IEnumerable<string> addresses)
        {
            addresses.Aggregate(_protocolContextQueues as ITransportManager<BlockingCollection<IMessagingContext<ZeroMQProtocolContext>>>, (q, a) => { q.Unregister(new Uri(a)); return q; });
        }

        protected override IMessagingContext<ZeroMQProtocolContext> Create(IFeatureCollection contextFeatures)
        {
            MessagingContext<ZeroMQProtocolContext> protocolContext = new MessagingContext<ZeroMQProtocolContext>();

            Task.Factory.StartNew(() => {
                if (base.CancellationToken.IsCancellationRequested)
                {
                    protocolContext.Input(null, MiddlewarePipelineResult.Canceled);
                }

                if (base.ReceivedProtocolContextQueue.TryTake(out ZeroMQProtocolContext zeroMQProtocolContext, protocolContext.ReceiveInputWaitTimeout))
                {
                    protocolContext.Input(zeroMQProtocolContext);
                }
            });

            return protocolContext;
        }

        protected override ChannelMessageContext GetNullMessageContext(IMessagingContext<ChannelMessageContext> messagingContext)
        {
            throw new NotImplementedException();
        }

        protected override ChannelMessageContext GetRejectedMessageContext(IMessagingContext<ChannelMessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders, string faultAction = null)
        {
            return messagingContext.GetRejectedMessageContext(rejectionCode, rejectionHeaders, faultAction);
        }

        protected override async Task InvokeProtocolMessagingContextCallbackAsync(IMessagingContext<ChannelMessageContext> messagingContext)
        {
            if (messagingContext.Items.TryGetValue<ZeroMQProtocolContext>(out ZeroMQProtocolContext protocolContext))
            {
                if (messagingContext.OutputContext == null)

                    messagingContext.Output(GetNullMessageContext(messagingContext));

                using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateDictionaryWriter(XmlWriter.Create(protocolContext.OutputBody)))
                {
                    await messagingContext.OutputContext.Message.WriteMessageAsync(writer);
                }
            }
        }

        protected override void Dispose(IMessagingContext<ZeroMQProtocolContext> protocolContext)
        {
            protocolContext.Dispose();
        }

        protected override void Dispose(IMessagingContext<ZeroMQProtocolContext> protocolContext, Exception e)
        {
            base.Logger.LogError(e, "Disposing protocolContext reason.");
            protocolContext.Dispose();
        }
    }
}