using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Threading.Tasks;

namespace AllVerge.MessageProcessor.ZeroMQ.TcpDuplexListenerExample
{
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Description;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

    [ServiceContract()]
    [ResourceContract()]
    public interface IDuplexExampleServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        [PostMessageAction(IsOneWay = true)]
        void Message(Message message);
    }

    [ServiceContract(Namespace = TcpDuplexListenerExample.Message.NS, Name = "IDuplexExampleService", CallbackContract = typeof(IDuplexExampleServiceCallback))]
    [ResourceContract()]
    public interface IDuplexExampleService : IMapMessageFaultResult
    {
        [PostMessageAction(
            IsOneWay = true, 
            CallbackContractType = typeof(IDuplexExampleServiceCallback),
            CallbackContractMethodName = "Message"
        )]
        void Message(Message message);
        [WaitForReplyMessageAsyncMethod(ForReceivePostMethod = "Message")]
        Task<Message> GetReplyAsync();
    }

    [CollectionDataContract]
    public class MessageLines : List<String>
    {
        public MessageLines() { }

        public MessageLines(string[] lines) :
            base(lines)
        {
        }
    }

    public class Fault : IXmlSerializable
    {
        private MessageFault messageFault;
        private MessageVersion messageVersion;

        public Fault() { }

        public Fault(MessageFault messageFault, MessageVersion messageVersion)
        {
            this.messageFault = messageFault;
            this.messageVersion = messageVersion;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.Name == "fault" && reader.NamespaceURI == Message.NS)
            
                reader.Read();

            this.messageFault = MessageFaultHelper.CreateFault(reader, Int32.MaxValue);
        }

        public void WriteXml(XmlWriter writer)
        {
            this.messageFault.WriteTo(writer, this.messageVersion.Envelope);
        }

        public FaultException Exception => new FaultException(this.messageFault);
    }

    [DataContract(Name = "message", Namespace = NS)]
    public class Message
    {
        public const string NS = "http://exampleuri.org/";
        [DataMember]
        private MessageLines lines;
        [DataMember]
        private Fault fault;

        public Message() { }

        public Message(params String[] lines)
        {
            this.lines = new MessageLines(lines);
        }

        public Message(MessageFault fault, MessageVersion messageVersion)
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
