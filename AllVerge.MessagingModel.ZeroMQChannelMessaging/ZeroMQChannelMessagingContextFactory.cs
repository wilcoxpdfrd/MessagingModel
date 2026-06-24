using AllVerge.Core.ServiceModel.Channels;
using ServiceModel.MessagingApplication;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.Core.ServiceModel.ZeroMQ
{
    public class ZeroMQChannelMessagingContextFactory :
        ProtocolChannelMessagingContextFactory<ZeroMQApplicationHostingContext, ZeroMQProtocolContext>
    {
        private class MessagingApplicationContextScope : IDisposable
        {
            private bool disposedValue;
            private IApplicationMessagingContext<ChannelMessageContext> applicationMessagingContext;
            private Action<IMessagingContext<ChannelMessageContext>> disposeProtocolMessagingContext;

            public MessagingApplicationContextScope(IApplicationMessagingContext<ChannelMessageContext> applicationMessagingContext, Action<IMessagingContext<ChannelMessageContext>> disposeProtocolMessagingContext)
            {
                this.applicationMessagingContext = applicationMessagingContext;
                this.disposeProtocolMessagingContext = disposeProtocolMessagingContext;
            }

            public IApplicationMessagingContext<ChannelMessageContext> MessagingApplicationContext => applicationMessagingContext;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        if (disposeProtocolMessagingContext != null)
                            disposeProtocolMessagingContext.Invoke(applicationMessagingContext.MessagingHandlerContext);
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }

            // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // ~MessagingApplicationContextScope()
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

        public ZeroMQChannelMessagingContextFactory(IProtocolContextFactory<ZeroMQProtocolContext> protocolContextFactory, IProtocolMessagingContextAccessor<ChannelMessageContext> protocolMessagingContextAccessor) :
            base(protocolContextFactory, protocolMessagingContextAccessor)
        { }

        protected override ZeroMQApplicationHostingContext Create(ZeroMQProtocolContext protocolContext, IApplicationMessagingContext<ChannelMessageContext> applicationMessagingContext)
        {
            ZeroMQApplicationHostingContext context = default(ZeroMQApplicationHostingContext);

            context.ProtocolContext = protocolContext;

            context.Scope = new MessagingApplicationContextScope(applicationMessagingContext, DisposeProtocolHandlerContext);

            return context;
        }

        protected override ZeroMQProtocolContext GetRequestContext(ZeroMQApplicationHostingContext hostingApplicationContext, out IApplicationMessagingContext<ChannelMessageContext> applicationMessagingContext)
        {
            applicationMessagingContext = (hostingApplicationContext.Scope as MessagingApplicationContextScope)?.MessagingApplicationContext;

            return hostingApplicationContext.ProtocolContext;
        }

        protected override void DisposeApplicationContext(ZeroMQApplicationHostingContext applicationContext, Exception exception)
        {
            (applicationContext.Scope as MessagingApplicationContextScope)?.Dispose();

            ProtocolContextFactory.Dispose(applicationContext.ProtocolContext);
        }

        protected override void DisposeProtocolHandlerContext(IMessagingContext<ChannelMessageContext> context)
        {
            if (context is IDisposable)

                (context as IDisposable).Dispose();
        }
    }
}
