using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Xml.Linq;

namespace AllVerge.MessagingModel.Example.HttpChannelMessagingServer
{
    [CollectionDataContract]
    public class MessageLines : List<String>
    {
        public MessageLines() { }

        public MessageLines(string[] lines) :

        base(lines)
        {
        }
    }

    [DataContract(Name = "message", Namespace = NS)]
    public class Message
    {
        public const String NS = "http://tempuri.org/";

        [DataMember]
        private MessageLines lines;
        [DataMember]
        private Fault? fault;

        public Message() : this(Array.Empty<String>()) { }

        public Message(params String[] lines)
        {
            this.lines = new MessageLines(lines);
            this.fault = null;
        }

        public Message(MessageFault fault, MessageVersion messageVersion) : this(Array.Empty<String>())
        {
            this.fault = new Fault(fault, messageVersion);
        }

        public MessageLines Lines
        {
            get
            {
                if (this.fault != null)

                    throw this.fault.Exception;

                return this.lines;
            }
        }
    }
}
