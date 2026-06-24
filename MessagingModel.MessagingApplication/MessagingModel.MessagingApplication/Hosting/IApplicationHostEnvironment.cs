using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting
{
    public interface IApplicationHostEnvironment: IHostEnvironment
    {
        IFileProvider HostRootFileProvider { get; set; }
        string HostRootPath { get; set; }
    }
}