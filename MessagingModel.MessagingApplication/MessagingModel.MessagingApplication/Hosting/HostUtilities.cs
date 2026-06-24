using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    public class HostUtilities
    {
		public static bool ParseBool(IConfiguration configuration, string key)
		{
			if (!string.Equals("true", configuration[key], StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals("1", configuration[key], StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
	}
}
