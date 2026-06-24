using System;
using System.ComponentModel.Design;
using System.Security.Principal;
using System.Xml;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public class IncomingMessageInteractionContext : MessagingInteractionContext
    {
        private MessageProperties outgoingMessageProperties;

        public IncomingMessageInteractionContext(IServiceContainer services, IncomingMessageEventArgs incomingMessageEventArgs, IChannel channel = null) : 
            base(services, incomingMessageEventArgs, channel)
        {
            this.outgoingMessageProperties = null;
        }

        public override DateTimeOffset StartTimeUTC => this.IncomingMessageEventArgs.ReceivedTimeUTC;

        public override IPrincipal Principal => this.IncomingMessageEventArgs.Principal;

        public override string Action => this.IncomingMessageEventArgs.Action;

        public override UniqueId MessageId => this.IncomingMessageEventArgs.MessageId;

        public override UniqueId RelatedTo => this.IncomingMessageEventArgs.RelatesTo;

        public override object Tag => this.IncomingMessageEventArgs.Tag;

        public override MessageProperties OutgoingMessageProperties
        {
            get
            {
                MessageProperties outgoingMessageProperties = base.OutgoingMessageProperties;

                if (outgoingMessageProperties == null)
                {
                    if (this.outgoingMessageProperties == null)

                        this.outgoingMessageProperties = new MessageProperties();

                    return this.outgoingMessageProperties;
                }

                return outgoingMessageProperties;
            }
        }

        protected override bool CanSetOutgoingMessageEventArgs(OutgoingMessageEventArgs outgoingMessageEventArgs)
        {
            if (this.outgoingMessageProperties != null)
            {
                if (this.outgoingMessageProperties.Count > 0)
                {
                    outgoingMessageEventArgs.Properties.MergeProperties(this.outgoingMessageProperties);
                }

                this.outgoingMessageProperties = null;
            }

            return true;
        }
    }
}
