using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    /// <summary>
    /// Convenience structure that can be used, for instance, as the "ProtocolContextHost" generic type parameter in <see cref="BaseMessagingApplicationWithProtocolContextHostStartup{TProtocolContext, TMessagingContext}"/>
    /// </summary>
    /// <typeparam name="TProtocolContext"></typeparam>
    public struct ProtocolContextHost<TProtocolContext> : IProtocolContextHost<TProtocolContext>, IDisposable
    {
        public TProtocolContext ProtocolContext { get; set; }

        public IDisposable Scope { get; set; }

        public long StartTimestamp { get; set; }

        public bool EventLogEnabled { get; set; }

        public Activity Activity { get; set; }

        private struct ProtocolHostContextDisposer : IDisposable
        {
            private ProtocolContextHost<TProtocolContext> protocolHostContext;
            private Action<TProtocolContext> disposeAction;

            public ProtocolHostContextDisposer(ProtocolContextHost<TProtocolContext> protocolHostContext, Action<TProtocolContext> disposeAction)
            {
                this.protocolHostContext = protocolHostContext;
                this.disposeAction = disposeAction;
            }
            public void Dispose()
            {
                this.disposeAction(this.protocolHostContext.ProtocolContext);
            }
        }

        public void InitScope(Action<TProtocolContext> action) { this.Scope = new ProtocolHostContextDisposer(this, action); }
        
        public void Dispose()
        {
            if (this.Scope != null)
            {
                this.Scope.Dispose();
            }
        }
    }
}
