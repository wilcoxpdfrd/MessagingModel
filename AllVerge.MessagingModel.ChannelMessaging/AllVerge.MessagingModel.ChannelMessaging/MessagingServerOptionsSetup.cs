using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;

namespace AllVerge.MessagingModel.ChannelMessaging
{
    public class MessagingServerOptionsSetup : IConfigureOptions<MessagingServerOptions>
    {
	    private IServiceProvider _services;
        private IConfiguration _configuration;

        public MessagingServerOptionsSetup(IServiceProvider services, IConfiguration configuration)
        {
            _services = services;
            _configuration = configuration;
        }

        public void Configure(MessagingServerOptions options)
        {
            options.ApplicationServices = _services;
            options.UrlPrefixes = new UrlPrefixCollection();
        }
    }
}