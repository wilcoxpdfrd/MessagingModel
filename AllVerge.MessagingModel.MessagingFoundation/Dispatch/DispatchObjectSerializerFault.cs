using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    internal class DispatchObjectSerializerFault : MessageFault
    {
        private FaultCode code;

        private FaultReason reason;

        private string actor;

        private string node;

        private object detail;

        private XmlObjectSerializer serializer;

        public override string Actor
        {
            get
            {
                return this.actor;
            }
        }

        public override FaultCode Code
        {
            get
            {
                return this.code;
            }
        }

        public override bool HasDetail
        {
            get
            {
                return this.serializer != null;
            }
        }

        public override string Node
        {
            get
            {
                return this.node;
            }
        }

        public override FaultReason Reason
        {
            get
            {
                return this.reason;
            }
        }

        private object ThisLock
        {
            get
            {
                return this.code;
            }
        }

        public DispatchObjectSerializerFault(FaultCode code, FaultReason reason, object detail, XmlObjectSerializer serializer, string actor, string node)
        {
            this.code = code;
            this.reason = reason;
            this.detail = detail;
            this.serializer = serializer;
            this.actor = actor;
            this.node = node;
        }

        protected override void OnWriteDetailContents(XmlDictionaryWriter writer)
        {
            if (this.serializer != null)
            {
                object thisLock = this.ThisLock;
                lock (thisLock)
                {
                    this.serializer.WriteObject(writer, this.detail);
                }
            }
        }
    }
}
