using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public interface IProtocolContextFactory<ProtocolContext>
    {
        ProtocolContext Create(IFeatureCollection contextFeatures);

        void Dispose(ProtocolContext context);

        void Dispose(ProtocolContext context, Exception exception);
    }

    public static class ProtocolContextFactoryExtensions
    {
        public static bool TryGetHeader(this IDictionary<RejectHeaders, StringValues> notHandledHeaders, RejectHeaders header, out StringValues value)
        {
            if (notHandledHeaders == null || !notHandledHeaders.ContainsKey(header))
            {
                value = default(StringValues);

                return false;
            }

            value = notHandledHeaders[header];

            return true;
        }
    }
}
