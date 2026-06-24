using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    static class MessagingContextHostApplicationLoggerExtensions
    {
        private class HostingLogScope<MessageContext> : IReadOnlyList<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable, IReadOnlyCollection<KeyValuePair<string, object>>
        {
            private readonly IMessagingContext<MessageContext> _protocolMessagingContext;

            private readonly string _correlationId;

            private string _cachedToString;

            public int Count => 3;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>("RequestId", _protocolMessagingContext.BindingContext.InteractionContext.TraceIdentifier);
                        case 1:
                            {
                                PathString path = _protocolMessagingContext.BindingContext.InteractionContext.InputLocation;
                                return new KeyValuePair<string, object>("RequestPath", ((object)path).ToString());
                            }
                        case 2:
                            return new KeyValuePair<string, object>("CorrelationId", _correlationId);
                        default:
                            throw new ArgumentOutOfRangeException("index");
                    }
                }
            }

            public HostingLogScope(IMessagingContext<MessageContext> MessageContext, string correlationId)
            {
                _protocolMessagingContext = MessageContext;
                _correlationId = correlationId;
            }

            public override string ToString()
            {
                if (_cachedToString == null)
                {
                    _cachedToString = string.Format(CultureInfo.InvariantCulture, "RequestId:{0} RequestPath:{1}", _protocolMessagingContext.BindingContext.InteractionContext.TraceIdentifier, _protocolMessagingContext.BindingContext.InteractionContext.InputLocation);
                }
                return _cachedToString;
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                int num;
                for (int i = 0; i < Count; i = num)
                {
                    yield return this[i];
                    num = i + 1;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public static IDisposable RequestScope<MessageContext>(this ILogger logger, IMessagingContext<MessageContext> messagingContext, string correlationId)
        {
            return logger.BeginScope<HostingLogScope<MessageContext>>(new HostingLogScope<MessageContext>(messagingContext, correlationId));
        }
    }
}
