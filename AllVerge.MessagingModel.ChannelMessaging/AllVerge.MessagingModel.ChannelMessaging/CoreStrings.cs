using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using Microsoft.AspNetCore.Hosting;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    static class CoreStrings
    {
        public static String ServerAlreadyStarted
        {
            get => "Server has already started.";
        }

        public static String ServerNotStarted
        {
            get => "Server has not been started.";
        }

        public static string ArgumentMustBeInRange(object argument, object value, object minValue, object maxValue)
        {
            return string.Format(CultureInfo.CurrentCulture, $"{argument} value {value} must be between {minValue} and {maxValue}.", argument, value, minValue, maxValue);
        }
    }
}
