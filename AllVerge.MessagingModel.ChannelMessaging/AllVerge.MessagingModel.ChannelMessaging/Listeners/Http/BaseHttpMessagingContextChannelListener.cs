using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;

using AllVerge.MessagingModel.ChannelMessaging.Http;
using AllVerge.MessagingModel.MessagingApplication;
using AllVerge.MessagingModel.MessagingApplication.Http;

using System;
using System.Net;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;

namespace AllVerge.MessagingModel.ChannelMessaging.Listeners.Http
{
    public abstract class BaseHttpMessagingContextChannelListener<MessageContext> :
        BaseProtocolMessagingContextChannelListener<ProtocolContextHost<HttpContext>, HttpContext, MessageContext>
        where MessageContext: IMessageContext
    {
        public override bool MapToBindingContext(ProtocolContextHost<HttpContext> context, BindingContext bindingContext)
        {
            HttpContext protocolContext = context.ProtocolContext;

            bindingContext.ConnectionContext.Map(
                protocolContext.Connection.Id,
                protocolContext.Items,
                new IPEndPoint(protocolContext.Connection.LocalIpAddress, protocolContext.Connection.LocalPort),
                new IPEndPoint(protocolContext.Connection.RemoteIpAddress, protocolContext.Connection.RemotePort)
            );

            bindingContext.InteractionContext.Map(
                (HttpConnectionContextHeaders)new RequestHeaders(protocolContext.Request.Headers),
                protocolContext.Request.GetRequestUri().AbsoluteUri,
                protocolContext.Request.Method,
                new DateTimeOffset(context.StartTimestamp, TimeSpan.Zero),
                protocolContext.TraceIdentifier,
                protocolContext.User);

            return true;
        }
    }
}
