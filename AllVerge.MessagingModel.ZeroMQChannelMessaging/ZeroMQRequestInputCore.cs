using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

using AllVerge.Core.Resource;

using AllVerge.Core.ServiceModel.Channels;
using static AllVerge.Core.ServiceModel.Channels.HttpExtendedRequestMessageProperty;
using AllVerge.Core.ServiceModel.Transfer;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    internal class ZeroMQRequestInputCore : ZeroMQTransferMessagingInput
    {
        private ContentType cachedMimeContentType;
        private string cachedContentType;
        private ZeroMQRequestContextProvider zeroMQRequestContextProvider;

        public ZeroMQRequestInputCore(ZeroMQRequestContextProvider zeroMQRequestContextProvider) :
            base(zeroMQRequestContextProvider.Settings, true, false)
        {
            this.zeroMQRequestContextProvider = zeroMQRequestContextProvider;
        }

        protected ContentType MimeContentType
        {
            get
            {
                if (this.cachedMimeContentType == null)
                {
                    this.cachedMimeContentType = this.zeroMQRequestContextProvider.ContentType;
                }
                return this.cachedMimeContentType;
            }
        }

        protected override string ContentTypeCore
        {
            get
            {
                if (this.cachedContentType == null)
                {
                    this.cachedContentType = this.zeroMQRequestContextProvider.ContentType?.ToMediaTypePlusParameters();
                }
                return this.cachedContentType;
            }
        }

        public override long? ContentLength
        {
            get
            {
                if (this.HasFormContent)

                    return 0L;

                return this.zeroMQRequestContextProvider.ContentLength;
            }
        }

        protected bool HasFormContent
        {
            get
            {
                switch (this.MimeContentType?.MediaType)
                {
                    case MediaTypeConstants.APPLICATION_FORM_URLENCODED:
                    case MediaTypeConstants.MULTIPART_FORMDATA:

                        return true;
                }

                return false;
            }
        }

        protected override bool HasContent
        {
            get
            {
                return this.ContentLength > 0L;
            }
        }

        protected override string Action
        {
            get
            {
                return (this.zeroMQRequestContextProvider as IRequestContextProvider).GetAction();
            }
        }

        public override void ConfigureRequestMessage(ZeroMQRequestMessage message)
        {
            throw new NotImplementedException();
        }

        protected override void AddProperties(Message message)
        {
            message.Properties.Via = this.zeroMQRequestContextProvider.GetRequestUri();
        }

        protected override Stream GetInputStream()
        {
            IRequestBodyProvider bodyProvider = this.zeroMQRequestContextProvider;

            return bodyProvider.GetBody();
        }
    }
}
