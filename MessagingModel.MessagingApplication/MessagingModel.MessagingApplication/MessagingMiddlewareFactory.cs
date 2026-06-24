using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AllVerge.MessagingModel.MessagingApplication
{
    class MessagingMiddlewareFactory<MessageContext> : IMessagingMiddlewareFactory<MessageContext>
    {
        private readonly IServiceProvider _serviceProvider;

        public MessagingMiddlewareFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IMessagingMiddleware<MessageContext> Create(Type middlewareType)
        {
            return _serviceProvider.GetRequiredService(middlewareType) as IMessagingMiddleware<MessageContext>;
        }

        public void Release(IMessagingMiddleware<MessageContext> middleware)
        {
        }
    }
}
