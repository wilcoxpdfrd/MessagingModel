using AllVerge.MessagingModel.Markup.Json;
using AllVerge.MessagingModel.MessagingFoundation.Channels;
using AllVerge.MessagingModel.MarkupPrimitives.Xml;
using AllVerge.SystemPrimitives.Net.Mime;
using FastSerialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using static AutoMapper.Internal.ExpressionFactory;

namespace AllVerge.MessagingModel.MessagingFoundation.Tests.Channels
{
    public class MessageReceiverTests
    {
        [DataContract(Name = Graph<Value>.Name, Namespace = "")]
        class Graph<Value> where Value : struct
        {
            public const String Name = "Graph";

            Dictionary<string, Value>? leafs;
            List<Graph<Value>>? nodes;

            [DataMember(Name = "Leaf")]
            public Dictionary<String, Value> Leafs { get { if (this.leafs == null) this.leafs = new Dictionary<string, Value>(); return this.leafs; } }
            [DataMember(Name = "Node")]
            public List<Graph<Value>> Nodes { get { if (this.nodes == null) this.nodes = new List<Graph<Value>>(); return this.nodes; } }
            public Graph(params Graph<Value>[] children)
            {
                this.Nodes.AddRange(children);
            }
        }

        [Fact]
        public void MessageReceiverJsonDataContractTest()
        {
            DataContractJsonSerializer dataContractJsonSerializerGraph = new DataContractJsonSerializer(typeof(Graph<int>));
            
            Graph<int> requestGraph = new Graph<int>();
            
            requestGraph.Leafs.Add("Client", 1);
            requestGraph.Leafs.Add("Request", 1);

            Message requestMessage = Message.CreateMessage(MessageVersion.None, "http://myaction", requestGraph);

            HttpRequestMessageProperty httpRequestMessageProperty = new HttpRequestMessageProperty();

            httpRequestMessageProperty.Headers.Add(HttpRequestHeader.ContentType, MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE);
            
            requestMessage.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessageProperty);

            requestMessage.Properties.Add(MessageEncodingFormatProperty.Name, new MessageEncodingFormatProperty(MessageEncodingFormat.Json));

            List<ReceivedMessage> responses = new List<ReceivedMessage>();

            MessageReceiver messageReceiver1 = new MessageReceiver();

            Graph<int> responseGraph1 = new Graph<int>();

            responseGraph1.Leafs.Add("Handler", 1);
            responseGraph1.Leafs.Add("Response", 1);

            responseGraph1.Nodes.Add(requestGraph);

            using (MemoryStream ms = new MemoryStream())
            {
                dataContractJsonSerializerGraph.WriteObject(ms, responseGraph1);

                ms.Seek(0, SeekOrigin.Begin);

                Graph<int>? graph = (Graph<int>?)dataContractJsonSerializerGraph.ReadObject(ms);

                Assert.Equivalent(responseGraph1, graph);

                ms.Seek(0, SeekOrigin.Begin);

                string x = new StreamReader(ms).ReadToEnd();
            }

            Message message1 = Message.CreateMessage(MessageVersion.None, "http://myactionResponse", responseGraph1, dataContractJsonSerializerGraph);

            HttpResponseMessageProperty httpResponseMessageProperty1 = new HttpResponseMessageProperty() { StatusCode = HttpStatusCode.OK };
            
            httpResponseMessageProperty1.Headers.Add(HttpResponseHeader.ContentType, MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE);

            message1.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty1);

            message1.Properties.Via = new Uri("http://myaction");

            messageReceiver1.Message = message1;

            responses.Add(messageReceiver1.Message);

            MessageReceiver messageReceiver2 = new MessageReceiver();

            Graph<int> responseGraph2 = new Graph<int>();

            responseGraph2.Leafs.Add("Handler", 2);
            responseGraph2.Leafs.Add("Response", 1);

            responseGraph2.Nodes.Add(requestGraph);

            Message message2 = Message.CreateMessage(MessageVersion.None, "http://myactionResponse", responseGraph2, dataContractJsonSerializerGraph);

            HttpResponseMessageProperty httpResponseMessageProperty2 = new HttpResponseMessageProperty() { StatusCode = HttpStatusCode.OK };

            httpResponseMessageProperty2.Headers.Add(HttpResponseHeader.ContentType, MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE);

            message2.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty2);

            message2.Properties.Via = new Uri("http://myaction");

            messageReceiver2.Message = message2;

            responses.Add(messageReceiver2.Message);

            Message responseMessage = responses.DemuxResponses(requestMessage, out HttpStatusCode statusCode, out string[] actions);

            Assert.NotNull(responseMessage);

            Assert.Equal("http://myactionResponse", responseMessage.Headers.Action);

            if (responseMessage.Properties.TryGetProperty<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name, out HttpResponseMessageProperty httpResponseMessageProperty))
            {
                Assert.Equal(207, (int)httpResponseMessageProperty.StatusCode);

                Assert.Equal(MediaTypeConstants.APPLICATION_JSON_MEDIA_TYPE, httpResponseMessageProperty.Headers["Content-Type"]);
            }
            else

                Assert.Fail($"{nameof(responseMessage)} missing property {nameof(HttpResponseMessageProperty)}");

            Assert.Equal("http://myactionResponse", responseMessage.Headers.Action);

            string json;

            using (MemoryStream stream = new MemoryStream())
            {
                XmlDictionaryWriter writer =
                    JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false);

                responseMessage.WriteMessage(writer);

                writer.Flush();

                stream.Seek(0, SeekOrigin.Begin);

                json = new StreamReader(stream).ReadToEnd();
            }

            JToken jToken = JToken.Parse(json);

            JToken? bodyToken1 = jToken.SelectToken("$.D:multistatus.D:response[0].D:propstat.D:prop.s:Envelope.s:Body");

            Assert.NotNull(bodyToken1);

            Graph<int>? actualGraph1 = (Graph<int>?)dataContractJsonSerializerGraph.ReadObject(bodyToken1.ToStream());

            Assert.NotNull(actualGraph1);

            Assert.Equivalent(responseGraph1, actualGraph1, true);

            JToken? bodyToken2 = jToken.SelectToken("$.D:multistatus.D:response[1].D:propstat.D:prop.s:Envelope.s:Body");

            Assert.NotNull(bodyToken2);

            Graph<int>? actualGraph2 = (Graph<int>?)dataContractJsonSerializerGraph.ReadObject(bodyToken2.ToStream());

            Assert.NotNull(actualGraph2);

            Assert.Equivalent(responseGraph2, actualGraph2, true);
        }

        [Fact]
        public void MessageReceiverDataContractTest()
        {
            DataContractSerializer dataContractSerializerGraph = new DataContractSerializer(typeof(Graph<int>));

            Graph<int> graph = new Graph<int>();

            graph.Leafs.Add("Client", 1);
            graph.Leafs.Add("Request", 1);

            Message requestMessage = Message.CreateMessage(MessageVersion.Soap12WSAddressingAugust2004, "http://myaction", graph);

            HttpRequestMessageProperty httpRequestMessageProperty = new HttpRequestMessageProperty();

            httpRequestMessageProperty.Headers.Add(HttpRequestHeader.ContentType, MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE);

            requestMessage.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessageProperty);

            requestMessage.Properties.Add(MessageEncodingFormatProperty.Name, new MessageEncodingFormatProperty(MessageEncodingFormat.Json));

            List<ReceivedMessage> responses = new List<ReceivedMessage>();

            MessageReceiver messageReceiver1 = new MessageReceiver();

            Graph<int> graph1 = new Graph<int>();

            graph1.Leafs.Add("Response", 1);

            graph1.Nodes.Add(graph);

            Message message1 = Message.CreateMessage(MessageVersion.Soap12WSAddressingAugust2004, "http://myactionResponse", graph1, dataContractSerializerGraph);

            HttpResponseMessageProperty httpResponseMessageProperty1 = new HttpResponseMessageProperty() { StatusCode = HttpStatusCode.OK };

            httpResponseMessageProperty1.Headers.Add(HttpResponseHeader.ContentType, MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE);

            message1.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty1);

            message1.Properties.Via = new Uri("http://myaction");

            messageReceiver1.Message = message1;

            responses.Add(messageReceiver1.Message);

            MessageReceiver messageReceiver2 = new MessageReceiver();

            Graph<int> graph2 = new Graph<int>();

            graph2.Leafs.Add("Response", 1);

            graph1.Nodes.Add(graph);

            Message message2 = Message.CreateMessage(MessageVersion.Soap12WSAddressingAugust2004, "http://myactionResponse", graph2, dataContractSerializerGraph);

            HttpResponseMessageProperty httpResponseMessageProperty2 = new HttpResponseMessageProperty() { StatusCode = HttpStatusCode.OK };

            httpResponseMessageProperty2.Headers.Add(HttpResponseHeader.ContentType, MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE);

            message2.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty2);

            message2.Properties.Via = new Uri("http://myaction");

            messageReceiver2.Message = message2;

            responses.Add(messageReceiver2.Message);

            Message responseMessage = responses.DemuxResponses(requestMessage, out HttpStatusCode statusCode, out string[] actions);

            Assert.NotNull(responseMessage);

            Assert.Equal("http://myactionResponse", responseMessage.Headers.Action);

            if (responseMessage.Properties.TryGetProperty<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name, out HttpResponseMessageProperty httpResponseMessageProperty))
            {
                Assert.Equal(207, (int)httpResponseMessageProperty.StatusCode);

                Assert.Equal(MediaTypeConstants.APPLICATION_SOAP_PLUS_XML_MEDIA_TYPE, httpResponseMessageProperty.Headers["Content-Type"]);
            }
            else

                Assert.Fail($"{nameof(responseMessage)} missing property {nameof(HttpResponseMessageProperty)}");

            Assert.Equal("http://myactionResponse", responseMessage.Headers.Action);

            string xml;

            using (MemoryStream stream = new MemoryStream())
            {
                XmlDictionaryWriter writer =
                    XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, false);

                responseMessage.WriteBody(writer);

                writer.Flush();

                stream.Seek(0, SeekOrigin.Begin);

                xml = new StreamReader(stream).ReadToEnd();
            }

            XElement xElement = XElement.Parse(xml);

            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xElement.CreateNavigator().NameTable);

            xmlNamespaceManager.AddNamespace("s", MessageVersion.Soap12WSAddressingAugust2004.Envelope.Namespace);
            xmlNamespaceManager.AddNamespace("D", "DAV:");

            XElement? bodyElement1 = (xElement.XPathEvaluate("/D:multistatus/D:response[1]/D:propstat/D:prop/s:Envelope/s:Body", xmlNamespaceManager) as IEnumerable<Object>)?.OfType<XElement>().First();

            Assert.NotNull(bodyElement1?.FirstNode);

            Graph<int>? actualGraph1 = (Graph<int>?)dataContractSerializerGraph.ReadObject(bodyElement1.FirstNode.CreateReader());

            Assert.NotNull(actualGraph1);

            Assert.Equivalent(graph1, actualGraph1, true);

            XElement? bodyElement2 = (xElement.XPathEvaluate("/D:multistatus/D:response[2]/D:propstat/D:prop/s:Envelope/s:Body", xmlNamespaceManager) as IEnumerable<Object>)?.OfType<XElement>().First();

            Assert.NotNull(bodyElement2?.FirstNode);

            Graph<int>? actualGraph2 = (Graph<int>?)dataContractSerializerGraph.ReadObject(bodyElement2.FirstNode.CreateReader());

            Assert.NotNull(actualGraph2);

            Assert.Equivalent(graph2, actualGraph2, true);
        }
    }
}