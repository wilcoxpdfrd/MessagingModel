using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Net;

using Microsoft.Extensions.Hosting;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;

    using AllVerge.MessagingModel.ChannelMessaging.Listeners;

    using AllVerge.SystemPrimitives.Threading.Tasks;

    /// <summary>
    /// Abstract listener class.  
    /// Concrete implementations are used by a messaging context receiver to bind the listen addresses 
    /// supplied to it and receive associated messaging context channels.
    /// </summary>
    public abstract class AbstractMessagingContextChannelListener :
        IAbstractMessagingContextChannelListener
    {
        IApplicationHostEnvironment hostEnvironment;
        IEnumerable<string> listenAddresses;

        protected AbstractMessagingContextChannelListener()
        {
        }

        protected IServiceProvider Services { get; private set; }
        /// <summary>
        /// The addresses the listener is listening on after the listener has started.
        /// </summary>
        public CancellationToken CancellationToken { get; private set; }
        public IEnumerable<string> ListenAddresses { get; private set; }

        /// <summary>
        /// Called by the Messaging Context Receiver.
        /// </summary>
        /// <param name="hostEnvironment"></param>
        /// <param name="listenAddresses">The addresses provided to the receiver.</param>
        /// <param name="services"></param>
        /// <param name="cancellationToken"></param>
        public void Init(IApplicationHostEnvironment hostEnvironment, IEnumerable<string> listenAddresses, IServiceProvider services, CancellationToken cancellationToken)
        {
            this.Services = services;
            this.CancellationToken = cancellationToken;
            this.listenAddresses = listenAddresses;
            this.hostEnvironment = hostEnvironment;

            this.OnInit();
        }

        protected virtual void OnInit()
        {
        }

        Task IAbstractMessagingContextChannelListener.StartListeningAsync()
        {
            IList<string> listenAddresses = new List<String>(this.listenAddresses);

            return OnStartListeningAsync(this.hostEnvironment, listenAddresses, this.Services, this.CancellationToken).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully(out Object asyncState, out Exception exception))

                    this.ListenAddresses = listenAddresses;

                else throw exception;
            });
        }

        /// <summary>
        /// Implementation should select one or more of the <paramref name="listenAddresses"/> to bind to and start 
        /// listening on, removing from the list those addresses not selected.  
        /// Important note: The returned task should complete when the listener has started listening; 
        /// the listener should continue to listen asynchronously once started!
        /// </summary>
        /// <param name="hostEnvironment"></param>
        /// <param name="listenAddresses"></param>
        /// <param name="services"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task OnStartListeningAsync(IApplicationHostEnvironment hostEnvironment, IList<string> listenAddresses, IServiceProvider services, CancellationToken cancellationToken);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    OnDispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        protected virtual void OnDispose()
        {
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MessageListener() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    public abstract class MessagingContextChannelListener<MessageContext> :
        AbstractMessagingContextChannelListener,
        IMessagingContextChannelListener<MessageContext> where MessageContext : IMessageContext
    {
        public abstract Task<(bool success, IReceiveMessagingContextChannel<MessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync();
        public abstract bool MapToBindingContext(MessageContext context, BindingContext bindingContext);
    }

    public abstract class BaseProtocolMessagingContextChannelListener<ProtocolContext, MessageContext> :
        AbstractMessagingContextChannelListener,
        IMessagingContextChannelListener<ProtocolContext, MessageContext> where MessageContext : IMessageContext
    {
        public abstract Task<(bool, ProtocolContext)> TryReceiveContext();
        
        public abstract Task<(bool? success, IReceiveMessagingContextChannel<MessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync(ProtocolContext protocolContext);

        public abstract bool MapToBindingContext(IMessagingContext<ProtocolContext> context, BindingContext bindingContext);
    }

    public abstract class BaseProtocolMessagingContextChannelListener<ProtocolContextHost, ProtocolContext, MessageContext> :
        AbstractMessagingContextChannelListener,
        IMessagingContextChannelListener<ProtocolContextHost, ProtocolContext, MessageContext> 
        where ProtocolContextHost: IProtocolContextHost<ProtocolContext>
        where MessageContext : IMessageContext
    {
        public abstract Task<(bool? success, IReceiveMessagingContextChannel<MessageContext> messagingContextChannel)> TryAcceptMessagingContextChannelAsync(ProtocolContextHost protocolContextHost);

        public abstract bool MapToBindingContext(ProtocolContextHost protocolContextHost, BindingContext bindingContext);
    }
}
