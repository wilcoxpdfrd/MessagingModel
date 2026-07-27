using System;
using System.Collections.ObjectModel;

using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    public class ZeroMQBindings : KeyedCollection<String, TransportBindingElement>
    {
        static ZeroMQBindings()
        {
            // TryAddWellKnownTransportBindingElement(new ZeroMQIpcConnectionOrientedTransportBindingElement());
            TryAddWellKnownTransportBindingElement(new ZeroMQTcpConnectionOrientedTransportBindingElement());
        }

        protected override string GetKeyForItem(TransportBindingElement item)
        {
            return item.Scheme;
        }

        static ZeroMQBindings wellKnownBindings = new ZeroMQBindings();

        public static bool TryAddWellKnownTransportBindingElement(ZeroMQConnectionOrientedTransportBindingElementBase transportBindingElement)
        {
            bool contains = wellKnownBindings.Contains(transportBindingElement);

            if (!contains)

                wellKnownBindings.Add(transportBindingElement);

            return !contains;
        }

        public static Binding CreateBinding(MessageVersion messageVersion, ZeroMQMessageEncoding messageEncoding, string scheme)
        {
            if (wellKnownBindings.Contains(scheme))
            {
                return CreateBinding(messageVersion, messageEncoding, wellKnownBindings[scheme]);
            }

            return null;
        }

        public static Binding CreateBinding(MessageVersion messageVersion, ZeroMQMessageEncoding messageEncoding, TransportBindingElement transportBindingElement)
        {
            MessageEncodingBindingElement messageEncodingBindingElement;

            switch (messageEncoding)
            {
                //case ZeroMQMessageEncoding.Text:
                //    messageEncodingBindingElement = new TextMessageEncodingBindingElement() { WriteEncoding = Encoding.UTF8 };
                //    break;
                case ZeroMQMessageEncoding.Binary:
                    messageEncodingBindingElement = new BinaryMessageEncodingBindingElement();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(messageEncoding), PublicSR.Arg_ArgumentOutOfRangeException);
            }

            messageEncodingBindingElement.MessageVersion = messageVersion;

            return new CustomBinding(messageEncodingBindingElement, transportBindingElement);
        }
    }
}
