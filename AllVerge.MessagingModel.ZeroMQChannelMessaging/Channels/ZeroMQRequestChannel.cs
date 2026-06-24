using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AllVerge.Core.Threading;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using global::System.ServiceModel;
    using global::System.ServiceModel.Channels;
    using global::System.ServiceModel.Diagnostics;

    using global::System.Runtime;

    using AllVerge.Core.ServiceModel.Channels;
    using System.Threading.Tasks.Dataflow;

    internal class ZeroMQRequestChannel : RequestChannel
    {
        private CancellationTokenSource cancellationTokenSource;
        private ConcurrentQueue<(IZeroMQChannelRequest ChannelRequest, Message Message, TimeoutHelper TimeoutHelper)> requestQueue;
        private ConcurrentDictionary<Message, (TimeoutHelper Timeout, BufferBlock<Object> MessageBuffer)> requestResults;

        private class ZeroMQChannelRequest : IZeroMQChannelRequest, IRequest
        {
            private ZeroMQRequestChannel channel;
            private ZeroMQConnectionOrientedTransportChannelFactoryBase<IRequestChannel> factory;
            private EndpointAddress to;
            private Uri via;
            private Message enqueuedMessage;

            public ZeroMQChannelRequest(ZeroMQRequestChannel channel, ZeroMQConnectionOrientedTransportChannelFactoryBase<IRequestChannel> factory)
            {
                this.channel = channel;
                this.to = channel.RemoteAddress;
                this.via = channel.Via;
                this.factory = factory;
            }

            public ZeroMQRequestChannel Channel { get => this.channel; }
            public BufferManager BufferManager { get => factory.BufferManager; }
            public bool ManualAddressing { get => factory.ManualAddressing; }
            public EndpointAddress To { get => to; }
            public Uri Via { get => via; }

            public void SendRequest(Message message, TimeoutHelper timeoutHelper)
            {
                this.Channel.requestResults.TryAdd(message, (timeoutHelper, new BufferBlock<object>()));

                this.Channel.requestQueue.Enqueue((this, message, timeoutHelper));

                this.enqueuedMessage = message;
            }

            public Message WaitForReply(TimeoutHelper timeoutHelper)
            {
                return this.Channel.WaitForReplyAsync(this.enqueuedMessage, timeoutHelper.RemainingTime()).WaitForCompletionNoSpin();
            }

            public void Abort(RequestChannel requestChannel)
            {
                throw new NotImplementedException();
            }

            public void Fault(RequestChannel requestChannel)
            {
                throw new NotImplementedException();
            }

            public void OnReleaseRequest()
            {
            }
        }

        private class ZeroMQChannelAsyncRequest : TraceAsyncResult, IZeroMQChannelRequest, IAsyncRequest, IAsyncResult
        {
            private ZeroMQRequestChannel channel;
            private TransportChannelFactory<IRequestChannel> factory;
            private EndpointAddress to;
            private Uri via;
            private Message enqueuedMessage;
            private bool endCalled = false;

            public ZeroMQChannelAsyncRequest(ZeroMQRequestChannel channel) : this(channel, null, null)
            {

            }

            public ZeroMQChannelAsyncRequest(ZeroMQRequestChannel channel, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.channel = channel;
                this.to = channel.RemoteAddress;
                this.via = channel.Via;
                this.factory = channel.Factory;
            }

            public ZeroMQRequestChannel Channel { get => this.channel; }
            public BufferManager BufferManager { get => factory.BufferManager; }
            public bool ManualAddressing { get => factory.ManualAddressing; }
            public EndpointAddress To { get => to; }
            public Uri Via { get => via; }

            public void BeginSendRequest(Message message, TimeSpan timeout)
            {
                TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

                this.Channel.requestResults.TryAdd(message, (timeoutHelper, new BufferBlock<object>()));

                this.Channel.requestQueue.Enqueue((this, message, new TimeoutHelper(timeout)));

                this.enqueuedMessage = message;
            }

            public Message End(TimeSpan timeout)
            {
                if (this.endCalled)

                    throw new InvalidOperationException(InternalSR.AsyncResultAlreadyEnded);

                this.endCalled = true;

                return this.Channel.WaitForReplyAsync(this.enqueuedMessage, timeout).WaitForCompletionNoSpin();
            }

            public void Abort(RequestChannel requestChannel)
            {
                requestChannel.Abort();
            }

            public void Fault(RequestChannel requestChannel)
            {
                throw new NotImplementedException();
            }

            public void OnReleaseRequest()
            {
            }

            public Task SendRequestAsync(Message message, TimeoutHelper timeoutHelper)
            {
                return Task.Factory.FromAsync(new AsyncActionInResult<Message>(BeginSendRequest, message, timeoutHelper.RemainingTime()), r => AsyncActionInResult<Message>.End(r));
            }

            public Task<Message> ReceiveReplyAsync(TimeoutHelper timeoutHelper)
            {
                return Task.Factory.FromAsync(new AsyncFunctionResult<Message>(End, timeoutHelper.RemainingTime()), r => AsyncFunctionResult<Message>.End(r));
            }
        }

        private ServiceModelActivity activity;

        public ZeroMQRequestChannel(TransportChannelFactory<IRequestChannel> factory, EndpointAddress to, Uri via, bool manualAddressing)
            : base(factory, to, via, manualAddressing)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.requestQueue = new ConcurrentQueue<(IZeroMQChannelRequest ChannelRequest, Message Message, TimeoutHelper TimeoutHelper)>();
            this.requestResults = new ConcurrentDictionary<Message, (TimeoutHelper Timeout, BufferBlock<object> MessageBuffer)>();
        }

        TransportChannelFactory<IRequestChannel> Factory => (TransportChannelFactory<IRequestChannel>)base.Manager;

        //protected override IRequest CreateRequest(Message message)
        //{
        //    return new ZeroMQChannelRequest(this, Factory);
        //}

        protected override IAsyncRequest CreateAsyncRequest(Message message)//, AsyncCallback callback, object state)
        {
            if (DiagnosticUtility.ShouldUseActivity && activity == null)
            {
                activity = ServiceModelActivity.CreateActivity();

                if (FxTrace.Trace != null)
                {
                    FxTrace.Trace.TraceTransfer(activity.Id);
                }

                ServiceModelActivity.Start(activity, SSR.Format(SSR.ActivityReceiveBytes, base.RemoteAddress.Uri.ToString()), ActivityType.ReceiveBytes);
            }

            return new ZeroMQChannelAsyncRequest(this);//, callback, state);
        }

        protected override void OnOpening()
        {
            base.OnOpening();
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.PrepareOpen();

            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            this.OpenClientConnection(timeoutHelper.RemainingTime());

            this.CreateAndOpenTokenProviders(timeoutHelper.RemainingTime());

            return new AsyncCompletedResult(callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            AsyncCompletedResult.End(result);
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            IAsyncResult result = null;

            using (ServiceModelActivity.BoundOperation(this.activity))
            {
                PrepareClose(aborting: false);

                TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

                CloseTokenProviders(timeoutHelper.RemainingTime());

                CloseClientConnection(timeoutHelper.RemainingTime());

                result = base.WaitForPendingRequestsAsync(timeoutHelper.RemainingTime()).ToApm(callback, state);
            }

            ServiceModelActivity.Stop(this.activity);

            return result;
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            using (ServiceModelActivity.BoundOperation(this.activity))
            {
                result.ToApmEnd();
            }
            ServiceModelActivity.Stop(this.activity);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            this.PrepareOpen();

            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            this.OpenClientConnection(timeoutHelper.RemainingTime());

            this.CreateAndOpenTokenProviders(timeoutHelper.RemainingTime());
        }

        protected override void OnClose(TimeSpan timeout)
        {
            using (ServiceModelActivity.BoundOperation(this.activity))
            {
                this.PrepareClose(aborting: false);

                TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

                this.CloseTokenProviders(timeoutHelper.RemainingTime());

                this.CloseClientConnection(timeoutHelper.RemainingTime());

                this.WaitForPendingRequests(timeoutHelper.RemainingTime());
            }

            ServiceModelActivity.Stop(this.activity);
        }

        protected override void OnClosing()
        {
            base.OnClosing();
        }

        private void CloseClientConnection(TimeSpan timeout)
        {
            Task.WhenAll(
                Task.Run(async() =>
                {
                    TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

                    while (timeoutHelper.RemainingTime() > TimeSpan.Zero)
                    {
                        if (this.requestQueue.IsEmpty && this.requestResults.IsEmpty)

                            break;

                        else

                            await Task.Delay(25);
                    }
                })
            );
        }

        private void OpenClientConnection(TimeSpan timeout)
        {
            Task.Run(() =>
            {
                using (NetMQRuntime runtime = new NetMQRuntime())
                {
                    runtime.Run(RunClientAsync(timeout, this.cancellationTokenSource.Token));
                }
            });
        }

        private static bool TrySend(DealerSocket client, bool more, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (client.TrySendFrameEmpty(timeout, more))
            {
                return true;
            }

            return false;
        }

        private static bool TrySend(DealerSocket client, ref Msg msg, bool more, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (client.TrySend(ref msg, timeout, more))
            {
                msg.Close();

                return true;
            }

            msg.Close();

            return false;
        }

        private static bool TrySend(DealerSocket client, string message, bool more, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            Msg msg = default(Msg);

            msg.InitPool(SendReceiveConstants.DefaultEncoding.GetByteCount(message));

            int bytes = SendReceiveConstants.DefaultEncoding.GetBytes(message, 0, message.Length, msg.Data, 0);

            return TrySend(client, ref msg, more, timeout);
        }

        private static bool TrySend(DealerSocket client, WebHeaderCollection headerCollection, bool more, TimeoutHelper timeoutHelper, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool flag = true;

            if (headerCollection.Count > 0)
            {
                String lastKey = headerCollection.AllKeys.Last();

                foreach (String headerKey in headerCollection.AllKeys)
                {
                    bool isMore = true;

                    if (Object.ReferenceEquals(headerKey, lastKey))

                        isMore = more;

                    flag &= TrySend(client, $"{headerKey}:{headerCollection.Get(headerKey)}", isMore, timeoutHelper.RemainingTime());
                }
            }

            return flag;
        }

        private static bool TrySend(DealerSocket client, Msg bodyMsg, HttpExtendedRequestMessageProperty requestMessageProperty, TimeoutHelper timeoutHelper, CancellationToken cancellationToken = default(CancellationToken))
        {
            return
                TrySend(client, requestMessageProperty.RequestLine, true, timeoutHelper.RemainingTime()) &&
                TrySend(client, requestMessageProperty.Headers, true, timeoutHelper) &&
                TrySend(client, true, timeoutHelper.RemainingTime()) &&
                TrySend(client, ref bodyMsg, false, timeoutHelper.RemainingTime());
        }

        private async Task<Message> WaitForReplyAsync(Message enqueuedMessage, TimeSpan timeout)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);

            if (this.requestResults.TryGetValue(enqueuedMessage, out var requestResult))
            {
                Object result = await requestResult.MessageBuffer.ReceiveAsync(timeoutHelper.GetCancellationToken());

                this.requestResults.TryRemove(enqueuedMessage, out _);

                if (result is Message)

                    return (Message)result;

                else if (result is Exception)

                    throw (Exception)result;

                throw new TimeoutException();
            }

            throw new InvalidOperationException("");
        }

        enum ReceivedState
        {
            ReceivedNone,
            ReceivingMore,
            ReceivedTimeout,
            ReceivedAll
        }

        private async Task RunClientAsync(TimeSpan openTimeout, CancellationToken cancellationToken)
        {
            using (DealerSocket client = new DealerSocket())
            {
                client.Options.Identity = Guid.NewGuid().ToByteArray();

                try
                {
                    String serverAddress = ZeroMQProtocolSchemesHelper.NormalizeServerAddress(this.RemoteAddress.Uri.AbsoluteUri);
                    
                    client.Connect(serverAddress);
                }
                catch (Exception e)
                {
                    this.Fault(e);
                }

                if (this.State != CommunicationState.Faulted)
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (this.requestQueue.TryDequeue(out var enqueuedRequest))
                        {
                            Object result = null;

                            Msg bodyMsg = enqueuedRequest.Message.Translate(this.Factory.MessageEncoderFactory, enqueuedRequest.ChannelRequest.BufferManager, enqueuedRequest.TimeoutHelper.RemainingTime(), out HttpExtendedRequestMessageProperty requestMessageProperty);

                            if (TrySend(client, bodyMsg, requestMessageProperty, enqueuedRequest.TimeoutHelper, cancellationToken))
                            {
                                if (this.requestResults.TryGetValue(enqueuedRequest.Message, out (TimeoutHelper Timeout, BufferBlock<Object> MessageBuffer) requestResult))
                                {
                                    TimeoutHelper recieveTimeout = requestResult.Timeout;

                                    HttpExtendedResponseMessageProperty responseMessageProperty =
                                        new HttpExtendedResponseMessageProperty();

                                    List<ArraySegment<byte>> data = null;

                                    ReceivedState state = ReceivedState.ReceivedNone;

                                    while (state < ReceivedState.ReceivedTimeout)
                                    {
                                        Msg msg = default(Msg);

                                        msg.InitEmpty();

                                        if (client.TryReceive(ref msg, recieveTimeout.RemainingTime()))
                                        {
                                            if (data == null)
                                            {
                                                if (msg.Data.Length > 0)

                                                    responseMessageProperty.Headers.AddHeader(msg, this.Factory.BufferManager);

                                                else

                                                    data = new List<ArraySegment<byte>>();
                                            }
                                            else

                                                data.AddData(msg, this.Factory.BufferManager);

                                            state = msg.HasMore ? ReceivedState.ReceivingMore : ReceivedState.ReceivedAll;

                                            msg.Close();
                                        }
                                        else

                                            state = ReceivedState.ReceivedTimeout;
                                    }

                                    if (state == ReceivedState.ReceivedTimeout)

                                        result = new TimeoutException();

                                    else
                                    {
                                        try
                                        {
                                            result = data.CreateMessage(responseMessageProperty, this.Factory.MessageEncoderFactory, this.Factory.BufferManager);
                                        }
                                        catch (Exception e)
                                        {
                                            result = e;
                                        }
                                    }

                                    requestResult.MessageBuffer.Post(result);
                                }
                            }
                            else
                            {
                                if (this.requestResults.TryGetValue(enqueuedRequest.Message, out (TimeoutHelper Timeout, BufferBlock<Object> MessageBuffer) requestResult))
                                {
                                    requestResult.MessageBuffer.Post(new CommunicationException("Failed to send message."));
                                }
                            }
                        }
                        else

                            await Task.Yield();
                    }
                }
            }
        }

        private void PrepareClose(bool aborting)
        {
            if (this.cancellationTokenSource.Token.CanBeCanceled)

                this.cancellationTokenSource.Cancel();
        }

        private void PrepareOpen()
        {
        }

        private void CloseTokenProviders(TimeSpan timeout)
        {
        }

        private void CreateAndOpenTokenProviders(TimeSpan timeout)
        {
        }
    }
}
