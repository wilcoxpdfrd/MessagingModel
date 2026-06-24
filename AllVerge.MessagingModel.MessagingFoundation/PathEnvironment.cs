using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    public class PathEnvironment : IPathEnvironment
    {
        public PathEnvironment(String hostRootPath, String contentRootPath)
        {
            this.HostRootPath = hostRootPath;
            this.ContentRootPath = contentRootPath;
        }

        public string HostRootPath { get; }

        public string ContentRootPath { get; }
    }
}
