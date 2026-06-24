using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    public class DefaultMessagingContextHostApplication<MessageContext> :
       IHttpApplication<IMessagingContext<MessageContext>>
    {
        private readonly ILogger _logger;
        private readonly Func<IFeatureCollection, IMessagingContext<MessageContext>> _createProtocolMessagingContext;
        private readonly MessagingContextMiddlewareDelegate<MessageContext> _application;

        public DefaultMessagingContextHostApplication(ILogger logger, Func<IFeatureCollection, IMessagingContext<MessageContext>> createProtocolMessagingContext, MessagingContextMiddlewareDelegate<MessageContext> application) 
        {
            this._logger = logger;
            this._createProtocolMessagingContext = createProtocolMessagingContext;
            this._application = application;
        }

        IMessagingContext<MessageContext> IHttpApplication<IMessagingContext<MessageContext>>.CreateContext(IFeatureCollection contextFeatures)
        {
            return CreateContext(contextFeatures);
        }

        protected virtual IMessagingContext<MessageContext> CreateContext(IFeatureCollection contextFeatures)
        {
            return this._createProtocolMessagingContext(contextFeatures);
        }


        Task IHttpApplication<IMessagingContext<MessageContext>>.ProcessRequestAsync(IMessagingContext<MessageContext> context)
        {
            return this._application(context);
        }

        void IHttpApplication<IMessagingContext<MessageContext>>.DisposeContext(IMessagingContext<MessageContext> context, Exception exception)
        {
            this.DisposeContext(context, exception);
        }

        protected virtual void DisposeContext(IMessagingContext<MessageContext> context, Exception exception)
        {
            if (context is IDisposable)

                (context as IDisposable).Dispose();
        }
    }

    public class DefaultMessagingContextHostApplication<ProtocolContext, MessageContext> : 
        IHttpApplication<IMessagingContext<ProtocolContext>>
    {
        private readonly ILogger _logger;
        private readonly Func<IFeatureCollection, IMessagingContext<ProtocolContext>> _createProtocolContext;
        private readonly ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>> _application;

        public DefaultMessagingContextHostApplication(ILogger logger, Func<IFeatureCollection, IMessagingContext<ProtocolContext>> createProtocolContext, ContextMiddlewareDelegate<IMessagingContext<ProtocolContext>> application)
        {
            _logger = logger;
            _createProtocolContext = createProtocolContext;
            _application = application;
        }

        public ILogger Logger => _logger;

        IMessagingContext<ProtocolContext> IHttpApplication<IMessagingContext<ProtocolContext>>.CreateContext(IFeatureCollection contextFeatures)
        {
            return CreateContext(contextFeatures);
        }

        protected virtual IMessagingContext<ProtocolContext> CreateContext(IFeatureCollection contextFeatures)
        {
            return this._createProtocolContext(contextFeatures);
        }

        Task IHttpApplication<IMessagingContext<ProtocolContext>>.ProcessRequestAsync(IMessagingContext<ProtocolContext> context)
        {
            return this._application(context);
        }

        void IHttpApplication<IMessagingContext<ProtocolContext>>.DisposeContext(IMessagingContext<ProtocolContext> context, Exception exception)
        {
            DisposeContext(context, exception);
        }

        protected virtual void DisposeContext(IMessagingContext<ProtocolContext> context, Exception exception)
        {
            if (context is IDisposable)

                (context as IDisposable).Dispose();
        }
    }

    public class DefaultMessagingContextHostApplication<ProtocolContextHost, ProtocolContext, MessageContext> :
        IHttpApplication<ProtocolContextHost>
    {
        private readonly ILogger _logger;
        private readonly Func<IFeatureCollection, ProtocolContextHost> _createProtocolContextHost;
        private readonly ContextMiddlewareDelegate<ProtocolContextHost> _application;

        public DefaultMessagingContextHostApplication(ILogger logger, Func<IFeatureCollection, ProtocolContextHost> createProtocolContextHost, ContextMiddlewareDelegate<ProtocolContextHost> application)
        {
            _logger = logger;
            _createProtocolContextHost = createProtocolContextHost;
            _application = application;
        }

        public ILogger Logger => _logger;

        ProtocolContextHost IHttpApplication<ProtocolContextHost>.CreateContext(IFeatureCollection contextFeatures)
        {
            return CreateContext(contextFeatures);
        }

        protected virtual ProtocolContextHost CreateContext(IFeatureCollection contextFeatures)
        {
            return this._createProtocolContextHost(contextFeatures);
        }

        Task IHttpApplication<ProtocolContextHost>.ProcessRequestAsync(ProtocolContextHost protocolContextHost)
        {
            return this._application(protocolContextHost);
        }

        void IHttpApplication<ProtocolContextHost>.DisposeContext(ProtocolContextHost protocolContextHost, Exception exception)
        {
            DisposeContext(protocolContextHost, exception);
        }

        protected virtual void DisposeContext(ProtocolContextHost protocolContextHost, Exception exception)
        {
            if (protocolContextHost is IDisposable)

                (protocolContextHost as IDisposable).Dispose();
        }
    }
}