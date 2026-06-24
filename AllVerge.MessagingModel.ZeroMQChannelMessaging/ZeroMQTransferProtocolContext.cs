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
using System.Net;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging
{
    public class ZeroMQTransferProtocolContext : IProtocolContext
    {
        internal ZeroMQTransferProtocolContext(ZeroMQRequestContext requestContext)
        {
            this.Request = requestContext;

            this.Items = new Dictionary<Object, Object>();
        }

        public IDictionary<object, object> Items { get; set; }

        public string ConnectionId => throw new NotImplementedException();

        public string TraceIdentifier => this.Request.TraceIdentifier;

        internal ZeroMQRequestContext Request { get; }

        public IPrincipal User { get; internal set; }
    }
}