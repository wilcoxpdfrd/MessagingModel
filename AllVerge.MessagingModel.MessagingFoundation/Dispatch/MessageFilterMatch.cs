using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public class MessageFilterMatch
    {
        HttpRequestMessageProperty requestProperty;
        MessageFilter messageFilter;
        Object data;

        public MessageFilterMatch(MessageFilter messageFilter, Object data)
        {
            this.requestProperty = null;
            this.messageFilter = messageFilter;
            this.data = data;
        }

        public MessageFilter MessageFilter { get => messageFilter; }

        public object Data { get => data; }

        internal void SetRequestProperty(HttpRequestMessageProperty requestProperty)
        {
            this.requestProperty = requestProperty;
        }
    }
}
