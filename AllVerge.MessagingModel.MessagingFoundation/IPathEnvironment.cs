using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    public interface IPathEnvironment
    {
        string HostRootPath { get; }
        string ContentRootPath { get; }
    }
}
