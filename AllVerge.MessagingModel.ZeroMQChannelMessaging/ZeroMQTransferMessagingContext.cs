using AllVerge.Core.ServiceModel.Channels;
using AllVerge.Core.ServiceModel.Transfer;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;
using AllVerge.Core.Threading;
using Microsoft.AspNetCore.Http;
using NetMQ;
using ServiceModel.ChannelMessaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public class ZeroMQTransferMessagingContext : IZeroMQMessageContext<ZeroMQTransferMessagingContext>
    {
        private TaskCompletionSource<Message> handledMessagingContextResponseMessageTcs;
        private bool disposedValue;

        internal ZeroMQTransferMessagingContext(ZeroMQRequestContext requestContext)
        {
            this.handledMessagingContextResponseMessageTcs = new TaskCompletionSource<Message>();

            this.Request = requestContext;

            this.Items = new Dictionary<Object, Object>();
        }

        public IDictionary<object, object> Items { get; set; }
        internal ZeroMQRequestContext Request { get; }
        public IServiceProvider RequestServices { get; internal set; }
        public IPrincipal User { get; internal set; }
        public bool IsFault { get; internal set; }
        public bool HasResponseStarted { get; private set; }
        public MediaContentType ResponseContentType { get; internal set; }
        public long ResponseContentLength { get; internal set; }
        public UniqueId ResponseRelatesTo { get; internal set; }
        public Uri ResponseTo { get; internal set; }
        public string ResponseAction { get; internal set; }
        public Stream ResponseBody { get; internal set; }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Request.Close();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ZeroMQMessageContext()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        bool IChannelMessageMapper<ZeroMQTransferMessagingContext>.TryGetIncomingMessage(out Message incomingMessage)
        {
            throw new NotImplementedException();
        }

        ZeroMQTransferMessagingContext IChannelMessageMapper<ZeroMQTransferMessagingContext>.GetOutgoingMessagingContext(Message outgoingMessage)
        {
            throw new NotImplementedException();
        }

        internal ZeroMQTransferMessagingContext SetHandledMessagingContextResponse(Message response)
        {
            this.handledMessagingContextResponseMessageTcs.SetResult(response);

            return this;
        }

        internal Task<Message> GetHandledMessagingContextResponseMessageAsync()
        {
            return handledMessagingContextResponseMessageTcs.Task;
        }
   }
}