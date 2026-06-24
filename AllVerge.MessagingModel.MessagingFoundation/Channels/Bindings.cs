using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    public class Bindings : KeyedCollection<String, TransportBindingElement>
    {
        static Bindings()
        {
            TryAddWellKnownTransportBindingElement(new HttpTransportBindingElement());
            TryAddWellKnownTransportBindingElement(new HttpsTransportBindingElement());
            TryAddWellKnownTransportBindingElement(new TcpTransportBindingElement());
        }

        protected override string GetKeyForItem(TransportBindingElement item)
        {
            return item.Scheme;
        }

        static Bindings wellKnownBindings = new Bindings();

        public static bool TryAddWellKnownTransportBindingElement(TransportBindingElement transportBindingElement)
        {
            bool contains = wellKnownBindings.Contains(transportBindingElement);

            if (!contains)

                wellKnownBindings.Add(transportBindingElement);

            return !contains;
        }

        public static Binding CreateBinding(MessageVersion messageVersion, string scheme)
        {
            if (wellKnownBindings.Contains(scheme))
            {
                return CreateBinding(messageVersion, wellKnownBindings[scheme]);
            }

            return null;
        }

        public static Binding CreateBinding(MessageVersion messageVersion, TransportBindingElement transportBindingElement)
        {
            TextMessageEncodingBindingElement textMessageEncodingBindingElement = new TextMessageEncodingBindingElement();

            textMessageEncodingBindingElement.MessageVersion = messageVersion;
            textMessageEncodingBindingElement.WriteEncoding = Encoding.UTF8;

            return new CustomBinding(textMessageEncodingBindingElement, transportBindingElement);
        }

        public static Binding CreateBinding(MessageVersion messageVersion, NetHttpMessageEncoding messageEncoding, HttpTransportBindingElement transportBindingElement)
        {
            MessageEncodingBindingElement messageEncodingBindingElement;

            switch (messageEncoding)
            {
                case NetHttpMessageEncoding.Text:
                    messageEncodingBindingElement = new TextMessageEncodingBindingElement() { WriteEncoding = Encoding.UTF8 };
                    break;
                case NetHttpMessageEncoding.Binary:
                    messageEncodingBindingElement = new BinaryMessageEncodingBindingElement();
                    break;
                case NetHttpMessageEncoding.Mtom:
                    messageEncodingBindingElement = new MtomMessageEncodingBindingElement();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageEncoding), PublicSR.Arg_ArgumentOutOfRangeException);
            }

            messageEncodingBindingElement.MessageVersion = messageVersion;

            return new CustomBinding(messageEncodingBindingElement, transportBindingElement);
        }

        public static Binding CreateBinding(MessageVersion messageVersion, WSMessageEncoding messageEncoding, HttpTransportBindingElement transportBindingElement)
        {
            MessageEncodingBindingElement messageEncodingBindingElement;

            switch (messageEncoding)
            {
                case WSMessageEncoding.Text:
                    messageEncodingBindingElement = new TextMessageEncodingBindingElement() { WriteEncoding = Encoding.UTF8 };
                    break;
                case WSMessageEncoding.Mtom:
                    messageEncodingBindingElement = new MtomMessageEncodingBindingElement();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageEncoding), PublicSR.Arg_ArgumentOutOfRangeException);
            }

            messageEncodingBindingElement.MessageVersion = messageVersion;

            return new CustomBinding(messageEncodingBindingElement, transportBindingElement);
        }
    }
}
