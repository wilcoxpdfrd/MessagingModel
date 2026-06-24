using System;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.Example.ChannelMessagingServer
{
    using AllVerge.MessagingModel.ChannelMessaging;
    using AllVerge.MessagingModel.MessagingApplication;

    using AllVerge.SystemPrimitives.Collections;

    public class ExampleMessageContext : IMessageContext
    {
        private Exception fault;

        public ExampleMessageContext() : this(DateTime.MinValue)
        {
        }

        public ExampleMessageContext(string connectionId, string inputLocation, int request) : this(DateTime.Now)
        {
            this.Items.Add("connectionId", connectionId);
            this.Items.Add("traceIdentifier", $"{this.Items["connectionId"]}:{request}");
            this.Items.Add("inputLocation", inputLocation);
            this.Items.Add("request", request);
        }

        public ExampleMessageContext(string connectionId, string inputLocation, int poll, int pollSize) : this(DateTime.Now)
        {
            this.Items.Add("connectionId", connectionId);
            this.Items.Add("traceIdentifier", $"{this.Items["connectionId"]}:{poll}");
            this.Items.Add("inputLocation", inputLocation);
            this.Items.Add("poll", poll);
            this.Items.Add("pollSize", pollSize);
        }

        public ExampleMessageContext(string connectionId, string inputLocation, int poll, int pollIndex, int pollSize) : this(DateTime.Now)
        {
            this.Items.Add("connectionId", connectionId);
            this.Items.Add("traceIdentifier", $"{this.Items["connectionId"]}:{poll}.{pollIndex}"); 
            this.Items.Add("inputLocation", inputLocation);
            this.Items.Add("poll", poll);
            this.Items.Add("pollIndex", pollIndex);
            this.Items.Add("pollSize", pollSize);
        }

        public ExampleMessageContext(Exception fault) : this(DateTime.Now) { this.fault = fault; }

        private ExampleMessageContext(DateTime receivedTime)
        {
            this.Items = new Dictionary<Object, Object>();

            this.ReceivedTime = receivedTime;
        }

        public IDictionary<object, object> Items { get; set; }

        public DateTime ReceivedTime { get; }

        public Task CompleteMessagingAsync()
        {
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            if (this.fault != null)
                return $"{this.ReceivedTime}: {this.fault.Message}";
            return $"{this.ReceivedTime}: {this.Items.Aggregate(new StringBuilder(), (sb, i) => { if (sb.Length > 0) sb.Append(", "); sb.AppendFormat("{0}: {1}", i.Key.ToString(), i.Value.ToString()); return sb; } )}";
        }

        public object Clone()
        {
            return this;
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.Items.Clear();
        }

        public class ExampleConnectionHeaders : InteractionContext.Headers
        {
            public ExampleConnectionHeaders() : base(null, null, null) { }

            public ExampleConnectionHeaders(IDictionary<string, StringValues> headers, String host, Uri referer) : 
                base(headers, host, referer) { }
        }

        internal static void MapToBindingContext(ExampleMessageContext context, BindingContext bindingContext)
        {
            ExampleMessageContext.ExampleConnectionHeaders inputHeaders;

            if (context.Items.TryGetValue("inputLocation", out String inputLocation))
                inputHeaders = new ExampleMessageContext.ExampleConnectionHeaders(null, new Uri(inputLocation).Host, null);
            else
                inputHeaders = new ExampleMessageContext.ExampleConnectionHeaders();

            bindingContext.ConnectionContext.Map(
                connectionId: context.Items.GetValueOrDefault<String>("connectionId")
            );

            bindingContext.InteractionContext.Map(
                inputHeaders: inputHeaders,
                traceIdentifier: context.Items.GetValueOrDefault<String>("traceIdentifier"),
                inputLocation: context.Items.GetValueOrDefault<String>("inputLocation"),
                inputTime: context.ReceivedTime);
        }
    }
}