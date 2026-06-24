using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ChannelMessaging.Channels
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.SystemPrimitives.Runtime;
    using AllVerge.SystemPrimitives.Threading.Tasks;

    public abstract class AbstractMessagingContextChannel<MessageContext> : 
        IMessagingContextChannel<MessageContext>
    {
        private bool? isOpen;
        private bool disposedValue;

        private AbstractMessagingContextChannel() { this.isOpen = false; }

        protected AbstractMessagingContextChannel(MessagingChannelInteractions interactions) : this()
        {
            this.Interactions = interactions;
        }

        public virtual bool IsOpen { get => this.isOpen == true; }

        public MessagingChannelInteractions Interactions
        {
            get;
        }

        void IMessagingContextChannel<MessageContext>.MapConnection(ConnectionContext connectionContext)
        {
            MapConnection(connectionContext);
        }

        /// <summary>
        /// Implement to map the connection context onto the channel and it's properties.
        /// </summary>
        /// <remarks>Note to implementers.  The connection context items can be mapped to the channel properties.</remarks>
        /// <param name="connectionContext"></param>
        protected abstract void MapConnection(ConnectionContext connectionContext);

        public void Open(TimeSpan timeout)
        {
            OpenAsync(timeout).WaitForCompletion();
        }

        public async Task OpenAsync(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    "Timeout must be greater than or equal to TimeSpan.Zero. To disable timeout, specify TimeSpan.MaxValue.");
            }

            if (this.isOpen != true)
            {
                ICountdownTimer actualTimeout = timeout.StartCountdown();

                this._OnOpening();

                await OnOpenAsync(actualTimeout.RemainingTime());

                this._OnOpened();
            }
        }

        protected abstract Task OnOpenAsync(TimeSpan timeSpan);

        private void _OnOpening()
        {
            this.isOpen = null;

            this.OnOpening();
        }

        protected virtual void OnOpening()
        {
        }

        private void _OnOpened()
        {
            this.isOpen = true;

            this.OnOpened();

            if (this.Opened != null)

                this.Opened(this);
        }

        protected virtual void OnOpened()
        {
        }

        public void Close(TimeSpan timeout)
        {
            CloseAsync(timeout).WaitForCompletion();
        }

        public async Task CloseAsync(TimeSpan timeout)
        {
            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout), 
                    "Timeout must be greater than or equal to TimeSpan.Zero. To disable timeout, specify TimeSpan.MaxValue.");
            }

            if (this.isOpen == true)
            {
                ICountdownTimer actualTimeout = timeout.StartCountdown();

                this._OnClosing();
                
                await OnCloseAsync(actualTimeout.RemainingTime());

                this._OnClosed();
            }
        }

        protected abstract Task OnCloseAsync(TimeSpan timeSpan);

        private void _OnClosing()
        {
            this.isOpen = null;

            this.OnClosing();
        }

        protected virtual void OnClosing()
        {
        }

        private void _OnClosed()
        {
            this.isOpen = false;
            
            this.OnClosed();

            if (this.Closed != null)

                this.Closed(this);
        }

        protected virtual void OnClosed()
        {
        }

        public void Abort()
        {
            this._OnClosing();

            this.OnAbort();

            this._OnClosed();
        }

        protected abstract void OnAbort();

        public Action<IMessagingContextChannel<MessageContext>> Opened { get; set; }

        public Action<IMessagingContextChannel<MessageContext>> Closed { get; set; }

        /// <summary>
        /// Returns a string representation of the instance, including <see cref="Interactions"/>, and any extra details provided from <see cref="GetStringDetails"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.GetType().FullName}; {nameof(Interactions)}: {Interactions}, {GetStringDetails()}.";
        }

        /// <summary>
        /// Override to supply additional details about the instance.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetStringDetails()
        {
            return "no additional details available";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    OnDispose(disposing);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        protected virtual void OnDispose(bool disposing)
        {
            if (this.IsOpen == true)

                this.Abort();
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AbstractMessagingContextChannel()
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
}
