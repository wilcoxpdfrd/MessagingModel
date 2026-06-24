using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace AllVerge.MessagingModel.MessagingApplication
{
    /// <summary>
    /// Extensions to support registering services (that implement IDisposal) for disposal.
    /// </summary>
    public static class DisposalExtensions
    {
        static CancellationTokenSource applicationStoppingSource = new CancellationTokenSource();

        /// <summary>
        /// Registers the application cancellation token.
        /// </summary>
        /// <param name="applicationStopping"></param>
        public static void SetApplicationStoppingToken(CancellationToken applicationStopping)
        {
            applicationStopping.Register(
                () =>
                { 
                    applicationStoppingSource.Cancel();
                }
            );
        }

        /// <summary>
        /// Registers a serivce that implements IDisposal to be disposed when the application stops.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <returns></returns>
        public static T RegisterForDisposal<T>(this T service)
        {
            if (service is IAsyncDisposable)

                applicationStoppingSource.Token.Register(() => (service as IAsyncDisposable).DisposeAsync());

            else if (service is IDisposable)

                applicationStoppingSource.Token.Register(() => (service as IDisposable).Dispose());

            return service;
        }
    }
}