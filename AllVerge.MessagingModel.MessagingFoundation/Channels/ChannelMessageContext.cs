using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Xml;

using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    using AllVerge.MessagingModel.MessagingApplication;

    using Microsoft.Extensions.Primitives;
    using System.Runtime.CompilerServices;

    public abstract class ChannelMessageContext :
        IChannelMessageMapper<ChannelMessageContext>
    {
        private class ChannelMessagingContextHeaders : InteractionContext.Headers
        {
            public ChannelMessagingContextHeaders(MessageHeaders headers) :
                base(ExtractHeaders(headers, out string host, out Uri referrer), host, referrer)
            {
            }

            private static IDictionary<string, StringValues> ExtractHeaders(MessageHeaders messageHeaders, out string host, out Uri referrer)
            {
                MessageHeaderInfo[] messageHeaderInfos = new MessageHeaderInfo[messageHeaders.Count];

                messageHeaders.CopyTo(messageHeaderInfos, 0);

                host = messageHeaders.To != null ? messageHeaders.To.Host : null;
                referrer = messageHeaders.From != null ? messageHeaders.From.Uri : null;

                IDictionary<string, StringValues> headers = messageHeaderInfos.Aggregate(new Dictionary<String, StringValues>(), (s, mhi) =>
                {
                    if (mhi is MessageHeader)
                    {
                        s.Add(mhi.Name, (mhi as MessageHeader).ToString());
                    }
                    return s;
                });

                return headers;
            }
        }

        private IDictionary<object, object> items;
        private IServiceProvider requestServices;

        protected ChannelMessageContext() { items = new Dictionary<object, object>(); }

        public ITransportProtocolContext TransportProtocolContext { get; protected set; }

        public Message Message { get; protected set; }

        public long MessageId { get; protected set; }

        public DateTime MessageDateTime { get; protected set; }

        public IDictionary<object, object> Items { get => items; set => SetItems(value); }

        public IServiceProvider RequestServices { get => requestServices; internal set { requestServices = value; OnRequestServicesAvailable(); } }

        private void OnRequestServicesAvailable()
        {
        }

        private void SetItems(IDictionary<object, object> items)
        {
            if (items == null)
                this.items = null;
            else
                items.Aggregate(this.items, (c, i) => { c.Add(i.Key, i.Value); return c; });
        }

        bool IChannelMessageMapper<ChannelMessageContext>.TryGetIncomingMessage(out Message incomingMessage, out DateTime received)
        {
            incomingMessage = Message;

            received = MessageDateTime;

            return incomingMessage != null;
        }

        ChannelMessageContext IChannelMessageMapper<ChannelMessageContext>.GetOutgoingMessagingContext(Message outgoingMessage)
        {
            return Create(this, outgoingMessage);
        }

        public abstract object Clone();

        public abstract void Dispose();

        /// <summary>
        /// Creates a null <see cref="ChannelMessageContext"/>.
        /// </summary>
        /// <returns></returns>
        public static ChannelMessageContext Create()
        {
            return new NullChannelMessagingContext();
        }

        public static ChannelMessageContext Create(ITransportProtocolContext transportProtocolContext, Message incomingMessage, long incomingMessageId, DateTime received)
        {
            return new IncomingChannelMessagingContext(transportProtocolContext, incomingMessage, incomingMessageId, received);
        }

        public static ChannelMessageContext Create(ChannelMessageContext incomingContext, Message outgoingMessage)
        {
            if (incomingContext is IncomingChannelMessagingContext)

                return new OutgoingChannelMessageContext((IncomingChannelMessagingContext)incomingContext, outgoingMessage, DateTime.Now);

            if (incomingContext == null)

                return new OutgoingChannelMessageContext(new IncomingChannelMessagingContext(null, new NullMessage(), -1, DateTime.Now), outgoingMessage, DateTime.Now);

            throw new ArgumentException($"The argument is not an {nameof(IncomingChannelMessagingContext)}", nameof(incomingContext));
        }

        public static bool MapContext(ChannelMessageContext context, BindingContext bindingContext)
        {
            if ((context as IChannelMessageMapper<ChannelMessageContext>).TryGetIncomingMessage(out Message incomingMessage, out DateTime received))
            {
                string connectionId = context.TransportProtocolContext.ConnectionId;
                ConnectionContext.Map(
                    bindingContext.ConnectionContext,
                    connectionId: connectionId,
                    localIPEndpoint: context.TransportProtocolContext.LocalEndPoint,
                    remoteIPEndpoint: context.TransportProtocolContext.RemoteEndPoint,
                    items: context.Items
                );

                string traceIdentifier;
                string inputLocation;

                if (incomingMessage.Version == MessageVersion.Soap11WSAddressing10 ||
                    incomingMessage.Version == MessageVersion.Soap12WSAddressing10)
                {
                    traceIdentifier = $"{connectionId}:{context.Message.Headers.MessageId}";
                    inputLocation = context.Message.Headers.To.AbsoluteUri;
                }
                else
                {
                    traceIdentifier = $"{connectionId}:{context.MessageId}";
                    inputLocation = context.Message.Properties.Via.AbsoluteUri;
                }

                InteractionContext.Map(
                    bindingContext.InteractionContext,
                    inputHeaders: new ChannelMessagingContextHeaders(incomingMessage.Headers),
                    traceIdentifier: traceIdentifier,
                    inputLocation: inputLocation,
                    inputTime: received);

                return true;
            }

            return false;
        }

        private class NullChannelMessagingContext : ChannelMessageContext
        {
            public NullChannelMessagingContext()
            { 
                Message = new NullMessage();
                MessageDateTime = DateTime.Now;
            }

            public override object Clone()
            {
                return this;
            }

            public override void Dispose()
            {
                (Message as IDisposable)?.Dispose();
            }
        }

        private class IncomingChannelMessagingContext : ChannelMessageContext
        {
            public IncomingChannelMessagingContext(ITransportProtocolContext transportProtocolContext, Message incomingMessage, long incomingMessageId, DateTime received)
            {
                this.TransportProtocolContext = transportProtocolContext;
                this.Message = incomingMessage;
                this.MessageId = incomingMessageId;
                this.MessageDateTime = received;
            }

            public override object Clone()
            {
                throw new NotImplementedException();
            }

            public override void Dispose()
            {
            }
        }

        private class OutgoingChannelMessageContext : ChannelMessageContext
        {
            public OutgoingChannelMessageContext(Message outgoingMessage, DateTime outgoingMessageDateTime)
            {
                this.Message = outgoingMessage;
                this.MessageDateTime = outgoingMessageDateTime;
            }

            public OutgoingChannelMessageContext(IncomingChannelMessagingContext incomingContext, Message outgoingMessage, DateTime outgoingMessageDateTime)
            {
                this.TransportProtocolContext = incomingContext.TransportProtocolContext;
                this.IncomingContext = incomingContext;
                this.Message = outgoingMessage;
                this.MessageDateTime = outgoingMessageDateTime;
            }

            public IncomingChannelMessagingContext IncomingContext;

            public override object Clone()
            {
                throw new NotImplementedException();
            }

            public override void Dispose()
            {
            }
        }
    }
}