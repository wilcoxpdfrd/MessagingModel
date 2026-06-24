using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace AllVerge.MessagingModel.MessagingApplication
{
    static class Resources
    {
        private static Type hostingResourcesType = typeof(WebHostBuilder).Assembly.GetType("Microsoft.AspNetCore.Hosting.Resources");
        private static Type abstractionsResourcesType = typeof(UseMiddlewareExtensions).Assembly.GetType("Microsoft.AspNetCore.Http.Abstractions.Resources");

        public static String WebHostBuilder_SingleInstance
        {
            get => GetHostString(nameof(WebHostBuilder_SingleInstance));
        }

        public static String FormatException_UseMiddlewareExplicitArgumentsNotSupported(object o1)
        {
            return string.Format(CultureInfo.CurrentCulture, GetMiddlewareString("Exception_UseMiddlewareExplicitArgumentsNotSupported"), o1);
        }

        internal static string FormatException_UseMiddleMutlipleInvokes(object p0, object p1)
        {
            return string.Format(CultureInfo.CurrentCulture, GetMiddlewareString("Exception_UseMiddleMutlipleInvokes"), p0, p1);
        }

        public static string FormatException_UseMiddlewareNoInvokeMethod(object p0, object p1, object p2)
        {
            return string.Format(CultureInfo.CurrentCulture, GetMiddlewareString("Exception_UseMiddlewareNoInvokeMethod"), p0, p1, p2);
        }

        internal static string FormatException_UseMiddlewareNonTaskReturnType(object p0, object p1, object p2)
        {
            return string.Format(CultureInfo.CurrentCulture, GetMiddlewareString("Exception_UseMiddlewareNonTaskReturnType"), p0, p1, p2);
        }

        internal static string FormatException_UseMiddlewareNoParameters(object p0, object p1, object p2)
        {
            return string.Format(CultureInfo.CurrentCulture, GetMiddlewareString("Exception_UseMiddlewareNoParameters"), p0, p1, p2);
        }

        internal static string FormatException_UseMiddlewareIServiceProviderNotAvailable(object p0)
        {
            return string.Format(CultureInfo.CurrentCulture, GetMiddlewareString("Exception_UseMiddlewareIServiceProviderNotAvailable"), p0);
        }

        internal static string FormatException_UseMiddlewareNoMiddlewareFactory(object p0)
        {
            return string.Format(CultureInfo.CurrentCulture, GetMiddlewareString("Exception_UseMiddlewareNoMiddlewareFactory"), p0);
        }

        internal static string FormatException_UseMiddlewareUnableToCreateMiddleware(object p0, object p1)
        {
            return string.Format(CultureInfo.CurrentCulture, GetMiddlewareString("Exception_UseMiddlewareUnableToCreateMiddleware"), p0, p1);
        }

        internal static string FormatException_UseMessageMiddlewareMustImplementInterface(object p0, object p1, object p2)
        {
            return string.Format(CultureInfo.CurrentCulture, "Message middleware must implement {0}.  Type {1} implements {2}.", p0, p1);
        }
        
        internal static string FormatException_InvokeDoesNotSupportRefOrOutParams(object p0)
        {
            return string.Format(CultureInfo.CurrentCulture, GetMiddlewareString("Exception_InvokeDoesNotSupportRefOrOutParams"), p0);
        }

        internal static string FormatException_InvokeMiddlewareNoService(object p0, object p1)
        {
            return string.Format(CultureInfo.CurrentCulture, GetHostString("Exception_InvokeMiddlewareNoService"), p0, p1);
        }
        private static string GetHostString(string propertyKey)
        {
            return hostingResourcesType.GetProperty(propertyKey, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null).ToString();
        }

        private static string GetMiddlewareString(string propertyKey)
        {
            return abstractionsResourcesType.GetProperty(propertyKey, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty).GetValue(null).ToString();
        }
    }
}
