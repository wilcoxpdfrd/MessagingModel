using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace AllVerge.MessagingModel.ChannelMessaging.Examples.Tests
{
    using AllVerge.MessagingModel.MessagingFoundation.Configuration;

    using AllVerge.MessagingModel.ZeroMQChannelMessaging;
    using AllVerge.MessagingModel.ZeroMQChannelMessaging.Configuration;

    using AllVerge.MessageProcessor.ZeroMQ.TcpDuplexListenerExample;
    using System.Reflection;
    using Xunit.Abstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities;

    public class TestContext: IDisposable
    {
        public Dictionary<string, string?> Properties;

        public TestContext()
        {
            Properties = new Dictionary<string, string?>();
            SetProperty("invokeCount");
            SetProperty("completionDelay");
        }

        public void Dispose()
        {
            Properties.Clear();
        }

        private void SetProperty(string name)
        {
            Properties.Add(name, System.Environment.GetEnvironmentVariable(name));
        }
    }

    public class ZeroMQ_TcpDuplexListenerExampleTests : IClassFixture<TestContext>
    {
        private readonly ITestOutputHelper output;

        class TestClient : IDisposable
        {
            private readonly ITestOutputHelper output;
            private ZeroMQConnectionOrientedBindingElement bindingElement;
            bool running = false;
            Action? OnClosing = null;

            public TestClient(ZeroMQConnectionOrientedBindingElement bindingElement, ITestOutputHelper output)
            {
                this.bindingElement = bindingElement;
                this.output = output;
            }

            public Task<bool> SendTestMessagesAsync(int clientId)
            {
                this.output.WriteLine("SendTestMessagesAsync");

                return Task<bool>.Run(async () =>
                {
                    this.output.WriteLine("running SendTestMessagesAsync");

                    int invoked = 0;
                    int replies = 0;
                    int faults = 0;

                    IDuplexExampleServiceClient? duplexServiceClient = null;

                    DuplexExampleServiceClient.OnHandleCallbackMessage onProcessReplyMessage = message =>
                    {
                        try
                        {
                            this.output.WriteLine($"Received: {String.Join("\n", message.Lines)}/{++replies} total duplex reply messages.");
                        }
                        catch (Exception e)
                        {
                            this.output.WriteLine($"Received exception: {e.Message}/{++faults} total duplex reply faults/{++replies} total duplex reply messages.");
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

                        running = true;
                    }
                    catch (Exception e)
                    {
                        this.output.WriteLine($"Error: {e.Message}");
                    }

                    OnClosing = () => (duplexServiceClient as ICommunicationObject)?.Close();

                    while (true)
                    {
                        if (true)
                        {
                            await Task.Delay(10);

                            if (invoked >= _invokeCount)

                                break;
                        }

                        if (!running)

                            break;

                        try
                        {
                            this.output.WriteLine($"Invoking: {++invoked}");

                            duplexServiceClient?.Message(new Message($"clientId: {clientId}", $"{invoked}"));
                        }
                        catch (Exception e)
                        {
                            if (e is TargetInvocationException)
                            {
                                while (e is TargetInvocationException)

                                    e = e.InnerException ?? new Exception("Unexpected null InnerException of TargetInvocationException.");
                            }

                            this.output.WriteLine($"Invoking duplex service faulted: {e.Message}");
                        }
                    }

                    // Give it time for all async replies to complete ...

                    if (_invokeCount > 0)

                        await Task.Delay(TimeSpan.FromSeconds(_completionDelay));

                    (duplexServiceClient as ICommunicationObject)?.Close();

                    return _invokeCount > 0 && _invokeCount == invoked && invoked == replies;
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

        static int _invokeCount;
        static int _completionDelay;

        public ZeroMQ_TcpDuplexListenerExampleTests(TestContext context, ITestOutputHelper output)
        {
            this.output = output;

            this.output.WriteLine($"Executing {nameof(ZeroMQ_TcpDuplexListenerExampleTests)}");

            if (Int32.TryParse(context.Properties["invokeCount"]?.ToString(), out Int32 invokeCount))

                _invokeCount = invokeCount;

            else

                _invokeCount = 100;

            if (Int32.TryParse(context.Properties["completionDelay"]?.ToString(), out Int32 completionDelay))

                _completionDelay = completionDelay;

            else

                _completionDelay = 5;
        }

        [Fact]
        public async Task TcpDuplexPollerExampleTestAsync()
        {
            this.output.WriteLine($"Executing {nameof(TcpDuplexPollerExampleTestAsync)}");

            int openAndSendTimeoutSeconds = 60 * 60; // 1;
            TestClient testClient =
                new TestClient(
                    ZeroMQConnectionOrientedBindingElement.CreateTcpConnectionOrientedBindingElement(ZeroMQMessageEncoding.Binary).SetOpenTimeout(TimeSpan.FromSeconds(openAndSendTimeoutSeconds)).SetSendTimeout(TimeSpan.FromSeconds(openAndSendTimeoutSeconds)),
                    this.output);

            bool result = await testClient.SendTestMessagesAsync(1);

            Assert.True(result, "Send test messages was unsuccessful.");
        }
    }
}