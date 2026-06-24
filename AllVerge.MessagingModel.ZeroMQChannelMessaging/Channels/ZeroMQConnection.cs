using AllVerge.MessagingModel.MessagingFoundation.Channels;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQConnection : ZeroMQConnectionBase
    {
        private DealerSocket socket;
        private CancellationTokenSource readCancellation;
        private BlockingCollection<Object> messages;
        private (int, ByteBuffer)? currentMessage;
        private int currentSectionIndex;
        private int currentSectionOffset;

        public ZeroMQConnection(DealerSocket socket, IConnectionBufferPool connectionBufferPool) :
            base(connectionBufferPool)
        {
            this.socket = socket;
            this.readCancellation = new CancellationTokenSource();
            this.ConnectionId = new UniqueId(this.socket.Options.Identity);
            this.messages = new BlockingCollection<Object>();
            ReadAsync();
        }

        public override UniqueId ConnectionId { get; }

        protected override void WriteCore(byte[] writeBuffer, int size, TimeSpan timeout)
        {
            this.sendFrameAction(writeBuffer, size, false, timeout);
        }

        void sendFrameAction(byte[] buffer, int size, bool more, TimeSpan timeout)
        {
            ZeroMQRuntime.Run(() =>
            {
                if (more)

                    this.socket.SendMoreFrame(buffer, size);

                else

                    this.socket.SendFrame(buffer, size);
            });
        }

        Task ReadAsync()
        {
            return ZeroMQRuntime.Run((Func<Task>)(async () =>
            {
                while (!readCancellation.Token.IsCancellationRequested)
                {
                    bool hasMore = true;

                    int bytesRead = 0;

                    ByteBuffer buffer = new ByteBuffer(base.AsyncReadBufferSize);

                    Exception readException = null;

                    while (hasMore)
                    {
                        (byte[] buffer, bool more) receiveResult = default;

                        try
                        {
                            receiveResult =
                                await socket.ReceiveFrameBytesAsync(readCancellation.Token);
                        }
                        catch (InvalidOperationException e)
                        {
                            // No ZeroMQ runtime, bail ...

                            DiagnosticUtility.ExceptionUtility.TraceHandledException(e, TraceEventType.Error);

                            this.Abort();
                        }
                        catch (AggregateException e)
                        {
                            bool operationCancelled = false;

                            foreach (Exception ex in e.InnerExceptions)
                            {
                                if (ex is OperationCanceledException)

                                    operationCancelled = true;

                                DiagnosticUtility.ExceptionUtility.TraceHandledException(ex, TraceEventType.Error);
                            }

                            if (operationCancelled)

                                this.Abort();

                            else

                                readException = e;
                        }
                        catch (TaskCanceledException)
                        {
                            this.Abort();
                        }

                        if (this.State == ChannelState.Open)
                        {
                            if (readException == null)
                            {
                                BufferedStream stream = buffer.OpenSection();

                                stream.Write(receiveResult.buffer, 0, receiveResult.buffer.Length);

                                buffer.CloseSection();

                                bytesRead += receiveResult.buffer.Length;

                                hasMore = receiveResult.more;
                            }
                            else
                            {
                                hasMore = false;

                                buffer = null;
                            }
                        }
                        else
                        {
                            hasMore = false;

                            buffer = null;
                        }
                    }

                    if (readException != null)

                        this.messages.Add(readException);

                    else if (buffer != null)
                    {
                        buffer.Close();

                        this.messages.Add(new Tuple<int, ByteBuffer>(bytesRead, buffer));
                    }
                }
            }));
        }

        protected override async Task<int> ReadCoreAsync(byte[] buffer, int offset, int size, TimeSpan timeout)
        {
            try
            {
                int bytesRead = 0;

                if (this.currentMessage == null)
                {
                    this.currentMessage =
                        await this.GetNextMessageAsync(timeout);

                    this.currentSectionIndex = 0;
                    this.currentSectionOffset = 0;
                }

                int totalMessageBytes = this.currentMessage.Value.Item1;

                if (totalMessageBytes > 0)
                {
                    ByteBuffer messageBuffer = this.currentMessage.Value.Item2;
                    int messageSections = messageBuffer.SectionCount;

                    for (int sectionIndex = this.currentSectionIndex; sectionIndex < messageBuffer.SectionCount; sectionIndex++)
                    {
                        byte[] currentSection = ((MemoryStream)messageBuffer.GetSection(sectionIndex)).ToArray();

                        if (currentSection.Length <= size)
                        {
                            currentSection.CopyTo(buffer, offset);

                            bytesRead += currentSection.Length;

                            if (this.currentSectionIndex == messageSections - 1)

                                this.currentMessage = null;

                            else

                                offset += currentSection.Length;
                        }
                        else
                        {
                            int minSize = Math.Min(size, currentSection.Length - currentSectionOffset);

                            currentSection.AsSpan(this.currentSectionOffset, minSize).ToArray().CopyTo(buffer, offset);

                            bytesRead += minSize;

                            this.currentSectionOffset += minSize;

                            if (this.currentSectionOffset == currentSection.Length)
                            {
                                if (this.currentSectionIndex == messageSections - 1)
                                {
                                    this.currentMessage = null;
                                }
                                else
                                {
                                    this.currentSectionIndex++;
                                    this.currentSectionOffset = 0;
                                }
                            }
                        }
                    }
                }
                else

                    this.currentMessage = null;

                return bytesRead;
            }
            catch (TaskCanceledException)
            {
            }

            return 0;
        }

        private Task<(int, ByteBuffer)> GetNextMessageAsync(TimeSpan timeout)
        {
            TaskCompletionSource<(int, ByteBuffer)> source =
                new TaskCompletionSource<(int, ByteBuffer)>();

            Task.Run(() =>
            {
                try
                {
                    double timeoutMilliseconds = timeout.TotalMilliseconds;

                    if (timeoutMilliseconds > Int32.MaxValue)

                        timeoutMilliseconds = Int32.MaxValue;

                    if (this.messages.TryTake(out Object message, (int)timeoutMilliseconds, readCancellation.Token))
                    {
                        if (message is Exception)

                            throw (Exception)message;

                        if (message is Tuple<int, ByteBuffer>)
                        {
                            Tuple<int, ByteBuffer> buffer = (Tuple<int, ByteBuffer>)message;

                            source.SetResult((buffer.Item1, buffer.Item2));
                        }
                    }
                    else

                        source.SetResult((0, null));
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new TaskCanceledException($"{nameof(GetNextMessageAsync)} exception occured.", e);
                }
            });

            return source.Task;
        }

        protected override void AbortingCore()
        {
            StopReceiving();
        }

        protected override Action ClosedCore(TimeSpan timeout)
        {
            return StopReceiving();
        }

        private Action StopReceiving()
        {
            return () =>
            {
                try
                {
                    this.readCancellation.Cancel();
                }
                catch (AggregateException e)
                {
                    // catch required as socket async receive methods don't unregister cancellation listeners ..

                    DiagnosticUtility.ExceptionUtility.TraceHandledException(e, TraceEventType.Error);
                }

                this.socket.Close();

                this.messages.Dispose();
            };
        }
    }
}