using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Diagnostics
{
    public class TimespanIndicator : TypedIndicator
    {
        private TimeSpan value;

        public TimespanIndicator(string categoryName, string instanceName, string name, string measures, TimeSpan value) : base(categoryName, instanceName, name, measures)
        {
            this.value = value;
        }

        public TimeSpan Value
        {
            get
            {
                return value;
            }
        }
    }
}
