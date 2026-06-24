
using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    /// <summary>
    /// Defines the contract for a dispatcher operation acceptor that associates incoming messages with a dispatcher/operation 
    /// with which to route the message.
    /// </summary>
	public interface IChannelDispatcherManager
    {
        /// <summary>
        /// Configures a dispatcher factory.
        /// </summary>
        /// <param name="pathEnvironment"></param>
        /// <param name="services"></param>
        void ConfigureEnvironment(IPathEnvironment pathEnvironment, IServiceProvider services);

        /// <summary>
        /// Gets an indication whether the internal channel implements <see cref="IDuplexChannel"/>.  
        /// When false, indicates that the internal channel implements <see cref="IReplyChannel"/>.
        /// </summary>
        bool IsDuplexChannelListener { get; }

        bool TryMatchDispatcherOperation(ref Message receivedMessage, out IDispatcherRuntime dispatcherRuntime);
    }
}
