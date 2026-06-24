using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;

    public interface IInteractionContextChannel
    {
		EndpointAddress LocalAddress {  get;  }

		EndpointAddress RemoteAddress { get; }

		string SessionId { get; }

        TimeSpan SendTimeout { get; }

        /// <summary>
        /// Returns the call back channel <typeparamref name="T"/> when a <see cref="ServiceChannel"/> is present and the interaction is duplex.  Otherwise returns null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dispatcher"></param>
        /// <returns></returns>
        T GetCallbackChannel<T>(IMessagingDispatcher dispatcher) where T : class;
    }
}
