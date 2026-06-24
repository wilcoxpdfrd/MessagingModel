using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AllVerge.MessagingModel.MessagingApplication
{

    public interface IMessagingApplicationContextMiddleware<MessageContext>
    {
        void Init(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken cancellationToken);
        Task<bool> ReadyAsync();
        Task InvokeAsync(IMessagingContext<MessageContext> messagingContext);
    }
}
