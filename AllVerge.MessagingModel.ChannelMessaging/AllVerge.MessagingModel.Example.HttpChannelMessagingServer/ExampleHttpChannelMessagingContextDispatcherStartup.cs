using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;

using AllVerge.SystemPrimitives.Reflection;

using Microsoft.Extensions.Options;

namespace AllVerge.MessagingModel.Example.HttpChannelMessagingServer
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Hosting;

    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.MessagingModel.ChannelMessaging.Listeners;
    using AllVerge.MessagingModel.HttpChannelMessaging.Listeners;

    using AllVerge.MessagingModel.MessagingApplication.Builder;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Http;
    using AllVerge.MessagingModel.HttpChannelMessaging;
    using AllVerge.MessagingModel.MessagingFoundation.Configuration;
    using AllVerge.MessagingModel.MessagingFoundation.Client;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Xml.Linq;

    public class ExampleHttpChannelMessagingContextDispatcherStartup :
        BaseHttpChannelMessagingContextDispatcherStartup
    {
        ClientLoadTester loadTester;

        public ExampleHttpChannelMessagingContextDispatcherStartup(IConfiguration configuration) : base(configuration)
        {
            this.loadTester = new ClientLoadTester(
                new ResourceTransferBindingElement()
                {
                    Format = MessageEncodingFormat.Text
                }.GetBinding(), 
                new ResourceTransferBindingElement().GetBinding(),
                new NetHttpBinding());

            this.loadTester.RegisterForDisposal();
        }

        [ServiceContract(Namespace = "http://tempuri.org/", Name = "IHealthCheck")]
        [ResourceContract]
        interface IHealthCheck
        {
            [GetResourceAction("/health")]
            String Health();
        }
        class ClientLoadTester : IDisposable
        {
            private bool? running = null;
            private AggregateException? exception;
            private Action? OnClosing = null;
            private Binding healthCheckBinding;
            private Binding transferBinding;
            private Binding duplexBinding;

            public ClientLoadTester(Binding healthCheckBinding, Binding transferBinding, Binding duplexBinding)
            {
                Console.WriteLine("Starting test client(s) ...");

                this.healthCheckBinding = healthCheckBinding;
                this.transferBinding = transferBinding;
                this.duplexBinding = duplexBinding;
                ReadyAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)

                        this.exception = t.Exception;

                    else
                    {
                        this.running = true;
                        StartSendTestMessagesAsync(1, 0, 0);
                        //StartSendDuplexTestMessagesAsync(2, 0, 100);
                        StartSendTestMessagesAsync(3, 0, 0);
                        //StartSendDuplexTestMessagesAsync(4, 0, 100);
                    }
                });
            }

            private async Task ReadyAsync()
            {
                ResourceClient<IHealthCheck> serviceClient = 
                    new ResourceClient<IHealthCheck>(
                        this.healthCheckBinding, 
                        new EndpointAddress("http://localhost:5261"));

                Func<String> getServiceHealth = () =>
                {
                    try
                    {
                        return serviceClient.Channel.Health();
                    }
                    catch (Exception e)
                    {
#pragma warning disable CS8603 // Possible null reference return.
                        Console.WriteLine($"Health check faulted: {e.Message}");
                        Console.WriteLine("Retrying Health check ...");
                        return null;
#pragma warning restore CS8603 // Possible null reference return.
                    }
                };

                while (getServiceHealth() != "Healthy")
                {
                    await Task.Delay(1000);

                    if (running == false)

                        break;
                }

                Console.WriteLine("Service reported healthy.");
            }

            private Task StartSendTestMessagesAsync(int clientId, int startDelaySeconds, int sendNextPauseMS, int requests = -1)
            {
                return Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(startDelaySeconds));

                    int invoked = 0;
                    int results = 0;

                    IExampleService? serviceClient;

                    try
                    {
                        serviceClient =
                            new ExampleServiceClient(
                                this.transferBinding,
                                new EndpointAddress(ExampleService.ServiceUrl));
                        
                        OnClosing = () => ((ICommunicationObject)serviceClient).Close();

                        ICommunicationObject co = (ICommunicationObject)serviceClient;

                        while (running == true && co.State <= CommunicationState.Opened)
                        {
                            if (invoked > 0)
                            {
                                if (sendNextPauseMS > 0)
                                    await Task.Delay(sendNextPauseMS);
                                else
                                    await Task.Yield();
                            }

                            if (requests >= 0 && invoked == requests)
                                continue;

                            try
                            {
                                Console.WriteLine($"Sending clientId: {clientId}-request: {++invoked}");

                                Message? message = serviceClient?.Message(new Message($"clientId: {clientId}", $"{invoked}", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()));

                                if (message != null)
                                {
                                    long sentMs = long.Parse(message.Lines.ElementAt(2));
                                    long receivedMs = long.Parse(message.Lines.ElementAt(3));
                                    long repliedMs = long.Parse(message.Lines.ElementAt(4));
                                    long receivedReplyMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                                    Console.WriteLine($"Received: {message.Lines.ElementAt(0)}-{message.Lines.ElementAt(1)}/{++results} total half-duplex results (received/replied/received reply = {receivedMs - sentMs}/{repliedMs - sentMs}/{receivedReplyMs - sentMs}).");
                                }
                                else

                                    Console.WriteLine($"Received: clientId: {clientId}-empty reply/{++results} total half-duplex results.");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Received: clientId: {clientId}-fault/{++results} total half-duplex results.\n{e.Message}");
                            }

                            if (serviceClient != null && requests >= 0 && results == requests)
                                ((ICommunicationObject)serviceClient).Close();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e.Message}");
                    }
                });
            }

            private Task StartSendDuplexTestMessagesAsync(int clientId, int startDelaySeconds, int pauseBeforeSendMS, int requests = -1)
            {
                return Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(startDelaySeconds));

                    int invoked = 0;
                    int results = 0;

                    IDuplexExampleServiceClient? duplexServiceClient = null;

                    DuplexExampleResourceClient.OnHandleCallbackMessage onProcessReplyMessage = message =>
                    {
                        try
                        {
                            long sentMs = long.Parse(message.Lines.ElementAt(2));
                            long receivedMs = long.Parse(message.Lines.ElementAt(3));
                            long repliedMs = long.Parse(message.Lines.ElementAt(4));
                            long receivedReplyMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                            Console.WriteLine($"Received: {message.Lines.ElementAt(0)}-{message.Lines.ElementAt(1)}/{++results} total duplex results (received/replied/received reply = {receivedMs - sentMs}/{repliedMs - sentMs}/{receivedReplyMs - sentMs}).");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Received: clientId: {clientId}-fault/{++results} total duplex results.\n{e.Message}");
                        }

                        if (duplexServiceClient != null && requests >= 0 && results == requests)
                            ((ICommunicationObject)duplexServiceClient).Close();
                    };

                    try
                    {
                        duplexServiceClient =
                            new DuplexExampleResourceClient(
                                onProcessReplyMessage,
                                this.duplexBinding,
                                new EndpointAddress(ExampleDuplexService.ServiceUrl));

                        OnClosing = () => ((ICommunicationObject)duplexServiceClient).Close();

                        ICommunicationObject co = (ICommunicationObject)duplexServiceClient;

                        while (running == true && co.State <= CommunicationState.Opened)
                        {
                            if (invoked > 0)
                            {
                                if (pauseBeforeSendMS > 0)
                                    await Task.Delay(pauseBeforeSendMS);
                                else
                                    await Task.Yield();
                            }

                            if (requests >= 0 && invoked == requests)
                                continue;

                            try
                            {
                                Console.WriteLine($"Sending clientId: {clientId}-request: {++invoked}");

                                duplexServiceClient.Message(new Message($"clientId: {clientId}", $"{invoked}", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()));
                            }
                            catch (AggregateException e)
                            {
                                StringBuilder sb = e.InnerExceptions.Aggregate(new StringBuilder(), (sb, ie) => { sb.Append("-->"); sb.AppendLine(ie.Message); return sb; });

                                Console.WriteLine($"Sending: clientId: {clientId}-fault/{++results} total duplex results.\n{sb.ToString()}");
                            }
                            catch (OperationCanceledException e)
                            {
                                Console.WriteLine($"Sending: clientId: {clientId}-fault/{++results} total duplex results.\n{e.Message}");
                            }
                            catch (ObjectDisposedException e)
                            {
                                Console.WriteLine($"Sending: clientId: {clientId}-fault/{++results} total duplex results.\n{e.Message}");
                            }
                            catch (WebSocketException e)
                            {
                                Console.WriteLine($"Sending: clientId: {clientId}-fault/{++results} total duplex results.\n{e.Message}");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Sending: clientId: {clientId}-client fault/{++results} total duplex results.\n{e.Message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e.Message}");
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

                            try
                            {
                                this.OnClosing();
                            }
                            catch (CommunicationObjectFaultedException)
                            {
                                // no-op
                            }
                            catch (Exception)
                            {
                                throw;
                            } 
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

        protected override ImplementationTypeInfo<IMessagingContextReceiver<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext>> GetProtocolMessagingContextReceiverTypeInfo()
        {
            return ImplementationTypeInfo<IMessagingContextReceiver<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext>>.GetImplementationTypeInfo<HttpChannelMessagingContextChannelReceiver>();
        }

        protected override void OnConfigureHttpServices(IServiceCollection services)
        {
            base.OnConfigureHttpServices(services);

            services.AddHealthChecks();
            services.AddSingleton<IMessagingContextChannelListener<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext>>(new HttpRequestResponseChannelMessagingContextChannelListener());
            services.AddSingleton<IMessagingContextChannelListener<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext>>(new HttpAsynchronousRequestResponseChannelMessagingContextChannelListener());

            services.AddSingleton<IConfigureOptions<MessagingReceiveOptions>, MessagingReceiveOptionsSetup>();

            services.AddScoped<ExampleScoped>();
        }

        protected override void OnConfigureHttpMessagingApp(IApplicationBuilder app, IMessagingApplicationBuilder<ProtocolContextHost<HttpContext>, HttpContext, ChannelMessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
            app.UseHealthChecks("/health");

            messagingApp.UseMessagingApplication<ExampleChannelMessagingContextMiddleware, HttpContext, ChannelMessageContext>();
        }
    }
}
