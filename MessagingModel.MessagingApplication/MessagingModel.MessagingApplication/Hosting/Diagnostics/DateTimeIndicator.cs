using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Diagnostics
{
    public class DateTimeIndicator : TypedIndicator
    {
        private DateTime value;

        public DateTimeIndicator(string categoryName, string instanceName, string name, string measurement, DateTime value) : base(categoryName, instanceName, name, measurement)
        {
            this.value = value;
        }

        public DateTime Value
        {
            get
            {
                return value;
            }
        }
    }
}
