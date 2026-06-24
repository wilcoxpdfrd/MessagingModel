using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

using System.ServiceModel;
using System.ServiceModel.Channels;

using Microsoft.Extensions.CommandLineUtils;

namespace AllVerge.MessageProcessor.ZeroMQ.TcpDuplexListenerExample
{
    using AllVerge.SystemPrimitives.Logging;

    using AllVerge.MessagingModel.ZeroMQChannelMessaging;
    using AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration;
    public class ZeroMQTcpDuplexExampleClientProgram
    {
        class TestClient : IDisposable
        {
            private ZeroMQConnectionOrientedBindingElement bindingElement;
            bool running = true;
            Action OnClosing = null;

            public TestClient(ZeroMQConnectionOrientedBindingElement bindingElement)
            {
                this.bindingElement = bindingElement;

                StartSendTestMessagesAsync(1);
            }

            private Task StartSendTestMessagesAsync(int clientId)
            {
                return Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    int invoked = 0;
                    int replied = 0;

                    IDuplexExampleServiceClient duplexServiceClient = null;

                    DuplexExampleServiceClient.OnHandleCallbackMessage onProcessReplyMessage = message =>
                    {
                        try
                        {
                            Console.WriteLine($"Received: {String.Join("\n", message.Lines)}/{++replied} total duplex reply(ies).");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Received exception: {e.Message}/{++replied} total duplex reply(ies).");
                        }
                    };

                    DuplexExampleServiceClient.OnHandleCallbackMessageFault onProcessReplyFault = (MessageFault fault, MessageVersion messageVersion, UniqueId relatesTo, out System.ServiceModel.Channels.Message faultMessage) =>
                    {
                        faultMessage = System.ServiceModel.Channels.Message.CreateMessage(messageVersion, "http://tempuri.org/IDuplexExampleService/Message", new Message(fault, messageVersion));
                    };

                    try
                    {
                        duplexServiceClient =
                            new DuplexExampleServiceClient(
                                onProcessReplyMessage,
                                onProcessReplyFault,
                                new ZeroMQConnectionOrientedSoap12MessageBinding(this.bindingElement), new EndpointAddress(ZeroMQTcpDuplexExampleProgram.ExampleDuplexService.ServiceUrl));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e.Message}");

                        running = false;
                    }

                    OnClosing = () => ((ICommunicationObject)duplexServiceClient).Close();

                    while (true)
                    {
                        if (true)
                        {
                            await Task.Delay(10);
                            //if (invoked > 10)
                            //    System.Diagnostics.Debugger.Break();
                            //if (invoked > 0)
                            //    continue;
                        }

                        if (!running)

                            break;

                        try
                        {
                            Console.WriteLine($"Invoking: {++invoked}");

                            duplexServiceClient.Message(new Message($"clientId: {clientId}", $"{invoked}"));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Invoking duplex service faulted: {e.Message}");
                        }
                    }
                });
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        this.running = false;

                        if (this.OnClosing != null)

                            this.OnClosing();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~TestClient() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion
        }

        public static void Main(string[] args)
        {
            var cmd = new CommandLineApplication();

            cmd.FullName = typeof(ZeroMQTcpDuplexExampleClientProgram).Namespace;

            var listenUrlArg =
                cmd.Option("-l|--listenUrl <value>", $"Specifies the {cmd.FullName} (base) listen url (required)", CommandOptionType.SingleValue);

            String listenUri = null;

            cmd.OnExecute(() =>
            {
                if (listenUrlArg.HasValue())

                    listenUri = listenUrlArg.Value();

                else

                    return 1;

                return 0;
            });

           cmd.HelpOption("-?|-h|--help");

            Logger logger = Logger.GetInstance<ZeroMQTcpDuplexExampleClientProgram>();

            if (cmd.Execute(args) != 0)
            {
                cmd.ShowHelp();

                logger.Log(LoggerType.Info, Severity.DEBUG, $"exiting {nameof(ZeroMQTcpDuplexExampleClientProgram)} with code 1");

                Environment.Exit(1);
            }
            else if (cmd.IsShowingInformation)
            {
                logger.Log(LoggerType.Info, Severity.DEBUG, $"exiting {nameof(ZeroMQTcpDuplexExampleClientProgram)} with code 0");

                Environment.Exit(0);
            }

            try
            {
                TestClient testClient = new TestClient(ZeroMQConnectionOrientedBindingElement.CreateTcpConnectionOrientedBindingElement(ZeroMQMessageEncoding.Binary));
            }
            catch (Exception e)
            {
                logger.Log(e);
            }
        }
    }
}
