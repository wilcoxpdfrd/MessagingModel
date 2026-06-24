using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AllVerge.MessagingModel.Example.HttpChannelMessagingServer
{
    using AllVerge.MessagingModel.BaseService;
    using AllVerge.MessagingModel.HttpChannelMessaging;
    using AllVerge.MessagingModel.MessagingApplication.Builder;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;
    using AllVerge.MessagingModel.MessagingFoundation;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Configuration;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.SystemPrimitives.Net;
    using System.Runtime.CompilerServices;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

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
            MessagingHost.CreateBuilderForProtocol<HttpChannelMessagingHostBuilder, ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext, ExampleHttpChannelMessagingContextDispatcherStartup>(args)
            .UseKestrel()
            .UseIISIntegration()
            .ConfigureResourceDispatcherHost()
                .AddResourceDispatcherEndpoint<ExampleService, IExampleService>(
                    new ResourceTransferBindingElement() { HostNameComparisonMode = HostNameComparisonMode.Exact, TransportMaxPendingAccepts = 10 }.GetBinding(),
                    ExampleService.ServiceUrl)
                .AddResourceDispatcherEndpoint<ExampleDuplexService, IDuplexExampleService>(
                    new NetHttpBinding() { HostNameComparisonMode = HostNameComparisonMode.Exact },
                    ExampleDuplexService.ServiceUrl)
            .Apply()
            .Build();
    }
    class ExampleService : BaseService, IExampleService
    {
        public static String ServiceUrl =>
            $"{TransportProtocolSchemes.HTTP_DELIMITED}localhost:8091/exampleservice";

        public Message Message(Message message)
        {
            DateTimeOffset receivedTimeUTC = MessagingInteractionContextAccessor.Current.InteractionContext.StartTimeUTC;

            return new Message(message.Lines.ElementAt(0), $"reply {message.Lines.ElementAt(1)}", message.Lines.ElementAt(2), receivedTimeUTC.ToUnixTimeMilliseconds().ToString(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        }
    }

    class ExampleDuplexService : BaseService, IDuplexExampleService
    {
        public static String ServiceUrl =>
            $"{TransportProtocolSchemes.HTTP_DELIMITED}localhost:8090/exampleduplexservice";

        public ExampleDuplexService() : base() { }

        TaskCompletionSource<Message> tcs = new TaskCompletionSource<Message>();

        public void Message(Message message)
        {
            DateTimeOffset receivedTimeUTC = MessagingInteractionContextAccessor.Current.InteractionContext.StartTimeUTC;

            Message callbackMessage = new HttpChannelMessagingServer.Message(message.Lines.ElementAt(0), $"reply {message.Lines.ElementAt(1)}", message.Lines.ElementAt(2), receivedTimeUTC.ToUnixTimeMilliseconds().ToString(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());

            this.tcs.SetResult(callbackMessage);
        }

        public object Map(System.ServiceModel.Channels.MessageFault messageFault, MessageVersion messageVersion)
        {
            return new Message(messageFault, messageVersion);
        }

        Task<Message> IDuplexExampleService.GetMessageResponseAsync()
        {
            return tcs.Task;
        }
    }
}
