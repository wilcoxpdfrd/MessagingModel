using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public interface IProtocolContextAccessorFactory<ProtocolContext>
    {
        void RegisterListenAddresses(ICollection<string> addresses);
        void GetProtocolContextAccessor(out IProtocolContextAccessor<ProtocolContext> protocolContextAccessor);
    }
}
