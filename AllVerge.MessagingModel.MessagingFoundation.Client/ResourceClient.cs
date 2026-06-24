using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace AllVerge.MessagingModel.MessagingFoundation.Client
{
    using AllVerge.MessagingModel.MessagingFoundation.Channels;
    using AllVerge.MessagingModel.MessagingFoundation.Client.Resource;

    using AllVerge.SystemPrimitives.Encodings;
    using AllVerge.SystemPrimitives.Threading.Tasks;

    public class ResourceClient<T> : ICommunicationObject, IDisposable where T : class
    {
        private ReaderWriterLockSlim sycnRoot;
        private Func<Object[], Object> clientChannelFactory;
        private Object[] clientChannelFactoryArgs;
        List<IEndpointBehavior> endPointBehaviors = null;
        ResourceClientChannel<T> clientChannel;

        //ToDo:  wireup events ...

        public event EventHandler Closed;
        public event EventHandler Closing;
        public event EventHandler Faulted;
        public event EventHandler Opened;
        public event EventHandler Opening;

        public CommunicationState State
        {
            get
            {
                if (TryEnsureClientChannel(false))

                    return this.clientChannel.State;

                return CommunicationState.Faulted;
            }
        }

        protected ResourceClient()
        {
             this.sycnRoot = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="ResourceClient{T}"/> class.
        /// </summary>
        /// <param name="binding">The binding with which to make calls to the service.</param>
        /// <param name="remoteAddress">The address of the service endpoint.</param>
        public ResourceClient(Binding binding, EndpointAddress remoteAddress, params IEndpointBehavior[] endpointBehaviors) : 
            this()
        {
            Type clientChannelType = typeof(ResourceClientChannel<T>);
            this.clientChannelFactory = (args) => Activator.CreateInstance(clientChannelType, args);
            this.clientChannelFactoryArgs = new Object[] { binding, remoteAddress };
            if (binding is ResourceTransferBinding)
                endpointBehaviors.Append(new ResourceEndpointAttributeChannelActionBehavior()).Aggregate(this, (t, b) => { t.AddEndpointBehavior(b); return t; });
        }

        /// <summary>
        /// Initialize a new instance of a <see cref="ResourceClient{T}"/> derived class, for a duplex channel.
        /// </summary>
        /// <param name="duplexClientChannelType"></param>
        /// <param name="instanceContext"></param>
        /// <param name="binding"></param>
        /// <param name="remoteAddress"></param>
        /// <param name="endpointBehaviors"></param>
        protected ResourceClient(Type duplexClientChannelType, InstanceContext instanceContext, Binding binding, EndpointAddress remoteAddress, params IEndpointBehavior[] endpointBehaviors)
            : this()
        {
            this.clientChannelFactory = (args) => Activator.CreateInstance(duplexClientChannelType, args);
            this.clientChannelFactoryArgs = new Object[] { instanceContext, binding, remoteAddress };
            if (binding is ResourceTransferBinding)
                endpointBehaviors.Append(new ResourceEndpointAttributeChannelActionBehavior()).Aggregate(this, (t, b) => { t.AddEndpointBehavior(b); return t; });
        }

        public void AddEndpointBehavior(IEndpointBehavior endpointBehavior)
        {
            if (this.clientChannel != null)

                throw new InvalidOperationException($"Cannot add {nameof(endpointBehavior)} to created client channel.");

            if (this.endPointBehaviors == null)
            {
                this.endPointBehaviors = new List<IEndpointBehavior>();
            }

            this.endPointBehaviors.Add(endpointBehavior);
        }

        protected string Base64UrlEncode(string value)
        {
            return value.Base64UrlEncode();
        }

        public void Abort()
        {
            if (this.clientChannel != null)
            {
                this.clientChannel.Abort();
            }
        }

        /// <summary>
        /// Causes the <see cref="ResourceClient{T}"/> object to transition from its current state into the closed state.
        /// </summary>
        public void Close()
        {
            if (this.clientChannel != null)
            {
                this.clientChannel.Close();
            }
        }

        void ICommunicationObject.Close(TimeSpan timeout)
        {
            if (this.clientChannel != null)

                (this.clientChannel as ICommunicationObject).Close(timeout);
        }

        IAsyncResult ICommunicationObject.BeginClose(AsyncCallback callback, object state)
        {
            if (this.clientChannel != null)

                return (this.clientChannel as ICommunicationObject).BeginClose(callback, state);

            return callback.CompleteWith(state);
        }

        IAsyncResult ICommunicationObject.BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (this.clientChannel != null)

                return (this.clientChannel as ICommunicationObject).BeginClose(timeout, callback, state);

            return callback.CompleteWith(state);
        }

        void ICommunicationObject.EndClose(IAsyncResult result)
        {
            if (this.clientChannel != null)

                (this.clientChannel as ICommunicationObject).EndClose(result);
        }

        public void Open()
        {
            if (TryEnsureClientChannel(false))

                (this.clientChannel as ICommunicationObject).Open();
        }

        void ICommunicationObject.Open(TimeSpan timeout)
        {
            if (TryEnsureClientChannel(false))

                (this.clientChannel as ICommunicationObject).Open(timeout);
        }

        IAsyncResult ICommunicationObject.BeginOpen(AsyncCallback callback, object state)
        {
            if  (TryEnsureClientChannel(false))

                return (this.clientChannel as ICommunicationObject).BeginOpen(callback, state);

            return Task.FromCanceled(CancellationToken.None);
        }

        IAsyncResult ICommunicationObject.BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (TryEnsureClientChannel(false))

                return (this.clientChannel as ICommunicationObject).BeginOpen(timeout, callback, state);

            return Task.FromCanceled(CancellationToken.None);
        }

        void ICommunicationObject.EndOpen(IAsyncResult result)
        {
            if (TryEnsureClientChannel(false))

                (this.clientChannel as ICommunicationObject).EndOpen(result);
        }

        void IDisposable.Dispose()
        {
            if (this.clientChannel != null)

                (this.clientChannel as IDisposable).Dispose();
        }

        public T Channel
        {
            get
            {
                if (TryEnsureClientChannel(true))

                    return this.clientChannel.Channel;

                return null;
            }
        }

        public IClientChannel InnerChannel
        {
            get
            {
                if (TryEnsureClientChannel(true))

                    return this.clientChannel.InnerChannel;

                return null;
            }
        }

        private bool TryEnsureClientChannel(bool ensureOpen)
        {
            if (this.clientChannel == null || this.clientChannel.State == CommunicationState.Faulted)
            {
                sycnRoot.TryEnterUpgradeableReadLock(6000);

                if (sycnRoot.IsUpgradeableReadLockHeld)
                {
                    try
                    {
                        if (this.clientChannel == null || this.clientChannel.State == CommunicationState.Faulted)
                        {
                            sycnRoot.TryEnterWriteLock(5000);

                            if (sycnRoot.IsWriteLockHeld)
                            {
                                if (this.clientChannel != null)
                                {
                                    if (this.clientChannel.State == CommunicationState.Faulted)
                                    {
                                        ICommunicationObject client = this.clientChannel;

                                        this.clientChannel = null;

                                        ThreadPool.QueueUserWorkItem(BackgroundAbort, client);
                                    }
                                }

                                if (this.clientChannel == null)
                                {
                                    this.clientChannel = (ResourceClientChannel<T>)this.clientChannelFactory(this.clientChannelFactoryArgs);

                                    if (this.endPointBehaviors != null)
                                    {
                                        foreach (IEndpointBehavior endpointBehavior in this.endPointBehaviors)

                                            this.clientChannel.Endpoint.EndpointBehaviors.Add(endpointBehavior);
                                    }

                                    SetChannelFactoryConfiguration(this.clientChannel.ChannelFactory);

                                    if (this.clientChannel.State == CommunicationState.Created && ensureOpen)

                                        (this.clientChannel as ICommunicationObject).Open();
                                }
                            }
                            else

                                return false;
                        }
                    }
                    finally
                    {
                        if (sycnRoot.IsWriteLockHeld)

                            sycnRoot.ExitWriteLock();

                        sycnRoot.ExitUpgradeableReadLock();
                    }
                }
                else

                    return false;
            }

            return true;
        }

        protected void InvokeServiceAsync(BeginParameterizedOperationDelegate beginOperation, TimeSpan timeout, object[] args, Action<AsyncOperationCompletedEventArgs> completedCallBack, object userState, EndOperationWithResultsDelegate endOperation)
        {
            if (TryEnsureClientChannel(true))

                this.clientChannel.InvokeServiceAsync(beginOperation, args, timeout, completedCallBack, userState, endOperation);
        }

        protected virtual void SetChannelFactoryConfiguration(ChannelFactory<T> channelFactory)
        {
        }

        private static void BackgroundAbort(Object stateInfo)
        {
            ICommunicationObject co = stateInfo as ICommunicationObject;

            switch (co.State)
            {
                case CommunicationState.Opening:
                case CommunicationState.Opened:
                case CommunicationState.Faulted:
                    Console.WriteLine($"Service client with state {co.State} taken out of service.  Abort commencing.");
                    co.Abort();
                    break;
                default:
                    Console.WriteLine($"Service client with state {co.State} taken out of service.");
                    break;
            }
        }
    }

    //internal delegate void SetEndpointBehaviors(ServiceEndpoint endpoint);
}
