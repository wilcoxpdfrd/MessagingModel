using System;

using AllVerge.MessagingModel.MessagingApplication;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using AllVerge.MessagingModel.ChannelMessaging;
using Microsoft.AspNetCore.Hosting.Server.Features;
using AllVerge.MessagingModel.MessagingApplication.Hosting;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer
{
    internal class ExampleMessagingContextReceiver :
        BaseMessagingContextReceiver<ExampleMessageContext>
    {
        public ExampleMessagingContextReceiver(IApplicationHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider services) : 
            base(hostEnvironment, hostApplicationLifetime, services)
        {
        }

        protected override ExampleMessageContext GetNullMessageContext(IMessagingContext<ExampleMessageContext> messagingContext)
        {
            throw new NotImplementedException();
        }

        protected override ExampleMessageContext GetRejectedMessageContext(IMessagingContext<ExampleMessageContext> messagingContext, RejectCode rejectionCode, IDictionary<RejectHeaders, StringValues> rejectionHeaders, string faultAction = null)
        {
            throw new NotImplementedException();
        }
    }
}