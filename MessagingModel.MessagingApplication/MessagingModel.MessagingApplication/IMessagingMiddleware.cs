using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AllVerge.MessagingModel.MessagingApplication
{
    /// <summary>
    /// Defines middleware that can be added to the application's request pipeline.
    /// When the application is an <see cref="Microsoft.AspNetCore.Http"/> application, this inteface provides a bridge from a <see cref="RequestDelegate"/> to a <see cref="MessagingContextMiddlewareDelegate{MessageContext}"/>.
    /// Othewise the server should call the <see cref="MessagingContextMiddlewareDelegate{MessageContext}"/> directly.
    /// </summary>
    /// <typeparam name="MessageContext"></typeparam>
    public interface IMessagingMiddleware<MessageContext> : IMiddleware
    {
        Task InvokeAsync(IMessagingContext<MessageContext> context, MessagingContextMiddlewareDelegate<MessageContext> next);
    }
}