using AllVerge.Core.Resource;
using AllVerge.Core.ServiceModel.Base;
using AllVerge.Core.ServiceModel.Channels;
using AllVerge.Core.ServiceModel.ZeroMQ.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using ServiceModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public class ZeroMQTransferMessagingContextHandlerContext :
        ProtocolMessagingContext<ZeroMQTransferMessagingContext>
    {
        private static readonly String ZeroMQMessagingContextHandlerContextItem = nameof(ZeroMQTransferMessagingContextHandlerContext);

        public ZeroMQTransferMessagingContextHandlerContext(IFeatureCollection features, IApplicationMessagingContext<ZeroMQTransferMessagingContext> applicationMessagingContext) : 
            base(features, applicationMessagingContext)
        {
            this.AddAuthenticationChangeListener(ZeroMQMessagingContextHandlerContext_OnAuthenticationChange);
        }

        protected override void MapProtocolProperties(IFeatureCollection features)
        {
            throw new NotImplementedException();
        }

        private void ZeroMQMessagingContextHandlerContext_OnAuthenticationChange(IPrincipal user)
        {
            this.User = user;
        }

        public override IServiceProvider Services
        {
            get => base.Services;

            set
            {
                base.Services = value;

                if (base.ReceivedContext != null)
                {
                    ZeroMQTransferMessagingContext receivedContext = base.ReceivedContext;

                    TrySetRequestServices(receivedContext);
                }
            }
        }

        public override IDictionary<object, object> Items
        {
            get
            {
                if (base.ReceivedContext != null)
                {
                    ZeroMQTransferMessagingContext receivedContext = base.ReceivedContext;

                    if (receivedContext.Items == null)

                        receivedContext.Items = new Dictionary<Object, Object>();

                    if (base.Items != receivedContext.Items)

                        base.Items = receivedContext.Items;
                }

                return base.Items;
            }
            set
            {
                if (base.ReceivedContext != null)
                {
                    ZeroMQTransferMessagingContext receivedContext = base.ReceivedContext;

                    receivedContext.Items = value;

                    var _ = Items;
                }
                else

                    base.Items = value;
            }
        }

        public override UniqueId RelatesTo
        {
            get
            {
                if (base.RelatesTo == null && this.ReceivedContext != null)
                {
                    ZeroMQTransferMessagingContext recievedContext = this.ReceivedContext;

                    if (recievedContext.Request.Headers.TryGetRequestID(out StringValues requestID))

                        base.RelatesTo = new UniqueId(requestID[0]);
                }

                return base.RelatesTo;
            }
        }

        public override EndpointAddress ReplyTo
        {
            get
            {
                if (this.ReceivedContext != null)
                {
                    ZeroMQTransferMessagingContext recievedContext = this.ReceivedContext;

                    
                    base.ReplyTo = new EndpointAddress(UriUtils.CreateUri(recievedContext.Request.RequestUri, ZeroMQProtocolSchemesHelper.ZEROMQ_ROUTING_KEY_ROOT_PATH, recievedContext.Request.RoutingKey.ToString()));
                }

                return base.ReplyTo;
            }
        }

        public override IPrincipal User
        {
            get
            {
                if (base.ReceivedContext != null)
                {
                    ZeroMQTransferMessagingContext receivedContext = base.ReceivedContext;

                    if (base.User != receivedContext.User)

                        base.User = receivedContext.User;
                }

                return base.User;
            }

            protected set
            {
                if (base.ReceivedContext != null)
                {
                    if (value == null || value is ClaimsPrincipal)
                    {
                        ZeroMQTransferMessagingContext receivedContext = base.ReceivedContext;

                        receivedContext.User = (ClaimsPrincipal)value;

                        var _ = User;
                    }
                    else

                        throw new InvalidOperationException($"value must be a {nameof(ClaimsPrincipal)}.");
                }
                else

                    base.User = value;
            }
        }

        public override string TraceIdentifier
        {
            get
            {
                if (base.ReceivedContext != null)
                {
                    ZeroMQTransferMessagingContext receivedContext = base.ReceivedContext;

                    if (base.TraceIdentifier != receivedContext.Request.TraceIdentifier)

                        base.TraceIdentifier = receivedContext.Request.TraceIdentifier;
                }

                return base.TraceIdentifier;
            }

            protected set
            {
                if (base.ReceivedContext != null)
                {
                    ZeroMQTransferMessagingContext receivedContext = base.ReceivedContext;

                    receivedContext.Request.TraceIdentifier = value;

                    var _ = TraceIdentifier;
                }
                else

                    base.TraceIdentifier = value;
            }
        }

        protected override ZeroMQTransferMessagingContext CreateNullMessagingContext()
        {
            throw new NotImplementedException();
        }

        protected override void OnAfterReceived(Object source, EventArgs eventArgs)
        {
            if (this.ReceivedContext != null)
            {
                ZeroMQTransferMessagingContext receivedContext = this.ReceivedContext;

                var (_, _, _) = (this.User, this.Items, this.TraceIdentifier);

                this.Items.Add(ZeroMQMessagingContextHandlerContextItem, this);

                TrySetRequestServices(receivedContext);
            }
        }


        protected override void OnMessagingComplete(TaskStatus status)
        {
            throw new NotImplementedException();
        }

        private void TrySetRequestServices(ZeroMQTransferMessagingContext receivedContext)
        {
            if (receivedContext.RequestServices == null)

                receivedContext.RequestServices = this.Services;
        }

        protected override void OnDispose()
        {
            this.Items.Remove(ZeroMQMessagingContextHandlerContextItem);
        }
    }
}