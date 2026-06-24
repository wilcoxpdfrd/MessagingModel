using System;
using System.Threading.Tasks;
using System.Linq;
using System.Xml;
using System.ServiceModel.Channels;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AllVerge.MessageProcessor.ZeroMQ.TcpDuplexListenerExample
{
    using AllVerge.MessagingModel.MessagingApplication.Builder;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.ZeroMQChannelMessaging;
    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.SystemPrimitives.Net;
    using AllVerge.MessagingModel.BaseService;
    using AllVerge.MessagingModel.MessagingFoundation;
    using AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration;
    using System.Net;

    public class ZeroMQTcpDuplexExampleProgram
    {
        static void Main(string[] args)
        {
            try
            {
                BuildWebHost(args).Run();
            }
            catch (TaskCanceledException) { }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            MessagingHost.CreateBuilderForProtocol<ZeroMQChannelMessagingHostBuilder, ZeroMQProtocolContext, ChannelMessageContext, ZeroMQTcpDuplexExampleStartup>(args)
            .UseProtocolMessagingContextServer<ZeroMQChannelMessagingContextServer, ZeroMQProtocolContext, ChannelMessageContext>(o => o.UrlPrefixes.Add(new Uri(TransportProtocolSchemes.ZEROMQ_TCP_DELIMITED)))
            .ConfigureResourceDispatcherHost().AddResourceDispatcherEndpoint<ExampleDuplexService, IDuplexExampleService>(
                new ZeroMQConnectionOrientedSoap12MessageBinding(new ZeroMQConnectionOrientedBindingElement()), ExampleDuplexService.ServiceUrl)
            .Apply()
            .Build();

        public class ExampleDuplexService : BaseService, IDuplexExampleService
        {
            TaskCompletionSource<Message> tcs = new TaskCompletionSource<Message>();

            public static String ServiceUrl =>
                $"{TransportProtocolSchemes.ZEROMQ_TCP_DELIMITED}localhost:65535";//{IPUtility.GetAvailablePort()}";

            public Message Message(Message message)
            {
                int messageIndex = message.Lines.Select(l => int.Parse(l)).FirstOrDefault();

                if (messageIndex % 10 == 1)

                    throw new Exception($"Exception deliberately thrown for: {messageIndex}");

                return new Message(message.Lines.Select(l => $"reply: {l}").ToArray());
            }

            public object Map(MessageFault messageFault, MessageVersion messageVersion)
            {
                return new Message(messageFault, messageVersion);
            }

            void IDuplexExampleService.Message(Message message)
            {
                Message callbackMessage = new Message(message.Lines.ElementAt(0), $"reply {message.Lines.ElementAt(1)}");

                this.tcs.SetResult(callbackMessage);
            }

            Task<Message> IDuplexExampleService.GetReplyAsync()
            {
                return tcs.Task;
            }

        }
    }
}
