using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using AllVerge.MessagingModel.MessagingApplication.Builder;

using AllVerge.MessagingModel.ChannelMessaging;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer
{
    class ExampleListenerProgram
    {
        static void Main(string[] args)
        {
            try
            {
                BuildWebHost(args).Run();
            }
            catch (TaskCanceledException)
            { }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            MessagingHost.CreateDefaultBuilder<ExampleMessageContext, ExampleListenerStartup>(args)
            .UseMessagingContextServer<ExampleMessagingContextServer, ExampleMessageContext>(o => o.UrlPrefixes.Add(new Uri("uri:*")))
            .UseUrls("uri:examplemessaging", "uri:exampleasyncmessaging", "uri:examplepollmessaging")
            .PreferHostingUrls(true)
            .Build();
    }
}
