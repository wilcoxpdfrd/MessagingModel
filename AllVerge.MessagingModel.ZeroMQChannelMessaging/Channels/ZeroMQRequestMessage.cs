using AllVerge.Core.ServiceModel.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public class ZeroMQRequestMessage : RequestMessageBase<ZeroMQResponseMessage>
    {
        private ZeroMQRequestContext requestContext;
        private Transfer.MessageContent content;

        internal ZeroMQRequestMessage() : base()
        {
        }

        internal ZeroMQRequestMessage(ZeroMQRequestContext requestContext) : base()
        {
            this.TrySetRequestContext(requestContext);
        }

        internal bool TrySetRequestContext(ZeroMQRequestContext requestContext)
        {
            if (requestContext != null)
            {
                this.requestContext = requestContext;

                this.content = new Transfer.StreamedMessageContent(
                    new MaxMessageSizeStream(requestContext.GetZeroMQTransferMessagingInput(true).GetInputStream(true), requestContext.Listener.MaxReceivedMessageSize),
                    requestContext.ContentType,
                    requestContext.ContentLength,
                    requestContext.ContentEncoding,
                    requestContext.Headers,
                    requestContext.RequestUri);

                return true;
            }

            return false;
        }

        internal ZeroMQRequestContext RequestContext { get => requestContext; }

        public override Transfer.MessageContent Content
        {
            set
            {
                this.TrySetRequestContext((ZeroMQRequestContext)value.GetPropertiesProvider());
            }
            get
            {
                return this.content;
            }
        }

        internal override Message PrepareRequest()
        {
            return requestContext.RequestMessage;
        }

        internal override ZeroMQResponseMessage PrepareResponse(Message response)
        {
            if (response.Properties.Encoder == null)

                response.Properties.Encoder = requestContext.RequestMessage.Properties.Encoder;

            return new ZeroMQResponseMessage(response, this);
        }
    }
}