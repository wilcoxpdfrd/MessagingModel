using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public class MessagingContext<MessageContext> :
        MessagingScope, IMessagingContext<MessageContext>
    {
        private event Action<IPrincipal> OnAuthenticationChange;

        private List<Action<IPrincipal>> authenticationChangeListeners;

        private TaskCompletionSource<IMessagingContext<MessageContext>> receivedInputSource;
        private CancellationTokenSource receiveInputSourceCancellationTokenSource;
        private CancellationTokenRegistration receiveInputSourceCancellationTokenRegistration;
        private static readonly TimeSpan defaultReceiveInputWaitTimeout = TimeSpan.FromMilliseconds(1000);

        public MessagingContext(TimeSpan? receiveInputWaitTimeout = null) :
            this(new BindingContext(), receiveInputWaitTimeout)
        {
        }

        public MessagingContext(BindingContext bindingContext, TimeSpan? receiveInputWaitTimeout = null)
        {
            this.BindingContext = bindingContext;
            this.ApplicationContext = new MessagingApplicationContext();

            this.ReceiveInputWaitTimeout = receiveInputWaitTimeout ?? defaultReceiveInputWaitTimeout;

            ConfigureReceiveInputTimeout();

            this.authenticationChangeListeners = new List<Action<IPrincipal>>();

            this.AddAuthenticationChangeListener(MessagingContextHandlerContext_OnAuthenticationChange);
        }

        private void ConfigureReceiveInputTimeout()
        {
            this.receivedInputSource = new TaskCompletionSource<IMessagingContext<MessageContext>>();

            this.receiveInputSourceCancellationTokenSource = new CancellationTokenSource();
            this.receiveInputSourceCancellationTokenSource.CancelAfter((int)this.ReceiveInputWaitTimeout.TotalMilliseconds);

            this.receiveInputSourceCancellationTokenRegistration = this.receiveInputSourceCancellationTokenSource.Token.Register(() => this.receivedInputSource.SetCanceled());
        }

        public override IDictionary<object, object> Items
        {
            get
            {
                if (base.Items == null)

                    base.Items = new Dictionary<Object, Object>();

                return base.Items;
            }
            set
            {
                base.Items = value;
            }
        }

        public TimeSpan ReceiveInputWaitTimeout { get; }

        /// <summary>
        /// Gets the Messaging Application Context object.
        /// </summary>
        public IApplicationMessagingContext ApplicationContext { get; private set; }

        /// <summary>
        /// Gets an indication that the <see cref="MessagingContext{MessageContext}"/> can be bound.  
        /// Typically, this will be true when the <see cref="InputContext"/> has been mapped to the <see cref="BindingContext"/> in <see cref="OnAfterInputReceived(object, EventArgs)"/>.
        /// </summary>
        public bool CanBind { get; protected set; }
        
        /// <summary>
        /// Gets the <see cref="AllVerge.MessagingModel.MessagingApplication.BindingContext"/> object.
        /// </summary>
        public BindingContext BindingContext { get; private set; }

        /// <summary>
        /// Gets the received <see cref="MessageContext"/> object for this message handler.
        /// </summary>
        public MessageContext InputContext { get; private set; }

        /// <summary>
        /// Gets an Identifier that uniquely relates the received message with a set of interrelated messages.
        /// </summary>
        public virtual UniqueId CorrelationId { get; protected set; }
        /// <summary>
        /// Get EndpointAddress for where to send a reply to the message; 
        /// used in custom routing scenairos.
        /// </summary>

        /// <summary>
        /// Gets an Identifier that uniquely identifies the received message.
        /// </summary>
        public virtual UniqueId RequestId { get; protected set; }

        /// <summary>
        /// Gets any unique Id that relates the received message with an antecedent message.
        /// </summary>
        public virtual UniqueId RelatesTo { get; protected set; }

        public virtual ApplicationEndpointAddress OutputTo { get; protected set; }

        /// <summary>
        /// Gets or sets the object used to manage user session data for this request.
        /// </summary>
        public ISession Session { get; protected set; }

        /// <summary>
        /// Gets the <see cref="ISession.Id"/> for the request, or null, if <see cref="Session"/> is null.
        /// </summary>
        public String SessionId => this.Session?.Id;

        public Exception Fault { get; private set; }

        public Action<IMessagingContext<MessageContext>> OnInputProtocolMessagingContextAvailable { get; internal set; }

        /// <summary>
        /// Gets the response <see cref="IHeaderDictionary"/> object for this request.
        /// </summary>
        public IDictionary<MiddlewarePipelineResultHeaders, StringValues> ResultHeaders { get; protected set; }

        public virtual MiddlewarePipelineResult Result { get; protected set; }

        /// <summary>
        /// Gets the response <see cref="MessageContext"/> object for this request.
        /// </summary>
        public MessageContext OutputContext { get; private set; }

        protected virtual IMessagingChannelAccessor<MessageContext> GetMessagingChannelAcceptor()
        {
            return new MessagingChannelAccessor();
        }

        public void AddAuthenticationChangeListener(Action<IPrincipal> authenticationChangeListener)
        {
            authenticationChangeListeners.Add(authenticationChangeListener);

            OnAuthenticationChange += authenticationChangeListener;
        }

        public void RemoveAuthenticationChangeListener(Action<IPrincipal> authenticationChangeListener)
        {
            if (authenticationChangeListeners.Contains(authenticationChangeListener))
            {
                authenticationChangeListeners.Remove(authenticationChangeListener);

                OnAuthenticationChange -= authenticationChangeListener;
            }
        }

        private void MessagingContextHandlerContext_OnAuthenticationChange(IPrincipal user)
        {
            this.BindingContext.InteractionContext.User = user;
        }

        public static void AddAuthenticationChangeListener(MessagingScope messagingScope, Action<IPrincipal> authenticationChangeListener)
        {
            IMessagingContext<MessageContext> messagingContext = (messagingScope as IMessagingContext<MessageContext>);
            if (messagingContext != null)
            {
                messagingContext.AddAuthenticationChangeListener(authenticationChangeListener);
            }
        }

        public static void AuthenticationChanged(MessagingScope messagingScope, IPrincipal user)
        {
            MessagingContext<MessageContext> messagingContext = (messagingScope as MessagingContext<MessageContext>);
            if (messagingContext != null)
            {
                messagingContext.OnAuthenticationChange(user);
            }
        }

        public virtual bool Input(MessageContext inputContext)
        {
            return this.Input(inputContext, MiddlewarePipelineResult.NotHandled);
        }

        public virtual bool Input(MessageContext inputContext, MiddlewarePipelineResult result)
        {
            if (!this.receivedInputSource.Task.IsCompleted)
            {
                this.Result = result;

                this.InputContext = inputContext;

                if (OnInputProtocolMessagingContextAvailable != null)

                    OnInputProtocolMessagingContextAvailable(this);

                this.receiveInputSourceCancellationTokenRegistration.Dispose();

                this.receivedInputSource.SetResult(this);

                EventArgs eventArgs = new EventArgs();

                OnAfterInputReceived(this, eventArgs);

                return true;
            }

            return false;
        }

        public Task<IMessagingContext<MessageContext>> InputReceivedAsync()
        {
            return this.receivedInputSource.Task;
        }

        /// <summary>
        /// Override to set the value of <see cref="CanBind"/> (defaults to true), and perform any other appropriate work.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventArgs"></param>
        protected virtual void OnAfterInputReceived(Object source, EventArgs eventArgs)
        {
            this.CanBind = true;
        }

        public void EnteringMiddleware()
        {
            this.Result = MiddlewarePipelineResult.NotHandled;
        }

        public virtual void Output(MessageContext outputContext)
        {
            this.Output(outputContext, MiddlewarePipelineResult.Completed);
        }

        public virtual void Output(MessageContext outputContext, MiddlewarePipelineResult result)
        {
            this.OutputContext = outputContext;
            
            this.Result = result;

            EventArgs eventArgs = new EventArgs();

            OnAfterOutputReceived(this, eventArgs);
        }

        public virtual void Output(MessageContext outputContext, MiddlewarePipelineResult result, IDictionary<MiddlewarePipelineResultHeaders, StringValues> resultHeaders = null)
        {
            this.OutputContext = outputContext;

            this.ResultHeaders = resultHeaders;

            this.Result = result;

            EventArgs eventArgs = new EventArgs();

            OnAfterOutputReceived(this, eventArgs);
        }

        /// <summary>
        /// Override to map and validate <see cref="ResultHeaders"/> based on <see cref="Result"/>, etc.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventArgs"></param>
        protected virtual void OnAfterOutputReceived(Object source, EventArgs eventArgs)
        {
        }

        public override string ToString()
        {
            return $"{this.BindingContext.InteractionContext.TraceIdentifier}: {this.Result}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                OnDispose();

                base.Dispose(disposing);

                if (this.ApplicationContext != null)
                {
                    this.ApplicationContext = null;
                }

                if (this.BindingContext != null)
                {
                    this.BindingContext.Dispose();
                }

                foreach (var authenticationChangeListener in authenticationChangeListeners)

                    OnAuthenticationChange -= authenticationChangeListener;

                if (this.InputContext != null && this.InputContext is IDisposable)

                    ((IDisposable)this.InputContext).Dispose();

                if (this.OutputContext != null && this.OutputContext is IDisposable)

                    ((IDisposable)this.OutputContext).Dispose();

                this.InputContext = default(MessageContext);
                this.OutputContext = default(MessageContext);
                this.ResultHeaders = null;
                this.Fault = null;
                this.Session = null;
            }
        }

        /// <summary>
        /// Dispose and de-reference any IDisposable objects introduced in the sub-class.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        protected class MessagingChannelAccessor : 
            IMessagingChannelAccessor<MessageContext>
        {
            IMessagingChannel<MessageContext> messagingChannel;

            public void Set<MessagingChannel>(MessagingChannel messagingChannel)
                where MessagingChannel : class, IMessagingChannel<MessageContext>
            {
                this.messagingChannel = messagingChannel;
            }

            public MessagingChannel Get<MessagingChannel>() 
                where MessagingChannel : class, IMessagingChannel<MessageContext>
            {
                return this.messagingChannel as MessagingChannel;
            }

            public async Task DisposeMessagingChannelAsync(IMessagingChannel<MessageContext> messagingChannel)
            {
                (messagingChannel as IDisposable)?.Dispose();

                if (messagingChannel is IAsyncDisposable)
                
                    await (messagingChannel as IAsyncDisposable).DisposeAsync();
            }
        }
    }
}
