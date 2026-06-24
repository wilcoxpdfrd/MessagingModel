using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public interface IProtocolContextAccessor<ProtocolContext>
    {
        /// <summary>
        /// Event that fires when the connection is closed.
        /// </summary>
        event Action<String> OnConnectionClosed;

        /// <summary>
        /// Receives the <paramref name="protocolContext"/>.  The connection timeout count-down should be reset to zero when this occurs.
        /// </summary>
        /// <param name="protocolContext"></param>
        void SetProtocolContextAsync(ProtocolContext protocolContext);
        /// <summary>
        /// Gets a task that completes with <typeparamref name="ProtocolContext"/> result when the result is received on the connection listening on <paramref name="listenUri"/>.
        /// </summary>
        /// <param name="listenUri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ProtocolContext> GetProtocolContextAsync(Uri listenUri, CancellationToken cancellationToken);
        /// <summary>
        /// Gets a task that gives the <paramref name="connectionId"/> as result when the connection times out.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        Task<String> WaitForConnectionTimeoutAsync(String connectionId);
    }
}
