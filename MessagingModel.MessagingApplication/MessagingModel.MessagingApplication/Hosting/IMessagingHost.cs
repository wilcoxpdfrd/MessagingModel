using Microsoft.AspNetCore.Hosting;
using System;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    public interface IMessagingHost : IWebHost
    {
        void Initialize();
    }
}