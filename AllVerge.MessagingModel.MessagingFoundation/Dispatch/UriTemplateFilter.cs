using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public class UriTemplateFilter : MessageFilter
    {
        private UriTemplate uriTemplate;
        private Uri baseAddress;
        private string method;
        private string name;

        public UriTemplateFilter(UriTemplate uriTemplate, Uri baseAddress, String method, string name) 
        {
            this.uriTemplate = uriTemplate;
            this.baseAddress = baseAddress;
            this.method = method;
            this.name = name;
        }

        public String Action => $"{this.method}/{this.name}";

        public UriTemplate Template { get => uriTemplate; }

        public override bool Match(MessageBuffer buffer)
        {
            using  (Message message = buffer.CreateMessage())
            {
                return this.Match(message);
            }
        }

        public override bool Match(Message message)
        {
            if (message.Properties.TryGetProperty<HttpRequestMessageProperty>(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))
                return this.Match(httpRequestMessageProperty);
            return false;
        }

        public bool Match(HttpRequestMessageProperty httpRequestMessageProperty)
        {
            if (httpRequestMessageProperty != null)
            {
                return this.uriTemplate.Match(baseAddress, httpRequestMessageProperty.HttpRequestMessage.RequestUri) != null;
            }

            return false;
        }

        protected override IMessageFilterTable<FilterData> CreateFilterTable<FilterData>()
        {
            return new UriTemplateFilterTable<FilterData>();
        }
    }
}
