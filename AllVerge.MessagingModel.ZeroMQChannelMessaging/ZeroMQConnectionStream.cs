using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using AllVerge.MessagingModel.MessagingFoundation.Channels;
using AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    /// <summary>
    /// Stream wrapper around the ZerMQConnection.
    /// </summary>
    public class ZeroMQConnectionStream : Stream
    {
        private ZeroMQConnectionBase connection;
        private TimeSpan readTimeout;
        private TimeSpan writeTimeout;
        private long length;
        private long position;

        internal ZeroMQConnectionStream(ZeroMQConnectionBase connection, TimeSpan readTimeout, TimeSpan writeTimeout)
        {
            this.connection = connection;
            this.readTimeout = readTimeout;
            this.writeTimeout = writeTimeout;
            this.length = 0;
        }

        public override bool CanRead => this.connection.State == ChannelState.Open && this.readTimeout > TimeSpan.Zero;

        public override bool CanSeek => false;

        public override bool CanWrite => this.connection.State == ChannelState.Open && this.writeTimeout > TimeSpan.Zero;

        public override long Length => this.length;

        public override long Position { get => this.position; set => throw new NotImplementedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            this.length += count;
            this.position += count;
            return this.connection.Read(buffer, offset, count, this.readTimeout);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.length += count;
            this.position += count;
            this.connection.Write(buffer, offset, count, false, this.writeTimeout);
        }
    }
}
