using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    using AllVerge.SystemPrimitives.Collections;
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels;

    public class ZeroMQProtocolContext : IChannelMessageMapper<ChannelMessageContext>, ITransportProtocolContext
    {
        private TaskCompletionSource<Message> handledMessagingContextResponseMessageTcs;
        private ZeroMQConnectionBase connection;
        private string sequenceId;
        private bool disposedValue;
        private bool disposedValue1;

        internal ZeroMQProtocolContext(ZeroMQConnectionBase zeroMQConnection, TimeSpan defaultReceiveTimeout, TimeSpan defaultSendTimeout, string sequenceId)
        {
            this.handledMessagingContextResponseMessageTcs = new TaskCompletionSource<Message>();
            this.connection = zeroMQConnection;
            this.InputBody = new ZeroMQConnectionStream(this.connection, defaultReceiveTimeout, TimeSpan.Zero);
            this.OutputBody = new ZeroMQConnectionStream(this.connection, TimeSpan.Zero, defaultSendTimeout);
            this.sequenceId = sequenceId;
            this.Items = new Dictionary<Object, Object>();
        }

        public IDictionary<object, object> Items { get; set; }

        public string ConnectionId => this.connection?.ConnectionId.ToString();

        public IPEndPoint LocalEndPoint => this.connection?.LocalIPEndPoint;

        public IPEndPoint RemoteEndPoint => this.connection?.RemoteIPEndPoint;

        public EndpointAddress LocalAddress => this.connection?.LocalAddress;

        public EndpointAddress RemoteAddress => this.connection?.RemoteAddress;

        public string TraceIdentifier => this.ConnectionId + ":" + this.sequenceId;

        public IPrincipal User { get; internal set; }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public Stream InputBody { get; }

        public Stream OutputBody { get; }

        public ChannelMessageContext GetOutgoingMessagingContext(Message outgoingMessage)
        {
            throw new NotImplementedException();
        }

        public bool TryGetIncomingMessage(out Message incomingMessage, out DateTime received)
        {
            if (this.Items.TryGetValue(out ChannelMessageContext incomingChannelMessagingContext))
            {
                incomingMessage = incomingChannelMessagingContext.Message;
                received = incomingChannelMessagingContext.MessageDateTime;
            }
            else
            {
                incomingMessage = null;
                received = default(DateTime);
            }

            return incomingMessage != null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue1)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue1 = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ZeroMQProtocolContext()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}