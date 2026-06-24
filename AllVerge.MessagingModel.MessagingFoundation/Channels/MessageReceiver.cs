using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using AllVerge.SystemPrimitives.Net.Mime;

using AllVerge.MessagingModel.Markup.Json;
using AllVerge.MessagingModel.MessagingFoundation.Http;
using AllVerge.MessagingModel.MessagingFoundation.Interactions.Routing;

using static AllVerge.MessagingModel.MessagingFoundation.Http.HttpStatusExtensions;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    public class MessageReceiver
    {
        private DateTime startTimestamp;
        private ReceivedMessage receivedMessage;

        public MessageReceiver() : base()
        {
            this.startTimestamp = DateTime.UtcNow;
            this.receivedMessage = new ReceivedMessage();
        }

        public ReceivedMessage Message
        {
            set { if (value.IsReceived) { this.receivedMessage = value; this.receivedMessage.ElapsedTime = GetElapsedTime(); } }
            get { return this.receivedMessage; }
        }

        protected TimeSpan GetElapsedTime()
        {
            return DateTime.UtcNow.Subtract(this.startTimestamp);
        }
    }

    public class AsyncMessageReceiver : MessageReceiver, ISimplexSessionRouter
    {
        private TimeoutHelper timeout;
        private ManualResetEvent waitHandle;
        private Exception exception;

        public AsyncMessageReceiver(TimeoutHelper timeout) : base()
        {
            this.timeout = timeout;
            this.waitHandle = new ManualResetEvent(false);
            this.exception = null;
        }

        public Exception Exception
        {
            get { return exception; }
        }

        public TimeSpan RemainingTime()
        {
            return this.timeout.RemainingTime();
        }

        public bool Wait()
        {
            if (!this.Message.IsReceived)
            {
                if (!this.waitHandle.WaitOne(this.timeout.RemainingTime()))

                    this.Message.ElapsedTime = base.GetElapsedTime();
            }

            return this.Message.IsReceived;
        }

        public void RouteMessage(Message message)
        {
            try
            {
                this.Message = message;
            }
            catch (Exception e)
            {
                this.exception = e;
            }

            this.waitHandle.Set();
        }
    }

    public interface IReceivedMessage
    {
        bool IsReceived { get; }
        TimeSpan ElapsedTime { get; }
        bool IsEmpty { get; }
        bool IsFault { get; }
        bool IsSoapFault { get; }
        String Action { get; }
        MediaContentType ContentType { get; }
        MessageVersion Version { get; }
        MessageHeaders Headers { get; }
        MessageProperties Properties { get; }
        MessageFault GetSoapFault(out String faultAction);
        void WriteStartBody(XmlDictionaryWriter writer);
        void WriteBodyContents(XmlDictionaryWriter writer);
    }

    public class ReceivedMessage : IReceivedMessage, IDisposable
    {
        private bool received;
        private TimeSpan elapsedTime;
        private bool isEmpty;
        private bool isFault;
        private bool isSoapFault;
        private string action;
        private MediaContentType contentType;
        private MessageVersion version;
        private MessageHeaders headers;
        private MessageProperties properties;
        private MessageFault soapFault;
        private Action<XmlWriter> writeStartBody;
        private Action<XmlDictionaryWriter> writeBodyContents;

        internal ReceivedMessage()
        {
            this.received = false;
            this.elapsedTime = TimeSpan.Zero;
        }

        protected void SetMessage(Message receivedMessage)
        {
            if (receivedMessage == null)

                throw new ArgumentNullException("receivedMessage");

            this.isEmpty = receivedMessage.IsEmpty;
            this.action = receivedMessage.GetAction(false);
            this.version = receivedMessage.Version;

            if (receivedMessage.Properties.TryGetProperty(HttpResponseMessageProperty.Name, out HttpResponseMessageProperty httpResponseMessageProperty))
            {
                if (httpResponseMessageProperty.TryGetMediaContentType(out MediaContentType responseMediaContentType))
                    this.contentType = responseMediaContentType;
                this.isFault = httpResponseMessageProperty.StatusCode >= System.Net.HttpStatusCode.Ambiguous;
                this.isSoapFault = false;
            }
            else if (receivedMessage.Properties.TryGetProperty(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))
            {
                if (httpRequestMessageProperty.TryGetMediaContentType(out MediaContentType requestMediaContentType))
                    this.contentType = requestMediaContentType;
                this.isFault = false;
                this.isSoapFault = false;
            }
            else if (receivedMessage.Version.Envelope == EnvelopeVersion.Soap11)
            {
                this.contentType = MessageVersion.Soap11.CreateMessageContentType();
                this.isSoapFault = receivedMessage.IsFault;
                this.isFault = receivedMessage.IsFault;
            }
            else if (receivedMessage.Version.Envelope == EnvelopeVersion.Soap12)
            {
                this.contentType = MessageVersion.Soap12.CreateMessageContentType();
                this.isSoapFault = receivedMessage.IsFault;
                this.isFault = receivedMessage.IsFault;
            }
            else
            {
                this.contentType = MessageVersion.Default.CreateMessageContentType();
                this.isSoapFault = receivedMessage.IsFault;
                this.isFault = receivedMessage.IsFault;
            }
            this.properties = new MessageProperties(receivedMessage.Properties);
            this.headers = new MessageHeaders(receivedMessage.Headers);
            if (this.isSoapFault)
                this.soapFault = MessageFault.CreateFault(receivedMessage, Int32.MaxValue);
            else
            {
                this.writeStartBody = receivedMessage.WriteStartBody; 
                this.writeBodyContents = receivedMessage.WriteBodyContents;
            }
            this.received = true;
        }

        public bool IsReceived
        {
            get { return this.received; }
        }

        public TimeSpan ElapsedTime
        {
            get { return this.elapsedTime; }

            internal set { this.elapsedTime = value; }
        }

        public bool IsEmpty
        {
            get { ValidateReceived(); return this.isEmpty; }
        }

        public bool IsFault
        {
            get { ValidateReceived(); return this.isFault; }
        }

        public bool IsSoapFault
        {
            get { ValidateReceived(); return this.isSoapFault; }
        }

        public string Action
        {
            get { ValidateReceived(); return this.action; }
        }

        public MediaContentType ContentType
        {
            get { ValidateReceived(); return this.contentType; }
        }

        public MessageVersion Version
        {
            get { ValidateReceived(); return this.version; }
        }

        public MessageHeaders Headers
        {
            get { ValidateReceived(); return this.headers; }
        }

        public MessageProperties Properties
        {
            get { ValidateReceived(); return this.properties; }
        }

        public MessageFault GetSoapFault(out String faultAction)
        {
            ValidateReceived();

            if (!this.IsSoapFault)

                throw new InvalidOperationException("Message is not faulted.");

            if (this.soapFault == null)

                faultAction = this.version.Addressing.FaultAction;

            else

                faultAction = null;

            return this.soapFault;
        }

        public void WriteStartBody(XmlDictionaryWriter writer)
        {
            ValidateReceived();

            if (this.IsSoapFault)

                throw new InvalidOperationException("Message is soap fault.");

            this.writeStartBody(writer);
        }

        public void WriteBodyContents(XmlDictionaryWriter writer)
        {
            ValidateReceived();

            if (this.IsSoapFault)

                throw new InvalidOperationException("Message is soap fault.");

            this.writeBodyContents(writer);
        }

        private void ValidateReceived()
        {
            if (!this.IsReceived)

                throw new InvalidOperationException("Message has not been received.");
        }

        public void Dispose()
        {
        }

        public static implicit operator ReceivedMessage(Message message)
        {
            ReceivedMessage receivedMessage = new ReceivedMessage();

            receivedMessage.SetMessage(message);

            return receivedMessage;
        }
    }

    public static class ReceivedMessageExtensions
    {
        private class BufferedStreamBodyWriter : BodyWriter, IDisposable
        {
            private const string InvalidReaderPositionOnCreateMessage = "The XmlReader used for the body of the message must be positioned on an element.";

            private MemoryStream stream;
            private Encoding encoding;
            private bool isJson;
            private bool ownsStream;

            public BufferedStreamBodyWriter(MemoryStream stream) : this(stream, Encoding.UTF8, false, false) { }

            public BufferedStreamBodyWriter(MemoryStream stream, Encoding encoding, bool ownsStream, bool isJson) : base(true)
            {
                this.stream = stream;
                this.encoding = encoding;
                this.ownsStream = ownsStream;
                this.isJson = isJson;
            }

            protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
            {
                using (XmlDictionaryReader reader = GetStreamDictionaryReader())
                {
                    XmlNodeType xmlNodeType = reader.MoveToContent();

                    while (!reader.EOF && xmlNodeType != XmlNodeType.EndElement)
                    {
                        if (xmlNodeType != XmlNodeType.Element)
                        {
                            throw new ArgumentException(InvalidReaderPositionOnCreateMessage, "reader");
                        }
                        writer.WriteNode(reader, false);

                        xmlNodeType = reader.MoveToContent();
                    }
                }
            }

            private XmlDictionaryReader GetStreamDictionaryReader()
            {
                return this.stream.CreateXmlDictionaryReader(this.encoding, this.isJson);
            }

            #region IDisposable Support
            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        if (this.ownsStream)

                            this.stream.Close();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~BufferedStreamBodyWriter() {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            //   Dispose(false);
            // }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: uncomment the following line if the finalizer is overridden above.
                // GC.SuppressFinalize(this);
            }
            #endregion
        }

        public static XmlDictionaryReader CreateXmlDictionaryReader(this MemoryStream memoryStream, Encoding encoding, bool isJson)
        {
            Stream stream = new MemoryStream();

            memoryStream.Seek(0, SeekOrigin.Begin);

            memoryStream.CopyTo(stream);

            memoryStream.Seek(0, SeekOrigin.Begin);

            if (isJson)
                return JsonReaderWriterFactory.CreateJsonReader(stream, encoding, XmlDictionaryReaderQuotas.Max, (r) => { });
            else
                return XmlDictionaryReader.CreateDictionaryReader(XmlReader.Create(new StreamReader(stream, encoding)));
        }

        public static BodyWriter CreateBufferedStreamBodyWriter(this MemoryStream stream)
        {
            return new BufferedStreamBodyWriter(stream);
        }

        public static BodyWriter CreateBufferedStreamBodyWriter(this MemoryStream stream, Encoding encoding, bool ownsStream, bool isJson)
        {
            return new BufferedStreamBodyWriter(stream, encoding, ownsStream, isJson);
        }

        static string httpGlobalNamespace = "http:";
        static string davNamespace = "DAV:";
        static string xmlnsNamespace = "http://www.w3.org/2000/xmlns/";

        public static Message DemuxResponses(this IEnumerable<IReceivedMessage> responseMessages, Message requestMessage, out HttpStatusCode statusCode, out String[] actions)
        {
            MediaContentType acceptContentType;

            if (requestMessage.Properties.TryGetProperty(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))
            {
                String acceptType = httpRequestMessageProperty.Headers[HttpRequestHeader.Accept];

                if (string.IsNullOrWhiteSpace(acceptType))
                {
                    if (httpRequestMessageProperty.TryGetMediaContentType(out MediaContentType mediaContentType))

                        acceptContentType = mediaContentType;

                    else

                        acceptContentType = new MediaContentType(MediaTypeConstants.ANY_MEDIA_TYPE);
                }
                else

                    acceptContentType = new MediaContentType(acceptType);
            }
            else if (requestMessage.Properties.TryGetProperty(MessageEncodingFormatProperty.Name, out MessageEncodingFormatProperty resourceTransferFormatMessageProperty))

                acceptContentType = resourceTransferFormatMessageProperty.Format.CreateMessageContentType(out MessageVersion messageVersion);

            else

                acceptContentType = requestMessage.Version.CreateMessageContentType();

            bool isJsonOutput = acceptContentType.TransferFormat == MessageEncodingFormat.Json;

            Message deMuxMessage = null;

            actions = responseMessages.Aggregate(new List<String>(), (_actions, _message) =>
            {
                if (!_actions.Contains(_message.Action))
                    _actions.Add(_message.Action);
                return _actions;
            }).ToArray();

            string action = actions.Count() == 1 ? actions[0] : requestMessage.GetResponseAction();

            int results = responseMessages.Count();

            MemoryStream stream = new MemoryStream();

            if (results > 0)
            {
                deMuxMessage =
                    Message.CreateMessage(
                        requestMessage.Version,
                        action,
                        new BufferedStreamBodyWriter(stream, Encoding.UTF8, true, isJsonOutput));

                MemoryStream ms = isJsonOutput ? new MemoryStream() : stream;

                List<JToken> bodyTokens = new List<JToken>();

                using (XmlDictionaryWriter writer =
                     XmlDictionaryWriter.CreateDictionaryWriter(
                         XmlWriter.Create(ms, new XmlWriterSettings() { OmitXmlDeclaration = true, Encoding = Encoding.UTF8, CloseOutput = false })))
                {
                    writer.WriteStartDocument();

                    if (results > 1)
                    {
                        writer.WriteStartElement("D", "multistatus", davNamespace);

                        writer.WriteAttributeString("xmlns", "h", xmlnsNamespace, httpGlobalNamespace);

                        foreach (IReceivedMessage responseMessage in responseMessages)
                        {
                            HttpStatusCode httpStatusCode;
                            String httpStatusDescription;
                            WebHeaderCollection httpHeaders;

                            if (responseMessage.Properties.TryGetProperty<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name, out HttpResponseMessageProperty httpResponseMessageProperty))
                            {
                                httpStatusCode = httpResponseMessageProperty.StatusCode;
                                httpStatusDescription = httpStatusCode.SupplyStatusCodeDescription(httpResponseMessageProperty.StatusDescription);
                                httpHeaders = httpResponseMessageProperty.Headers;
                            }
                            else
                            {

                                httpStatusCode = responseMessage.IsFault ? HttpStatusCode.InternalServerError : HttpStatusCode.OK;
                                httpStatusDescription = httpStatusCode.SupplyStatusCodeDescription();
                                httpHeaders = new WebHeaderCollection();
                                httpHeaders.Add(HttpResponseHeader.ContentType, responseMessage.ContentType);
                            }

                            writer.WriteStartElement("response", davNamespace);

                            writer.WriteElementString("href", davNamespace, responseMessage.Action);

                            writer.WriteStartElement("propstat", davNamespace);

                            writer.WriteElementString("status", davNamespace, $"HTTP / 1.1 {(int)httpStatusCode} {httpStatusDescription}");

                            writer.WriteStartElement("prop", davNamespace);

                            writer.WriteStartElement("h", "headers", httpGlobalNamespace);

                            foreach (string headerName in httpHeaders.AllKeys)
                            {
                                writer.WriteElementString("h", headerName, httpGlobalNamespace, httpHeaders[headerName]);
                            }

                            writer.WriteEndElement();

                            writer.WriteStartElement("s", "Envelope", responseMessage.Version.Envelope.Namespace);

                            foreach (MessageHeader messageHeader in responseMessage.Headers)
                            {
                                messageHeader.WriteHeader(writer, requestMessage.Version);
                            }

                            responseMessage.WriteStartBody(writer);

                            if (isJsonOutput)
                            {
                                using (MemoryStream ms1 = new MemoryStream())
                                {
                                    using (XmlDictionaryWriter xmlDictionaryWriter = JsonReaderWriterFactory.CreateJsonWriter(ms1))
                                    {
                                        responseMessage.WriteBodyContents(xmlDictionaryWriter);

                                        xmlDictionaryWriter.Flush();

                                        ms1.Seek(0, SeekOrigin.Begin);

                                        bodyTokens.Add(JToken.Parse(new StreamReader(ms1).ReadToEnd()));
                                    }
                                }
                            }
                            else

                                responseMessage.WriteBodyContents(writer);

                            writer.WriteEndElement(); // body

                            writer.WriteEndElement(); // envelope

                            writer.WriteEndElement(); // prop

                            writer.WriteEndElement(); // propstat

                            writer.WriteEndElement(); // response

                        }

                        writer.WriteEndElement(); // multistatus                                                      
                    }
                    else // results.count() == 1
                    {
                        IReceivedMessage responseMessage = responseMessages.First();

                        deMuxMessage.Headers.CopyHeadersFrom(responseMessage.Headers);

                        deMuxMessage.Properties.CopyProperties(responseMessage.Properties);

                        responseMessage.WriteStartBody(writer);

                        responseMessage.WriteBodyContents(writer);

                        writer.WriteEndElement(); // body
                    }

                    writer.WriteEndDocument();

                    writer.Flush();
                }

                if (isJsonOutput)
                {
                    ms.Seek(0, SeekOrigin.Begin);

                    JToken rootToken = XElement.Load(ms).ToJToken();

                    for (int i = 0; i < results; i++)
                    {
                        JToken bodyToken = bodyTokens.ElementAt(i);

                        rootToken.SelectToken($"$.D:multistatus.D:response[{i}].D:propstat.D:prop.s:Envelope.s:Body").Parent.SetOrAddToken(bodyToken);
                    }

                    JsonWriter jsonWriter = new JsonTextWriter(new StreamWriter(stream));

                    rootToken.WriteTo(jsonWriter);

                    jsonWriter.Flush();
                }
            }

            stream.Seek(0, SeekOrigin.Begin);

            if (results > 1)

                statusCode = ExtendedHttpStatusCode.MultiStatus;

            else if (responseMessages.Any(r => r.IsFault))

                statusCode = HttpStatusCode.InternalServerError;

            else

                statusCode = HttpStatusCode.OK;

            deMuxMessage.Properties.Add(HttpResponseMessageProperty.Name, new HttpResponseMessageProperty() { StatusCode = statusCode }.SetHeader(HttpHeaderNames.ContentType, acceptContentType.MediaType));

            if (isJsonOutput)
            {
                deMuxMessage.Properties.Add(MessageEncodingFormatProperty.Name, new MessageEncodingFormatProperty(MessageEncodingFormat.Json));
            }

            return deMuxMessage;
        }
    }
}
