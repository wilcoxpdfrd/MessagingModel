using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

using AllVerge.Core.Collections;
using AllVerge.Core.ServiceModel.Channels;
using AllVerge.Core.ServiceModel.Http;
using AllVerge.Core.ServiceModel.Messaging;
using AllVerge.Core.ServiceModel.Methods;
using NetMQ;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    static class ZeroMQMsgHelpers
    {
        public static Msg Translate(this Message message, MessageEncoderFactory messageEncoderFactory, BufferManager bufferManager, TimeSpan timeout, out HttpExtendedRequestMessageProperty requestMessageProperty)
        {
            ArraySegment<byte> xmlBuffer = messageEncoderFactory.Encoder.WriteMessage(message, Int16.MaxValue, bufferManager);

            if (message.Version.Envelope == EnvelopeVersion.Soap11)
            {
                if (!message.Properties.TryGetProperty(HttpExtendedRequestMessageProperty.Name, out requestMessageProperty))
                {
                    requestMessageProperty = new HttpRequestMessageProperty();

                    message.Properties.Add(HttpRequestMessageProperty.Name, requestMessageProperty);
                }

                if (message.Version.Addressing == AddressingVersion.WSAddressingAugust2004)
                
                    requestMessageProperty.RequestUri = message.Headers.To;

                else if (message.Version.Addressing == AddressingVersion.WSAddressing10)
                {
                    if (message.Headers.To != null)

                        requestMessageProperty.RequestUri = message.Headers.To;

                    else

                        requestMessageProperty.RequestUri = new Uri("http://www.w3.org/2005/08/addressing/anonymous");
                }

                Encoding contentEncoding = TextMessageEncoderFactory.GetSupportedEncodings().First();

                string contentType = $"{TextMessageEncoderFactory.Soap11MediaType}; charset={contentEncoding.WebName}";

                if (message.Headers.TryGetAction(out String action))
                {
                    requestMessageProperty.Headers[HttpHeaderNames.SOAPAction] = action;
                }

                requestMessageProperty.MessageContentType = new MediaContentType(contentType);
                requestMessageProperty.Headers[HttpRequestHeader.Accept] = contentType;
                requestMessageProperty.Method = ResourceMethods.POST;
            }
            else if (message.Version.Envelope == EnvelopeVersion.Soap12)
            {
                if (!message.Properties.TryGetProperty(HttpExtendedRequestMessageProperty.Name, out requestMessageProperty))
                {
                    requestMessageProperty = new HttpRequestMessageProperty();

                    message.Properties.Add(HttpRequestMessageProperty.Name, requestMessageProperty);
                }

                if (message.Version.Addressing == AddressingVersion.WSAddressingAugust2004)

                    requestMessageProperty.RequestUri = message.Headers.To;

                else if (message.Version.Addressing == AddressingVersion.WSAddressing10)
                {
                    if (message.Headers.To != null)

                        requestMessageProperty.RequestUri = message.Headers.To;

                    else

                        requestMessageProperty.RequestUri = new Uri("http://www.w3.org/2005/08/addressing/anonymous");
                }

                Encoding contentEncoding = TextMessageEncoderFactory.GetSupportedEncodings().First();

                string contentType = $"{TextMessageEncoderFactory.Soap12MediaType}; charset={contentEncoding.WebName}";

                if (message.Headers.TryGetAction(out String action))
                    requestMessageProperty.MessageContentType = new MediaContentType(contentType, (MediaContentType.PARAMETER_KEY_SOAP_12_ACTION, action, true));
                else
                    requestMessageProperty.MessageContentType = new MediaContentType(contentType, (MediaContentType.PARAMETER_KEY_SOAP_12_ACTION, null, false));
                requestMessageProperty.Headers[HttpRequestHeader.Accept] = contentType;
                requestMessageProperty.Method = ResourceMethods.POST;
            }
            else
            {
                if (!message.Properties.TryGetProperty(HttpExtendedRequestMessageProperty.Name, out requestMessageProperty))
                {
                    requestMessageProperty = new HttpRequestMessageProperty();

                    message.Properties.Add(HttpRequestMessageProperty.Name, requestMessageProperty);
                }

                Encoding contentEncoding = TextMessageEncoderFactory.GetSupportedEncodings().First();

                string contentType = $"{TextMessageEncoderFactory.XmlMediaType}; charset={contentEncoding.WebName}";

                requestMessageProperty.Headers[HttpRequestHeader.ContentType] = contentType;
                requestMessageProperty.Headers[HttpRequestHeader.Accept] = contentType;
                requestMessageProperty.Method = ResourceMethods.POST;
            }

            Msg msg = new Msg();

            int size = xmlBuffer.Count - xmlBuffer.Offset;

            msg.InitPool(size);

            Array.Copy(xmlBuffer.ToArray(), msg.Data, size);

            requestMessageProperty.MessageContentLength = size;

            return msg;
        }

        public static Message CreateMessage(this IEnumerable<ArraySegment<byte>> data, HttpExtendedResponseMessageProperty responseMessageProperty, MessageEncoderFactory messageEncoderFactory, BufferManager bufferManager)
        {
            int dataSegments = data.Count();

            long contentLength = responseMessageProperty.MessageContentLength64;
            MediaContentType contentType = responseMessageProperty.MessageContentType;

            Message receivedMessage;

            if (dataSegments > 0)
            {
                // tbd: when dataSegments > 1 ... 
                // consider contentLength ... 
                // maybe total bytes in all segments > Int32.Max?

                var buffer = new ArraySegment<byte>(data.First().Array, 0, (int)contentLength);

                receivedMessage = messageEncoderFactory.Encoder.ReadMessage(buffer, bufferManager, contentType.ToString());

                data.ReturnData(bufferManager);
            }
            else
            {
                receivedMessage = Message.CreateMessage(contentType.MessageVersion, responseMessageProperty.GetAction());
            }

            receivedMessage.Properties.Add(HttpExtendedResponseMessageProperty.Name, responseMessageProperty);

            return receivedMessage;
        }

        public static String ToString(this Msg msg, BufferManager bufferManager)
        {
            String message;

            int dataLength = msg.Data.Length;

            if (dataLength > 0)
            {
                byte[] bytes = bufferManager.TakeBuffer(dataLength);

                msg.CloneData().CopyTo(bytes, 0);

                message = Encoding.UTF8.GetString(bytes, 0, dataLength);

                bufferManager.ReturnBuffer(bytes);
            }
            else

                message = string.Empty;

            return message;
        }

        public static void AddData(this List<ArraySegment<byte>> data, Msg msg, BufferManager bufferManager)
        {
            int dataLength = msg.Data.Length;

            if (dataLength > 0)
            {
                byte[] bytes = bufferManager.TakeBuffer(dataLength);

                msg.CloneData().CopyTo(bytes, 0);

                data.Add(new ArraySegment<byte>(bytes));
            }
            else

                data.Add(new ArraySegment<byte>());
        }

        public static void ReturnData(this IEnumerable<ArraySegment<byte>> data, BufferManager bufferManager)
        {
            foreach (ArraySegment<byte> segment in data)
            {
                if (segment.Array.Length > 0)

                    bufferManager.ReturnBuffer(segment.Array);
            }
        }

        public static void AddHeader(this WebHeaderCollection headers, Msg msg, BufferManager bufferManager)
        {
            int dataLength = msg.Data.Length;

            if (dataLength > 0)
            {
                byte[] bytes = bufferManager.TakeBuffer(dataLength);

                msg.CloneData().CopyTo(bytes, 0);

                ArraySegment<byte> s = new ArraySegment<byte>(bytes, 0, dataLength);

                int indexOfColon = Array.IndexOf(s.Array, (byte)':');

                if (indexOfColon < 0)

                    headers.Add(Encoding.UTF8.GetString(s.ToArray()));

                else

                    headers.Add(Encoding.UTF8.GetString(s.Slice(0, indexOfColon).ToArray()), Encoding.UTF8.GetString(s.Slice(indexOfColon + 1, dataLength - indexOfColon - 1).ToArray()));

                bufferManager.ReturnBuffer(s.Array);
            }
        }
    }
}
