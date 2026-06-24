using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    public class ApplicationHostEnvironment : IApplicationHostEnvironment
    {
        private String environmentName;
        private String applicationName;
        private String contentRootPath;
        private IFileProvider contentRootFileProvider;
        private String hostRootPath;
        private IFileProvider hostRootFileProvider;

        public ApplicationHostEnvironment()
        {
        }

        public string EnvironmentName { get => this.environmentName; set => this.environmentName = value; }
        public string ApplicationName { get => this.applicationName; set => this.applicationName = value; }
        public string ContentRootPath { get => this.contentRootPath; set => this.contentRootPath = value; }
        public IFileProvider ContentRootFileProvider { get => this.contentRootFileProvider; set => this.contentRootFileProvider = value; }
        public string HostRootPath { get => this.hostRootPath; set => this.hostRootPath = value; }
        public IFileProvider HostRootFileProvider { get => this.hostRootFileProvider; set => this.hostRootFileProvider = value; }
    }
}
