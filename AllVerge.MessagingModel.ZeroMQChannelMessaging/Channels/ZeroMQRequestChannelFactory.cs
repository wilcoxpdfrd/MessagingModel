using AllVerge.Core.ServiceModel.Channels;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    internal class ZeroMQRequestChannelFactory<TChannel> : TransportChannelFactory<TChannel>, ITransferTransportFactorySettings
    {
        public ZeroMQRequestChannelFactory(ZeroMQTransferTransportBindingElementBase bindingElement, BindingContext context)
            : base(bindingElement, context)
        {
            this.Scheme = bindingElement.Scheme;
            if (bindingElement.TransferMode == TransferMode.Buffered)
            {
                if (bindingElement.MaxReceivedMessageSize > int.MaxValue)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new ArgumentOutOfRangeException("bindingElement.MaxReceivedMessageSize",
                        SSR.MaxReceivedMessageSizeMustBeInIntegerRange));
                }

                if (bindingElement.MaxBufferSize != bindingElement.MaxReceivedMessageSize)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("bindingElement",
                        SSR.MaxBufferSizeMustMatchMaxReceivedMessageSize);
                }
            }
            else
            {
                if (bindingElement.MaxBufferSize > bindingElement.MaxReceivedMessageSize)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("bindingElement",
                        SSR.MaxBufferSizeMustNotExceedMaxReceivedMessageSize);
                }
            }

            MaxBufferSize = bindingElement.MaxBufferSize;
            TransferMode = bindingElement.TransferMode;
            KeepAliveEnabled = bindingElement.KeepAliveEnabled;
        }

        public bool KeepAliveEnabled { get; }

        public int MaxBufferSize { get; }

        public TransferMode TransferMode { get; }

        public override string Scheme { get; }

        public new int MaxBufferPoolSize => (int)base.MaxBufferPoolSize;

        public new int MaxReceivedMessageSize => (int)base.MaxReceivedMessageSize;

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override TChannel OnCreateChannel(EndpointAddress address, Uri via)
        {
            ValidateCreateChannelParameters(address, via);
            if (typeof(TChannel) == typeof(IRequestChannel))
            {
                return (TChannel)(object)new ZeroMQRequestChannel((ZeroMQRequestChannelFactory<IRequestChannel>)(object)this, address, via, base.ManualAddressing);
            }
            throw new NotSupportedException(SSR.Format(SSR.UnsupportedChannelInterfaceType, typeof(TChannel)));
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
        }

        protected override void OnOpening()
        {
            base.OnOpening();
            base.BufferManager = BufferManager.CreateBufferManager(MaxBufferPoolSize, GetMaxBufferSize());
        }

        protected virtual void ValidateCreateChannelParameters(EndpointAddress remoteAddress, Uri via)
        {
            ValidateScheme(via);
            if (base.MessageVersion.Addressing == AddressingVersion.None && remoteAddress.Uri != via)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(CreateToMustEqualViaException(remoteAddress.Uri, via));
            }
        }

        internal Exception CreateToMustEqualViaException(Uri to, Uri via)
        {
            //The binding specified requires that the to and via URIs must match because the Addressing Version is set to None. The to URI specified was '{0}'. The via URI specified was '{1}'.
            return new ArgumentException(SSR.Format(SSR.HttpToMustEqualVia, to, via));
        }
    }
}
