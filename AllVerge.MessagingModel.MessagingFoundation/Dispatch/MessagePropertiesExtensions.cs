using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation.Dispatch
{
    public static class MessagePropertiesExtensions
    {
        public static MessageProperties Clone(this MessageProperties messageProperties)
        {
            MessageProperties clone = new MessageProperties();

            foreach (KeyValuePair<String, Object> property in messageProperties)
            {
                // messageProperties[string] will call CreateCopyOfPropertyValue, so we don't need to repeat that here
                clone[property.Key] = property.Value;
            }

            return clone;
        }
    }
}
