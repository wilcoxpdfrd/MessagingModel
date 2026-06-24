using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.MessagingFoundation.Channels
{
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;

    public class InteractionChannel : IInteractionContextChannel
    {
        private IChannel channel;
        private Binding binding;

        public InteractionChannel(IChannel channel)
        {
            this.channel = channel;

            if (this.channel is IContextChannel)
            {
                IContextChannel contextChanel = channel as IContextChannel;

                this.LocalAddress = contextChanel.LocalAddress;
                this.RemoteAddress = contextChanel.RemoteAddress;
                this.SessionId = contextChanel.SessionId;
            }
            else
            {
                if (channel is IInputChannel)
                {
                    IInputChannel inputChanel = channel as IInputChannel;

                    this.LocalAddress = inputChanel.LocalAddress;
                }

                if (channel is IOutputChannel)
                {
                    IOutputChannel outputChanel = channel as IOutputChannel;

                    this.RemoteAddress = outputChanel.RemoteAddress;
                }

                if (channel is ISession)
                {
                    ISession session = channel as ISession;

                    this.SessionId = session.Id;
                }
            }
        }

        private InteractionChannel(MessageVersion version, EndpointAddress address, Uri via, string sessionId)
        {
            this.binding = Bindings.CreateBinding(version, address.Uri.Scheme);
            this.RemoteAddress = address;
            if (via != null)
                this.LocalAddress = new EndpointAddress(via);
            this.SessionId = sessionId;
        }

        public EndpointAddress LocalAddress { get; }

        public EndpointAddress RemoteAddress { get; }

        public string SessionId { get; }

        public TimeSpan SendTimeout => this.channel == null ? this.binding.SendTimeout : (this.channel as IContextChannel).OperationTimeout;

        public static InteractionChannel CreateChannel(IChannel channel)
        {
            return new InteractionChannel(channel);
        }

        public static InteractionChannel CreateChannel(MessageVersion version, EndpointAddress address, Uri via, String sessionId = null)
        {
            return new InteractionChannel(version, address, via, sessionId);
        }

        public T GetCallbackChannel<T>(IMessagingDispatcher dispatcher) where T : class
        {
            if (dispatcher.HasDuplexCallback<T>())
            {
                return GetCallBackChannel<T>();
            }

            return null;
        }

        private T GetCallBackChannel<T>() where T : class
        {
            if (this.channel != null)
            {
                if (this.channel is ServiceChannel)
                
                    return (T)(this.channel as ServiceChannel).Proxy;

                return null;
            }
            else

                return new ResourceClientChannel<T>(this.binding, this.RemoteAddress).Channel;
        }
    }
}
