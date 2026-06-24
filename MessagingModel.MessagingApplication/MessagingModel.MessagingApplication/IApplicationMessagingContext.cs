using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public interface IApplicationMessagingContext
    {
        long StartTimestamp { get; set; }
        Activity Activity { get; set; }
        bool EventLogEnabled { get; set; }
        IDisposable Scope { get; set; }
    }
}