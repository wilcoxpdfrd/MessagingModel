using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Example.HttpChannelMessagingServer
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Description;
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;

    [ServiceContract()]
    [ResourceContract()]
    public interface IDuplexExampleServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        [PostMessageAction(IsOneWay = true)]
        void Message(Message message);
    }

    [ServiceContract(Namespace = HttpChannelMessagingServer.Message.NS, Name = "IDuplexExampleService", CallbackContract = typeof(IDuplexExampleServiceCallback))]
    [ResourceContract]
    public interface IDuplexExampleService : IMapMessageFaultResult
    {
        [PostMessageAction(
            IsOneWay = true, 
            CallbackContractType = typeof(IDuplexExampleServiceCallback), 
            CallbackContractMethodName = "Message")]
        void Message(Message message);
        [WaitForReplyMessageAsyncMethod(ForReceivePostMethod = "Message")]
        Task<Message> GetMessageResponseAsync();
    }

    public class Fault : IXmlSerializable
    {
        private MessageFault messageFault;
        private MessageVersion messageVersion;

        public Fault(MessageFault messageFault, MessageVersion messageVersion)
        {
            this.messageFault = messageFault;
            this.messageVersion = messageVersion;
        }

        public XmlSchema GetSchema()
        {
#pragma warning disable CS8603 // Possible null reference return.
            return null;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public void ReadXml(XmlReader reader)
        {
            this.messageFault = MessageFaultHelper.CreateFault(reader, Int32.MaxValue);
        }

        public void WriteXml(XmlWriter writer)
        {
            this.messageFault.WriteTo(writer, this.messageVersion.Envelope);
        }

        public Exception Exception => new Exception(this.messageFault.Reason.GetMatchingTranslation(CultureInfo.CurrentCulture).Text, this.messageFault.GetDetail<FaultException>());
    }
}
