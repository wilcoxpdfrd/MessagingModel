using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace AllVerge.MessagingModel.MessagingApplication
{
    interface IMessagingMiddlewareFactory<MessageContext>
    {
        IMessagingMiddleware<MessageContext> Create(Type middlewareType);
        void Release(IMessagingMiddleware<MessageContext> middleware);
    }
}
