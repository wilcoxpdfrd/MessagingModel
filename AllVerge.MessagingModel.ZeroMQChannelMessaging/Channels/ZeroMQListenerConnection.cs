using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using NetMQ;
    using NetMQ.Sockets;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.SystemPrimitives.Net;

    internal class RoutingKeyEndPoint: IPEndPoint
    {
        public RoutingKeyEndPoint(RoutingKey routingKey) : base(new IPAddress(Convert.FromBase64String(routingKey.ToString())), 0)
        {
            this.RoutingKey = routingKey;
        }

        public RoutingKey RoutingKey { get; }
    }

    internal class ZeroMQListenerConnection :
        ZeroMQConnectionBase
    {
        private bool closedForWriting;
        private RoutingKey routingKey;
        private ZeroMQConnectionListener connectionListener;

        private BlockingCollection<(int, ByteBuffer)> messages;
        private (int, ByteBuffer)? currentMessage;
        private int currentSectionIndex;
        private int currentSectionOffset;

        public ZeroMQListenerConnection(RoutingKey routingKey, ZeroMQConnectionListener connectionListener) :
            base(connectionListener.ConnectionBufferPool)
        {
            this.routingKey = routingKey;
            this.connectionListener = connectionListener;
            this.ConnectionId = new UniqueId(this.routingKey.ToString());
            this.LocalIPEndPoint = new IPEndPoint(Dns.GetHostAddresses(connectionListener.ListenUri.Host)[0], connectionListener.ListenUri.Port);
            this.LocalAddress = new EndpointAddress(connectionListener.ListenUri);
            this.RemoteIPEndPoint = new RoutingKeyEndPoint(routingKey);
            this.RemoteAddress = new EndpointAddress(new UriBuilder(this.LocalAddress.Uri.Scheme, this.RemoteIPEndPoint.ToString()).Uri.AbsoluteUri);
            this.messages = new BlockingCollection<(int, ByteBuffer)>();
        }

        public override UniqueId ConnectionId { get; }

        public override IPEndPoint LocalIPEndPoint { get; }

        public override EndpointAddress LocalAddress { get; }

        public override IPEndPoint RemoteIPEndPoint { get; }

        public override EndpointAddress RemoteAddress { get; }

        internal BlockingCollection<(int, ByteBuffer)> Messages => messages;

        protected override async Task<int> ReadCoreAsync(byte[] buffer, int offset, int size, TimeSpan timeout)
        {
            try
            {
                if (this.currentMessage == null)
                {
                    this.currentMessage =
                        await this.GetNextMessageAsync(this.routingKey, timeout);

                    this.currentSectionIndex = 0;
                    this.currentSectionOffset = 0;
                }

                int totalMessageBytes = this.currentMessage.Value.Item1;
                ByteBuffer messageBuffer = this.currentMessage.Value.Item2;
                int messageSections = messageBuffer.SectionCount;

                int bytesRead = 0;

                for (int sectionIndex = this.currentSectionIndex; sectionIndex < messageBuffer.SectionCount; sectionIndex++)
                {
                    byte[] currentSection = ((MemoryStream)messageBuffer.GetSection(sectionIndex)).ToArray();

                    if (currentSection.Length <= size)
                    {
                        currentSection.CopyTo(buffer, offset);

                        bytesRead += currentSection.Length;

                        if (this.currentSectionIndex == messageSections - 1)

                            this.currentMessage = null;
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

                return bytesRead;
            }
            catch (TaskCanceledException)
            {
            }

            return 0;
        }

        private Task<(int, ByteBuffer)> GetNextMessageAsync(RoutingKey routingKey, TimeSpan timeout)
        {
            TaskCompletionSource<(int, ByteBuffer)> source =
                new TaskCompletionSource<(int, ByteBuffer)>();

            Task.Run(() =>
            {
                if (this.messages.TryTake(out (int, ByteBuffer) bufferedMessage, timeout))

                    source.SetResult(bufferedMessage);

                else

                    source.SetCanceled();
            });

            return source.Task;
        }

        ByteBuffer _writeBuffer;
        protected override void WriteCore(byte[] writeBuffer, int size, TimeSpan timeout)
        {
            if (_writeBuffer != null)
            {
                _writeBuffer.OpenSection().Write(writeBuffer, 0, size);

                _writeBuffer.CloseSection();

                if (size == 1 && writeBuffer[0] == (int)FramingRecordType.End)
                {
                    _writeBuffer.Close();

                    ZeroMQRuntime.Run(() =>
                    {
                        this.connectionListener.RouterSocket.SendMoreFrame(this.routingKey);

                        for (int sectionIndex = 0; sectionIndex < _writeBuffer.SectionCount; sectionIndex++)
                        {
                            byte[] currentSection = ((MemoryStream)_writeBuffer.GetSection(sectionIndex)).ToArray();

                            if (sectionIndex == _writeBuffer.SectionCount - 1)

                                this.connectionListener.RouterSocket.SendFrame(currentSection, currentSection.Length);

                            else

                                this.connectionListener.RouterSocket.SendMoreFrame(currentSection, currentSection.Length);
                        }

                        _writeBuffer = null;
                    });
                }
            }
            else if (size == 1 && writeBuffer[0] == (int)FramingRecordType.UnsizedEnvelope)
            {
                _writeBuffer = new ByteBuffer(Int32.MaxValue);

                _writeBuffer.OpenSection().Write(writeBuffer, 0, size);

                _writeBuffer.CloseSection();
            }
            else
            {
                ZeroMQRuntime.Run(() =>
                {
                    this.connectionListener.RouterSocket.SendMoreFrame(this.routingKey);
                    this.connectionListener.RouterSocket.SendFrame(writeBuffer, size);
                });
            }
        }

        protected override void ThrowIfClosed(bool reading)
        {
            if (reading)
                base.ThrowIfClosed(true);
            else if (this.closedForWriting)
                base.ThrowIfClosed(false);
        }

        protected override Action Closing()
        {
            return () =>
            {
                if (this.State == ChannelState.Closed)

                    this.closedForWriting = true;
            };
        }

        protected override void AbortingCore()
        {
            StopListening();
        }

        protected override Action ClosedCore(TimeSpan timeout)
        {
            return () =>
            {
                StopListening();
            };
        }

        private void StopListening()
        {
            this.connectionListener.StopListening(this.routingKey);
        }
    }
}
