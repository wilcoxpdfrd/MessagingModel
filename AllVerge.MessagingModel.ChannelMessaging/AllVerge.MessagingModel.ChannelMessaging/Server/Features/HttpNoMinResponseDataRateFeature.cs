using System;
using System.Collections.Generic;
using System.Text;

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
#endif

namespace AllVerge.MessagingModel.ChannelMessaging.Server.Features
{
#if NET6_0_OR_GREATER
    internal class HttpNoMinResponseDataRateFeature : IHttpMinResponseDataRateFeature
    {
        public MinDataRate MinDataRate { get; set; }
    }
#endif
}
