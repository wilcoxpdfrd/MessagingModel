using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.Extensions.Primitives;

    using AllVerge.MessagingModel.MessagingApplication;

    public class ZeroMQProtocolContextFactory : IProtocolContextFactory<ZeroMQProtocolContext>
    {
        public ZeroMQProtocolContext Create(IFeatureCollection features)
        {
            return new ZeroMQProtocolContext(null, defaultReceiveTimeout: TimeSpan.Zero, defaultSendTimeout: TimeSpan.Zero, null);
        }

        public void Dispose(ZeroMQProtocolContext context)
        {
        }

        public void Dispose(ZeroMQProtocolContext context, Exception exception)
        {
        }

        public Task RejectAsync(ZeroMQProtocolContext context, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders)
        {
            throw new NotImplementedException($"{nameof(ZeroMQProtocolContextFactory)}::{nameof(RejectAsync)}");
        }
    }
}
