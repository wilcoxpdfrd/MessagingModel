using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Security.Authentication.ExtendedProtection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    /// <summary>
    /// ZeroMQConnectionOrientedTransportBinding base element.  
    /// Used to configure and construct ZeroMQ ChannelFactories and ChannelListeners.
    /// </summary>
    public abstract class ZeroMQConnectionOrientedTransportBindingElementBase : ConnectionOrientedTransportBindingElement
    {
        protected ZeroMQConnectionOrientedTransportBindingElementBase() : base()
        {
            ExposeConnectionProperty = true;
            ConnectionPoolSettings = new ZeroMQConnectionPoolSettings();
            ExtendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);           
        }

        protected ZeroMQConnectionOrientedTransportBindingElementBase(ZeroMQConnectionOrientedTransportBindingElementBase elementToBeCloned)
            : base(elementToBeCloned)
        {
            ExposeConnectionProperty = true;
            ConnectionPoolSettings = elementToBeCloned.ConnectionPoolSettings.Clone();
            ExtendedProtectionPolicy = elementToBeCloned.ExtendedProtectionPolicy;
        }

        public ZeroMQConnectionPoolSettings ConnectionPoolSettings { get; }
        public ExtendedProtectionPolicy ExtendedProtectionPolicy { get; internal set; }

        public new int MaxPendingConnections { get => base.MaxPendingConnections; set { base.MaxPendingConnections = value; this.IsMaxPendingConnectionsSet = true; } }
        
        public bool IsMaxPendingConnectionsSet { get; private set; }

        public virtual bool IsMatch(BindingElement bindingElement)
        {
            if (ConnectionOrientedTransportBindingElement.IsMatch(this, bindingElement))
            {
                ZeroMQConnectionOrientedTransportBindingElementBase c = bindingElement as ZeroMQConnectionOrientedTransportBindingElementBase;

                if (c != null)
                {
                    if (this.ConnectionPoolSettings != c.ConnectionPoolSettings)
                        return false;
                    if (this.ExtendedProtectionPolicy != c.ExtendedProtectionPolicy)
                        return false;
                    return true;
                }
            }

            return false;
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            throw new NotImplementedException($"Must override {this.GetType().FullName}::{nameof(BuildChannelFactory)}.");
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            throw new NotImplementedException($"Must override {this.GetType().FullName}::{nameof(BuildChannelListener)}.");
        }

        // client
        // server
        [DefaultValue(TransportDefaults.MaxBufferSize)]
        public override T GetProperty<T>(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            return context.GetInnerProperty<T>();
        }
    }
}
