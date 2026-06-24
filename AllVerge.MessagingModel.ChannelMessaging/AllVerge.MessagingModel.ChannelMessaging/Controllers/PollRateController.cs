using AllVerge.MessagingModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Controllers
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    using AllVerge.SystemPrimitives.Collections;

    using AllVerge.MessagingModel.ChannelMessaging.Channels;
    using AllVerge.MessagingModel.ChannelMessaging.Listeners;

    internal class PollRateController<MessageContext> :
        BaseController<MessageContext>
        where MessageContext : IMessageContext
    {
        private class DynamicRateMessagePoller : IMessagingContextRatePoller<MessageContext>
        {
            private IBindingContextMapper<MessageContext> bindingContextMapper;
            protected AbstractPollMessagingContextChannel<MessageContext> pollMessagingContextChannel;
            private PollRateController<MessageContext> rateController;
            private bool disposedValue;

            public DynamicRateMessagePoller(IBindingContextMapper<MessageContext> bindingContextMapper, AbstractPollMessagingContextChannel<MessageContext> pollMessagingContextChannel, PollRateController<MessageContext> rateController)
            {
                this.bindingContextMapper = bindingContextMapper;
                this.pollMessagingContextChannel = pollMessagingContextChannel;
                this.rateController = rateController;
            }

            public int PollSize => this.pollMessagingContextChannel.PollSize;

            public int PollTimeoutMS => this.pollMessagingContextChannel.PollTimeoutMS;

            public int HandleIntervalMS { get; set; }

            public int PollIntervalMS { get; set; }

            public ReceiveMessagesAsync<MessageContext> ReceiveMessagesAsync => pollMessagingContextChannel.ReceiveMessagesAsync;

            public Task CallBackAsync(IMessagingContext<MessageContext> messagingContext)
            {
                if (messagingContext.Result == MiddlewarePipelineResult.TooBusy)

                    this.rateController.RecalculateRates(true);

                return pollMessagingContextChannel.HandledMessagingCallBackAsync(messagingContext);
            }

            public bool MapToBindingContext(MessageContext context, BindingContext bindingContext)
            {
                return this.bindingContextMapper.MapToBindingContext(context, bindingContext);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        if (pollMessagingContextChannel.IsOpen)

                            pollMessagingContextChannel.Dispose();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }

            // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // ~DynamicRateMessagePoller()
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
        }

        IList<IMessagingContextRatePoller<MessageContext>> messagePollers;
        int initialMaxMessagesHandledPerSecond;
        int currentMaxMessagesHandledPerSecond;
        private long state;
        private double recalculateThrottleByPercent;
        private int recalculateTimerPeriodMs;
        Timer recalculateTimer;

        public PollRateController(ILogger logger, MessagingPollOptions messagingPollerOptions, Func<IMessagingContext<MessageContext>, RejectCode, IDictionary<RejectHeaders, StringValues>, Task> prepareRejectedMessagingContext, Func<IMessagingContext<MessageContext>, Action, Action, Task> receivedMessagingContext,  CancellationToken cancellationToken) :
            base(logger, prepareRejectedMessagingContext, receivedMessagingContext, cancellationToken)
        {
            this.messagePollers = new List<IMessagingContextRatePoller<MessageContext>>();
            this.initialMaxMessagesHandledPerSecond = messagingPollerOptions.MaxMessagesHandledPerSecond;
            this.recalculateThrottleByPercent = messagingPollerOptions.MaxMessagesThrottleByPercent;
            this.recalculateTimerPeriodMs = messagingPollerOptions.MaxMessagesRecalculatePeriodSeconds * 1000;
            this.state = (long)RunningState.Running;
            this.RecalculateRates();
            this.recalculateTimer = new Timer((s) => (s as PollRateController<MessageContext>).RecalculateRates(false), this, recalculateTimerPeriodMs, recalculateTimerPeriodMs);
        }

        public RunningState State { get => (RunningState)Interlocked.Read(ref this.state); }

        public async Task AcceptChannelAsync(IBindingContextMapper<MessageContext> bindingContextMapper, AbstractPollMessagingContextChannel<MessageContext> pollMessagingContextChannel)
        {
            while (this.State == RunningState.Paused)

                await Task.Delay(500);

            IMessagingContextRatePoller<MessageContext> messagingContextRatePoller = 
                new DynamicRateMessagePoller(bindingContextMapper, pollMessagingContextChannel, this);

            _ = PollMeterAndProcessMessagesAsync(messagingContextRatePoller, pollMessagingContextChannel);

            this.messagePollers.Add(messagingContextRatePoller);

            this.RecalculateRates();
        }

        private bool Pause()
        {
            // Returns true only if state is changed from running to paused.  
            // Returns false if already paused.
            return Interlocked.CompareExchange(ref this.state, (long)RunningState.Paused, (long)RunningState.Running) == (long)RunningState.Running;
        }

        private bool Run()
        {
            // Returns true only if state is changed from paused to running.  
            // Otherwise returns false (already running).
            return Interlocked.CompareExchange(ref this.state, (long)RunningState.Running, (long)RunningState.Paused) == (long)RunningState.Paused;
        }

        private void RecalculateRates(bool? throttleDown = null)
        {
            if (this.messagePollers.Count == 0)

                return;

            if (throttleDown == false) // timer call to throttle up
            {
                if (this.currentMaxMessagesHandledPerSecond >= this.initialMaxMessagesHandledPerSecond)

                    return;
            }

            if (!Pause())

                return;

            if (throttleDown.HasValue)
            {
                if (throttleDown.Value)
                {
                    this.recalculateTimer.Change(recalculateTimerPeriodMs, recalculateTimerPeriodMs);

                    if (this.currentMaxMessagesHandledPerSecond > this.messagePollers.Count() * 2)

                        this.currentMaxMessagesHandledPerSecond = (int)(this.currentMaxMessagesHandledPerSecond * this.recalculateThrottleByPercent);
                }
                else
                {
                    if (this.currentMaxMessagesHandledPerSecond < this.initialMaxMessagesHandledPerSecond)

                        this.currentMaxMessagesHandledPerSecond = (int)(this.currentMaxMessagesHandledPerSecond / this.recalculateThrottleByPercent);
                }
            }
            else

                this.currentMaxMessagesHandledPerSecond = this.initialMaxMessagesHandledPerSecond;

            CalculateNormalizedRates(this.currentMaxMessagesHandledPerSecond, out IEnumerable<(IMessagingContextRatePoller<MessageContext> MessagePoller, int NormalizedRate)> normalizedRatePollers, this.messagePollers);

            this.messagePollers = new List<IMessagingContextRatePoller<MessageContext>>(normalizedRatePollers.Select(p =>
            {
                if (p.NormalizedRate > 0)
                {
                    CalculateIntervals(p.MessagePoller.PollTimeoutMS, p.MessagePoller.PollSize, p.NormalizedRate, out int handleIntervalMS, out int pollIntervalMS);

                    p.MessagePoller.HandleIntervalMS = handleIntervalMS;
                    p.MessagePoller.PollIntervalMS = handleIntervalMS;
                }

                return p.MessagePoller;
            }));

            this.Run();
        }

        public IEnumerable<IMessagingContextRatePoller<MessageContext>> DynamicRateMessagePollers
        {
            get => this.messagePollers;
        }

        private async Task PollMeterAndProcessMessagesAsync(IMessagingContextRatePoller<MessageContext> messagingContextRatePoller, AbstractPollMessagingContextChannel<MessageContext> pollMessagingContextChannel)
        {
            while (!CancellationToken.IsCancellationRequested && pollMessagingContextChannel.IsOpen)
            {
                while (this.State == RunningState.Paused)
                {
                    if (CancellationToken.IsCancellationRequested)

                        break;

                    await Task.Delay(messagingContextRatePoller.HandleIntervalMS, CancellationToken);

                    await Task.Yield();
                }

                if (CancellationToken.IsCancellationRequested)

                    break;

                Task receiveMessagesTask =
                    await messagingContextRatePoller.ReceiveMessagesAsync(messagingContextRatePoller.PollSize, TimeSpan.FromMilliseconds(messagingContextRatePoller.PollTimeoutMS), CancellationToken).ContinueWith(async receivedMessageTask =>
                    {
                        if (receivedMessageTask.IsFaulted)

                            await receivedMessageTask;

                        foreach (IMessagingContext<MessageContext> receivedMessageContext in receivedMessageTask.Result)
                        {
                            while (this.State == RunningState.Paused)
                            {
                                await Task.Delay(messagingContextRatePoller.HandleIntervalMS, CancellationToken);

                                await Task.Yield();
                            }

                            receivedMessageContext.BindingContext.ConnectionContext.Items.Add<IMessagingContextChannel<MessageContext>>(pollMessagingContextChannel);

                            await this.ReceivedMessagingContext(receivedMessageContext, () => { }, () => { });

                            await Task.Yield();
                        }
                    });

                if (receiveMessagesTask.IsFaulted)

                    await Task.FromException(receiveMessagesTask.Exception);

                if (messagingContextRatePoller.PollIntervalMS > 0)

                    await Task.Delay(messagingContextRatePoller.PollIntervalMS, CancellationToken);
            }
        }

        private void CalculateNormalizedRates(int targetRate, out IEnumerable<(IMessagingContextRatePoller<MessageContext> MessagePoller, int NormalizedRate)> normalizedRatePollers, IEnumerable<IMessagingContextRatePoller<MessageContext>> messagePollers)
        {
            var rates = messagePollers.Select(p => new { MessagePoller = p, Rate = (double)p.PollSize / (p.PollTimeoutMS / 1000) });

            double totalRate = rates.Select(t => t.Rate).Sum();

            List<(IMessagingContextRatePoller<MessageContext> MessagePoller, int NormalizedRate)> normalizedRatePollerList =
                new List<(IMessagingContextRatePoller<MessageContext> MessagePoller, int NormalizedRate)>();

            rates.Aggregate(normalizedRatePollerList, (seed, t) => { seed.Add((t.MessagePoller, (int)Math.Round(targetRate * (t.Rate / totalRate), MidpointRounding.AwayFromZero))); return seed; });

            normalizedRatePollers = normalizedRatePollerList;
        }

        private static void CalculateIntervals(int pollTimeoutMS, int pollCount, int maxRatePerSecond, out int handleIntervalMS, out int pollIntervalMS)
        {
            double timeoutSec = (double)pollTimeoutMS / 1000;

            double rate = (double)pollCount / timeoutSec;

            double factor;

            if (rate > maxRatePerSecond)
            {
                factor = (double)maxRatePerSecond / (double)rate;
            }
            else
            {
                factor = (double)rate / (double)maxRatePerSecond;
            }

            handleIntervalMS = (int)(1000 / (rate * factor));

            int handleTimeMS = handleIntervalMS * pollCount;

            if (handleTimeMS > pollTimeoutMS)

                pollIntervalMS = handleTimeMS - pollTimeoutMS;

            else

                pollIntervalMS = 0;
        }


        protected override void OnDispose()
        {
            int i = this.messagePollers.Count;

            while (i > 0)
            {
                var messagingContextRatePoller = RemovePoller(i - 1);

                messagingContextRatePoller.Dispose();

                i = this.messagePollers.Count;
            }

            IMessagingContextRatePoller<MessageContext> RemovePoller(int index)
            {
                IMessagingContextRatePoller<MessageContext> messagingContextRatePoller = this.messagePollers[index];

                this.messagePollers.RemoveAt(index);

                return messagingContextRatePoller;
            }
        }
    }
}