using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public class InteractionContext : IDisposable
    {
        private bool disposedValue;

        public class Headers : IDisposable
        {
            private bool disposedValue;
            private Dictionary<string, StringValues> headers;

            protected Headers(IDictionary<string, StringValues> headers, String host, Uri referer)
            {
                if (headers != null)
                    this.headers = new Dictionary<String, StringValues>(headers);
                else
                    this.headers = new Dictionary<String, StringValues>();
                this.Host = host;
                this.Referer = referer;
            }

            public IReadOnlyDictionary<string, StringValues> RawHeaders => this.headers;
            public String Host { get; }
            public Uri Referer { get; }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        this.OnDisposing();
                        this.headers.Clear();
                        this.headers = null;
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }

            protected virtual void OnDisposing() { }

            // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // ~InputHeaders()
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
        }

        /// <summary>
        /// Gets or sets a unique identifier to represent the current message in trace logs, 
        /// possibly constructed by concatenating a connection Id and a message identifier.
        /// </summary>
        public string TraceIdentifier { get; protected set; }

        public virtual Headers InputHeaders { get; protected set; }

        public string InputLocation { get; protected set; }

        public string InputVerb { get; protected set; }

        public DateTimeOffset InputTime { get; protected set; }

        /// <summary>
        /// Gets or sets the user for this request.
        /// </summary>
        public IPrincipal User { get; set; }

        public static void Map(
            InteractionContext interactionContext,
            InteractionContext.Headers inputHeaders = null,
            string inputLocation = null,
            string inputVerb = null,
            DateTimeOffset inputTime = default(DateTimeOffset),
            string traceIdentifier = null,
            IPrincipal user = null
        )
        {
            interactionContext.InputHeaders = inputHeaders;
            interactionContext.InputLocation = inputLocation;
            interactionContext.InputVerb = inputVerb;
            interactionContext.InputTime = inputTime;
            interactionContext.TraceIdentifier = traceIdentifier;
            interactionContext.User = user;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.OnDispose();
                    this.InputHeaders?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        protected virtual void OnDispose()
        {
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~InteractionContext()
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
    }

    public static class InteractionContextExtensions
    {
        public static void Map(
            this InteractionContext interactionContext,
            InteractionContext.Headers inputHeaders = null,
            string inputLocation = null,
            string inputMethod = null,
            DateTimeOffset inputTime = default(DateTimeOffset),
            string traceIdentifier = null,
            IPrincipal user = null)
        {
            InteractionContext.Map(interactionContext, inputHeaders, inputLocation, inputMethod, inputTime, traceIdentifier, user);
        }
    }
}
