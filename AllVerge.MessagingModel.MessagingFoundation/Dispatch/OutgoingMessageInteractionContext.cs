using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public class OutgoingMessageInteractionContext : MessagingInteractionContext
    {
        public OutgoingMessageInteractionContext(OutgoingMessageEventArgs outgoingMessageEventArgs) : base(outgoingMessageEventArgs)
        {
        }

        public override DateTimeOffset StartTimeUTC => this.OutgoingMessageEventArgs.SentTimeUTC;

        public override IPrincipal Principal => this.OutgoingMessageEventArgs.Principal;

        public override string Action => this.OutgoingMessageEventArgs.Action;

        public override UniqueId MessageId => this.OutgoingMessageEventArgs.MessageId;

        public override UniqueId RelatedTo => this.OutgoingMessageEventArgs.RelatesTo;

        public override object Tag => this.OutgoingMessageEventArgs.Tag;

        protected override bool CanSetIncomingMessageEventArgs(IncomingMessageEventArgs incomingMessageEventArgs)
        {
            return true;
        }
    }
}
