using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    internal class AMMMFR : Resource
    {
        /// <summary>
        /// Gets the formatted string mapped to <paramref name="format"/>.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Format(string format, params object[] args)
        {
            return Format(format, AMMMFR.Culture, args);
        }

        /// <summary>
        /// Gets the localized formatted string mapped to <paramref name="name"/>.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="culture"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Format(string format, CultureInfo culture, params object[] args)
        {
            if (args == null || args.Length == 0)
                return format;
            for (int index = 0; index < args.Length; ++index)
            {
                string str = args[index] as string;
                if (str != null && str.Length > 1024)
                    args[index] = (object)(str.Substring(0, 1021) + "...");
            }
            return string.Format((IFormatProvider)CultureInfo.CurrentCulture, format, args);
        }
    }
}
