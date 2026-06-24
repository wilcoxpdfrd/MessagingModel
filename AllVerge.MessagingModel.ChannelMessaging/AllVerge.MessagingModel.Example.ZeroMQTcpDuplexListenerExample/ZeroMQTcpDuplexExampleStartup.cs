using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

using Microsoft.Extensions.Configuration;

namespace AllVerge.MessageProcessor.ZeroMQ.TcpDuplexListenerExample
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingApplication.Builder;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.ZeroMQChannelMessaging;
    using AllVerge.SystemPrimitives.Net;
    using Humanizer;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System.Threading;

    public class ZeroMQTcpDuplexExampleStartup : ZeroMQTcpListenerStartup
    {
        public ZeroMQTcpDuplexExampleStartup(IConfiguration configuration) : base(configuration) { }

        protected override void OnConfigureMessagingApp(IMessagingApplicationBuilder<ZeroMQProtocolContext, ChannelMessageContext> messagingApp, ILoggerFactory loggerFactory, IHostEnvironment hostEnvironment, CancellationToken applicationStopping)
        {
            // TestClientAsync();
        }

        private Task TestClientAsync()
        {
            return Task.Run(() => {

                ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");

                startInfo.WorkingDirectory = FileSystemUtils.GetFullPath("..\\..\\..\\..\\AllVerge.MessagingModel.ChannelMessaging.Examples.Tests\\bin\\Debug\\net6.0");

                startInfo.CreateNoWindow = true;
                startInfo.ErrorDialog = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;

                startInfo.EnvironmentVariables["invokeCount"] = "100";
                startInfo.EnvironmentVariables["completionDelay"] = "15";

                startInfo.ArgumentList.Add("test");
                startInfo.ArgumentList.Add("AllVerge.MessagingModel.ChannelMessaging.Examples.Tests.dll");
                startInfo.ArgumentList.Add("--logger:console;verbosity=detailed");
                startInfo.ArgumentList.Add("--filter");
                startInfo.ArgumentList.Add("FullyQualifiedName=AllVerge.MessagingModel.ChannelMessaging.Examples.Tests.ZeroMQ_TcpDuplexListenerExampleTests.TcpDuplexPollerExampleTestAsync");

                Console.WriteLine($"Starting process: dotnet {String.Join(" ", startInfo.ArgumentList.ToArray())}");

                try
                {
                    Process process = new Process();

                    process.StartInfo = startInfo;

                    process.OutputDataReceived += (sendingProcess, dataLine) => Console.WriteLine(dataLine.Data);
                    process.ErrorDataReceived += (sendingProcess, errorLine) => Console.Error.WriteLine(errorLine.Data);

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    Console.WriteLine($"Process ended: dotnet {String.Join(" ", startInfo.ArgumentList.ToArray())}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}");
                }
            });
        }
    }
}
