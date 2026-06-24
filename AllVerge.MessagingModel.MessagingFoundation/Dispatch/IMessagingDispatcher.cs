using System;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    /// <summary>
    /// Contract for messaging dispatchers.
    /// </summary>
    public interface IMessagingDispatcher
    {
        /// <summary>
        /// Configures the singleston instance of the messaging dispatcher.
        /// </summary>
        /// <param name="singletonInstance"></param>
        /// <param name="pathEnvironment"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="endpointDispatcher"></param>
        void ConfigureSingletonDispatcher(Object singletonInstance, IPathEnvironment pathEnvironment, IServiceProvider serviceProvider, Uri listenUri);

        /// <summary>
        /// Clones <paramref name="singletonInstance"/> and returns the cloned instance.  
        /// The instance will be used to process a new request when per-call is specified.
        /// </summary>
        /// <param name="singletonInstance"></param>
        /// <returns></returns>
        Object CloneDispatcherInstanceFromSingleton(Object singletonInstance);

        /// <summary>
        /// Gets the host paths environment for the message listener and handler.
        /// </summary>
        /// <returns><see cref="IPathEnvironment"/>.</returns>
        IPathEnvironment GetPathEnvironment();
        /// <summary>
        /// Gets the address the host listens to.
        /// </summary>
        /// <returns><see cref="Uri"/>.</returns>
        Uri GetListenerAddress();
        /// <summary>
        /// Gets the service provider for the handler processing the message.
        /// </summary>
        /// <returns><see cref="IServiceProvider"/>.</returns>
        IServiceProvider GetServiceProvider();
        /// <summary>
        /// Indicates whether the message handler is an intermediate (true) or terminating handler (false).
        /// </summary>
        bool IsIntermediateHandler { get; }
        /// <summary>
        /// Indicates that the message handler has a duplex callback assignable to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">An interface type defining a callback method which dispatches the outgoing message.</typeparam>
        /// <returns>true if the message interaction is duplex, and the call back interface is assignable to <typeparamref name="T"/>;  otherwise false.</returns>
        bool HasDuplexCallback<T>() where T : class;
        /// <summary>
        /// Raised upon receiving an incoming message.
        /// </summary>
        /// <param name="source">The instance of the runtime handling the incoming message.</param>
        /// <param name="args">The arguments to the event.</param>
        void OnReceivedIncomingMessage(Object source, IncomingMessageEventArgs args);
        /// <summary>
        /// Raised prior to dispatching an outgoing message.
        /// </summary>
        /// <param name="source">The instance of the runtime dispatching the outgoing message.</param>
        /// <param name="args">The arguments to the event.</param>
        void OnDispatchOutgoingMessage(Object source, OutgoingMessageEventArgs args);
    }
}
