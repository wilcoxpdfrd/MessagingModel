using System;
using System.Collections.Generic;
using System.Text;

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Connections.Features;
#endif

namespace AllVerge.MessagingModel.ChannelMessaging.Connections.Features
{
#if NET6_0_OR_GREATER
    internal class NoPersistentStateFeature : IPersistentStateFeature
    {
        public IDictionary<object, object> State => throw new NotImplementedException();
    }
#endif
}
