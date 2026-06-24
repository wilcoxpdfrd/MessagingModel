using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MessagingApplication
{
    public struct MessagingApplicationContext: 
        IApplicationMessagingContext
    {
        public long StartTimestamp
        {
            get;
            set;
        }

        public Activity Activity
        {
            get;
            set;
        }

        public bool EventLogEnabled
        {
            get;
            set;
        }

        public IDisposable Scope
        {
            get;
            set;
        }
    }
}
