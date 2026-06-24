using System;
using System.Collections.Generic;
using System.ServiceModel.Configuration;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Configuration
{
    public static class BindingElementExtensions
    {
        public static BindingElement SetOpenTimeout<BindingElement>(this BindingElement bindingElement, TimeSpan openTimeout)
            where BindingElement : StandardBindingElement
        {
            bindingElement.OpenTimeout = openTimeout;
            return bindingElement;
        }
        public static BindingElement SetCloseTimeout<BindingElement>(this BindingElement bindingElement, TimeSpan closeTimeout)
            where BindingElement : StandardBindingElement
        {
            bindingElement.CloseTimeout = closeTimeout;
            return bindingElement;
        }
        public static BindingElement SetSendTimeout<BindingElement>(this BindingElement bindingElement, TimeSpan sendTimeout)
            where BindingElement : StandardBindingElement
        {
            bindingElement.SendTimeout = sendTimeout;
            return bindingElement;
        }
        public static BindingElement SetReceiveTimeout<BindingElement>(this BindingElement bindingElement, TimeSpan receiveTimeout)
            where BindingElement : StandardBindingElement
        {
            bindingElement.ReceiveTimeout = receiveTimeout;
            return bindingElement;
        }
    }
}
