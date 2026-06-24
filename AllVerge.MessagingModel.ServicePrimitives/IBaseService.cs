using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllVerge.MessagingModel.ServicePrimitives
{
    using AllVerge.MessagingModel.MessagingFoundation;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;

    /// <summary>
    /// Base service interface.
    /// </summary>
    public interface IBaseService : IMessagingDispatcher, IPolicyService, IPolicyActionService, IDiagnosticService, IDiagnosticServiceAction, IDisposable
    {
        /// <summary>
        /// Sets the the service host environment and service address.  
        /// Invoked once when an instance of the service is created; 
        /// the instance should be configured when this method is called.
        /// </summary>
        /// <param name="pathEnvironment"></param>
        /// <param name="serviceAddress"></param>
        void SetEnvironmentAndConfigure(IPathEnvironment pathEnvironment, Uri serviceAddress, IServiceProvider serviceProvider);

        /// <summary>
        /// Implement to clone the configured "singleton" service instance, providing a per-call instance.
        /// </summary>
        /// <returns></returns>
        IBaseService CloneConfiguredInstance();

        /// <summary>
        /// Notifies that ContextInstance is available.  Invoked once per request.
        /// </summary>
        /// <param name="instanceMode"></param>
        void OnMessageHandlerContextAvailable();
    }
}
