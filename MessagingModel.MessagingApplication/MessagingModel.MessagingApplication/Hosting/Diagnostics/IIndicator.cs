using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Diagnostics
{
    public interface IIndicator
    {
        String CategoryName { get; }
        String InstanceName { get; }
        String Name { get; }
        String Measurement { get; }
    }
}
